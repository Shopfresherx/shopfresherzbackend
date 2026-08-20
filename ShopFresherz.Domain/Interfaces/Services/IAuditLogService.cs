namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Writes audit log events for sensitive actions.</summary>
public interface IAuditLogService
{
    Task LogAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
