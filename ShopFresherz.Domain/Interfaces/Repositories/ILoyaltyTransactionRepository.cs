using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for loyalty points transaction history.</summary>
public interface ILoyaltyTransactionRepository
{
    /// <summary>Adds a new loyalty transaction.</summary>
    Task AddAsync(LoyaltyTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Returns paginated loyalty transactions for a user.</summary>
    Task<(IReadOnlyList<LoyaltyTransaction> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
