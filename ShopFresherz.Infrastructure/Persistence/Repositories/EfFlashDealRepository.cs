using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IFlashDealRepository"/>.</summary>
internal sealed class EfFlashDealRepository : IFlashDealRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfFlashDealRepository"/>.</summary>
    public EfFlashDealRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<FlashDeal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FlashDeals
            .Include(f => f.Product)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FlashDeal>> GetLiveDealsAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = DateTime.UtcNow;

        return await _context.FlashDeals
            .Include(f => f.Product)
                .ThenInclude(p => p.Images)
            .Where(f => f.IsActive && f.StartsAt <= utcNow && f.EndsAt > utcNow)
            .OrderBy(f => f.EndsAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FlashDeal>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return GetLiveDealsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FlashDeal>> GetExpiredActiveAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = DateTime.UtcNow;

        return await _context.FlashDeals
            .Where(d => d.IsActive && d.EndsAt <= utcNow)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FlashDeal?> GetActiveDealForProductAsync(Guid productId, Guid? variantId, CancellationToken cancellationToken = default)
    {
        DateTime utcNow = DateTime.UtcNow;

        return await _context.FlashDeals
            .Where(f => f.ProductId == productId
                        && f.VariantId == variantId
                        && f.IsActive
                        && f.StartsAt <= utcNow
                        && f.EndsAt > utcNow)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasOverlapAsync(
        Guid productId,
        Guid? variantId,
        DateTime start,
        DateTime end,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FlashDeals
            .AsNoTracking()
            .AnyAsync(f =>
                f.ProductId == productId &&
                f.VariantId == variantId &&
                f.IsActive &&
                (!excludeId.HasValue || f.Id != excludeId.Value) &&
                f.StartsAt < end &&
                f.EndsAt > start,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(FlashDeal deal, CancellationToken cancellationToken = default)
    {
        await _context.FlashDeals.AddAsync(deal, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(FlashDeal deal)
    {
        _context.FlashDeals.Update(deal);
    }
}
