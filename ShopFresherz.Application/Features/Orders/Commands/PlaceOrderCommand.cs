using System.Text.Json;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;
using CartEntity = ShopFresherz.Domain.Entities.Cart;

namespace ShopFresherz.Application.Features.Orders.Commands;

internal sealed record OrderSourceItem(Guid ProductId, Guid? VariantId, int Quantity);

/// <summary>Command for converting the active cart into a confirmed order.</summary>
/// <param name="UserId">The authenticated user's ID (null for guest checkout).</param>
/// <param name="Request">The order creation payload.</param>
public sealed record PlaceOrderCommand(Guid? UserId, CreateOrderRequest Request)
    : IRequest<Result<CreateOrderResponse>>;

/// <summary>Handler for <see cref="PlaceOrderCommand"/>.</summary>
public sealed class PlaceOrderCommandHandler
    : IRequestHandler<PlaceOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IFlutterwavePaymentService _flutterwavePayment;
    private readonly IEmailService _email;
    private readonly IBankTransferDetailsProvider _bankTransferDetails;

    /// <summary>Initialises the handler.</summary>
    public PlaceOrderCommandHandler(
        IUnitOfWork uow,
        IFlutterwavePaymentService flutterwavePayment,
        IEmailService email,
        IBankTransferDetailsProvider bankTransferDetails)
    {
        _uow = uow;
        _flutterwavePayment = flutterwavePayment;
        _email = email;
        _bankTransferDetails = bankTransferDetails;
    }

    /// <inheritdoc />
    public async Task<Result<CreateOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        CreateOrderRequest req = command.Request;

        // Resolve cart.
        CartEntity? cart = command.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(command.UserId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(req.GuestSessionId)
                ? await _uow.Carts.GetBySessionIdAsync(req.GuestSessionId, cancellationToken)
                : null;

        List<OrderSourceItem> sourceItems = cart is not null && cart.Items.Any()
            ? cart.Items.Select(i => new OrderSourceItem(i.ProductId, i.VariantId, i.Quantity)).ToList()
            : req.Items.Select(i => new OrderSourceItem(i.ProductId, null, i.Quantity)).ToList();

        if (!sourceItems.Any())
        {
            return Error.Validation("Cart is empty. Add items before placing an order.");
        }

        // Resolve delivery address JSON snapshot.
        string addressJson;
        string deliveryState;
        if (req.AddressId.HasValue)
        {
            Address? addr = await _uow.Addresses.GetByIdAsync(req.AddressId.Value, cancellationToken);
            if (addr is null) return Error.NotFound("Delivery address");
            deliveryState = addr.State;
            addressJson = JsonSerializer.Serialize(new
            {
                addr.Label, addr.Line1, addr.Line2,
                addr.City, addr.State, addr.PostalCode,
            });
        }
        else if (req.InlineAddress is not null)
        {
            deliveryState = req.InlineAddress.State;
            addressJson = JsonSerializer.Serialize(req.InlineAddress);
        }
        else
        {
            return Error.Validation("A delivery address is required.");
        }

        // Pay on Delivery is only offered in Osun State, where the business is based and
        // can physically collect cash. Every other state must pay online up front.
        if (req.PaymentMethod == PaymentMethod.PayOnDelivery &&
            !string.Equals(deliveryState.Trim(), "Osun", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(
                "Pay on Delivery is only available for orders within Osun State. " +
                "Please choose Card, Bank Transfer, or PAY NOW for delivery to other states.");
        }

        // Build order items + reserve stock.
        List<OrderItem> items = new();
        decimal itemsSubtotal = 0m;

        foreach (OrderSourceItem sourceItem in sourceItems)
        {
            if (sourceItem.Quantity <= 0)
            {
                return Error.Validation("Order item quantity must be greater than zero.");
            }

            Product? product = await _uow.Products.GetByIdWithLockAsync(sourceItem.ProductId, cancellationToken);
            if (product is null) return Error.NotFound($"Product {sourceItem.ProductId}");

            decimal unitPrice = sourceItem.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == sourceItem.VariantId)?.Price ?? product.Price
                : product.Price;

            int availableQty = sourceItem.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == sourceItem.VariantId)?.AvailableQty ?? 0
                : product.AvailableQty;

            if (availableQty < sourceItem.Quantity)
            {
                return Error.Validation($"Insufficient stock for '{product.Name}'. Only {availableQty} unit(s) available.");
            }

            // Reserve stock.
            if (sourceItem.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == sourceItem.VariantId);
                if (variant is not null) variant.ReservedQty += sourceItem.Quantity;
            }
            else
            {
                product.ReservedQty += sourceItem.Quantity;
            }

            _uow.Products.Update(product);

            decimal lineTotal = unitPrice * sourceItem.Quantity;
            itemsSubtotal += lineTotal;

            items.Add(new OrderItem
            {
                ProductId   = sourceItem.ProductId,
                VariantId   = sourceItem.VariantId,
                Quantity    = sourceItem.Quantity,
                UnitPrice   = unitPrice,
                LineTotal   = lineTotal,
                ProductSnapshotJson = JsonSerializer.Serialize(new
                {
                    Name   = product.Name,
                    SKU    = product.SKU,
                    Slug   = product.Slug,
                    ImageUrl = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.DisplayUrl,
                }),
            });
        }

        // Apply coupon.
        decimal discount = 0m;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            coupon = await _uow.Coupons.GetByCodeAsync(req.CouponCode, cancellationToken);
            if (coupon is not null && coupon.IsActive && (coupon.ExpiresAt is null || coupon.ExpiresAt > DateTime.UtcNow))
            {
                if (coupon.MinimumOrderAmount is null || itemsSubtotal >= coupon.MinimumOrderAmount)
                {
                    discount = coupon.Type == Domain.Enums.CouponType.Percentage
                        ? itemsSubtotal * (coupon.Value / 100m)
                        : coupon.Value;
                }
            }
        }

        decimal deliveryFee = req.DeliveryMethod switch
        {
            DeliveryMethod.Express  => 3500m,
            DeliveryMethod.Pickup   => 0m,
            DeliveryMethod.Standard => 1500m,
            _                       => 1500m,
        };
        int deliveryDays = req.DeliveryMethod switch
        {
            DeliveryMethod.Express  => 3,
            DeliveryMethod.Standard => 5,
            _                       => 5,
        };
        decimal total       = itemsSubtotal - discount + deliveryFee;

        string orderNumber = await _uow.Orders.GenerateOrderNumberAsync(cancellationToken);

        Order order = new()
        {
            OrderNumber         = orderNumber,
            UserId              = command.UserId,
            GuestEmail          = req.GuestEmail,
            Status              = OrderStatus.Pending,
            PaymentStatus       = PaymentStatus.Unpaid,
            PaymentMethod       = req.PaymentMethod,
            Subtotal            = itemsSubtotal,
            DiscountAmount      = discount,
            DeliveryFee         = deliveryFee,
            Total               = total,
            CouponId            = coupon?.Id,
            DeliveryAddressJson = addressJson,
            DeliveryMethod      = req.DeliveryMethod,
            EstimatedDelivery   = DateTime.UtcNow.AddDays(deliveryDays),
            Notes               = req.Notes,
            Items               = items,
        };

        await _uow.Orders.AddAsync(order, cancellationToken);

        if (coupon is not null)
        {
            coupon.UsedCount++;
            _uow.Coupons.Update(coupon);
        }

        // Clear the cart when this order was created from a backend cart.
        if (cart is not null)
        {
            cart.Items.Clear();
            _uow.Carts.Update(cart);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Initiate payment.
        string? paymentUrl  = null;
        string? paymentRef  = null;
        BankDetailsDto? bankDetails = null;

        if (req.PaymentMethod == PaymentMethod.BankTransfer)
        {
            BankTransferDetails details = _bankTransferDetails.GetDetails();
            bankDetails = new BankDetailsDto
            {
                BankName = details.BankName,
                AccountNumber = details.AccountNumber,
                AccountName = details.AccountName,
            };
        }
        else if (req.PaymentMethod is PaymentMethod.Card or PaymentMethod.USSD)
        {
            User? recipientUser = command.UserId.HasValue
                ? await _uow.Users.GetByIdAsync(command.UserId.Value, cancellationToken)
                : null;
            string recipientEmail = recipientUser?.Email ?? req.GuestEmail ?? string.Empty;
            string recipientName = recipientUser is not null
                ? $"{recipientUser.FirstName} {recipientUser.LastName}".Trim()
                : "Guest";
            string recipientPhone = recipientUser?.Phone ?? string.Empty;

            Domain.Interfaces.Services.PaymentInitResult? payResult =
                await _flutterwavePayment.InitializeAsync(
                    recipientEmail,
                    recipientName,
                    recipientPhone,
                    order.Id,
                    orderNumber,
                    total,
                    cancellationToken);

            if (payResult is null)
            {
                return Error.Internal("Payment initialisation failed. Please try again.");
            }

            order.PaymentReference = payResult.Reference;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            paymentUrl = payResult.AuthorisationUrl;
            paymentRef = payResult.Reference;
        }

        _ = _email.SendOrderConfirmationAsync(
            req.GuestEmail ?? string.Empty,
            "Customer",
            orderNumber,
            total,
            req.PaymentMethod.ToString(),
            order.DeliveryMethod,
            order.EstimatedDelivery,
            cancellationToken: CancellationToken.None);

        return Result<CreateOrderResponse>.Success(new CreateOrderResponse
        {
            OrderId          = order.Id,
            OrderNumber      = orderNumber,
            PaymentUrl       = paymentUrl,
            PaymentReference = paymentRef,
            BankDetails      = bankDetails,
            Total            = total,
        });
    }
}

/// <summary>Validator for <see cref="PlaceOrderCommand"/>.</summary>
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.Request.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");

        RuleFor(x => x.Request.DeliveryMethod)
            .IsInEnum().WithMessage("A valid delivery method is required.");

        RuleFor(x => x.Request.GuestEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.GuestEmail));

        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Request.GuestEmail))
            .WithMessage("Guest email is required for guest checkout.");
    }
}
