using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Queries;

/// <summary>Query for paginated product listing with optional filters.</summary>
public sealed record GetProductsQuery(
    int Page        = 1,
    int PageSize    = 20,
    int? CategoryId = null,
    Guid? BrandId   = null,
    decimal? PriceMin  = null,
    decimal? PriceMax  = null,
    decimal? RatingMin = null,
    string? SortBy     = null
) : IRequest<Result<PagedResult<ProductSummaryDto>>>;

/// <summary>Handler for <see cref="GetProductsQuery"/>.</summary>
public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetProductsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<Product> items, int total) = await _uow.Products.GetPagedAsync(
            page,
            pageSize,
            query.CategoryId,
            query.BrandId,
            query.PriceMin,
            query.PriceMax,
            query.RatingMin,
            query.SortBy,
            cancellationToken);

        IReadOnlyList<ProductSummaryDto> dtos =
            _mapper.Map<IReadOnlyList<ProductSummaryDto>>(items);

        return Result<PagedResult<ProductSummaryDto>>.Success(
            new PagedResult<ProductSummaryDto>(dtos, total, page, pageSize));
    }
}
