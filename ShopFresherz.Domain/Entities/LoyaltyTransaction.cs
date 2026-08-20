using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Records a single loyalty points transaction (earn, redeem, expire, or bonus).
/// Points expire 12 months from earn date.
/// Earn rate: 1 pt per ₦100 spent on Delivered orders.
/// Redeem rate: 100 pts = ₦100, max 20% of order value.
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    /// <summary>Gets or sets the user whose balance is affected.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the event type (Earned, Redeemed, Expired, Bonus).</summary>
    public LoyaltyEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the points delta.
    /// Positive = credits (earn/bonus). Negative = debits (redeem/expire).
    /// </summary>
    public int Points { get; set; }

    /// <summary>Gets or sets a human-readable description of the transaction.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the order ID that triggered this transaction (if applicable).</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Gets or sets the UTC date/time when these points expire (12 months from earn).</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);
}
