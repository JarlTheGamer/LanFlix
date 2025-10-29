import { TMDBClient } from '../clients/tmdb.client';
import { config } from '../config/env';
import logger from '../utils/logger';
import { cacheManager } from '../utils/cache-manager';
import axios from 'axios';
import fs from 'fs/promises';
import path from 'path';
import Content from '../models/Content';

interface MovieMetadata {
  tmdbId: number;
  title: string;
  originalTitle: string;
  overview: string;
  releaseDate: string;
  runtime: number;
  voteAverage: number;
  voteCount: number;
  genres: string[];
  status: string;
  posterPath?: string;
  backdropPath?: string;
  cast?: Array<{ name: string; character: string }>;
  director?: string;
  fetchedAt: string;
}

interface SeriesMetadata {
  tmdbId: number;
  title: string;
  originalTitle: string;
  overview: string;
  firstAirDate: string;
  lastAirDate?: string;
  numberOfSeasons: number;
  numberOfEpisodes: number;
  genres: string[];
  voteAverage: number;
  voteCount: number;
  status: string;
  posterPath?: string;
  backdropPath?: string;
  seasons: Array<{
    seasonNumber: number;
    episodeCount: number;
    airDate: string;
  }>;
  fetchedAt: string;
}

/**
 * Service for managing content metadata from TMDB
 * Handles fetching, caching, and storing metadata for movies and TV series
 */
export class MetadataService {
  private tmdbClient: TMDBClient;
  private imageBaseUrl = 'https://image.tmdb.org/t/p';
  private posterSize = 'w500';
  private backdropSize = 'w1280';
  private metadataStalenessDays = 7;

  constructor(tmdbClient?: TMDBClient) {
    this.tmdbClient = tmdbClient || new TMDBClient();
  }

  /**
   * Fetch movie metadata from TMDB
   */
  async fetchMovieMetadata(tmdbId: number): Promise<MovieMetadata> {
    const cacheKey = cacheManager.generateKey('metadata', 'movie', tmdbId);
    
    return cacheManager.get(
      cacheKey,
      async () => {
        logger.info(`Fetching movie metadata from TMDB: ${tmdbId}`);
        
        // Gracefully handle API failures
        const details = await this.tmdbClient.getMovieDetails(tmdbId).catch((error) => {
          logger.error(`TMDB API unavailable for movie ${tmdbId}:`, error.message);
          throw new Error('TMDB API unavailable - cannot fetch metadata');
        });

        const metadata: MovieMetadata = {
          tmdbId: details.id,
          title: details.title,
          originalTitle: details.original_title,
          overview: details.overview,
          releaseDate: details.release_date,
          runtime: details.runtime,
          voteAverage: details.vote_average,
          voteCount: details.vote_count,
          genres: details.genres.map(g => g.name),
          status: details.status,
          posterPath: details.poster_path || undefined,
          backdropPath: details.backdrop_path || undefined,
          cast: details.credits?.cast
            ?.slice(0, 10)
            .map(c => ({ name: c.name, character: c.character })),
          director: details.credits?.crew?.find(c => c.job === 'Director')?.name,
          fetchedAt: new Date().toISOString()
        };

        return metadata;
      },
      { ttl: 7 * 24 * 60 * 60 * 1000 } // Cache for 7 days
    );
  }

