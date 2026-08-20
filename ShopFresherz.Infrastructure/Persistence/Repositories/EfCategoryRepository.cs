using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICategoryRepository"/>.</summary>
internal sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfCategoryRepository"/>.</summary>
    public EfCategoryRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        // Load all categories; EF Core will wire up the navigation properties automatically.
        return await _context.Categories
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetTopLevelAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        string normalised = slug.ToLowerInvariant();
        return await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Slug == normalised && (!excludeId.HasValue || c.Id != excludeId.Value),
                cancellationToken);
    }

    /// <inheritdoc />
    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }
}
