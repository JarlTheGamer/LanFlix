using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Lanflix.Infrastructure.Services.Caching;

/// <summary>
/// L1 cache implementation using in-memory caching for fast access
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagIndex;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _tagIndex = new ConcurrentDictionary<string, HashSet<string>>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogTrace("Memory cache hit for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogTrace("Memory cache miss for key: {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            // Default expiration of 5 minutes for memory cache
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        }

        // Add eviction callback to clean up tag index
        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            RemoveFromTagIndex(k.ToString()!);
        });

        _memoryCache.Set(key, value, options);
        _logger.LogTrace("Set memory cache for key: {Key} with expiration: {Expiration}", key, expiration);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        RemoveFromTagIndex(key);
        _logger.LogTrace("Removed memory cache for key: {Key}", key);

        return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_tagIndex.TryGetValue(tag, out var keys))
        {
            foreach (var key in keys.ToList())
            {
                _memoryCache.Remove(key);
            }

            _tagIndex.TryRemove(tag, out _);
            _logger.LogDebug("Removed {Count} memory cache entries for tag: {Tag}", keys.Count, tag);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets a cache entry with associated tags for tag-based invalidation
    /// </summary>
    public Task SetWithTagsAsync<T>(string key, T value, string[] tags, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Add key to tag index
        foreach (var tag in tags)
        {
            _tagIndex.AddOrUpdate(
                tag,
                _ => new HashSet<string> { key },
                (_, existing) =>
                {
                    existing.Add(key);
                    return existing;
                });
        }

        return SetAsync(key, value, expiration, cancellationToken);
    }

    private void RemoveFromTagIndex(string key)
    {
        foreach (var tagEntry in _tagIndex)
        {
            tagEntry.Value.Remove(key);
            if (tagEntry.Value.Count == 0)
            {
                _tagIndex.TryRemove(tagEntry.Key, out _);
            }
        }
    }
}
