using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;
/// <summary>Repository contract for Notification persistence operations.</summary>
public interface INotificationRepository
{
    /// <summary>Retrieves a notification by its unique identifier.</summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns paginated notifications for a user.</summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns paginated notifications for admin (all users).</summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of unread notifications across all users.</summary>
    Task<int> GetTotalUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks a notification as read.</summary>
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Marks all notifications as read for a user.</summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new notification.</summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>Marks a notification as modified.</summary>
    void Update(Notification notification);
}