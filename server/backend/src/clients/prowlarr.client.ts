import axios, { AxiosInstance, AxiosError } from 'axios';
import { config } from '../config/env';
import logger from '../utils/logger';

interface ProwlarrIndexer {
  id: number;
  name: string;
  enable: boolean;
  protocol: string;
  priority: number;
  downloadClientId: number;
  tags: number[];
}

interface ProwlarrSearchResult {
  guid: string;
  indexerId: number;
  indexer: string;
  title: string;
  sortTitle: string;
  size: number;
  publishDate: string;
  downloadUrl: string;
  infoUrl: string;
  categories: Array<{
    id: number;
    name: string;
  }>;
  protocol: string;
  seeders?: number;
  leechers?: number;
  age: number;
  ageHours: number;
  ageMinutes: number;
  imdbId?: string;
  tmdbId?: number;
  tvdbId?: number;
}

interface NormalizedSearchResult {
  title: string;
  size: number;
  publishDate: string;
  indexer: string;
  protocol: string;
  seeders?: number;
  leechers?: number;
  downloadUrl: string;
  infoUrl: string;
  imdbId?: string;
  tmdbId?: number;
  tvdbId?: number;
  categories: string[];
  quality?: string;
}

export class ProwlarrClient {
  private client: AxiosInstance;
  private baseURL: string;
  private apiKey: string;

