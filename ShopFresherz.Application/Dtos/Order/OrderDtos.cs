using System.Text.Json;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Application.Dtos.Order;

/// <summary>Snapshot of the delivery address embedded in an order response.</summary>
public sealed class DeliveryAddressSnapshot
{
    /// <summary>Gets or sets the friendly label (e.g., "Home").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
}

/// <summary>Snapshot of the product embedded in an order line item.</summary>
public sealed class ProductSnapshot
{
    /// <summary>Gets or sets the product name at order time.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product SKU at order time.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary 540px image URL at order time.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the product slug at order time.</summary>
    public string Slug { get; set; } = string.Empty;
}

/// <summary>Order line item DTO.</summary>
public sealed class OrderItemDto
{
    /// <summary>Gets or sets the order item ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the selected variant ID.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the product snapshot at time of order.</summary>
    public ProductSnapshot? ProductSnapshot { get; set; }

    /// <summary>Gets or sets the quantity ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price at order time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the computed line total.</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>Full order DTO returned to clients and admins.</summary>
public sealed class OrderDto
{
    /// <summary>Gets or sets the order ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable order reference.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user ID.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the current order status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the payment status.</summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>Gets or sets the payment method.</summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Gets or sets the subtotal before discounts.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Gets or sets the discount amount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Gets or sets the delivery fee.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// Gets or sets the VAT amount charged at order time. Always 0 for orders placed after
    /// VAT was removed; non-zero only on historical orders placed while VAT applied.
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>Gets or sets the final total.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the delivery address snapshot.</summary>
    public DeliveryAddressSnapshot? DeliveryAddress { get; set; }

    /// <summary>Gets or sets the selected delivery method.</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDelivery { get; set; }

    /// <summary>Gets or sets the courier tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the customer order note.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the order line items.</summary>
    public IReadOnlyList<OrderItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the UTC timestamp when the order was placed.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request payload for creating a new order from the active cart.</summary>
public sealed class CreateOrderRequest
{
    /// <summary>Gets or sets direct checkout line items from the storefront payload.</summary>
    public List<CreateOrderItemRequest> Items { get; set; } = new();

    /// <summary>Gets or sets the saved address ID to deliver to (mutually exclusive with InlineAddress).</summary>
    public Guid? AddressId { get; set; }

    /// <summary>Gets or sets the saved address ID string from the storefront payload.</summary>
    public string? ShippingAddressId
    {
        get => AddressId?.ToString();
        set => AddressId = ParseNullableGuid(value, nameof(ShippingAddressId));
    }

    /// <summary>Gets or sets an inline address (mutually exclusive with AddressId).</summary>
    public InlineAddressRequest? InlineAddress { get; set; }

    /// <summary>Gets or sets the selected delivery method.</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the nested delivery object from the storefront payload.</summary>
    public CreateOrderDeliveryRequest? Delivery
    {
        get => null;
        set
        {
            if (value is not null)
            {
                DeliveryMethod = value.ToDeliveryMethod();
            }
        }
    }

    /// <summary>Gets or sets the selected payment method.</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Gets or sets the nested payment object from the storefront payload.</summary>
    public CreateOrderPaymentRequest? Payment
    {
        get => null;
        set
        {
            if (value is not null)
            {
                PaymentMethod = value.ToPaymentMethod();
            }
        }
    }

    /// <summary>Gets or sets the nested pricing object from the storefront payload.</summary>
    public CreateOrderPricingRequest? Pricing { get; set; }

    /// <summary>Gets or sets the coupon code to apply (optional).</summary>
    public string? CouponCode { get; set; }

    /// <summary>Gets or sets the nested coupon object from the storefront payload.</summary>
    public CreateOrderCouponRequest? Coupon
    {
        get => null;
        set
        {
            if (value is not null)
            {
                CouponCode = value.Code;
            }
        }
    }

    /// <summary>Gets or sets optional customer notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the guest email for guest checkout (required if not authenticated).</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Gets or sets the guest session ID for guest checkout.</summary>
    public string? GuestSessionId { get; set; }

