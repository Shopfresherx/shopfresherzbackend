namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for Redis distributed cache operations.
/// TTLs per PRD Section 17: catalog 5 min, flash deals 30 sec, search results 2 min.
/// </summary>
public interface ICacheService
{
    /// <summary>Retrieves a cached value by key. Returns null if not found or expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Stores a value with the given TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Removes a cached entry by key.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes all cached entries whose keys match the given prefix pattern.</summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
