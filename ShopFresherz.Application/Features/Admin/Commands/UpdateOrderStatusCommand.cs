using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Application.Features.Payments.Services;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Admin.Commands;

/// <summary>Admin command for updating an order's status.</summary>
public sealed record UpdateOrderStatusCommand(
    string OrderNumber,
    UpdateOrderStatusRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateOrderStatusCommand"/>.</summary>
public sealed class UpdateOrderStatusCommandHandler
    : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ISmsService _sms;
    private readonly IOrderPaymentConfirmationService _paymentConfirmation;

    public UpdateOrderStatusCommandHandler(
        IUnitOfWork uow,
        ISmsService sms,
        IOrderPaymentConfirmationService paymentConfirmation)
    {
        _uow = uow;
        _sms = sms;
        _paymentConfirmation = paymentConfirmation;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateOrderStatusCommand command,
        CancellationToken cancellationToken)
    {
        Order? order = await _uow.Orders.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("Order");
        }

        if (command.Request.NewStatus is null)
        {
            return Error.Validation("New status is required.");
        }

        OrderStatus newStatus = command.Request.NewStatus.Value;

        bool confirmsBankTransfer =
            order.PaymentMethod == PaymentMethod.BankTransfer &&
            order.PaymentStatus != PaymentStatus.Paid &&
            newStatus is OrderStatus.Paid or OrderStatus.Processing;

        if (confirmsBankTransfer)
        {
            return await _paymentConfirmation.CompleteAsync(
                order,
                order.GuestEmail,
                newStatus,
                cancellationToken);
        }

        if (!IsLegalTransition(order.Status, newStatus))
        {
            return Error.Validation($"Cannot transition order from {order.Status} to {newStatus}.");
        }

        if (newStatus == OrderStatus.Shipped)
        {
            if (string.IsNullOrWhiteSpace(command.Request.TrackingNumber))
            {
                return Error.Validation("Tracking number is required when shipping an order.");
            }

            order.TrackingNumber = command.Request.TrackingNumber;
        }

        order.Status = newStatus;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        if (newStatus == OrderStatus.Shipped &&
            order.UserId.HasValue &&
            !string.IsNullOrWhiteSpace(command.Request.TrackingNumber))
        {
            User? user = await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(user?.Phone))
            {
                _ = _sms.SendDeliveryNotificationAsync(
                    user.Phone,
                    order.OrderNumber,
                    command.Request.TrackingNumber,
                    CancellationToken.None);
            }
        }

        return Result<bool>.Success(true);
    }

    private static bool IsLegalTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
        {
            return true;
        }

        if (next == OrderStatus.Cancelled)
        {
            return current is not OrderStatus.Delivered and not OrderStatus.Refunded;
        }

        return current switch
        {
            OrderStatus.Pending => next == OrderStatus.AwaitingPayment,
            OrderStatus.AwaitingPayment => next is OrderStatus.Paid,
            OrderStatus.Paid => next == OrderStatus.Processing,
            OrderStatus.Processing => next == OrderStatus.Shipped,
            OrderStatus.Shipped => next == OrderStatus.Delivered,
            OrderStatus.Delivered => next == OrderStatus.RefundRequested,
            OrderStatus.RefundRequested => next == OrderStatus.Refunded,
            _ => false,
        };
    }
}