    private static Guid? ParseNullableGuid(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out Guid parsed)
            ? parsed
            : throw new JsonException($"{propertyName} must be a valid GUID.");
    }
}

/// <summary>Line item supplied by direct storefront checkout payloads.</summary>
public sealed class CreateOrderItemRequest
{
    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product name supplied by the storefront.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the requested quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the storefront unit price. Server-calculated prices are authoritative.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the storefront image URL.</summary>
    public string? Image { get; set; }
}

/// <summary>Nested delivery object supplied by the storefront checkout payload.</summary>
public sealed class CreateOrderDeliveryRequest
{
    /// <summary>Gets or sets the frontend delivery method string.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the frontend delivery fee. Server-calculated fees are authoritative.</summary>
    public decimal Fee { get; set; }

    /// <summary>Converts the frontend delivery method to the domain enum.</summary>
    public DeliveryMethod ToDeliveryMethod() => Method.Trim().ToLowerInvariant() switch
    {
        "standard" => DeliveryMethod.Standard,
        "express"  => DeliveryMethod.Express,
        "pickup"   => DeliveryMethod.Pickup,
        _          => throw new JsonException("delivery.method must be standard, express, or pickup."),
    };
}

/// <summary>Nested payment object supplied by the storefront checkout payload.</summary>
public sealed class CreateOrderPaymentRequest
{
    /// <summary>Gets or sets the frontend payment method string.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional saved card ID from the storefront payload.</summary>
    public string? SavedCardId { get; set; }

    /// <summary>Converts the frontend payment method to the domain enum.</summary>
    public PaymentMethod ToPaymentMethod() => Method.Trim().ToLowerInvariant() switch
    {
        "card"            => PaymentMethod.Card,
        "bank_transfer"   => PaymentMethod.BankTransfer,
        "banktransfer"    => PaymentMethod.BankTransfer,
        "pay_on_delivery" => PaymentMethod.PayOnDelivery,
        "payondelivery"   => PaymentMethod.PayOnDelivery,
        _                 => throw new JsonException("payment.method must be card, bank_transfer, or pay_on_delivery."),
    };
}

/// <summary>Nested pricing object supplied by the storefront checkout payload.</summary>
public sealed class CreateOrderPricingRequest
{
    /// <summary>Gets or sets the frontend subtotal.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Gets or sets the frontend discount amount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Gets or sets the frontend delivery fee.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>Gets or sets the frontend tax amount.</summary>
    public decimal Tax { get; set; }

    /// <summary>Gets or sets the frontend total.</summary>
    public decimal Total { get; set; }
}

/// <summary>Nested coupon object supplied by the storefront checkout payload.</summary>
public sealed class CreateOrderCouponRequest
{
    /// <summary>Gets or sets the coupon code.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the frontend discount amount. Server coupon calculation is authoritative.</summary>
    public decimal DiscountAmount { get; set; }
}

/// <summary>Inline address for one-time checkout without saving to profile.</summary>
public sealed class InlineAddressRequest
{
    /// <summary>Gets or sets the address label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
}

/// <summary>Response returned after a successful order creation.</summary>
public sealed class CreateOrderResponse
{
    /// <summary>Gets or sets the created order ID.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the Flutterwave hosted checkout URL to redirect the customer to.</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Gets or sets the payment reference for subsequent verification.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Gets or sets manual transfer instructions when bank transfer is selected.</summary>
    public BankDetailsDto? BankDetails { get; set; }

    /// <summary>Gets or sets the order total.</summary>
    public decimal Total { get; set; }
}

/// <summary>Merchant account details returned for manual bank transfers.</summary>
public sealed class BankDetailsDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Instructions { get; set; } =
        "Please make payment and send proof of payment via WhatsApp to complete your order.";
}

/// <summary>Request payload for updating order status (admin).</summary>
public sealed class UpdateOrderStatusRequest
{
    /// <summary>Gets or sets the new order status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the courier tracking number (required when transitioning to Shipped).</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDelivery { get; set; }
}
