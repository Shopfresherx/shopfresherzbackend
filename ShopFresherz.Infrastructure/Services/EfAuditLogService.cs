using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Persistence;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>EF Core audit log writer using an independent DbContext instance.</summary>
public sealed class EfAuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ShopFresherzDbContext> _contextFactory;
    private readonly ILogger<EfAuditLogService> _logger;

    public EfAuditLogService(
        IDbContextFactory<ShopFresherzDbContext> contextFactory,
        ILogger<EfAuditLogService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using ShopFresherzDbContext context =
                await _contextFactory.CreateDbContextAsync(cancellationToken);

            await context.AuditLogs.AddAsync(new AuditLog
            {
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log for {Action} on {EntityType}.", action, entityType);
        }
    }
}
