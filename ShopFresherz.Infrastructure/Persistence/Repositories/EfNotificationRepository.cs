using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;
/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
internal sealed class EfNotificationRepository : INotificationRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfNotificationRepository"/>.</summary>
    public EfNotificationRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> query = _context.Notifications
            .Include(n => n.User)
            .Where(n => n.UserId == userId);

        if (unreadOnly.HasValue && unreadOnly.Value)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<Notification> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> query = _context.Notifications
            .Include(n => n.User);

        if (unreadOnly.HasValue && unreadOnly.Value)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<Notification> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    /// <summary>Returns the total number of unread notifications across all users.</summary>
    public async Task<int> GetTotalUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => !n.IsRead, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
        if (notification is not null)
        {
            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        notifications.ForEach(n => n.IsRead = true);
        _context.Notifications.UpdateRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }
}