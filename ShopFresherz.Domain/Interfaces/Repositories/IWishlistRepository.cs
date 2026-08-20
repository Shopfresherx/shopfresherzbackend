using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Wishlist persistence operations.</summary>
public interface IWishlistRepository
{
    /// <summary>Returns all wishlist items for a user with product data.</summary>
    Task<IReadOnlyList<Wishlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a product is already on the user's wishlist.</summary>
    Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a specific wishlist entry.</summary>
    Task<Wishlist?> GetAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Adds a product to the wishlist.</summary>
    Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default);

    /// <summary>Removes a wishlist entry.</summary>
    void Remove(Wishlist wishlist);
}