  /**
   * Fetch TV series metadata from TMDB
   */
  async fetchSeriesMetadata(tmdbId: number): Promise<SeriesMetadata> {
    const cacheKey = cacheManager.generateKey('metadata', 'series', tmdbId);
    
    return cacheManager.get(
      cacheKey,
      async () => {
        logger.info(`Fetching series metadata from TMDB: ${tmdbId}`);
        
        // Gracefully handle API failures
        const details = await this.tmdbClient.getTVDetails(tmdbId).catch((error) => {
          logger.error(`TMDB API unavailable for series ${tmdbId}:`, error.message);
          throw new Error('TMDB API unavailable - cannot fetch metadata');
        });

        const metadata: SeriesMetadata = {
          tmdbId: details.id,
          title: details.name,
          originalTitle: details.original_name,
          overview: details.overview,
          firstAirDate: details.first_air_date,
          numberOfSeasons: details.number_of_seasons,
          numberOfEpisodes: details.number_of_episodes,
          genres: details.genres.map(g => g.name),
          voteAverage: details.vote_average,
          voteCount: details.vote_count,
          status: details.status,
          posterPath: details.poster_path || undefined,
          backdropPath: details.backdrop_path || undefined,
          seasons: details.seasons
            .filter(s => s.season_number > 0) // Exclude specials
            .map(s => ({
              seasonNumber: s.season_number,
              episodeCount: s.episode_count,
              airDate: s.air_date
            })),
          fetchedAt: new Date().toISOString()
        };

        return metadata;
      },
      { ttl: 7 * 24 * 60 * 60 * 1000 } // Cache for 7 days
    );
  }

