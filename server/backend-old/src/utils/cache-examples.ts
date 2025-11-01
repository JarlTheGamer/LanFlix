/**
 * Examples of how to use the CacheManager and RateLimiter
 * This file is for documentation purposes and demonstrates usage patterns
 */

import { cacheManager } from './cache-manager';
import {
  tmdbRateLimiter,
  sonarrRateLimiter,
  radarrRateLimiter,
  prowlarrRateLimiter,
  apiRateLimiter
} from './rate-limiter';

// ============================================================================
// CacheManager Examples
// ============================================================================

/**
 * Example 1: Basic cache usage with automatic fetch
 */
async function exampleBasicCache() {
  const movieId = 19995;
  
  // Cache will automatically fetch if not present
  const movieData = await cacheManager.get(
    `movie:${movieId}`,
    async () => {
      // This function only runs on cache miss
      const response = await fetch(`https://api.example.com/movie/${movieId}`);
      return response.json();
    },
    {
      ttl: 7 * 24 * 60 * 60 * 1000, // 7 days
      useRedis: true
    }
  );
  
  return movieData;
}

/**
 * Example 2: Cache key generation
 */
async function exampleCacheKeyGeneration() {
  const userId = 123;
  const contentType = 'movie';
  const page = 1;
  
  // Generate consistent cache keys
  const cacheKey = cacheManager.generateKey('library', userId, contentType, page);
  // Result: "library:123:movie:1"
  
  const data = await cacheManager.get(
    cacheKey,
    async () => {
      // Fetch library data
      return { items: [], total: 0 };
    }
  );
  
  return data;
}

/**
 * Example 3: Manual cache set and get
 */
async function exampleManualCache() {
  const key = 'trending:movies';
  const data = { movies: [/* ... */] };
  
  // Set cache manually
  await cacheManager.set(key, data, {
    ttl: 6 * 60 * 60 * 1000, // 6 hours
    useRedis: true
  });
  
  // Later, get from cache
  const cachedData = await cacheManager.get(
    key,
    async () => {
      // This won't be called if cache hit
      return { movies: [] };
    }
  );
  
  return cachedData;
}

/**
 * Example 4: Cache invalidation
 */
async function exampleCacheInvalidation() {
  const userId = 123;
  
  // Delete specific key
  await cacheManager.delete(`watchlist:${userId}`);
  
  // Delete all keys matching pattern
  await cacheManager.deletePattern(`user:${userId}:*`);
  
  // Clear all cache
  await cacheManager.clear();
}

/**
 * Example 5: Cache warming for popular content
 */
async function exampleCacheWarming() {
  await cacheManager.warmCache([
    {
      key: 'trending:movies',
      fetchFn: async () => {
        // Fetch trending movies
        return { movies: [] };
      },
      ttl: 6 * 60 * 60 * 1000 // 6 hours
    },
    {
      key: 'trending:series',
      fetchFn: async () => {
        // Fetch trending series
        return { series: [] };
      },
      ttl: 6 * 60 * 60 * 1000
    },
    {
      key: 'popular:movies',
      fetchFn: async () => {
        // Fetch popular movies
        return { movies: [] };
      },
      ttl: 6 * 60 * 60 * 1000
    }
  ]);
}

/**
 * Example 6: Cache statistics
 */
async function exampleCacheStats() {
  const stats = cacheManager.getStats();
  console.log(`Memory cache size: ${stats.memorySize}`);
  console.log(`Redis connected: ${stats.redisConnected}`);
}

// ============================================================================
// RateLimiter Examples
// ============================================================================

/**
 * Example 7: TMDB API rate limiting
 */
async function exampleTmdbRateLimit() {
  const endpoint = 'search/movie';
  
  try {
    // Check if request is allowed (max 40 requests per 10 seconds)
    await tmdbRateLimiter.enforceLimit(endpoint);
    
    // Make API request
    const response = await fetch('https://api.themoviedb.org/3/search/movie');
    return response.json();
  } catch (error) {
    // Rate limit exceeded
    console.error('TMDB rate limit exceeded:', error);
    throw error;
  }
}

/**
 * Example 8: User-specific rate limiting
 */
