namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Contract for product search indexing and querying.</summary>
public interface ISearchService
{
    Task IndexProductAsync(ProductSearchDocument doc, CancellationToken cancellationToken = default);

    Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default);

    Task<SearchResult> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);

    Task<InstantSearchResult> InstantSearchAsync(string query, CancellationToken cancellationToken = default);

    Task BulkIndexAsync(IEnumerable<ProductSearchDocument> docs, CancellationToken cancellationToken = default);
}

/// <summary>Elasticsearch product document.</summary>
public sealed class ProductSearchDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal AverageRating { get; set; }
    public int StockQty { get; set; }
    public int SoldCount { get; set; }
    public bool IsActive { get; set; }
    public string? ThumbUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record ProductSearchRequest(
    string? Query = null,
    int Page = 1,
    int PageSize = 20,
    int? CategoryId = null,
    Guid? BrandId = null,
    decimal? PriceMin = null,
    decimal? PriceMax = null,
    decimal? RatingMin = null,
    bool? InStockOnly = null,
    string? SortBy = null);

public sealed record SearchResult(
    IReadOnlyList<ProductSearchDocument> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record InstantSearchResult(
    IReadOnlyList<InstantProductHit> Products,
    IReadOnlyList<string> Categories);

public sealed record InstantProductHit(
    string Id,
    string Name,
    string Slug,
    string? ThumbUrl,
    decimal Price);
