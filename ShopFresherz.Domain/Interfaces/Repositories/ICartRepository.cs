using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Cart aggregate persistence operations.</summary>
public interface ICartRepository
{
    /// <summary>Retrieves a cart by its unique identifier including items.</summary>
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the active cart for an authenticated user.</summary>
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a guest cart by browser session ID.</summary>
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new cart to the store.</summary>
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary>Marks a cart as modified.</summary>
    void Update(Cart cart);

    /// <summary>Removes expired guest carts (called by a background job).</summary>
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
