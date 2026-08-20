using System.Text.Json;
using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Orders.Queries;

/// <summary>Query for paginated order history of the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record GetOrdersQuery(Guid UserId, int Page = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<OrderDto>>>;

/// <summary>Handler for <see cref="GetOrdersQuery"/>.</summary>
public sealed class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public GetOrdersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<OrderDto>>> Handle(
        GetOrdersQuery query,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);

        (IReadOnlyList<Order> items, int total) = await _uow.Orders.GetUserOrdersAsync(
            query.UserId, page, pageSize, cancellationToken);

        IReadOnlyList<OrderDto> dtos = items.Select(MapOrder).ToList();

        return Result<PagedResult<OrderDto>>.Success(
            new PagedResult<OrderDto>(dtos, total, page, pageSize));
    }

    private static OrderDto MapOrder(Order order)
    {
        DeliveryAddressSnapshot? addressSnapshot = null;
        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressJson))
        {
            addressSnapshot = JsonSerializer.Deserialize<DeliveryAddressSnapshot>(
                order.DeliveryAddressJson);
        }

        return new OrderDto
        {
            Id              = order.Id,
            OrderNumber     = order.OrderNumber,
            UserId          = order.UserId,
            Status          = order.Status,
            PaymentStatus   = order.PaymentStatus,
            PaymentMethod   = order.PaymentMethod,
            Subtotal        = order.Subtotal,
            DiscountAmount  = order.DiscountAmount,
            DeliveryFee     = order.DeliveryFee,
            VatAmount       = order.VatAmount,
            Total           = order.Total,
            DeliveryAddress = addressSnapshot,
            DeliveryMethod  = order.DeliveryMethod,
            EstimatedDelivery = order.EstimatedDelivery,
            TrackingNumber  = order.TrackingNumber,
            Notes           = order.Notes,
            CreatedAt       = order.CreatedAt,
            Items           = order.Items.Select(i =>
            {
                ProductSnapshot? snap = null;
                if (!string.IsNullOrWhiteSpace(i.ProductSnapshotJson))
                    snap = JsonSerializer.Deserialize<ProductSnapshot>(i.ProductSnapshotJson);

                return new OrderItemDto
                {
                    Id              = i.Id,
                    ProductId       = i.ProductId,
                    VariantId       = i.VariantId,
                    ProductSnapshot = snap,
                    Quantity        = i.Quantity,
                    UnitPrice       = i.UnitPrice,
                    LineTotal       = i.LineTotal,
                };
            }).ToList(),
        };
    }
}
