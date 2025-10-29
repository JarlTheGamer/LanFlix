import { createClient, RedisClientType } from 'redis';
import { config } from '../config/env';
import logger from './logger';

interface CacheEntry<T> {
  data: T;
  expiresAt: number;
}

interface CacheOptions {
  ttl?: number; // Time to live in milliseconds
  useRedis?: boolean; // Whether to use Redis for this cache entry
}

/**
 * Multi-layer cache manager with memory and Redis cache layers
 * Implements cache-aside pattern with automatic fallback to memory cache
 */
export class CacheManager {
  private memoryCache: Map<string, CacheEntry<any>>;
  private redisClient: RedisClientType | null;
  private redisConnected: boolean;
  private cleanupInterval: NodeJS.Timeout | null;

  constructor() {
    this.memoryCache = new Map();
    this.redisClient = null;
    this.redisConnected = false;
    this.cleanupInterval = null;
  }

  /**
   * Initialize the cache manager and connect to Redis if configured
   */
  async initialize(): Promise<void> {
    // Initialize Redis if URL is provided
    if (config.redis.url) {
      try {
        this.redisClient = createClient({
          url: config.redis.url
        });

        this.redisClient.on('error', (err) => {
          logger.error('Redis client error:', err);
          this.redisConnected = false;
        });

        this.redisClient.on('connect', () => {
          logger.info('Redis client connected');
          this.redisConnected = true;
        });

        this.redisClient.on('disconnect', () => {
          logger.warn('Redis client disconnected');
          this.redisConnected = false;
        });

        await this.redisClient.connect();
        logger.info('Cache manager initialized with Redis support');
      } catch (error) {
        logger.warn('Failed to connect to Redis, falling back to memory cache only:', error);
        this.redisClient = null;
        this.redisConnected = false;
      }
    } else {
      logger.info('Cache manager initialized with memory cache only (Redis not configured)');
    }

    // Start cleanup interval for expired memory cache entries (every hour)
    this.cleanupInterval = setInterval(() => {
      this.cleanupMemoryCache();
    }, 60 * 60 * 1000);
  }

  /**
   * Get a value from cache or fetch it using the provided function
   */
  async get<T>(
    key: string,
    fetchFn: () => Promise<T>,
    options: CacheOptions = {}
  ): Promise<T> {
    const { ttl = 3600000, useRedis = true } = options; // Default TTL: 1 hour

    // Check memory cache first
    const memoryEntry = this.memoryCache.get(key);
    if (memoryEntry && memoryEntry.expiresAt > Date.now()) {
      logger.debug(`Cache hit (memory): ${key}`);
      return memoryEntry.data as T;
    }

    // Check Redis cache if available and enabled for this entry
    if (useRedis && this.redisConnected && this.redisClient) {
      try {
        const redisData = await this.redisClient.get(key);
        if (redisData) {
          const parsed: CacheEntry<T> = JSON.parse(redisData);
          if (parsed.expiresAt > Date.now()) {
            logger.debug(`Cache hit (Redis): ${key}`);
            // Promote to memory cache for faster access
            this.memoryCache.set(key, parsed);
            return parsed.data;
          } else {
            // Expired entry, delete it
            await this.redisClient.del(key);
          }
        }
      } catch (error) {
        logger.warn(`Redis cache read error for key ${key}:`, error);
      }
    }

    // Cache miss - fetch from source
    logger.debug(`Cache miss: ${key}`);
    const data = await fetchFn();

    // Store in cache
    await this.set(key, data, { ttl, useRedis });

    return data;
  }

  /**
   * Set a value in cache
   */
  async set<T>(
    key: string,
    data: T,
    options: CacheOptions = {}
  ): Promise<void> {
    const { ttl = 3600000, useRedis = true } = options;
    const expiresAt = Date.now() + ttl;

    const cacheEntry: CacheEntry<T> = {
      data,
      expiresAt
    };

    // Store in memory cache
    this.memoryCache.set(key, cacheEntry);

    // Store in Redis if available and enabled
    if (useRedis && this.redisConnected && this.redisClient) {
      try {
        const ttlSeconds = Math.ceil(ttl / 1000);
        await this.redisClient.setEx(
          key,
          ttlSeconds,
          JSON.stringify(cacheEntry)
        );
        logger.debug(`Cache set (memory + Redis): ${key}, TTL: ${ttlSeconds}s`);
      } catch (error) {
        logger.warn(`Redis cache write error for key ${key}:`, error);
        logger.debug(`Cache set (memory only): ${key}`);
      }
    } else {
      logger.debug(`Cache set (memory only): ${key}`);
    }
  }

