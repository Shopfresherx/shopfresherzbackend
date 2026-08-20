using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for FlashDeal persistence operations.</summary>
public interface IFlashDealRepository
{
    /// <summary>Retrieves a flash deal by its unique identifier.</summary>
    Task<FlashDeal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all currently live flash deals (active, not expired, started).</summary>
    Task<IReadOnlyList<FlashDeal>> GetLiveDealsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns active flash deals that are currently live.</summary>
    Task<IReadOnlyList<FlashDeal>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns active flash deals whose end time has passed.</summary>
    Task<IReadOnlyList<FlashDeal>> GetExpiredActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the active flash deal for a specific product, if any.</summary>
    Task<FlashDeal?> GetActiveDealForProductAsync(Guid productId, Guid? variantId, CancellationToken cancellationToken = default);

    /// <summary>Checks for overlapping active deals for the same product and variant.</summary>
    Task<bool> HasOverlapAsync(
        Guid productId,
        Guid? variantId,
        DateTime start,
        DateTime end,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new flash deal.</summary>
    Task AddAsync(FlashDeal deal, CancellationToken cancellationToken = default);

    /// <summary>Marks a flash deal as modified.</summary>
    void Update(FlashDeal deal);
}
