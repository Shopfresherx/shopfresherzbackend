using System.Text.Json;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Features.Payments.Services;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Payments.Commands;

/// <summary>
/// Command for processing a verified successful payment webhook event.
/// Stock is deducted and order status updated only after gateway verification in the controller.
/// </summary>
public sealed record HandlePaymentWebhookCommand(
    string Event,
    string Reference,
    decimal AmountNgn,
    string Currency,
    string CustomerEmail) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="HandlePaymentWebhookCommand"/>.</summary>
public sealed class HandlePaymentWebhookCommandHandler
    : IRequestHandler<HandlePaymentWebhookCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderPaymentConfirmationService _paymentConfirmation;
    private readonly IEmailService _email;

    public HandlePaymentWebhookCommandHandler(
        IUnitOfWork uow,
        IOrderPaymentConfirmationService paymentConfirmation,
        IEmailService email)
    {
        _uow = uow;
        _paymentConfirmation = paymentConfirmation;
        _email = email;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        HandlePaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command.Event, "charge.completed", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(true);
        }

        // The two-step inline-popup flow stores the gateway reference in TxRef;
        // the legacy hosted-redirect flow stores the order number in PaymentReference.
        // Try TxRef first, then fall back so both flows keep working.
        Order? order =
            await _uow.Orders.GetByTxRefAsync(command.Reference, cancellationToken) ??
            await _uow.Orders.GetByPaymentReferenceAsync(command.Reference, cancellationToken);

        // Unknown reference: acknowledge so Flutterwave stops retrying.
        if (order is null)
        {
            return Result<bool>.Success(true);
        }

        if (order.PaymentStatus == PaymentStatus.Paid || order.Status == OrderStatus.Processing)
        {
            return Result<bool>.Success(true);
        }

        if (order.PaymentStatus != PaymentStatus.Unpaid ||
            order.Status is not (OrderStatus.Draft or OrderStatus.Pending or OrderStatus.AwaitingPayment))
        {
            return Result<bool>.Success(true);
        }

        if (command.AmountNgn < order.Total - 1m ||
            !string.Equals(command.Currency, "NGN", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(true);
        }

        // Atomically claim the order (Unpaid -> Verifying) so a concurrent confirm-order call
        // for the same order cannot also process this payment.
        int claimed = await _uow.Orders.TryClaimForVerificationAsync(order.Id, cancellationToken);
        if (claimed == 0)
        {
            // Already claimed or completed by confirm-order (or a prior webhook delivery).
            return Result<bool>.Success(true);
        }

        order.PaymentStatus = PaymentStatus.Verifying;

        Result<bool> completion = await _paymentConfirmation.CompleteAsync(
            order,
            command.CustomerEmail,
            OrderStatus.Processing,
            cancellationToken);

        if (!completion.IsSuccess)
        {
            // Release the claim so a retry (webhook redelivery or confirm-order) isn't stuck.
            order.PaymentStatus = PaymentStatus.Unpaid;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            return completion;
        }

        User? orderUser = order.UserId.HasValue
            ? await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken)
            : null;
        _ = _email.SendAdminOrderNotificationAsync(
            order.OrderNumber,
            orderUser is not null ? $"{orderUser.FirstName} {orderUser.LastName}".Trim() : "Guest",
            orderUser?.Email ?? command.CustomerEmail ?? order.GuestEmail ?? string.Empty,
            orderUser?.Phone ?? ExtractPhoneFromAddressJson(order.DeliveryAddressJson) ?? string.Empty,
            order.Total,
            order.PaymentMethod?.ToString() ?? "Card",
            order.DeliveryAddressJson,
            CancellationToken.None);

        return completion;
    }

    /// <summary>Extracts the "Phone" property from a checkout delivery-address JSON snapshot, if present.</summary>
    private static string? ExtractPhoneFromAddressJson(string deliveryAddressJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(deliveryAddressJson);
            return doc.RootElement.TryGetProperty("Phone", out JsonElement phone)
                ? phone.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
