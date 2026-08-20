namespace ShopFresherz.Application.Dtos.Notifications;

/// <summary>Notification DTO returned to clients.</summary>
public sealed class NotificationDto
{
    /// <summary>Gets or sets the notification ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the notification type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification link URL.</summary>
    public string LinkUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the notification is read.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the notification was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Response for notifications list with unread count.</summary>
public sealed class NotificationResponseDto
{
    /// <summary>Gets or sets the notification items.</summary>
    public IReadOnlyList<NotificationDto> Items { get; set; } = [];

    /// <summary>Gets or sets the number of unread notifications.</summary>
    public int UnreadCount { get; set; }
}