using System.Text.Json.Serialization;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Application.Dtos.Checkout;

// ─── initiate-payment ────────────────────────────────────────────────────────

/// <summary>A single line item supplied to the two-step checkout initiate-payment endpoint.</summary>
public sealed class InitiatePaymentItem
{
    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the optional product variant ID.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the quantity ordered.</summary>
    public int Quantity { get; set; }
}

/// <summary>Request payload for <c>POST /api/v1/checkout/initiate-payment</c>.</summary>
public sealed class InitiatePaymentRequest
{
    /// <summary>Gets or sets the line items to purchase.</summary>
    public List<InitiatePaymentItem> Items { get; set; } = new();

    /// <summary>Gets or sets the saved address ID (nullable when <see cref="InlineAddress"/> is supplied).</summary>
    public Guid? AddressId { get; set; }

    /// <summary>Gets or sets a one-time inline delivery address.</summary>
    public InlineAddressRequest? InlineAddress { get; set; }

    /// <summary>Gets or sets the delivery method (Standard | Express | Pickup).</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the payment method (Card | BankTransfer).</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Gets or sets the client-calculated pricing breakdown (re-validated server-side).</summary>
    public CreateOrderPricingRequest? Pricing { get; set; }

    /// <summary>Gets or sets the optional coupon code.</summary>
    public string? CouponCode { get; set; }

    /// <summary>Gets or sets the guest email (used when the request is unauthenticated).</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Gets or sets the guest display name (used when the request is unauthenticated).</summary>
    public string? GuestName { get; set; }

    /// <summary>Gets or sets the guest phone number (used when the request is unauthenticated).</summary>
    public string? GuestPhone { get; set; }
}

/// <summary>Response payload for <c>POST /api/v1/checkout/initiate-payment</c>.</summary>
public sealed class InitiatePaymentResponse
{
    /// <summary>Gets or sets the created order ID the frontend echoes back on confirmation.</summary>
    [JsonPropertyName("pendingOrderId")]
    public Guid PendingOrderId { get; set; }

    /// <summary>Gets or sets the human-readable order number (e.g., SFZ-2026-00001).</summary>
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the resolved payment method ("Card" | "BankTransfer" | "PayOnDelivery").</summary>
    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the manual bank transfer details (BankTransfer only).</summary>
    [JsonPropertyName("bankDetails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CheckoutBankDetailsDto? BankDetails { get; set; }

    /// <summary>Gets or sets the Flutterwave inline-popup configuration (Card only).</summary>
    [JsonPropertyName("flutterwaveConfig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlutterwaveConfigDto? FlutterwaveConfig { get; set; }

    /// <summary>Gets or sets the Flutterwave hosted checkout URL to redirect the customer to (Card only).</summary>
    [JsonPropertyName("paymentLink")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentLink { get; set; }

    /// <summary>Gets or sets an informational message for methods with no further client action (PayOnDelivery).</summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}

/// <summary>Manual bank-transfer details returned for the BankTransfer payment method.</summary>
public sealed class CheckoutBankDetailsDto
{
    [JsonPropertyName("bankName")]
    public string BankName { get; set; } = string.Empty;

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;
}

/// <summary>
/// Configuration object passed verbatim to Flutterwave's inline popup SDK on the frontend.
/// Property names are snake_case to match the SDK's expected payload exactly.
/// </summary>
public sealed class FlutterwaveConfigDto
{
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    [JsonPropertyName("tx_ref")]
    public string TxRef { get; set; } = string.Empty;

    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "NGN";

    [JsonPropertyName("customer")]
    public FlutterwaveCustomerDto Customer { get; set; } = new();

    [JsonPropertyName("meta")]
    public FlutterwaveMetaDto Meta { get; set; } = new();

    [JsonPropertyName("customizations")]
    public FlutterwaveCustomizationsDto Customizations { get; set; } = new();
}

/// <summary>Customer block of the Flutterwave inline popup configuration.</summary>
public sealed class FlutterwaveCustomerDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>Meta block of the Flutterwave inline popup configuration.</summary>
public sealed class FlutterwaveMetaDto
{
    [JsonPropertyName("pendingOrderId")]
    public string PendingOrderId { get; set; } = string.Empty;
}

/// <summary>Customizations block of the Flutterwave inline popup configuration.</summary>
public sealed class FlutterwaveCustomizationsDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "ShopFresherz";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Order payment";

    [JsonPropertyName("logo")]
    public string Logo { get; set; } = "https://res.cloudinary.com/dtcpnqmi9/image/upload/v1782151604/ShopFreshersV2LogoOrange_mvnyvo.png";
}

// ─── confirm-order ───────────────────────────────────────────────────────────

/// <summary>Request payload for <c>POST /api/v1/checkout/confirm-order</c>.</summary>
public sealed class ConfirmOrderRequest
{
    /// <summary>Gets or sets the Draft order ID returned by initiate-payment.</summary>
    public Guid PendingOrderId { get; set; }

    /// <summary>Gets or sets the Flutterwave transaction ID from the popup result.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the transaction reference echoed back by the popup.</summary>
    public string TxRef { get; set; } = string.Empty;

    /// <summary>Gets or sets the popup-reported status (e.g., "successful").</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Response payload for <c>POST /api/v1/checkout/confirm-order</c>.</summary>
public sealed class ConfirmOrderResponse
{
    /// <summary>Gets or sets the human-readable order number (e.g., SFZ-2026-00001).</summary>
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;
}
