using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
internal sealed class EfProductRepository : IProductRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfProductRepository"/>.</summary>
    public EfProductRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Raw SQL pessimistic lock — prevents overselling during concurrent checkouts.
        return await _context.Products
            .FromSqlRaw(
                "SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0} AND DeletedAt IS NULL",
                id)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        Guid? brandId = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        decimal? ratingMin = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (priceMin.HasValue)
        {
            query = query.Where(p => p.Price >= priceMin.Value);
        }

        if (priceMax.HasValue)
        {
            query = query.Where(p => p.Price <= priceMax.Value);
        }

        if (ratingMin.HasValue)
        {
            query = query.Where(p => p.AverageRating >= ratingMin.Value);
        }

        query = sortBy switch
        {
            "price_asc"    => query.OrderBy(p => p.Price),
            "price_desc"   => query.OrderByDescending(p => p.Price),
            "rating"       => query.OrderByDescending(p => p.AverageRating),
            "newest"       => query.OrderByDescending(p => p.CreatedAt),
            "bestsellers"  => query.OrderByDescending(p => p.SoldCount),
            _              => query.OrderByDescending(p => p.CreatedAt)
        };

        int total = await query.CountAsync(cancellationToken);
        List<Product> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Deliberately does not share query-building code with <see cref="GetPagedAsync"/> —
    /// the public storefront listing must always stay IsActive-only, and this admin
    /// listing must always be able to show any status; keeping them structurally
    /// independent means a change to one cannot accidentally alter the other.
    /// </remarks>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedForAdminAsync(
        int page,
        int pageSize,
        string? status = null,
        int? categoryId = null,
        Guid? brandId = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        decimal? ratingMin = null,
        string? sortBy = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        query = status switch
        {
            "active"   => query.Where(p => p.IsActive),
            "inactive" => query.Where(p => !p.IsActive),
            _          => query, // "all", null, or any other value: no IsActive filter.
        };

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (priceMin.HasValue)
        {
            query = query.Where(p => p.Price >= priceMin.Value);
        }

        if (priceMax.HasValue)
        {
            query = query.Where(p => p.Price <= priceMax.Value);
        }

        if (ratingMin.HasValue)
        {
            query = query.Where(p => p.AverageRating >= ratingMin.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));
        }

        query = sortBy switch
        {
            "price_asc"    => query.OrderBy(p => p.Price),
            "price_desc"   => query.OrderByDescending(p => p.Price),
            "rating"       => query.OrderByDescending(p => p.AverageRating),
            "newest"       => query.OrderByDescending(p => p.CreatedAt),
            "bestsellers"  => query.OrderByDescending(p => p.SoldCount),
            _              => query.OrderByDescending(p => p.CreatedAt)
        };

        int total = await query.CountAsync(cancellationToken);
        List<Product> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int limit, CancellationToken cancellationToken = default)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-30);

        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CreatedAt >= cutoff)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetBestDealsAsync(int limit, CancellationToken cancellationToken = default)
    {
        // "Best deals" = highest discount percentage based on CompareAtPrice -> Price.
        // Include only products where CompareAtPrice is meaningful.
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CompareAtPrice != null && p.CompareAtPrice > p.Price)
            .OrderByDescending(p => (p.CompareAtPrice!.Value - p.Price) / p.CompareAtPrice!.Value)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetBestSellersAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.SoldCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetRelatedAsync(Guid productId, int limit, CancellationToken cancellationToken = default)
    {
        Product? source = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (source == null)
        {
            return [];
        }

        return await _context.Products
            .Include(p => p.Images)
            .Where(p => p.Id != productId
                        && p.IsActive
                        && p.CategoryId == source.CategoryId)
            .OrderByDescending(p => p.AverageRating)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.StockQty <= threshold)
            .OrderBy(p => p.StockQty)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    /// <inheritdoc />
    public void Delete(Product product)
    {
        product.DeletedAt = DateTime.UtcNow;
        _context.Products.Update(product);
    }

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.Slug == slug, cancellationToken);
    }
}
