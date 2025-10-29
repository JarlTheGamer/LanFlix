import logger from './logger';

interface RateLimitConfig {
  maxRequests: number;
  windowMs: number;
  keyPrefix?: string;
}

interface RequestRecord {
  timestamps: number[];
}

/**
 * Rate limiter for API requests
 * Implements sliding window algorithm for accurate rate limiting
 */
export class RateLimiter {
  private maxRequests: number;
  private windowMs: number;
  private keyPrefix: string;
  private requests: Map<string, RequestRecord>;
  private cleanupInterval: NodeJS.Timeout | null;

  constructor(config: RateLimitConfig) {
    this.maxRequests = config.maxRequests;
    this.windowMs = config.windowMs;
    this.keyPrefix = config.keyPrefix || 'ratelimit';
    this.requests = new Map();
    this.cleanupInterval = null;

    // Start cleanup interval to remove old entries (every 5 minutes)
    this.cleanupInterval = setInterval(() => {
      this.cleanup();
    }, 5 * 60 * 1000);
  }

  /**
   * Check if a request is allowed under the rate limit
   * @param key - Unique identifier for the rate limit (e.g., user ID, IP address, API endpoint)
   * @returns true if request is allowed, false if rate limit exceeded
   */
  async checkLimit(key: string): Promise<boolean> {
    const fullKey = `${this.keyPrefix}:${key}`;
    const now = Date.now();

    // Get or create request record
    let record = this.requests.get(fullKey);
    if (!record) {
      record = { timestamps: [] };
      this.requests.set(fullKey, record);
    }

    // Remove timestamps outside the current window
    record.timestamps = record.timestamps.filter(
      timestamp => now - timestamp < this.windowMs
    );

    // Check if limit is exceeded
    if (record.timestamps.length >= this.maxRequests) {
      const oldestTimestamp = record.timestamps[0];
      const resetTime = oldestTimestamp + this.windowMs;
      const waitTime = Math.ceil((resetTime - now) / 1000);
      
      logger.debug(
        `Rate limit exceeded for ${key}: ${record.timestamps.length}/${this.maxRequests} requests in ${this.windowMs}ms window. Reset in ${waitTime}s`
      );
      return false;
    }

    // Add current timestamp
    record.timestamps.push(now);
    return true;
  }

  /**
   * Check rate limit and throw error if exceeded
   * @param key - Unique identifier for the rate limit
   * @throws Error if rate limit is exceeded
   */
  async enforceLimit(key: string): Promise<void> {
    const allowed = await this.checkLimit(key);
    if (!allowed) {
      const record = this.requests.get(`${this.keyPrefix}:${key}`);
      const oldestTimestamp = record?.timestamps[0] || Date.now();
      const resetTime = oldestTimestamp + this.windowMs;
      const waitTime = Math.ceil((resetTime - Date.now()) / 1000);
      
      throw new Error(
        `Rate limit exceeded. Maximum ${this.maxRequests} requests per ${this.windowMs / 1000} seconds. Try again in ${waitTime} seconds.`
      );
    }
  }

  /**
   * Get remaining requests for a key
   */
  getRemainingRequests(key: string): number {
    const fullKey = `${this.keyPrefix}:${key}`;
    const now = Date.now();
    const record = this.requests.get(fullKey);

    if (!record) {
      return this.maxRequests;
    }

    // Count valid timestamps within window
    const validTimestamps = record.timestamps.filter(
      timestamp => now - timestamp < this.windowMs
    );

    return Math.max(0, this.maxRequests - validTimestamps.length);
  }

  /**
   * Get time until rate limit resets for a key (in milliseconds)
   */
  getResetTime(key: string): number {
    const fullKey = `${this.keyPrefix}:${key}`;
    const now = Date.now();
    const record = this.requests.get(fullKey);

    if (!record || record.timestamps.length === 0) {
      return 0;
    }

    const oldestTimestamp = record.timestamps[0];
    const resetTime = oldestTimestamp + this.windowMs;
    return Math.max(0, resetTime - now);
  }

  /**
   * Reset rate limit for a specific key
   */
  reset(key: string): void {
    const fullKey = `${this.keyPrefix}:${key}`;
    this.requests.delete(fullKey);
    logger.debug(`Rate limit reset for ${key}`);
  }

  /**
   * Reset all rate limits
   */
  resetAll(): void {
    this.requests.clear();
    logger.debug('All rate limits reset');
  }

  /**
   * Clean up old entries that are outside the window
   */
  private cleanup(): void {
    const now = Date.now();
    let cleanedCount = 0;

    for (const [key, record] of this.requests.entries()) {
      // Remove timestamps outside window
      const validTimestamps = record.timestamps.filter(
        timestamp => now - timestamp < this.windowMs
      );

      if (validTimestamps.length === 0) {
        // No valid timestamps, remove the entire record
        this.requests.delete(key);
        cleanedCount++;
      } else {
        // Update with valid timestamps only
        record.timestamps = validTimestamps;
      }
    }

    if (cleanedCount > 0) {
      logger.debug(`Cleaned up ${cleanedCount} expired rate limit entries`);
    }
  }

  /**
   * Get statistics about current rate limits
   */
  getStats(): {
    totalKeys: number;
    activeKeys: number;
  } {
    const now = Date.now();
    let activeKeys = 0;

    for (const record of this.requests.values()) {
      const validTimestamps = record.timestamps.filter(
        timestamp => now - timestamp < this.windowMs
      );
      if (validTimestamps.length > 0) {
        activeKeys++;
      }
    }

    return {
      totalKeys: this.requests.size,
      activeKeys
    };
  }

  /**
   * Shutdown the rate limiter
   */
  shutdown(): void {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }
    this.requests.clear();
  }
}

/**
 * Pre-configured rate limiters for different services
 */

// TMDB API: Max 40 requests per 10 seconds
export const tmdbRateLimiter = new RateLimiter({
  maxRequests: 40,
  windowMs: 10 * 1000,
  keyPrefix: 'tmdb'
});

// Sonarr API: Max 10 requests per second
export const sonarrRateLimiter = new RateLimiter({
  maxRequests: 10,
  windowMs: 1000,
  keyPrefix: 'sonarr'
});

// Radarr API: Max 10 requests per second
export const radarrRateLimiter = new RateLimiter({
  maxRequests: 10,
  windowMs: 1000,
  keyPrefix: 'radarr'
});

// Prowlarr API: Max 5 searches per minute per user
export const prowlarrRateLimiter = new RateLimiter({
  maxRequests: 5,
  windowMs: 60 * 1000,
  keyPrefix: 'prowlarr'
});

// General API rate limiter: Max 100 requests per minute per user
export const apiRateLimiter = new RateLimiter({
  maxRequests: 100,
  windowMs: 60 * 1000,
  keyPrefix: 'api'
});
