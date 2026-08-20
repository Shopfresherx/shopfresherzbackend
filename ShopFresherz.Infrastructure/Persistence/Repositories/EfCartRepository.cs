using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICartRepository"/>.</summary>
internal sealed class EfCartRepository : ICartRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfCartRepository"/>.</summary>
    public EfCartRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Cart cart)
    {
        _context.Carts.Update(cart);
    }

    /// <inheritdoc />
    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Bulk soft-delete expired guest carts via ExecuteUpdateAsync for efficiency.
        DateTime utcNow = DateTime.UtcNow;
        await _context.Carts
            .Where(c => c.UserId == null && c.ExpiresAt <= utcNow && c.DeletedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.DeletedAt, utcNow),
                cancellationToken);
    }
}
