using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for back-in-stock notification requests.</summary>
public interface INotifyRequestRepository
{
    /// <summary>Returns unnotified requests whose products are currently available.</summary>
    Task<IReadOnlyList<NotifyRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new notification request.</summary>
    Task AddAsync(NotifyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a user already has a notification request for a product.</summary>
    Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}
