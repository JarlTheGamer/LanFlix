import axios, { AxiosInstance, AxiosError } from 'axios';
import { config } from '../config/env';
import logger from '../utils/logger';

interface TMDBMovie {
  id: number;
  title: string;
  original_title: string;
  overview: string;
  release_date: string;
  poster_path: string | null;
  backdrop_path: string | null;
  vote_average: number;
  vote_count: number;
  genre_ids: number[];
  popularity: number;
}

interface TMDBTVShow {
  id: number;
  name: string;
  original_name: string;
  overview: string;
  first_air_date: string;
  poster_path: string | null;
  backdrop_path: string | null;
  vote_average: number;
  vote_count: number;
  genre_ids: number[];
  popularity: number;
}

interface TMDBMovieDetails extends TMDBMovie {
  runtime: number;
  status: string;
  genres: Array<{ id: number; name: string }>;
  credits?: {
    cast: Array<{ name: string; character: string; order: number }>;
    crew: Array<{ name: string; job: string }>;
  };
}

interface TMDBTVDetails extends TMDBTVShow {
  number_of_seasons: number;
  number_of_episodes: number;
  status: string;
  genres: Array<{ id: number; name: string }>;
  seasons: Array<{
    season_number: number;
    episode_count: number;
    air_date: string;
  }>;
  credits?: {
    cast: Array<{ name: string; character: string; order: number }>;
    crew: Array<{ name: string; job: string }>;
  };
}

interface TMDBSearchResponse<T> {
  page: number;
  results: T[];
  total_pages: number;
  total_results: number;
}

interface RateLimitEntry {
  count: number;
  resetAt: number;
}

export class TMDBClient {
  private client: AxiosInstance;
  private baseURL = 'https://api.themoviedb.org/3';
  private apiKey: string;
  private rateLimiter: Map<string, RateLimitEntry>;
  private maxRequestsPer10Seconds = 40;
  private rateLimitWindow = 10000; // 10 seconds in milliseconds

  constructor(apiKey?: string) {
    this.apiKey = apiKey || config.externalServices.tmdb.apiKey;
    this.rateLimiter = new Map();

    if (!this.apiKey) {
      logger.warn('TMDB API key not configured');
    }

    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 10000,
      params: {
        api_key: this.apiKey
      }
    });

    // Request interceptor for rate limiting
    this.client.interceptors.request.use(async (config) => {
      await this.checkRateLimit();
      return config;
    });

    // Response interceptor for logging
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        logger.error('TMDB API error', {
          url: error.config?.url,
          status: error.response?.status,
          message: error.message
        });
        throw error;
      }
    );
  }

  /**
   * Check and enforce rate limiting (40 requests per 10 seconds)
   */
  private async checkRateLimit(): Promise<void> {
    const now = Date.now();
    const key = 'tmdb_requests';
    
    let entry = this.rateLimiter.get(key);

    // Clean up expired entries
    if (entry && now >= entry.resetAt) {
      this.rateLimiter.delete(key);
      entry = undefined;
    }

    if (!entry) {
      // Start new rate limit window
      this.rateLimiter.set(key, {
        count: 1,
        resetAt: now + this.rateLimitWindow
      });
      return;
    }

    if (entry.count >= this.maxRequestsPer10Seconds) {
      // Rate limit exceeded, wait until reset
      const waitTime = entry.resetAt - now;
      logger.warn(`TMDB rate limit reached, waiting ${waitTime}ms`);
      await new Promise(resolve => setTimeout(resolve, waitTime));
      
      // Reset counter
      this.rateLimiter.set(key, {
        count: 1,
        resetAt: Date.now() + this.rateLimitWindow
      });
    } else {
      // Increment counter
      entry.count++;
    }
  }

  /**
   * Retry logic with exponential backoff
   */
  private async retryRequest<T>(
    requestFn: () => Promise<T>,
    maxRetries = 0,
    baseDelay = 1000
  ): Promise<T> {
    try {
      return await requestFn();
    } catch (error) {
      // Don't retry - fail fast
      if (axios.isAxiosError(error)) {
        const status = error.response?.status;
        if (status === 401) {
          logger.error('TMDB API authentication failed - check your API key in .env');
        }
      }
      throw error;
    }
  }

  /**
   * Search for movies
   */
  async searchMovie(query: string, page = 1): Promise<TMDBSearchResponse<TMDBMovie>> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBSearchResponse<TMDBMovie>>('/search/movie', {
        params: { query, page }
      });
      return response.data;
    });
  }

  /**
   * Search for TV shows
   */
  async searchTV(query: string, page = 1): Promise<TMDBSearchResponse<TMDBTVShow>> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBSearchResponse<TMDBTVShow>>('/search/tv', {
        params: { query, page }
      });
      return response.data;
    });
  }

  /**
   * Get movie details
   */
  async getMovieDetails(id: number): Promise<TMDBMovieDetails> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBMovieDetails>(`/movie/${id}`, {
        params: { append_to_response: 'credits' }
      });
      return response.data;
    });
  }

  /**
   * Get TV show details
   */
  async getTVDetails(id: number): Promise<TMDBTVDetails> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBTVDetails>(`/tv/${id}`, {
        params: { append_to_response: 'credits' }
      });
      return response.data;
    });
  }

  /**
   * Get trending content
   */
  async getTrending(
    mediaType: 'movie' | 'tv' | 'all' = 'all',
    timeWindow: 'day' | 'week' = 'week'
  ): Promise<TMDBSearchResponse<TMDBMovie | TMDBTVShow>> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBSearchResponse<TMDBMovie | TMDBTVShow>>(
        `/trending/${mediaType}/${timeWindow}`
      );
      return response.data;
    });
  }

  /**
   * Get popular content
   */
  async getPopular(
    mediaType: 'movie' | 'tv',
    page = 1
  ): Promise<TMDBSearchResponse<TMDBMovie | TMDBTVShow>> {
    return this.retryRequest(async () => {
      const response = await this.client.get<TMDBSearchResponse<TMDBMovie | TMDBTVShow>>(
        `/${mediaType}/popular`,
        { params: { page } }
      );
      return response.data;
    });
  }

  /**
   * Test connection to TMDB
   */
  async testConnection(): Promise<boolean> {
    try {
      const response = await this.client.get('/configuration');
      return response.status === 200;
    } catch (error) {
      logger.error('TMDB connection test failed:', error);
      throw new Error('Failed to connect to TMDB API');
    }
  }
}
