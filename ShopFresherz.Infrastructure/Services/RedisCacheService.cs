using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Redis-backed distributed cache implementation using StackExchange.Redis.
/// Keys are JSON-serialised with System.Text.Json. Cache misses return null.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase          _db;
    private readonly IServer            _server;
    private readonly ILogger<RedisCacheService> _logger;

    /// <summary>Initialises a new instance of <see cref="RedisCacheService"/>.</summary>
    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db     = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints()[0]);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            RedisValue value = await _db.StringGetAsync(key);
            if (!value.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {Key}. Returning null.", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            string serialised = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, serialised, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {Key}. Continuing without cache.", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}.", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            RedisKey[] keys = _server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE BY PATTERN failed for pattern {Pattern}.", pattern);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS check failed for key {Key}. Returning false.", key);
            return false;
        }
    }
}
