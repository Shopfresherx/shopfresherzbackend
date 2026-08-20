using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for SavedCard (bank card) persistence operations.</summary>
public interface IPaymentMethodRepository
{
    /// <summary>Retrieves a saved card by its unique identifier.</summary>
    Task<SavedCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all non-deleted saved cards for the given user.</summary>
    Task<IReadOnlyList<SavedCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new saved card.</summary>
    Task AddAsync(SavedCard card, CancellationToken cancellationToken = default);

    /// <summary>Marks a saved card as modified.</summary>
    void Update(SavedCard card);

    /// <summary>Soft-deletes a saved card.</summary>
    void Delete(SavedCard card);
}
