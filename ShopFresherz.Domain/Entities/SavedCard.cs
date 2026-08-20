using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A saved payment card belonging to a registered user.
/// </summary>
public class SavedCard : BaseEntity
{
    /// <summary>Gets or sets the owning user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = null!;

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

    /// <summary>Gets or sets the payment gateway token/authorisation code for recurring charges.</summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>Gets or sets whether this is the user's default payment method.</summary>
    public bool IsDefault { get; set; } = false;
}
