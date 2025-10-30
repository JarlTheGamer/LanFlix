import axios, { AxiosInstance, AxiosError } from 'axios';
import { config } from '../config/env';
import logger from '../utils/logger';

interface SonarrSeries {
  id: number;
  title: string;
  sortTitle: string;
  status: string;
  overview: string;
  network: string;
  airTime: string;
  images: Array<{
    coverType: string;
    url: string;
  }>;
  seasons: Array<{
    seasonNumber: number;
    monitored: boolean;
  }>;
  year: number;
  path: string;
  qualityProfileId: number;
  languageProfileId: number;
  seasonFolder: boolean;
  monitored: boolean;
  tvdbId: number;
  tvRageId: number;
  tvMazeId: number;
  imdbId: string;
  titleSlug: string;
  rootFolderPath: string;
  added: string;
}

interface SonarrSearchResult {
  title: string;
  sortTitle: string;
  status: string;
  overview: string;
  network: string;
  airTime: string;
  images: Array<{
    coverType: string;
    url: string;
  }>;
  seasons: Array<{
    seasonNumber: number;
    monitored: boolean;
  }>;
  year: number;
  tvdbId: number;
  tvRageId: number;
  tvMazeId: number;
  imdbId: string;
  titleSlug: string;
}

interface SonarrQueueItem {
  id: number;
  seriesId: number;
  episodeId: number;
  series: {
    title: string;
  };
  episode: {
    seasonNumber: number;
    episodeNumber: number;
    title: string;
  };
  quality: {
    quality: {
      name: string;
    };
  };
  size: number;
  title: string;
  sizeleft: number;
  timeleft: string;
  estimatedCompletionTime: string;
  status: string;
  trackedDownloadStatus: string;
  trackedDownloadState: string;
  downloadId: string;
  protocol: string;
  downloadClient: string;
  indexer: string;
  outputPath: string;
}

interface SonarrQueueResponse {
  page: number;
  pageSize: number;
  sortKey: string;
  sortDirection: string;
  totalRecords: number;
  records: SonarrQueueItem[];
}

interface AddSeriesOptions {
  tvdbId: number;
  title: string;
  qualityProfileId: number;
  rootFolderPath: string;
  seasonFolder?: boolean;
  monitored?: boolean;
  searchForMissingEpisodes?: boolean;
}

export class SonarrClient {
  private client: AxiosInstance;
  private baseURL: string;
  private apiKey: string;

