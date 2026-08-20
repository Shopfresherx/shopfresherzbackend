using System.Text.Json;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Orders.Queries;

/// <summary>Query for retrieving a single order by its human-readable order number.</summary>
/// <param name="OrderNumber">The order number (e.g. SFZ-2026-00001).</param>
/// <param name="RequestingUserId">The requesting user's ID for ownership validation (null = admin bypass).</param>
public sealed record GetOrderByNumberQuery(string OrderNumber, Guid? RequestingUserId)
    : IRequest<Result<OrderDto>>;

/// <summary>Handler for <see cref="GetOrderByNumberQuery"/>.</summary>
public sealed class GetOrderByNumberQueryHandler
    : IRequestHandler<GetOrderByNumberQuery, Result<OrderDto>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public GetOrderByNumberQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<OrderDto>> Handle(
        GetOrderByNumberQuery query,
        CancellationToken cancellationToken)
    {
        Order? order = await _uow.Orders.GetByOrderNumberAsync(query.OrderNumber, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("Order");
        }

        if (query.RequestingUserId.HasValue && order.UserId != query.RequestingUserId)
        {
            return Error.Forbidden();
        }

        DeliveryAddressSnapshot? addressSnapshot = null;
        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressJson))
        {
            addressSnapshot = JsonSerializer.Deserialize<DeliveryAddressSnapshot>(
                order.DeliveryAddressJson);
        }

        OrderDto dto = new()
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

        return Result<OrderDto>.Success(dto);
    }
}
