using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPromotionalSectionRepository"/>.</summary>
internal sealed class EfPromotionalSectionRepository : IPromotionalSectionRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfPromotionalSectionRepository"/>.</summary>
    public EfPromotionalSectionRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TDto>> GetBySectionKeyAsync<TDto>(
        string sectionKey,
        CancellationToken cancellationToken,
        Func<PromotionalSection, TDto> selector)
    {
        List<PromotionalSection> entities = await _context.PromotionalSections
            .AsNoTracking()
            .Where(p => p.SectionKey == sectionKey && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        return entities.Select(selector).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PromotionalSection>> GetRawBySectionKeyAsync(
        string sectionKey,
        CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .AsNoTracking()
            .Where(p => p.SectionKey == sectionKey && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionalSection>> GetAllBySectionKeyAsync(
        string sectionKey,
        CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .AsNoTracking()
            .Where(p => p.SectionKey == sectionKey)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionalSection?> GetByProductIdAsync(
        string sectionKey,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .FirstOrDefaultAsync(
                p => p.SectionKey == sectionKey && p.ProductId == productId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionalSection?> GetBySlugIdAsync(
        string slugId,
        CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .FirstOrDefaultAsync(p => p.SlugId == slugId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteBySectionKeyAsync(string sectionKey, CancellationToken cancellationToken)
    {
        // Bypass global query filter so we also catch previously soft-deleted rows.
        List<PromotionalSection> rows = await _context.PromotionalSections
            .IgnoreQueryFilters()
            .Where(p => p.SectionKey == sectionKey)
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (PromotionalSection row in rows)
        {
            row.DeletedAt = now;
            row.UpdatedAt = now;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionalSection>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(p => p.SectionKey)
            .ThenBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionalSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PromotionalSections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PromotionalSection section, CancellationToken cancellationToken)
    {
        await _context.PromotionalSections.AddAsync(section, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteBySlugIdAsync(string slugId, CancellationToken cancellationToken)
    {
        // Tracked query (no AsNoTracking) so changes persist on SaveChangesAsync.
        List<PromotionalSection> rows = await _context.PromotionalSections
            .IgnoreQueryFilters()
            .Where(p => p.SlugId == slugId)
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (PromotionalSection row in rows)
        {
            row.DeletedAt = now;
            row.UpdatedAt = now;
        }
    }
}
