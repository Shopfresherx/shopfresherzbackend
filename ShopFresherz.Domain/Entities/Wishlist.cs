using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a product saved to a user's wishlist.
/// Triggers a price-drop email alert when the product price decreases.
/// </summary>
public class Wishlist : BaseEntity
{
    /// <summary>Gets or sets the user who saved this product.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the wishlisted product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product navigation property.</summary>
    public Product Product { get; set; } = null!;
}
