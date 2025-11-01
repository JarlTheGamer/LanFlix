using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Lanflix.Infrastructure.Services.Caching;

/// <summary>
/// L2 cache implementation using Redis for distributed caching
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogTrace("Redis cache miss for key: {Key}", key);
                return default;
            }

            var value = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
            _logger.LogTrace("Redis cache hit for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Redis cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 1 hour for Redis cache
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);
            _logger.LogTrace("Set Redis cache for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Redis cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogTrace("Removed Redis cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from Redis cache for key: {Key}", key);
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var tagKey = $"tag:{tag}";

            // Get all keys associated with this tag
            var keys = await db.SetMembersAsync(tagKey);

            if (keys.Length > 0)
            {
                // Remove all keys
                var tasks = keys.Select(k => db.KeyDeleteAsync(k.ToString())).ToList();
                tasks.Add(db.KeyDeleteAsync(tagKey)); // Also remove the tag set itself

                await Task.WhenAll(tasks);

                _logger.LogDebug("Removed {Count} Redis cache entries for tag: {Tag}", keys.Length, tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing values by tag from Redis cache for tag: {Tag}", tag);
        }
    }

    /// <summary>
    /// Sets a cache entry with associated tags for tag-based invalidation
    /// </summary>
    public async Task SetWithTagsAsync<T>(string key, T value, string[] tags, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();

            // Set the value
            await SetAsync(key, value, expiration, cancellationToken);

            // Add key to tag sets
            var tagTasks = tags.Select(tag => db.SetAddAsync($"tag:{tag}", key));
            await Task.WhenAll(tagTasks);

            // Set expiration on tag sets (slightly longer than the cached value)
            var tagExpiration = expiration?.Add(TimeSpan.FromMinutes(5)) ?? TimeSpan.FromHours(2);
            var expirationTasks = tags.Select(tag => db.KeyExpireAsync($"tag:{tag}", tagExpiration));
            await Task.WhenAll(expirationTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value with tags in Redis cache for key: {Key}", key);
        }
    }
}
