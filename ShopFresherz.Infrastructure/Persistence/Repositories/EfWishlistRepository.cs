using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IWishlistRepository"/>.</summary>
internal sealed class EfWishlistRepository : IWishlistRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfWishlistRepository"/>.</summary>
    public EfWishlistRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Wishlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .Include(w => w.Product)
                .ThenInclude(p => p.Images)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Wishlist?> GetAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        await _context.Wishlists.AddAsync(wishlist, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(Wishlist wishlist)
    {
        _context.Wishlists.Remove(wishlist);
    }
}
