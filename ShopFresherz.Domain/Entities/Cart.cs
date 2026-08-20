using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Shopping cart supporting both guest (session-based) and authenticated user sessions.
/// Guest carts are merged into the user cart on login via POST /cart/merge.
/// </summary>
public class Cart : BaseEntity
{
    /// <summary>Gets or sets the authenticated user ID (null for guest carts).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the authenticated user navigation property.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the browser session identifier for guest carts.</summary>
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the UTC expiry for this cart (30 days from creation).</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    /// <summary>Gets or sets the coupon code currently applied to this cart.</summary>
    public string? CouponCode { get; set; }

    /// <summary>Gets or sets the line items in this cart.</summary>
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
