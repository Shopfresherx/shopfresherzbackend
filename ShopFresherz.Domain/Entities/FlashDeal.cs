using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A time-limited flash deal offering a reduced price on a specific product or variant.
/// Cached in Redis with a 30-second TTL. Price hierarchy: Flash &gt; Sale &gt; Base.
/// </summary>
public class FlashDeal : BaseEntity
{
    /// <summary>Gets or sets the product this deal applies to.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the specific variant ID (null if deal applies to base product).</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the discounted flash sale price (NGN).</summary>
    public decimal SalePrice { get; set; }

    /// <summary>Gets or sets the original price captured at deal creation time.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>Gets or sets the UTC date/time when this deal goes live.</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Gets or sets the UTC date/time when this deal expires.</summary>
    public DateTime EndsAt { get; set; }

    /// <summary>Gets or sets the optional maximum number of units that can be sold at flash price.</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Gets or sets the count of units sold at flash price (real-time, shown in admin).</summary>
    public int SoldQuantity { get; set; } = 0;

    /// <summary>Gets or sets a value indicating whether this deal is admin-enabled.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets a value indicating whether this deal has passed its end time.</summary>
    public bool IsExpired => DateTime.UtcNow > EndsAt;

    /// <summary>Gets a value indicating whether this deal is currently live and accepting orders.</summary>
    public bool IsLive => IsActive && !IsExpired && DateTime.UtcNow >= StartsAt;
}
