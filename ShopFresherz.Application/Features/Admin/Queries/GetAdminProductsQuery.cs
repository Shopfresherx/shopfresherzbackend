using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>
/// Query for paginated product listing for admin management. Unlike
/// <see cref="ShopFresherz.Application.Features.Products.Queries.GetProductsQuery"/>,
/// this can return products of any status via <see cref="Status"/>.
/// </summary>
public sealed record GetAdminProductsQuery(
    int Page           = 1,
    int PageSize       = 20,
    string? Status     = null,
    int? CategoryId    = null,
    Guid? BrandId      = null,
    decimal? PriceMin  = null,
    decimal? PriceMax  = null,
    decimal? RatingMin = null,
    string? SortBy     = null,
    string? Search     = null
) : IRequest<Result<PagedResult<ProductSummaryDto>>>;

/// <summary>Handler for <see cref="GetAdminProductsQuery"/>.</summary>
public sealed class GetAdminProductsQueryHandler
    : IRequestHandler<GetAdminProductsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetAdminProductsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetAdminProductsQuery query,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<Product> items, int total) = await _uow.Products.GetPagedForAdminAsync(
            page,
            pageSize,
            query.Status,
            query.CategoryId,
            query.BrandId,
            query.PriceMin,
            query.PriceMax,
            query.RatingMin,
            query.SortBy,
            query.Search,
            cancellationToken);

        IReadOnlyList<ProductSummaryDto> dtos =
            _mapper.Map<IReadOnlyList<ProductSummaryDto>>(items);

        return Result<PagedResult<ProductSummaryDto>>.Success(
            new PagedResult<ProductSummaryDto>(dtos, total, page, pageSize));
    }
}
