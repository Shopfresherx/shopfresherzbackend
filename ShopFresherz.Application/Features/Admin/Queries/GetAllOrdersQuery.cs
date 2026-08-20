using System.Text.Json;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Admin query for paginated orders.</summary>
public sealed record GetAllOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? PaymentStatus = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<PagedResult<OrderDto>>>;

/// <summary>Handler for <see cref="GetAllOrdersQuery"/>.</summary>
public sealed class GetAllOrdersQueryHandler
    : IRequestHandler<GetAllOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllOrdersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<OrderDto>>> Handle(
        GetAllOrdersQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<Order> items, int total) = await _uow.Orders.GetAllAsync(
            page,
            pageSize,
            query.Status,
            query.PaymentStatus,
            query.From,
            query.To,
            cancellationToken);

        return Result<PagedResult<OrderDto>>.Success(
            new PagedResult<OrderDto>(items.Select(MapOrder).ToList(), total, page, pageSize));
    }

    private static OrderDto MapOrder(Order order)
    {
        DeliveryAddressSnapshot? addressSnapshot = null;
        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressJson))
        {
            addressSnapshot = JsonSerializer.Deserialize<DeliveryAddressSnapshot>(order.DeliveryAddressJson);
        }

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            DeliveryFee = order.DeliveryFee,
            VatAmount = order.VatAmount,
            Total = order.Total,
            DeliveryAddress = addressSnapshot,
            DeliveryMethod = order.DeliveryMethod,
            EstimatedDelivery = order.EstimatedDelivery,
            TrackingNumber = order.TrackingNumber,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i =>
            {
                ProductSnapshot? snap = null;
                if (!string.IsNullOrWhiteSpace(i.ProductSnapshotJson))
                {
                    snap = JsonSerializer.Deserialize<ProductSnapshot>(i.ProductSnapshotJson);
                }

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    ProductSnapshot = snap,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal,
                };
            }).ToList(),
        };
    }
}