  constructor(baseURL?: string, apiKey?: string) {
    this.baseURL = baseURL || config.externalServices.sonarr.url;
    this.apiKey = apiKey || config.externalServices.sonarr.apiKey;

    if (!this.apiKey) {
      logger.warn('Sonarr API key not configured');
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
        logger.error('Sonarr API error', {
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
        logger.error('Sonarr API error', {
          url: error.config?.url,
          status: error.response?.status,
          message: error.message,
          data: error.response?.data
        });
        throw error;
      }
    );

    logger.info('Sonarr configuration updated');
  }

  /**
   * Test connection to Sonarr
   */
  async testConnection(): Promise<boolean> {
    try {
      const response = await this.client.get('/api/v3/system/status');
      logger.info('Sonarr connection successful', { version: response.data.version });
      return true;
    } catch (error) {
      logger.error('Sonarr connection failed', { error });
      return false;
    }
  }

  /**
   * Get health check status
   */
  async getHealth(): Promise<any> {
    try {
      const response = await this.client.get('/api/v3/health');
      return response.data;
    } catch (error) {
      logger.error('Failed to get Sonarr health status', { error });
      throw error;
    }
  }

  /**
   * Search for series by title
   */
  async searchSeries(query: string): Promise<SonarrSearchResult[]> {
    try {
      const response = await this.client.get<SonarrSearchResult[]>('/api/v3/series/lookup', {
        params: { term: query }
      });
      return response.data;
    } catch (error) {
      logger.error('Failed to search series in Sonarr', { query, error });
      throw error;
    }
  }

  /**
   * Add a series to Sonarr
   */
  async addSeries(options: AddSeriesOptions): Promise<SonarrSeries> {
    try {
      const payload = {
        tvdbId: options.tvdbId,
        title: options.title,
        qualityProfileId: options.qualityProfileId,
        titleSlug: options.title.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
        images: [],
        seasons: [],
        path: `${options.rootFolderPath}/${options.title}`,
        rootFolderPath: options.rootFolderPath,
        seasonFolder: options.seasonFolder !== false,
        monitored: options.monitored !== false,
        addOptions: {
          searchForMissingEpisodes: options.searchForMissingEpisodes !== false
        }
      };

      const response = await this.client.post<SonarrSeries>('/api/v3/series', payload);
      logger.info('Series added to Sonarr', { title: options.title, id: response.data.id });
      return response.data;
    } catch (error) {
      logger.error('Failed to add series to Sonarr', { options, error });
      throw error;
    }
  }

  /**
   * Get all series
   */
  async getSeries(): Promise<SonarrSeries[]> {
    try {
      const response = await this.client.get<SonarrSeries[]>('/api/v3/series');
      return response.data;
    } catch (error) {
      logger.error('Failed to get series from Sonarr', { error });
      throw error;
    }
  }

  /**
   * Get series by ID
   */
  async getSeriesById(id: number): Promise<SonarrSeries> {
    try {
      const response = await this.client.get<SonarrSeries>(`/api/v3/series/${id}`);
      return response.data;
    } catch (error) {
      logger.error('Failed to get series by ID from Sonarr', { id, error });
      throw error;
    }
  }

  /**
   * Get download queue
   */
  async getQueue(page = 1, pageSize = 20): Promise<SonarrQueueResponse> {
    try {
      const response = await this.client.get<SonarrQueueResponse>('/api/v3/queue', {
        params: {
          page,
          pageSize,
          includeUnknownSeriesItems: false
        }
      });
      return response.data;
    } catch (error) {
      logger.error('Failed to get queue from Sonarr', { error });
      throw error;
    }
  }

  /**
   * Delete series from Sonarr
   */
  async deleteSeries(id: number, deleteFiles = false): Promise<void> {
    try {
      await this.client.delete(`/api/v3/series/${id}`, {
        params: {
          deleteFiles,
          addImportListExclusion: false
        }
      });
      logger.info('Series deleted from Sonarr', { id, deleteFiles });
    } catch (error) {
      logger.error('Failed to delete series from Sonarr', { id, error });
      throw error;
    }
  }

  /**
   * Get root folders
   */
  async getRootFolders(): Promise<Array<{ id: number; path: string; freeSpace: number }>> {
    try {
      const response = await this.client.get('/api/v3/rootfolder');
      return response.data;
    } catch (error) {
      logger.error('Failed to get root folders from Sonarr', { error });
      throw error;
    }
  }

  /**
   * Get quality profiles
   */
  async getQualityProfiles(): Promise<Array<{ id: number; name: string }>> {
    try {
      const response = await this.client.get('/api/v3/qualityprofile');
      return response.data;
    } catch (error) {
      logger.error('Failed to get quality profiles from Sonarr', { error });
      throw error;
    }
  }

  /**
   * Get episodes for a series
   */
  async getEpisodes(seriesId: number): Promise<Array<{
    id: number;
    seriesId: number;
    seasonNumber: number;
    episodeNumber: number;
    title: string;
    monitored: boolean;
    hasFile: boolean;
  }>> {
    try {
      const response = await this.client.get('/api/v3/episode', {
        params: { seriesId }
      });
      return response.data;
    } catch (error) {
      logger.error('Failed to get episodes from Sonarr', { seriesId, error });
      throw error;
    }
  }

  /**
   * Update episode monitoring status
   */
  async updateEpisode(episodeId: number, updates: { monitored: boolean }): Promise<void> {
    try {
      // First get the episode
      const episode = await this.client.get(`/api/v3/episode/${episodeId}`);

      // Update with new values
      await this.client.put(`/api/v3/episode/${episodeId}`, {
        ...episode.data,
        ...updates
      });

      logger.info('Episode updated in Sonarr', { episodeId, updates });
    } catch (error) {
      logger.error('Failed to update episode in Sonarr', { episodeId, error });
      throw error;
    }
  }

  /**
   * Search for a specific episode
   */
  async searchEpisode(episodeId: number): Promise<void> {
    try {
      await this.client.post('/api/v3/command', {
        name: 'EpisodeSearch',
        episodeIds: [episodeId]
      });
      logger.info('Episode search triggered in Sonarr', { episodeId });
    } catch (error) {
      logger.error('Failed to trigger episode search in Sonarr', { episodeId, error });
      throw error;
    }
  }

  /**
   * Search for all episodes in a season
   */
  async searchSeason(seriesId: number, seasonNumber: number): Promise<void> {
    try {
      await this.client.post('/api/v3/command', {
        name: 'SeasonSearch',
        seriesId,
        seasonNumber
      });
      logger.info('Season search triggered in Sonarr', { seriesId, seasonNumber });
    } catch (error) {
      logger.error('Failed to trigger season search in Sonarr', { seriesId, seasonNumber, error });
      throw error;
    }
  }
}
