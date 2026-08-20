namespace ShopFresherz.Application.Dtos.Notifications;

/// <summary>User notification preferences DTO.</summary>
public sealed class NotificationPreferencesDto
{
    /// <summary>Gets or sets whether order notifications are enabled.</summary>
    public bool OrderUpdates { get; set; } = true;

    /// <summary>Gets or sets whether promotional emails are enabled.</summary>
    public bool Promotions { get; set; } = true;

    /// <summary>Gets or sets whether stock notifications are enabled.</summary>
    public bool BackInStock { get; set; } = true;

    /// <summary>Gets or sets whether wishlist reminders are enabled.</summary>
    public bool WishlistReminders { get; set; } = true;

    /// <summary>Gets or sets whether product review reminders are enabled.</summary>
    public bool ReviewReminders { get; set; } = true;
}