  constructor(baseURL?: string, apiKey?: string) {
    this.baseURL = baseURL || config.externalServices.prowlarr.url;
    this.apiKey = apiKey || config.externalServices.prowlarr.apiKey;

    if (!this.apiKey) {
      logger.warn('Prowlarr API key not configured');
    }

    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        'X-Api-Key': this.apiKey,
        'Content-Type': 'application/json'
      }
    });

    // Response interceptor for logging
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        logger.error('Prowlarr API error', {
          url: error.config?.url,
          status: error.response?.status,
          message: error.message,
          data: error.response?.data
        });
        throw error;
      }
    );
  }

  /**
   * Update configuration dynamically
   */
  updateConfig(baseURL?: string, apiKey?: string): void {
    if (baseURL) this.baseURL = baseURL;
    if (apiKey) this.apiKey = apiKey;
    
    // Reinitialize the client with new config
    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        'X-Api-Key': this.apiKey,
        'Content-Type': 'application/json'
      }
    });

    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        logger.error('Prowlarr API error', {
          url: error.config?.url,
          status: error.response?.status,
          message: error.message,
          data: error.response?.data
        });
        throw error;
      }
    );
    
    logger.info('Prowlarr configuration updated');
  }

  /**
   * Test connection to Prowlarr
   */
  async testConnection(): Promise<boolean> {
    try {
      const response = await this.client.get('/api/v1/system/status');
      logger.info('Prowlarr connection successful', { version: response.data.version });
      return true;
    } catch (error) {
      logger.error('Prowlarr connection failed', { error });
      return false;
    }
  }

  /**
   * Get health check status
   */
  async getHealth(): Promise<any> {
    try {
      const response = await this.client.get('/api/v1/health');
      return response.data;
    } catch (error) {
      logger.error('Failed to get Prowlarr health status', { error });
      throw error;
    }
  }

  /**
   * Get all indexers
   */
  async getIndexers(): Promise<ProwlarrIndexer[]> {
    try {
      const response = await this.client.get<ProwlarrIndexer[]>('/api/v1/indexer');
      return response.data;
    } catch (error) {
      logger.error('Failed to get indexers from Prowlarr', { error });
      throw error;
    }
  }

  /**
   * Test an indexer
   */
  async testIndexer(id: number): Promise<boolean> {
    try {
      await this.client.post(`/api/v1/indexer/test/${id}`);
      logger.info('Indexer test successful', { id });
      return true;
    } catch (error) {
      logger.error('Indexer test failed', { id, error });
      return false;
    }
  }

  /**
   * Search across all indexers
   */
  async search(
    query: string,
    type?: 'movie' | 'tv' | 'all',
    limit = 100
  ): Promise<ProwlarrSearchResult[]> {
    try {
      const params: any = {
        query,
        limit,
        type: type || 'search'
      };

      // Map type to Prowlarr categories
      if (type === 'movie') {
        params.categories = [2000]; // Movies category
      } else if (type === 'tv') {
        params.categories = [5000]; // TV category
      }

      const response = await this.client.get<ProwlarrSearchResult[]>('/api/v1/search', {
        params
      });

      logger.info('Prowlarr search completed', {
        query,
        type,
        resultsCount: response.data.length
      });

      return response.data;
    } catch (error) {
      logger.error('Failed to search in Prowlarr', { query, type, error });
      throw error;
    }
  }

  /**
   * Parse and normalize search results
   */
  normalizeSearchResults(results: ProwlarrSearchResult[]): NormalizedSearchResult[] {
    return results.map(result => {
      // Extract quality from title (common patterns: 1080p, 720p, 2160p, etc.)
      const qualityMatch = result.title.match(/\b(2160p|1080p|720p|480p|4K|UHD|HD|SD)\b/i);
      const quality = qualityMatch ? qualityMatch[1].toUpperCase() : undefined;

      return {
        title: result.title,
        size: result.size,
        publishDate: result.publishDate,
        indexer: result.indexer,
        protocol: result.protocol,
        seeders: result.seeders,
        leechers: result.leechers,
        downloadUrl: result.downloadUrl,
        infoUrl: result.infoUrl,
        imdbId: result.imdbId,
        tmdbId: result.tmdbId,
        tvdbId: result.tvdbId,
        categories: result.categories.map(cat => cat.name),
        quality
      };
    });
  }

  /**
   * Filter and sort search results
   */
  filterAndSortResults(
    results: NormalizedSearchResult[],
    options: {
      minSeeders?: number;
      maxSize?: number; // in bytes
      preferredQuality?: string;
      sortBy?: 'seeders' | 'size' | 'date';
    } = {}
  ): NormalizedSearchResult[] {
    let filtered = [...results];

    // Filter by minimum seeders (for torrents)
    if (options.minSeeders !== undefined) {
      const minSeeders = options.minSeeders;
      filtered = filtered.filter(r => 
        r.protocol !== 'torrent' || (r.seeders && r.seeders >= minSeeders)
      );
    }

    // Filter by maximum size
    if (options.maxSize !== undefined) {
      const maxSize = options.maxSize;
      filtered = filtered.filter(r => r.size <= maxSize);
    }

    // Sort results
    const sortBy = options.sortBy || 'seeders';
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'seeders':
          return (b.seeders || 0) - (a.seeders || 0);
        case 'size':
          return a.size - b.size;
        case 'date':
          return new Date(b.publishDate).getTime() - new Date(a.publishDate).getTime();
        default:
          return 0;
      }
    });

    // Prioritize preferred quality
    if (options.preferredQuality) {
      const preferred = filtered.filter(r => 
        r.quality?.toLowerCase() === options.preferredQuality?.toLowerCase()
      );
      const others = filtered.filter(r => 
        r.quality?.toLowerCase() !== options.preferredQuality?.toLowerCase()
      );
      filtered = [...preferred, ...others];
    }

    return filtered;
  }

  /**
   * Get best result from search
   */
  getBestResult(
    results: ProwlarrSearchResult[],
    options: {
      minSeeders?: number;
      maxSize?: number;
      preferredQuality?: string;
    } = {}
  ): NormalizedSearchResult | null {
    const normalized = this.normalizeSearchResults(results);
    const filtered = this.filterAndSortResults(normalized, {
      ...options,
      sortBy: 'seeders'
    });

    return filtered.length > 0 ? filtered[0] : null;
  }
}
