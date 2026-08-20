using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Orders.Commands;

/// <summary>Command for cancelling an order that has not yet been shipped.</summary>
/// <param name="OrderNumber">The order number to cancel.</param>
/// <param name="RequestingUserId">The requesting user's ID for ownership validation (null = admin).</param>
public sealed record CancelOrderCommand(string OrderNumber, Guid? RequestingUserId)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public CancelOrderCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        Order? order = await _uow.Orders.GetByOrderNumberAsync(
            command.OrderNumber, cancellationToken);

        if (order is null) return Error.NotFound("Order");

        if (command.RequestingUserId.HasValue && order.UserId != command.RequestingUserId)
        {
            return Error.Forbidden();
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            return Error.Validation("Cannot cancel an order that has already been shipped or delivered.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Error.Validation("Order is already cancelled.");
        }

        // Release reserved stock.
        foreach (OrderItem item in order.Items)
        {
            Product? product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue;

            if (item.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                    variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
            }
            else
            {
                product.ReservedQty = Math.Max(0, product.ReservedQty - item.Quantity);
            }

            _uow.Products.Update(product);
        }

        order.Status = OrderStatus.Cancelled;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
