using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IHomepageBannerRepository"/>.</summary>
internal sealed class EfHomepageBannerRepository : IHomepageBannerRepository
{
    private readonly ShopFresherzDbContext _context;

    public EfHomepageBannerRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomepageBanner>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = DateTime.UtcNow;

        return await _context.HomepageBanners
            .AsNoTracking()
            .Where(b => b.IsActive
                && (b.StartsAt == null || b.StartsAt <= utcNow)
                && (b.EndsAt == null || b.EndsAt > utcNow))
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HomepageBanner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.HomepageBanners.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(HomepageBanner banner, CancellationToken cancellationToken = default)
    {
        await _context.HomepageBanners.AddAsync(banner, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(HomepageBanner banner)
    {
        _context.HomepageBanners.Update(banner);
    }
}
