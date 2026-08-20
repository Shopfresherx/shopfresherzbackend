using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Admin query for products at or below a stock threshold.</summary>
public sealed record GetLowStockQuery(int Threshold = 5)
    : IRequest<Result<IReadOnlyList<LowStockDto>>>;

/// <summary>Handler for <see cref="GetLowStockQuery"/>.</summary>
public sealed class GetLowStockQueryHandler
    : IRequestHandler<GetLowStockQuery, Result<IReadOnlyList<LowStockDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetLowStockQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LowStockDto>>> Handle(
        GetLowStockQuery query,
        CancellationToken cancellationToken)
    {
        int threshold = Math.Max(0, query.Threshold);
        IReadOnlyList<Product> products = await _uow.Products.GetLowStockAsync(threshold, cancellationToken);

        IReadOnlyList<LowStockDto> dtos = products.Select(p => new LowStockDto
        {
            Id = p.Id,
            SKU = p.SKU,
            Name = p.Name,
            StockQty = p.StockQty,
            ReservedQty = p.ReservedQty,
            AvailableQty = p.AvailableQty,
            CategoryName = p.Category.Name,
            BrandName = p.Brand.Name,
        }).ToList();

        return Result<IReadOnlyList<LowStockDto>>.Success(dtos);
    }
}
