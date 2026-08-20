using System.Text.Json;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Checkout;
using ShopFresherz.Application.Features.Payments.Services;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Checkout.Commands.ConfirmOrder;

/// <summary>
/// Second step of the two-step inline-popup checkout. Verifies the Flutterwave
/// transaction server-side and, on success, finalises the Draft order via the
/// shared <see cref="IOrderPaymentConfirmationService"/> (stock, loyalty, email).
/// </summary>
/// <param name="UserId">The authenticated user's ID (null for guest checkout).</param>
/// <param name="Request">The confirm-order payload.</param>
public sealed record ConfirmOrderCommand(Guid? UserId, ConfirmOrderRequest Request)
    : IRequest<Result<ConfirmOrderResponse>>;

/// <summary>Handler for <see cref="ConfirmOrderCommand"/>.</summary>
public sealed class ConfirmOrderCommandHandler
    : IRequestHandler<ConfirmOrderCommand, Result<ConfirmOrderResponse>>
{
    private const decimal AmountToleranceNgn = 1m;

    private readonly IUnitOfWork _uow;
    private readonly IFlutterwavePaymentService _flutterwave;
    private readonly IOrderPaymentConfirmationService _paymentConfirmation;
    private readonly IEmailService _email;

    public ConfirmOrderCommandHandler(
        IUnitOfWork uow,
        IFlutterwavePaymentService flutterwave,
        IOrderPaymentConfirmationService paymentConfirmation,
        IEmailService email)
    {
        _uow = uow;
        _flutterwave = flutterwave;
        _paymentConfirmation = paymentConfirmation;
        _email = email;
    }

    /// <inheritdoc />
    public async Task<Result<ConfirmOrderResponse>> Handle(
        ConfirmOrderCommand command,
        CancellationToken cancellationToken)
    {
        ConfirmOrderRequest req = command.Request;

        Order? order = await _uow.Orders.GetByIdAsync(req.PendingOrderId, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("Order");
        }

        if (!string.Equals(req.TxRef, order.TxRef, StringComparison.Ordinal))
        {
            return Error.Validation("Payment verification failed.");
        }

        // Idempotency: a non-Draft order has already been processed (or is being processed).
        if (order.Status != OrderStatus.Draft)
        {
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return Result<ConfirmOrderResponse>.Success(new ConfirmOrderResponse
                {
                    OrderNumber = order.OrderNumber,
                });
            }

            return Error.Validation("This order can no longer be confirmed.");
        }

        // Popup reported a non-successful payment: cancel and release reserved stock.
        if (!string.Equals(req.Status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            await CancelAndReleaseStockAsync(order, cancellationToken);
            return Error.Validation("Payment was not successful.");
        }

        // Atomically claim the order for verification (Unpaid -> Verifying) so a concurrent
        // confirm-order call or webhook delivery for the same order cannot also process it.
        int claimed = await _uow.Orders.TryClaimForVerificationAsync(order.Id, cancellationToken);
        if (claimed == 0)
        {
            Order? current = await _uow.Orders.GetByIdAsync(order.Id, cancellationToken);
            if (current?.PaymentStatus == PaymentStatus.Paid)
            {
                return Result<ConfirmOrderResponse>.Success(new ConfirmOrderResponse
                {
                    OrderNumber = current.OrderNumber,
                });
            }

            return Error.Validation("This order is already being confirmed. Please wait a moment and refresh.");
        }

        order.PaymentStatus = PaymentStatus.Verifying;

        // Authoritative server-side verification against Flutterwave.
        FlutterwaveVerificationResult? verification =
            await _flutterwave.VerifyAsync(req.TransactionId, cancellationToken);

        bool verified =
            verification is not null &&
            string.Equals(verification.Status, "successful", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(verification.TxRef, order.TxRef, StringComparison.Ordinal) &&
            verification.Amount >= order.Total - AmountToleranceNgn &&
            string.Equals(verification.Currency, "NGN", StringComparison.OrdinalIgnoreCase);

        if (!verified)
        {
            // Release the claim so a legitimate retry can attempt verification again.
            order.PaymentStatus = PaymentStatus.Unpaid;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            return Error.Validation("Payment verification failed.");
        }

        Result<bool> completion = await _paymentConfirmation.CompleteAsync(
            order,
            order.GuestEmail,
            OrderStatus.Processing,
            cancellationToken);

        if (!completion.IsSuccess)
        {
            // Release the claim so a legitimate retry isn't permanently stuck at Verifying.
            order.PaymentStatus = PaymentStatus.Unpaid;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            return completion.Error;
        }

        User? orderUser = order.UserId.HasValue
            ? await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken)
            : null;
        _ = _email.SendAdminOrderNotificationAsync(
            order.OrderNumber,
            orderUser is not null ? $"{orderUser.FirstName} {orderUser.LastName}".Trim() : "Guest",
            orderUser?.Email ?? order.GuestEmail ?? string.Empty,
            orderUser?.Phone ?? ExtractPhoneFromAddressJson(order.DeliveryAddressJson) ?? string.Empty,
            order.Total,
            nameof(PaymentMethod.Card),
            order.DeliveryAddressJson,
            CancellationToken.None);

        return Result<ConfirmOrderResponse>.Success(new ConfirmOrderResponse
        {
            OrderNumber = order.OrderNumber,
        });
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

    /// <summary>Cancels a Draft order and releases the stock reserved at initiate-payment.</summary>
    private async Task CancelAndReleaseStockAsync(Order order, CancellationToken cancellationToken)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue;

            if (item.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                {
                    variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
                }
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
    }
}

/// <summary>Validator for <see cref="ConfirmOrderCommand"/>.</summary>
public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.Request.PendingOrderId)
            .NotEmpty().WithMessage("pendingOrderId is required.");

        RuleFor(x => x.Request.Status)
            .NotEmpty().WithMessage("Payment status is required.");

        RuleFor(x => x.Request.TxRef)
            .NotEmpty().WithMessage("txRef is required.");

        RuleFor(x => x.Request.TransactionId)
            .NotEmpty()
            .When(x => string.Equals(x.Request.Status, "successful", StringComparison.OrdinalIgnoreCase))
            .WithMessage("transactionId is required for a successful payment.");
    }
}
