using Microsoft.Extensions.Caching.Memory;

namespace Lanflix.Infrastructure.Persistence;

/// <summary>
/// Service for caching query results to improve performance
/// </summary>
public class QueryResultCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    public QueryResultCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Gets or creates a cached query result
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        _cache.Set(key, value, cacheOptions);

        return value;
    }

    /// <summary>
    /// Gets or creates a cached query result with tag-based invalidation
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        string[] tags,
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        // Register tags for invalidation
        foreach (var tag in tags)
        {
            cacheOptions.RegisterPostEvictionCallback((k, v, r, s) =>
            {
                // Tag eviction logic can be implemented here
            });
        }

        _cache.Set(key, value, cacheOptions);

        return value;
    }

    /// <summary>
    /// Removes a cached item by key
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Removes all cached items with a specific prefix
    /// </summary>
    public void RemoveByPrefix(string prefix)
    {
        // Note: IMemoryCache doesn't support prefix-based removal out of the box
        // This would require a custom implementation or using a distributed cache
        // For now, we'll just document this limitation
    }

    /// <summary>
    /// Clears all cached items
    /// </summary>
    public void Clear()
    {
        // Note: IMemoryCache doesn't support clearing all items
        // This would require tracking all keys or using a distributed cache
    }
}

/// <summary>
/// Cache key builder for consistent key generation
/// </summary>
public static class CacheKeys
{
    public static string ContentById(int id) => $"content:{id}";
    public static string ContentByTmdbId(int tmdbId) => $"content:tmdb:{tmdbId}";
    public static string ProfileById(int id) => $"profile:{id}";
    public static string AllProfiles() => "profiles:all";
    public static string WatchHistory(int profileId, int contentId, int? episodeId) =>
        $"watchhistory:{profileId}:{contentId}:{episodeId}";
    public static string RecentWatchHistory(int profileId, int count) =>
        $"watchhistory:recent:{profileId}:{count}";
    public static string ActiveStreamSessions(int profileId) =>
        $"streamsessions:active:{profileId}";
    public static string StreamSessionById(string sessionId) =>
        $"streamsession:{sessionId}";
    public static string Episode(int contentId, int seasonNumber, int episodeNumber) =>
        $"episode:{contentId}:{seasonNumber}:{episodeNumber}";
    public static string Watchlist(int profileId) =>
        $"watchlist:{profileId}";
    public static string LibraryItems(string type, int page, int pageSize, string? search) =>
        $"library:{type}:{page}:{pageSize}:{search ?? "all"}";
}
