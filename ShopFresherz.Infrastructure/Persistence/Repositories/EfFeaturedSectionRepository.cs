using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IFeaturedSectionRepository"/>.</summary>
internal sealed class EfFeaturedSectionRepository : IFeaturedSectionRepository
{
    private readonly ShopFresherzDbContext _context;

    public EfFeaturedSectionRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<FeaturedSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FeaturedSections
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeaturedSection>> GetActiveBySectionAsync(
        string section,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeaturedSections
            .AsNoTracking()
            .Where(f => f.IsActive && f.Section == section)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeaturedSection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeaturedSections
            .AsNoTracking()
            .OrderBy(f => f.Section)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(FeaturedSection card, CancellationToken cancellationToken = default)
    {
        await _context.FeaturedSections.AddAsync(card, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(FeaturedSection card)
    {
        _context.FeaturedSections.Update(card);
    }

    /// <inheritdoc />
    public void Delete(FeaturedSection card)
    {
        card.DeletedAt = DateTime.UtcNow;
        _context.FeaturedSections.Update(card);
    }
}
