using System.Text.Json;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Checkout;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Checkout.Commands.InitiatePayment;

/// <summary>
/// First step of the two-step inline-popup checkout. Validates items and pricing,
/// creates a Draft order with reserved stock, and returns either Flutterwave inline
/// config (Card) or manual bank details (BankTransfer). No gateway call is made here.
/// </summary>
/// <param name="UserId">The authenticated user's ID (null for guest checkout).</param>
/// <param name="Request">The initiate-payment payload.</param>
public sealed record InitiatePaymentCommand(Guid? UserId, InitiatePaymentRequest Request)
    : IRequest<Result<InitiatePaymentResponse>>;

/// <summary>Handler for <see cref="InitiatePaymentCommand"/>.</summary>
public sealed class InitiatePaymentCommandHandler
    : IRequestHandler<InitiatePaymentCommand, Result<InitiatePaymentResponse>>
{
    private const decimal RoundingToleranceNgn = 1m;

    private readonly IUnitOfWork _uow;
    private readonly IFlutterwavePaymentService _flutterwave;
    private readonly IBankTransferDetailsProvider _bankTransferDetails;
    private readonly IEmailService _email;

    public InitiatePaymentCommandHandler(
        IUnitOfWork uow,
        IFlutterwavePaymentService flutterwave,
        IBankTransferDetailsProvider bankTransferDetails,
        IEmailService email)
    {
        _uow = uow;
        _flutterwave = flutterwave;
        _bankTransferDetails = bankTransferDetails;
        _email = email;
    }

    /// <inheritdoc />
    public async Task<Result<InitiatePaymentResponse>> Handle(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        InitiatePaymentRequest req = command.Request;

        if (req.Items is null || req.Items.Count == 0)
        {
            return Error.Validation("At least one item is required to start checkout.");
        }

        if (req.PaymentMethod is not (PaymentMethod.Card or PaymentMethod.BankTransfer or PaymentMethod.PayOnDelivery))
        {
            return Error.Validation("Payment method must be Card, BankTransfer, or PayOnDelivery.");
        }

        // Resolve customer details for the order + Flutterwave config.
        string customerEmail;
        string customerName;
        string customerPhone;
        if (command.UserId.HasValue)
        {
            User? user = await _uow.Users.GetByIdAsync(command.UserId.Value, cancellationToken);
            customerEmail = user?.Email ?? req.GuestEmail ?? string.Empty;
            customerName = user is not null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : req.GuestName ?? "Guest";
            customerPhone = user?.Phone ?? req.GuestPhone ?? string.Empty;
        }
        else
        {
            customerEmail = req.GuestEmail ?? string.Empty;
            customerName = string.IsNullOrWhiteSpace(req.GuestName) ? "Guest" : req.GuestName.Trim();
            customerPhone = req.GuestPhone ?? string.Empty;
        }

        // Resolve delivery address snapshot.
        string addressJson;
        string deliveryState;
        if (req.AddressId.HasValue)
        {
            Address? addr = await _uow.Addresses.GetByIdAsync(req.AddressId.Value, cancellationToken);
            if (addr is null) return Error.NotFound("Delivery address");
            if (!command.UserId.HasValue || addr.UserId != command.UserId.Value)
            {
                return Error.Forbidden("The selected delivery address does not belong to this account.");
            }

            deliveryState = addr.State;
            addressJson = JsonSerializer.Serialize(new
            {
                addr.Label, addr.Line1, addr.Line2,
                addr.City, addr.State, addr.PostalCode,
                Phone = customerPhone,
            });
        }
        else if (req.InlineAddress is not null)
        {
            deliveryState = req.InlineAddress.State;
            addressJson = JsonSerializer.Serialize(new
            {
                req.InlineAddress.Label, req.InlineAddress.Line1, req.InlineAddress.Line2,
                req.InlineAddress.City, req.InlineAddress.State, req.InlineAddress.PostalCode,
                Phone = customerPhone,
            });
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

        // Build order items, validate stock, and reserve.
        List<OrderItem> items = new();
        decimal itemsSubtotal = 0m;

        foreach (InitiatePaymentItem sourceItem in req.Items)
        {
            if (sourceItem.Quantity <= 0)
            {
                return Error.Validation("Order item quantity must be greater than zero.");
            }

            Product? product = await _uow.Products.GetByIdWithLockAsync(sourceItem.ProductId, cancellationToken);
            if (product is null) return Error.NotFound($"Product {sourceItem.ProductId}");

            ProductVariant? variant = sourceItem.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == sourceItem.VariantId)
                : null;

            if (sourceItem.VariantId.HasValue && variant is null)
            {
                return Error.Validation($"The selected variant for '{product.Name}' is invalid.");
            }

            decimal unitPrice = variant?.Price ?? product.Price;
            int availableQty = variant?.AvailableQty ?? product.AvailableQty;

            if (availableQty < sourceItem.Quantity)
            {
                return Error.Validation($"Insufficient stock for '{product.Name}'. Only {availableQty} unit(s) available.");
            }

            if (variant is not null)
            {
                variant.ReservedQty += sourceItem.Quantity;
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
                ProductId = sourceItem.ProductId,
                VariantId = sourceItem.VariantId,
                Quantity = sourceItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                ProductSnapshotJson = JsonSerializer.Serialize(new
                {
                    Name = product.Name,
                    SKU = product.SKU,
                    Slug = product.Slug,
                    ImageUrl = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.DisplayUrl,
                }),
            });
        }

        // Apply coupon discount (authoritative server calculation).
        decimal discount = 0m;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            coupon = await _uow.Coupons.GetByCodeAsync(req.CouponCode, cancellationToken);
            if (coupon is not null && coupon.IsActive && (coupon.ExpiresAt is null || coupon.ExpiresAt > DateTime.UtcNow))
            {
                if (coupon.MinimumOrderAmount is null || itemsSubtotal >= coupon.MinimumOrderAmount)
                {
                    discount = coupon.Type == CouponType.Percentage
                        ? itemsSubtotal * (coupon.Value / 100m)
                        : coupon.Value;
                    discount = Math.Clamp(discount, 0m, itemsSubtotal);
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
        decimal total = itemsSubtotal - discount + deliveryFee;

        // Never trust client pricing: validate the client total against the server total.
        if (req.Pricing is not null &&
            Math.Abs(req.Pricing.Total - total) > RoundingToleranceNgn)
        {
            return Error.Validation("Order total mismatch. Please refresh and try again.");
        }

        string orderNumber = await _uow.Orders.GenerateOrderNumberAsync(cancellationToken);

        Order order = new()
        {
            OrderNumber = orderNumber,
            UserId = command.UserId,
            GuestEmail = command.UserId.HasValue ? null : req.GuestEmail,
            // PayOnDelivery has no gateway/popup step and no confirm-order follow-up call,
            // so it goes straight to Pending rather than the two-step-checkout Draft state.
            Status = req.PaymentMethod == PaymentMethod.PayOnDelivery ? OrderStatus.Pending : OrderStatus.Draft,
            PaymentStatus = PaymentStatus.Unpaid,
            PaymentMethod = req.PaymentMethod,
            Subtotal = itemsSubtotal,
            DiscountAmount = discount,
            DeliveryFee = deliveryFee,
            Total = total,
            CouponId = coupon?.Id,
            DeliveryAddressJson = addressJson,
            DeliveryMethod = req.DeliveryMethod,
            EstimatedDelivery = DateTime.UtcNow.AddDays(deliveryDays),
            Items = items,
        };

        // Id is assigned client-side (BaseEntity), so it is safe to build tx_ref before saving.
        order.TxRef = $"TXN-{order.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (req.PaymentMethod == PaymentMethod.PayOnDelivery)
        {
            _ = _email.SendOrderConfirmationAsync(
                customerEmail, customerName, orderNumber, total, nameof(PaymentMethod.PayOnDelivery),
                order.DeliveryMethod, order.EstimatedDelivery, customerPhone, CancellationToken.None);
            _ = _email.SendAdminOrderNotificationAsync(
                orderNumber, customerName, customerEmail, customerPhone,
                total, nameof(PaymentMethod.PayOnDelivery), addressJson, CancellationToken.None);

            return Result<InitiatePaymentResponse>.Success(new InitiatePaymentResponse
            {
                PendingOrderId = order.Id,
                OrderNumber = orderNumber,
                PaymentMethod = nameof(PaymentMethod.PayOnDelivery),
                Message = "Your order has been placed. Payment will be collected on delivery.",
            });
        }

        if (req.PaymentMethod == PaymentMethod.BankTransfer)
        {
            BankTransferDetails details = _bankTransferDetails.GetDetails();

            _ = _email.SendOrderConfirmationAsync(
                customerEmail, customerName, orderNumber, total, nameof(PaymentMethod.BankTransfer),
                order.DeliveryMethod, order.EstimatedDelivery, customerPhone, CancellationToken.None);
            _ = _email.SendAdminOrderNotificationAsync(
                orderNumber, customerName, customerEmail, customerPhone,
                total, nameof(PaymentMethod.BankTransfer), addressJson, CancellationToken.None);

            return Result<InitiatePaymentResponse>.Success(new InitiatePaymentResponse
            {
                PendingOrderId = order.Id,
                OrderNumber = orderNumber,
                PaymentMethod = nameof(PaymentMethod.BankTransfer),
                BankDetails = new CheckoutBankDetailsDto
                {
                    BankName = details.BankName,
                    AccountNumber = details.AccountNumber,
                    AccountName = details.AccountName,
                    Instructions =
                        $"Transfer the exact total of ₦{total:N2} to the account above using {orderNumber} " +
                        "as the payment reference, then send your proof of payment to complete your order.",
                },
            });
        }

        PaymentInitResult? payResult = await _flutterwave.InitializeAsync(
            customerEmail,
            customerName,
            customerPhone,
            order.Id,
            orderNumber,
            total,
            cancellationToken);

        return Result<InitiatePaymentResponse>.Success(new InitiatePaymentResponse
        {
            PendingOrderId = order.Id,
            OrderNumber = orderNumber,
            PaymentMethod = nameof(PaymentMethod.Card),
            PaymentLink = payResult?.AuthorisationUrl,
            FlutterwaveConfig = new FlutterwaveConfigDto
            {
                PublicKey = _flutterwave.PublicKey,
                TxRef = order.TxRef,
                RedirectUrl = _flutterwave.CallbackUrl,
                Amount = total,
                Currency = "NGN",
                Customer = new FlutterwaveCustomerDto
                {
                    Email = customerEmail,
                    Name = string.IsNullOrWhiteSpace(customerName) ? "Guest" : customerName,
                    PhoneNumber = customerPhone,
                },
                Meta = new FlutterwaveMetaDto
                {
                    PendingOrderId = order.Id.ToString(),
                },
                Customizations = new FlutterwaveCustomizationsDto
                {
                    Title = "ShopFresherz",
                    Description = "Order payment",
                    Logo = "https://res.cloudinary.com/dtcpnqmi9/image/upload/v1782151604/ShopFreshersV2LogoOrange_mvnyvo.png",
                },
            },
        });
    }
}

/// <summary>Validator for <see cref="InitiatePaymentCommand"/>.</summary>
public sealed class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.Request.Items)
            .NotEmpty().WithMessage("At least one item is required to start checkout.");

        RuleFor(x => x.Request.DeliveryMethod)
            .IsInEnum().WithMessage("A valid delivery method is required.");

        RuleFor(x => x.Request.PaymentMethod)
            .Must(m => m is PaymentMethod.Card or PaymentMethod.BankTransfer or PaymentMethod.PayOnDelivery)
            .WithMessage("Payment method must be Card, BankTransfer, or PayOnDelivery.");

        RuleFor(x => x.Request.GuestEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.GuestEmail));

        RuleFor(x => x.Request.InlineAddress!.Line1)
            .NotEmpty()
            .When(x => x.Request.InlineAddress is not null)
            .WithMessage("inlineAddress.line1 is required.");

        RuleFor(x => x.Request.InlineAddress!.City)
            .NotEmpty()
            .When(x => x.Request.InlineAddress is not null)
            .WithMessage("inlineAddress.city is required.");

        RuleFor(x => x.Request.InlineAddress!.State)
            .NotEmpty()
            .When(x => x.Request.InlineAddress is not null)
            .WithMessage("inlineAddress.state is required.");

        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Request.GuestEmail))
            .WithMessage("Guest email is required for guest checkout.");
    }
}