async function exampleUserRateLimit() {
  const userId = 123;
  
  // Check if user can make API request (max 100 per minute)
  const allowed = await apiRateLimiter.checkLimit(`user:${userId}`);
  
  if (!allowed) {
    const resetTime = apiRateLimiter.getResetTime(`user:${userId}`);
    const remaining = apiRateLimiter.getRemainingRequests(`user:${userId}`);
    
    throw new Error(
      `Rate limit exceeded. ${remaining} requests remaining. Reset in ${Math.ceil(resetTime / 1000)}s`
    );
  }
  
  // Process request
  return { success: true };
}

/**
 * Example 9: Sonarr/Radarr rate limiting
 */
async function exampleSonarrRateLimit() {
  const operation = 'add-series';
  
  try {
    // Max 10 requests per second
    await sonarrRateLimiter.enforceLimit(operation);
    
    // Make Sonarr API request
    const response = await fetch('http://localhost:8989/api/v3/series');
    return response.json();
  } catch (error) {
    console.error('Sonarr rate limit exceeded:', error);
    throw error;
  }
}

/**
 * Example 10: Prowlarr search rate limiting (per user)
 */
async function exampleProwlarrRateLimit() {
  const userId = 123;
  const query = 'avatar';
  
  try {
    // Max 5 searches per minute per user
    await prowlarrRateLimiter.enforceLimit(`user:${userId}`);
    
    // Make Prowlarr search request
    const response = await fetch(`http://localhost:9696/api/v1/search?query=${query}`);
    return response.json();
  } catch (error) {
    console.error('Prowlarr rate limit exceeded:', error);
    
    const resetTime = prowlarrRateLimiter.getResetTime(`user:${userId}`);
    throw new Error(`Too many searches. Try again in ${Math.ceil(resetTime / 1000)} seconds.`);
  }
}

/**
 * Example 11: Rate limiter statistics
 */
async function exampleRateLimiterStats() {
  const stats = tmdbRateLimiter.getStats();
  console.log(`Total keys: ${stats.totalKeys}`);
  console.log(`Active keys: ${stats.activeKeys}`);
}

/**
 * Example 12: Combined cache and rate limiting
 */
async function exampleCombinedUsage() {
  const movieId = 19995;
  
  // Check rate limit first
  await tmdbRateLimiter.enforceLimit('movie-details');
  
  // Then use cache
  const movieData = await cacheManager.get(
    `movie:${movieId}`,
    async () => {
      // Only called on cache miss (and after rate limit check)
      const response = await fetch(`https://api.themoviedb.org/3/movie/${movieId}`);
      return response.json();
    },
    {
      ttl: 7 * 24 * 60 * 60 * 1000, // 7 days
      useRedis: true
    }
  );
  
  return movieData;
}

// ============================================================================
// Cache TTL Recommendations (from design document)
// ============================================================================

const CACHE_TTL = {
  // TMDB API
  TMDB_SEARCH_RESULTS: 6 * 60 * 60 * 1000,      // 6 hours
  TMDB_CONTENT_DETAILS: 7 * 24 * 60 * 60 * 1000, // 7 days
  TMDB_IMAGES: Infinity,                          // Never expire
  
  // Sonarr/Radarr
  SERIES_MOVIE_LIST: 5 * 60 * 1000,              // 5 minutes
  QUEUE_STATUS: 30 * 1000,                        // 30 seconds
  
  // Prowlarr
  SEARCH_RESULTS: 60 * 60 * 1000,                 // 1 hour
  INDEXER_LIST: 24 * 60 * 60 * 1000,             // 24 hours
  
  // Library
  LIBRARY_ITEMS: 5 * 60 * 1000,                   // 5 minutes
  RECENTLY_ADDED: 2 * 60 * 1000,                  // 2 minutes
  
  // User data
  WATCHLIST: 1 * 60 * 1000,                       // 1 minute
  WATCH_HISTORY: 1 * 60 * 1000,                   // 1 minute
};

export {
  exampleBasicCache,
  exampleCacheKeyGeneration,
  exampleManualCache,
  exampleCacheInvalidation,
  exampleCacheWarming,
  exampleCacheStats,
  exampleTmdbRateLimit,
  exampleUserRateLimit,
  exampleSonarrRateLimit,
  exampleProwlarrRateLimit,
  exampleRateLimiterStats,
  exampleCombinedUsage,
  CACHE_TTL
};
