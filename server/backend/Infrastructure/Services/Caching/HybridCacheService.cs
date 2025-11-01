using System.Diagnostics;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Caching;

/// <summary>
/// Two-tier hybrid cache implementation combining Memory (L1) and Redis (L2) caches
/// Implements cache-aside pattern with tag-based invalidation
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly MemoryCacheService _l1Cache;
    private readonly RedisCacheService _l2Cache;
    private readonly CachingMetrics? _metrics;
    private readonly ILogger<HybridCacheService> _logger;

    public HybridCacheService(
        IMemoryCache memoryCache,
        RedisCacheService redisCacheService,
        ILogger<HybridCacheService> logger,
        CachingMetrics? metrics = null)
    {
        _l1Cache = new MemoryCacheService(
            memoryCache,
            logger as ILogger<MemoryCacheService> ?? 
                throw new ArgumentNullException(nameof(logger)));
        _l2Cache = redisCacheService;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value from cache using cache-aside pattern
    /// Tries L1 (memory) first, then L2 (Redis), and populates L1 on L2 hit
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Try L1 cache first (memory)
        var l1Value = await _l1Cache.GetAsync<T>(key, cancellationToken);
        if (l1Value != null)
        {
            stopwatch.Stop();
            _metrics?.RecordCacheHit("L1");
            _metrics?.RecordOperationDuration(stopwatch.Elapsed.TotalMilliseconds, "get", "L1");
            _logger.LogTrace("L1 cache hit for key: {Key}", key);
            return l1Value;
        }

        // Try L2 cache (Redis)
        var l2Value = await _l2Cache.GetAsync<T>(key, cancellationToken);
        if (l2Value != null)
        {
            stopwatch.Stop();
            _metrics?.RecordCacheHit("L2");
            _metrics?.RecordOperationDuration(stopwatch.Elapsed.TotalMilliseconds, "get", "L2");
            _logger.LogTrace("L2 cache hit for key: {Key}, populating L1", key);
            
            // Populate L1 cache with shorter expiration (5 minutes)
            await _l1Cache.SetAsync(key, l2Value, TimeSpan.FromMinutes(5), cancellationToken);
            
            return l2Value;
        }

        stopwatch.Stop();
        _metrics?.RecordCacheMiss("hybrid");
        _metrics?.RecordOperationDuration(stopwatch.Elapsed.TotalMilliseconds, "get", "hybrid");
        _logger.LogTrace("Cache miss (L1 and L2) for key: {Key}", key);
        return default;
    }

    /// <summary>
    /// Sets a value in both L1 and L2 caches
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Set in both caches
        var l1Task = _l1Cache.SetAsync(key, value, expiration, cancellationToken);
        var l2Task = _l2Cache.SetAsync(key, value, expiration, cancellationToken);

        await Task.WhenAll(l1Task, l2Task);
        
        stopwatch.Stop();
        _metrics?.RecordOperationDuration(stopwatch.Elapsed.TotalMilliseconds, "set", "hybrid");
        _logger.LogTrace("Set hybrid cache for key: {Key} with expiration: {Expiration}", key, expiration);
    }

    /// <summary>
    /// Removes a value from both L1 and L2 caches
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var l1Task = _l1Cache.RemoveAsync(key, cancellationToken);
        var l2Task = _l2Cache.RemoveAsync(key, cancellationToken);

        await Task.WhenAll(l1Task, l2Task);
        
        _logger.LogTrace("Removed hybrid cache for key: {Key}", key);
    }

    /// <summary>
    /// Removes all cache entries associated with a tag from both L1 and L2 caches
    /// </summary>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var l1Task = _l1Cache.RemoveByTagAsync(tag, cancellationToken);
        var l2Task = _l2Cache.RemoveByTagAsync(tag, cancellationToken);

        await Task.WhenAll(l1Task, l2Task);
        
        _logger.LogDebug("Removed hybrid cache entries for tag: {Tag}", tag);
    }

    /// <summary>
    /// Sets a cache entry with associated tags for tag-based invalidation in both caches
    /// </summary>
    public async Task SetWithTagsAsync<T>(string key, T value, string[] tags, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var l1Task = _l1Cache.SetWithTagsAsync(key, value, tags, expiration, cancellationToken);
        var l2Task = _l2Cache.SetWithTagsAsync(key, value, tags, expiration, cancellationToken);

        await Task.WhenAll(l1Task, l2Task);
        
        _logger.LogTrace("Set hybrid cache with tags for key: {Key}, tags: {Tags}", key, string.Join(", ", tags));
    }

    /// <summary>
    /// Gets a value from cache or executes the factory function and caches the result
    /// Implements the cache-aside pattern
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Execute factory to get the value
        var value = await factory(cancellationToken);

        // Cache the value
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    /// <summary>
    /// Gets a value from cache or executes the factory function and caches the result with tags
    /// </summary>
    public async Task<T> GetOrSetWithTagsAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        string[] tags,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Execute factory to get the value
        var value = await factory(cancellationToken);

        // Cache the value with tags
        if (value != null)
        {
            await SetWithTagsAsync(key, value, tags, expiration, cancellationToken);
        }

        return value;
    }
}
