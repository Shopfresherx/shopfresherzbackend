namespace ShopFresherz.Application.Dtos.PaymentMethods;

/// <summary>Saved payment method summary DTO.</summary>
public sealed class PaymentMethodDto
{
    /// <summary>Gets or sets the payment method ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the card type (e.g., "Visa", "Mastercard", "Verve").</summary>
    public string CardType { get; set; } = string.Empty;

    /// <summary>Gets or sets the card number.</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the cardholder name.</summary>
    public string CardHolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiry month (1–12).</summary>
    public int ExpiryMonth { get; set; }

    /// <summary>Gets or sets the expiry year (4-digit).</summary>
    public int ExpiryYear { get; set; }

    /// <summary>Gets or sets whether this is the default payment method.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets when the payment method was added.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request payload for adding a bank card.</summary>
public sealed class CreatePaymentMethodRequest
{
    /// <summary>Gets or sets the card type (e.g., "Visa", "Mastercard", "Verve").</summary>
    public string CardType { get; set; } = string.Empty;

    /// <summary>Gets or sets the full card number (up to 19 digits).</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the cardholder name as it appears on the card.</summary>
    public string CardHolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiry month (1–12).</summary>
    public int ExpiryMonth { get; set; }

    /// <summary>Gets or sets the expiry year (4-digit, e.g., 2027).</summary>
    public int ExpiryYear { get; set; }

    /// <summary>Gets or sets whether this should be the default payment method.</summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>Request payload for updating a payment method.</summary>
public sealed class UpdatePaymentMethodRequest
{
    /// <summary>Gets or sets whether this should be the default payment method.</summary>
    public bool? IsDefault { get; set; }

    /// <summary>Gets or sets the updated cardholder name.</summary>
    public string? CardHolderName { get; set; }

    /// <summary>Gets or sets the updated expiry month.</summary>
    public int? ExpiryMonth { get; set; }

    /// <summary>Gets or sets the updated expiry year.</summary>
    public int? ExpiryYear { get; set; }
}
