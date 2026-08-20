using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Stores a user's back-in-stock notification request for an out-of-stock product.
/// Auto-notified and removed when the product is restocked.
/// </summary>
public class NotifyRequest : BaseEntity
{
    /// <summary>Gets or sets the requesting user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the requesting user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the out-of-stock product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets a value indicating whether the notification has been sent.</summary>
    public bool IsNotified { get; set; } = false;
}
