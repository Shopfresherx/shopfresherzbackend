using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Product aggregate persistence and catalog queries.</summary>
public interface IProductRepository
{
    /// <summary>Retrieves a product by its unique identifier including images and variants.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by its URL slug including all navigation properties for PDP.</summary>
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by its SKU.</summary>
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a pessimistic lock on the product row for stock reservation.
    /// Uses SELECT WITH (UPDLOCK, ROWLOCK) to prevent overselling.
    /// </summary>
    Task<Product?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated list of active products with optional filters.</summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        Guid? brandId = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        decimal? ratingMin = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of products for admin management, independent of
    /// <see cref="GetPagedAsync"/>. Unlike the public listing, status is controllable:
    /// "active" -&gt; only active products, "inactive" -&gt; only inactive products,
    /// "all" or null -&gt; no IsActive filter at all. Kept as a fully separate query
    /// path so a future change to the public storefront listing cannot accidentally
    /// change what admins see, or vice versa.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedForAdminAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>Returns products added in the last 30 days.</summary>
    Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns best deals by discount percentage (CompareAtPrice → Price).
    /// Only includes products where CompareAtPrice > Price.
    /// </summary>
    Task<IReadOnlyList<Product>> GetBestDealsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Returns the top N products by sold count.</summary>
    Task<IReadOnlyList<Product>> GetBestSellersAsync(int limit, CancellationToken cancellationToken = default);


    /// <summary>Returns related products by category and tags.</summary>
    Task<IReadOnlyList<Product>> GetRelatedAsync(Guid productId, int limit, CancellationToken cancellationToken = default);

    /// <summary>Returns products whose stock quantity is at or below the supplied threshold.</summary>
    Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);

    /// <summary>Adds a new product to the persistence store.</summary>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing product entity as modified.</summary>
    void Update(Product product);

    /// <summary>Soft-deletes a product by setting DeletedAt.</summary>
    void Delete(Product product);

    /// <summary>Checks whether a slug is already in use.</summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