  /**
   * Download poster image from TMDB
   */
  async downloadPosterImage(posterPath: string, contentId: number): Promise<string> {
    const imageUrl = `${this.imageBaseUrl}/${this.posterSize}${posterPath}`;
    const fileName = `${contentId}-poster.jpg`;
    const localPath = path.join(config.media.posterCachePath, fileName);

    try {
      // Ensure directory exists
      await fs.mkdir(config.media.posterCachePath, { recursive: true });

      // Download image
      const response = await axios.get(imageUrl, { responseType: 'arraybuffer' });
      await fs.writeFile(localPath, response.data);

      logger.info(`Downloaded poster image: ${fileName}`);
      return localPath;
    } catch (error) {
      logger.error(`Failed to download poster image for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Download backdrop image from TMDB
   */
  async downloadBackdropImage(backdropPath: string, contentId: number): Promise<string> {
    const imageUrl = `${this.imageBaseUrl}/${this.backdropSize}${backdropPath}`;
    const fileName = `${contentId}-backdrop.jpg`;
    const localPath = path.join(config.media.backdropCachePath, fileName);

    try {
      // Ensure directory exists
      await fs.mkdir(config.media.backdropCachePath, { recursive: true });

      // Download image
      const response = await axios.get(imageUrl, { responseType: 'arraybuffer' });
      await fs.writeFile(localPath, response.data);

      logger.info(`Downloaded backdrop image: ${fileName}`);
      return localPath;
    } catch (error) {
      logger.error(`Failed to download backdrop image for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Save metadata to media folder as JSON file
   */
  async saveMetadataToMediaFolder(
    contentId: number,
    mediaFolderPath: string
  ): Promise<void> {
    try {
      // Get content from database
      const content = await Content.findByPk(contentId);
      if (!content) {
        throw new Error(`Content not found: ${contentId}`);
      }

      // Fetch fresh metadata
      const metadata = content.type === 'movie'
        ? await this.fetchMovieMetadata(content.tmdbId)
        : await this.fetchSeriesMetadata(content.tmdbId);

      // Save to media folder
      const metadataPath = path.join(mediaFolderPath, 'metadata.json');
      await fs.writeFile(metadataPath, JSON.stringify(metadata, null, 2));

      logger.info(`Saved metadata to ${metadataPath}`);

      // Download and save poster if available
      if (metadata.posterPath) {
        const posterUrl = `${this.imageBaseUrl}/${this.posterSize}${metadata.posterPath}`;
        const posterPath = path.join(mediaFolderPath, 'poster.jpg');
        const response = await axios.get(posterUrl, { responseType: 'arraybuffer' });
        await fs.writeFile(posterPath, response.data);
        logger.info(`Saved poster to ${posterPath}`);
      }

      // Download and save backdrop if available
      if (metadata.backdropPath) {
        const backdropUrl = `${this.imageBaseUrl}/${this.backdropSize}${metadata.backdropPath}`;
        const backdropPath = path.join(mediaFolderPath, 'backdrop.jpg');
        const response = await axios.get(backdropUrl, { responseType: 'arraybuffer' });
        await fs.writeFile(backdropPath, response.data);
        logger.info(`Saved backdrop to ${backdropPath}`);
      }
    } catch (error) {
      logger.error(`Failed to save metadata to media folder for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Load metadata from media folder JSON file
   */
  async loadMetadataFromMediaFolder(
    mediaFolderPath: string
  ): Promise<MovieMetadata | SeriesMetadata | null> {
    try {
      const metadataPath = path.join(mediaFolderPath, 'metadata.json');
      const data = await fs.readFile(metadataPath, 'utf-8');
      const metadata = JSON.parse(data);

      logger.info(`Loaded metadata from ${metadataPath}`);
      return metadata;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
        logger.debug(`No metadata file found at ${mediaFolderPath}`);
        return null;
      }
      logger.error(`Failed to load metadata from ${mediaFolderPath}:`, error);
      throw error;
    }
  }

  /**
   * Check if metadata is stale (older than 7 days)
   */
  isMetadataStale(fetchedAt: string): boolean {
    const fetchedDate = new Date(fetchedAt);
    const now = new Date();
    const daysDiff = (now.getTime() - fetchedDate.getTime()) / (1000 * 60 * 60 * 24);
    return daysDiff > this.metadataStalenessDays;
  }

  /**
   * Refresh metadata for content if stale
   */
  async refreshMetadata(contentId: number): Promise<void> {
    try {
      const content = await Content.findByPk(contentId);
      if (!content) {
        throw new Error(`Content not found: ${contentId}`);
      }

      // Check if metadata is stale
      if (content.filePath) {
        const mediaFolder = path.dirname(content.filePath);
        const existingMetadata = await this.loadMetadataFromMediaFolder(mediaFolder);

        if (existingMetadata && !this.isMetadataStale(existingMetadata.fetchedAt)) {
          logger.debug(`Metadata for content ${contentId} is still fresh`);
          return;
        }
      }

      // Fetch fresh metadata
      logger.info(`Refreshing metadata for content ${contentId}`);
      const metadata = content.type === 'movie'
        ? await this.fetchMovieMetadata(content.tmdbId)
        : await this.fetchSeriesMetadata(content.tmdbId);

      // Update database
      await content.update({
        title: metadata.title,
        originalTitle: metadata.originalTitle,
        overview: metadata.overview,
        releaseDate: content.type === 'movie' 
          ? new Date((metadata as MovieMetadata).releaseDate)
          : new Date((metadata as SeriesMetadata).firstAirDate),
        voteAverage: metadata.voteAverage,
        voteCount: metadata.voteCount,
        genres: JSON.stringify(metadata.genres),
        status: metadata.status,
        runtime: content.type === 'movie' ? (metadata as MovieMetadata).runtime : undefined,
        updatedAt: new Date()
      });

      // Save to media folder if file path exists
      if (content.filePath) {
        const mediaFolder = path.dirname(content.filePath);
        await this.saveMetadataToMediaFolder(contentId, mediaFolder);
      }

      // Invalidate cache
      const cacheKey = cacheManager.generateKey('metadata', content.type, content.tmdbId);
      await cacheManager.delete(cacheKey);

      logger.info(`Metadata refreshed for content ${contentId}`);
    } catch (error) {
      logger.error(`Failed to refresh metadata for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Get metadata for content (from cache, database, or TMDB)
   */
  async getMetadata(
    tmdbId: number,
    type: 'movie' | 'series'
  ): Promise<MovieMetadata | SeriesMetadata> {
    return type === 'movie'
      ? this.fetchMovieMetadata(tmdbId)
      : this.fetchSeriesMetadata(tmdbId);
  }
}

export default new MetadataService();