  /**
   * Delete a specific key from cache
   */
  async delete(key: string): Promise<void> {
    this.memoryCache.delete(key);

    if (this.redisConnected && this.redisClient) {
      try {
        await this.redisClient.del(key);
        logger.debug(`Cache deleted: ${key}`);
      } catch (error) {
        logger.warn(`Redis cache delete error for key ${key}:`, error);
      }
    }
  }

  /**
   * Delete all keys matching a pattern
   */
  async deletePattern(pattern: string): Promise<void> {
    // Delete from memory cache
    const keysToDelete: string[] = [];
    for (const key of this.memoryCache.keys()) {
      if (this.matchPattern(key, pattern)) {
        keysToDelete.push(key);
      }
    }
    keysToDelete.forEach(key => this.memoryCache.delete(key));

    // Delete from Redis
    if (this.redisConnected && this.redisClient) {
      try {
        const keys = await this.redisClient.keys(pattern);
        if (keys.length > 0) {
          await this.redisClient.del(keys);
        }
        logger.debug(`Cache pattern deleted: ${pattern} (${keys.length} keys)`);
      } catch (error) {
        logger.warn(`Redis cache pattern delete error for pattern ${pattern}:`, error);
      }
    }
  }

  /**
   * Clear all cache entries
   */
  async clear(): Promise<void> {
    this.memoryCache.clear();

    if (this.redisConnected && this.redisClient) {
      try {
        await this.redisClient.flushDb();
        logger.info('Cache cleared (memory + Redis)');
      } catch (error) {
        logger.warn('Redis cache clear error:', error);
        logger.info('Cache cleared (memory only)');
      }
    } else {
      logger.info('Cache cleared (memory only)');
    }
  }

  /**
   * Warm cache with popular content
   */
  async warmCache(entries: Array<{ key: string; fetchFn: () => Promise<any>; ttl?: number }>): Promise<void> {
    logger.info(`Warming cache with ${entries.length} entries`);
    
    const promises = entries.map(async ({ key, fetchFn, ttl }) => {
      try {
        const data = await fetchFn();
        await this.set(key, data, { ttl });
      } catch (error) {
        logger.warn(`Failed to warm cache for key ${key}:`, error);
      }
    });

    await Promise.all(promises);
    logger.info('Cache warming completed');
  }

  /**
   * Generate a cache key from components
   */
  generateKey(...components: (string | number | boolean | undefined | null)[]): string {
    return components
      .filter(c => c !== undefined && c !== null)
      .map(c => String(c))
      .join(':');
  }

  /**
   * Clean up expired entries from memory cache
   */
  private cleanupMemoryCache(): void {
    const now = Date.now();
    let cleanedCount = 0;

    for (const [key, entry] of this.memoryCache.entries()) {
      if (entry.expiresAt <= now) {
        this.memoryCache.delete(key);
        cleanedCount++;
      }
    }

    if (cleanedCount > 0) {
      logger.debug(`Cleaned up ${cleanedCount} expired cache entries`);
    }
  }

  /**
   * Simple pattern matching for cache keys
   */
  private matchPattern(key: string, pattern: string): boolean {
    const regexPattern = pattern
      .replace(/\*/g, '.*')
      .replace(/\?/g, '.');
    const regex = new RegExp(`^${regexPattern}$`);
    return regex.test(key);
  }

  /**
   * Get cache statistics
   */
  getStats(): {
    memorySize: number;
    redisConnected: boolean;
  } {
    return {
      memorySize: this.memoryCache.size,
      redisConnected: this.redisConnected
    };
  }

  /**
   * Shutdown the cache manager
   */
  async shutdown(): Promise<void> {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }

    if (this.redisClient) {
      try {
        await this.redisClient.quit();
        logger.info('Redis client disconnected');
      } catch (error) {
        logger.warn('Error disconnecting Redis client:', error);
      }
    }
  }
}

// Export singleton instance
export const cacheManager = new CacheManager();
