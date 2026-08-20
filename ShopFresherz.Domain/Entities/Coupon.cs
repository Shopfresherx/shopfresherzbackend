using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A discount coupon with fixed or percentage value, usage limits, and optional restrictions.
/// Uses INT identity PK (lookup table convention per PRD).
/// </summary>
public class Coupon
{
    /// <summary>Gets or sets the integer identity primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the coupon code (case-insensitive, unique).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the discount is a percentage or fixed naira amount.</summary>
    public CouponType Type { get; set; }

    /// <summary>Gets or sets the discount value (percentage 0–100 or naira amount).</summary>
    public decimal Value { get; set; }

    /// <summary>Gets or sets the minimum order subtotal required to apply this coupon.</summary>
    public decimal? MinimumOrderAmount { get; set; }

    /// <summary>Gets or sets the maximum total number of times this coupon can be redeemed.</summary>
    public int? MaxUses { get; set; }

    /// <summary>Gets or sets the current total redemption count.</summary>
    public int UsedCount { get; set; } = 0;

    /// <summary>Gets or sets the maximum number of times a single user can use this coupon.</summary>
    public int? MaxUsesPerUser { get; set; }

    /// <summary>Gets or sets a value indicating whether this coupon can be stacked with others.</summary>
    public bool IsStackable { get; set; } = false;

    /// <summary>Gets or sets the UTC expiry date/time of this coupon.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this coupon is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets an optional restriction to a specific product.</summary>
    public Guid? RestrictToProductId { get; set; }

    /// <summary>Gets or sets an optional restriction to a specific category.</summary>
    public int? RestrictToCategoryId { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
