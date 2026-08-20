using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Search;

/// <summary>Fault-tolerant Elasticsearch-backed product search service.</summary>
public sealed class ElasticsearchService : ISearchService
{
    private const string Index = "shopfresherz-products";

    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchService> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.Value.Uri))
        {
            _logger.LogWarning("Elasticsearch URI is not configured. Search functionality will be disabled.");
            _client = null;
            return;
        }

        try
        {
            ElasticsearchClientSettings settings =
                new ElasticsearchClientSettings(new Uri(options.Value.Uri))
                    .DefaultIndex(Index);

            if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            {
                settings = settings.Authentication(new ApiKey(options.Value.ApiKey));
            }

            _client = new ElasticsearchClient(settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Elasticsearch client. Search functionality will be disabled.");
            _client = null;
        }
    }

    /// <inheritdoc />
    public async Task IndexProductAsync(ProductSearchDocument doc, CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("Elasticsearch client not configured. Skipping product indexing.");
            return;
        }

        try
        {
            IndexResponse response = await _client.IndexAsync(
                doc,
                i => i.Index(Index).Id(doc.Id),
                cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to index product {Id}: {Reason}", doc.Id, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index product {Id}.", doc.Id);
        }
    }

    /// <inheritdoc />
    public async Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteAsync<ProductSearchDocument>(productId, d => d.Index(Index), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete product {Id} from search index.", productId);
        }
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("Elasticsearch client not configured. Returning empty search results.");
            return new SearchResult([], 0, request.Page, request.PageSize);
        }

        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);
        int from = (page - 1) * pageSize;

        try
        {
            SearchResponse<ProductSearchDocument> response =
                await _client.SearchAsync<ProductSearchDocument>(s => s
                    .Index(Index)
                    .From(from)
                    .Size(pageSize)
                    .Query(q => BuildQuery(q, request))
                    .Sort(BuildSort(request.SortBy)),
                    cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Search failed: {Reason}", response.DebugInformation);
                return new SearchResult([], 0, page, pageSize);
            }

            return new SearchResult(
                response.Documents.ToList(),
                (int)response.Total,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch product search failed.");
            return new SearchResult([], 0, page, pageSize);
        }
    }

    /// <inheritdoc />
    public async Task<InstantSearchResult> InstantSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("Elasticsearch client not configured. Returning empty instant search results.");
            return new InstantSearchResult([], []);
        }

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new InstantSearchResult([], []);
        }

        try
        {
            SearchResponse<ProductSearchDocument> response =
                await _client.SearchAsync<ProductSearchDocument>(s => s
                    .Index(Index)
                    .Size(5)
                    .Query(q => q.Bool(b => b
                        .Must(m => m.MultiMatch(mm => mm
                            .Query(query)
                            .Fields(new[] { "name^3", "brand^2", "category^1.5", "tags^1.5", "description" })
                            .Fuzziness(new Fuzziness("AUTO"))
                            .Type(TextQueryType.BestFields)))
                        .Filter(f => f.Term(t => t.Field(d => d.IsActive).Value(true))))),
                    cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Instant search failed: {Reason}", response.DebugInformation);
                return new InstantSearchResult([], []);
            }

            IReadOnlyList<InstantProductHit> products = response.Documents
                .Select(d => new InstantProductHit(d.Id, d.Name, d.Slug, d.ThumbUrl, d.Price))
                .ToList();

            IReadOnlyList<string> categories = response.Documents
                .Where(d => d.Category is not null)
                .Select(d => d.Category!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            return new InstantSearchResult(products, categories);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch instant search failed.");
            return new InstantSearchResult([], []);
        }
    }

    /// <inheritdoc />
    public async Task BulkIndexAsync(IEnumerable<ProductSearchDocument> docs, CancellationToken cancellationToken = default)
    {
        try
        {
            BulkResponse response = await _client.BulkAsync(
                b => b.Index(Index).IndexMany(docs),
                cancellationToken);

            if (response.Errors)
            {
                _logger.LogWarning("Bulk index had errors: {Count} failed.", response.ItemsWithErrors.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch bulk product indexing failed.");
        }
    }

    private static Query BuildQuery(QueryDescriptor<ProductSearchDocument> q, ProductSearchRequest request)
    {
        List<Action<QueryDescriptor<ProductSearchDocument>>> filters =
        [
            f => f.Term(t => t.Field(d => d.IsActive).Value(true)),
        ];

        if (request.InStockOnly == true)
        {
            filters.Add(f => f.Range(r => r.NumberRange(n => n.Field(d => d.StockQty).Gt(0))));
        }

        if (request.PriceMin.HasValue || request.PriceMax.HasValue)
        {
            filters.Add(f => f.Range(r => r.NumberRange(n =>
            {
                n.Field(d => d.Price);
                if (request.PriceMin.HasValue) n.Gte((double)request.PriceMin.Value);
                if (request.PriceMax.HasValue) n.Lte((double)request.PriceMax.Value);
            })));
        }

        if (request.RatingMin.HasValue)
        {
            filters.Add(f => f.Range(r => r.NumberRange(n => n
                .Field(d => d.AverageRating)
                .Gte((double)request.RatingMin.Value))));
        }

        Action<BoolQueryDescriptor<ProductSearchDocument>> boolQuery = b =>
        {
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                b.Must(m => m.MultiMatch(mm => mm
                    .Query(request.Query)
                    .Fields(new[] { "name^3", "brand^2", "tags^1.5", "description" })
                    .Fuzziness(new Fuzziness("AUTO"))));
            }

            b.Filter(filters.ToArray());
        };

        return q.Bool(boolQuery);
    }

    private static Action<SortOptionsDescriptor<ProductSearchDocument>> BuildSort(string? sortBy)
    {
        return descriptor =>
        {
            _ = sortBy switch
            {
                "price_asc" => descriptor.Field(f => f.Field(d => d.Price).Order(SortOrder.Asc)),
                "price_desc" => descriptor.Field(f => f.Field(d => d.Price).Order(SortOrder.Desc)),
                "rating" => descriptor.Field(f => f.Field(d => d.AverageRating).Order(SortOrder.Desc)),
                "newest" => descriptor.Field(f => f.Field(d => d.CreatedAt).Order(SortOrder.Desc)),
                "best_selling" => descriptor.Field(f => f.Field(d => d.SoldCount).Order(SortOrder.Desc)),
                _ => descriptor.Score(s => s.Order(SortOrder.Desc)),
            };
        };
    }
}
