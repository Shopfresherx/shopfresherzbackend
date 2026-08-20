using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Features.Search.Commands;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Search.Queries;

/// <summary>Query for full product search.</summary>
public sealed record SearchProductsQuery(
    string? Query = null,
    int Page = 1,
    int PageSize = 20,
    decimal? PriceMin = null,
    decimal? PriceMax = null,
    decimal? RatingMin = null,
    bool? InStockOnly = null,
    string? SortBy = null) : IRequest<Result<SearchResult>>;

/// <summary>Handler for <see cref="SearchProductsQuery"/>.</summary>
public sealed class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, Result<SearchResult>>
{
    private readonly ISearchService _search;
    private readonly IUnitOfWork _uow;

    public SearchProductsQueryHandler(ISearchService search, IUnitOfWork uow)
    {
        _search = search;
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<SearchResult>> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        ProductSearchRequest request = new(
            Query: query.Query,
            Page: page,
            PageSize: pageSize,
            PriceMin: query.PriceMin,
            PriceMax: query.PriceMax,
            RatingMin: query.RatingMin,
            InStockOnly: query.InStockOnly,
            SortBy: query.SortBy);

        SearchResult result;
        try
        {
            result = await _search.SearchAsync(request, cancellationToken);
            if (result.Total > 0 || string.IsNullOrWhiteSpace(query.Query))
            {
                return Result<SearchResult>.Success(result);
            }
        }
        catch
        {
        }

        (IReadOnlyList<Product> items, int total) = await _uow.Products.GetPagedAsync(
            page,
            pageSize,
            priceMin: query.PriceMin,
            priceMax: query.PriceMax,
            ratingMin: query.RatingMin,
            sortBy: query.SortBy,
            cancellationToken: cancellationToken);

        IEnumerable<Product> filtered = items;
        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            string term = query.Query.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.TagsJson?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (query.InStockOnly == true)
        {
            filtered = filtered.Where(p => p.AvailableQty > 0);
        }

        IReadOnlyList<ProductSearchDocument> docs = filtered
            .Select(IndexProductCommandHandler.Map)
            .ToList();

        return Result<SearchResult>.Success(new SearchResult(docs, total, page, pageSize));
    }
}
