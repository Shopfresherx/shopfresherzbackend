using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;
/// <summary>
/// A user notification for order updates, promotions, back in stock, etc.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the notification type (e.g., OrderStatus, Promotional, BackInStock, WishlistReminder, ReviewReminder).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification link URL.</summary>
    public string LinkUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the notification is read.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the notification was created.</summary>
    public DateTime CreatedAt { get; set; }
}