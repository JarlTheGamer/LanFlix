import axios, { AxiosInstance, AxiosError } from 'axios';
import { config } from '../config/env';
import logger from '../utils/logger';

interface RadarrMovie {
  id: number;
  title: string;
  sortTitle: string;
  sizeOnDisk: number;
  status: string;
  overview: string;
  images: Array<{
    coverType: string;
    url: string;
  }>;
  year: number;
  hasFile: boolean;
  path: string;
  qualityProfileId: number;
  monitored: boolean;
  minimumAvailability: string;
  isAvailable: boolean;
  folderName: string;
  runtime: number;
  cleanTitle: string;
  imdbId: string;
  tmdbId: number;
  titleSlug: string;
  genres: string[];
  tags: number[];
  added: string;
  ratings: {
    imdb?: {
      value: number;
      votes: number;
    };
    tmdb?: {
      value: number;
      votes: number;
    };
  };
  movieFile?: {
    id: number;
    relativePath: string;
    path: string;
    size: number;
    dateAdded: string;
    quality: {
      quality: {
        name: string;
      };
    };
  };
}

interface RadarrSearchResult {
  title: string;
  sortTitle: string;
  status: string;
  overview: string;
  images: Array<{
    coverType: string;
    url: string;
  }>;
  year: number;
  runtime: number;
  imdbId: string;
  tmdbId: number;
  titleSlug: string;
  genres: string[];
  ratings: {
    imdb?: {
      value: number;
      votes: number;
    };
    tmdb?: {
      value: number;
      votes: number;
    };
  };
}

interface RadarrQueueItem {
  id: number;
  movieId: number;
  movie: {
    title: string;
    year: number;
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

interface RadarrQueueResponse {
  page: number;
  pageSize: number;
  sortKey: string;
  sortDirection: string;
  totalRecords: number;
  records: RadarrQueueItem[];
}

interface AddMovieOptions {
  tmdbId: number;
  title: string;
  year: number;
  qualityProfileId: number;
  rootFolderPath: string;
  monitored?: boolean;
  searchForMovie?: boolean;
  minimumAvailability?: 'announced' | 'inCinemas' | 'released';
}

export class RadarrClient {
  private client: AxiosInstance;
  private baseURL: string;
  private apiKey: string;

  constructor(baseURL?: string, apiKey?: string) {
    this.baseURL = baseURL || config.externalServices.radarr.url;
    this.apiKey = apiKey || config.externalServices.radarr.apiKey;

    if (!this.apiKey) {
      logger.warn('Radarr API key not configured');
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
        logger.error('Radarr API error', {
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
        logger.error('Radarr API error', {
          url: error.config?.url,
          status: error.response?.status,
          message: error.message,
          data: error.response?.data
        });
        throw error;
      }
    );
    
    logger.info('Radarr configuration updated');
  }

  /**
   * Test connection to Radarr
   */
  async testConnection(): Promise<boolean> {
    try {
      const response = await this.client.get('/api/v3/system/status');
      logger.info('Radarr connection successful', { version: response.data.version });
      return true;
    } catch (error) {
      logger.error('Radarr connection failed', { error });
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
      logger.error('Failed to get Radarr health status', { error });
      throw error;
    }
  }

  /**
   * Search for movies by title
   */
  async searchMovies(query: string): Promise<RadarrSearchResult[]> {
    try {
      const response = await this.client.get<RadarrSearchResult[]>('/api/v3/movie/lookup', {
        params: { term: query }
      });
      return response.data;
    } catch (error) {
      logger.error('Failed to search movies in Radarr', { query, error });
      throw error;
    }
  }

  /**
   * Add a movie to Radarr
   */
  async addMovie(options: AddMovieOptions): Promise<RadarrMovie> {
    try {
      const payload = {
        tmdbId: options.tmdbId,
        title: options.title,
        year: options.year,
        qualityProfileId: options.qualityProfileId,
        titleSlug: `${options.title.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${options.tmdbId}`,
        images: [],
        path: `${options.rootFolderPath}/${options.title} (${options.year})`,
        rootFolderPath: options.rootFolderPath,
        monitored: options.monitored !== false,
        minimumAvailability: options.minimumAvailability || 'released',
        addOptions: {
          searchForMovie: options.searchForMovie !== false
        }
      };

      const response = await this.client.post<RadarrMovie>('/api/v3/movie', payload);
      logger.info('Movie added to Radarr', { title: options.title, id: response.data.id });
      return response.data;
    } catch (error) {
      logger.error('Failed to add movie to Radarr', { options, error });
      throw error;
    }
  }

  /**
   * Get all movies
   */
  async getMovies(): Promise<RadarrMovie[]> {
    try {
      const response = await this.client.get<RadarrMovie[]>('/api/v3/movie');
      return response.data;
    } catch (error) {
      logger.error('Failed to get movies from Radarr', { error });
      throw error;
    }
  }

  /**
   * Get movie by ID
   */
  async getMovieById(id: number): Promise<RadarrMovie> {
    try {
      const response = await this.client.get<RadarrMovie>(`/api/v3/movie/${id}`);
      return response.data;
    } catch (error) {
      logger.error('Failed to get movie by ID from Radarr', { id, error });
      throw error;
    }
  }

  /**
   * Get movie by TMDB ID
   */
  async getMovieByTmdbId(tmdbId: number): Promise<RadarrMovie | null> {
    try {
      const allMovies = await this.getMovies();
      const movie = allMovies.find(m => m.tmdbId === tmdbId);
      return movie || null;
    } catch (error) {
      logger.error('Failed to get movie by TMDB ID from Radarr', { tmdbId, error });
      throw error;
    }
  }

  /**
   * Get download queue
   */
  async getQueue(page = 1, pageSize = 20): Promise<RadarrQueueResponse> {
    try {
      const response = await this.client.get<RadarrQueueResponse>('/api/v3/queue', {
        params: {
          page,
          pageSize,
          includeUnknownMovieItems: false
        }
      });
      return response.data;
    } catch (error) {
      logger.error('Failed to get queue from Radarr', { error });
      throw error;
    }
  }

  /**
   * Delete movie from Radarr
   */
  async deleteMovie(id: number, deleteFiles = false): Promise<void> {
    try {
      await this.client.delete(`/api/v3/movie/${id}`, {
        params: {
          deleteFiles,
          addImportListExclusion: false
        }
      });
      logger.info('Movie deleted from Radarr', { id, deleteFiles });
    } catch (error) {
      logger.error('Failed to delete movie from Radarr', { id, error });
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
      logger.error('Failed to get root folders from Radarr', { error });
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
      logger.error('Failed to get quality profiles from Radarr', { error });
      throw error;
    }
  }
}
