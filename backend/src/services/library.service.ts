import { Op } from 'sequelize';
import Content from '../models/Content';
import SeriesEpisode from '../models/SeriesEpisode';
import WatchHistory from '../models/WatchHistory';
import { MetadataService } from './metadata.service';
import { config } from '../config/env';
import logger from '../utils/logger';
import fs from 'fs/promises';
import path from 'path';
import { getPosterUrl, getBackdropUrl } from '../utils/image-url';

interface LibraryItem {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  originalTitle?: string;
  overview?: string;
  releaseDate?: string;
  posterUrl?: string;
  backdropUrl?: string;
  voteAverage?: number;
  voteCount?: number;
  genres: string[];
  runtime?: number;
  status?: string;
  filePath?: string;
  addedAt: string;
  watchProgress?: {
    progressSeconds: number;
    durationSeconds?: number;
    completed: boolean;
    lastWatchedAt: string;
  };
  episodes?: EpisodeInfo[];
}

interface EpisodeInfo {
  id: number;
  seasonNumber: number;
  episodeNumber: number;
  title?: string;
  overview?: string;
  airDate?: string;
  stillPath?: string;
  filePath?: string;
  watched: boolean;
}

interface LibraryFilters {
  type?: 'movie' | 'series';
  genre?: string;
  search?: string;
  sortBy?: 'addedAt' | 'title' | 'releaseDate' | 'voteAverage';
  sortOrder?: 'ASC' | 'DESC';
  limit?: number;
  offset?: number;
}

interface MediaFile {
  path: string;
  name: string;
  type: 'movie' | 'series';
  seasonNumber?: number;
  episodeNumber?: number;
}

/**
 * Service for managing the media library
 * Handles library items, scanning, adding/removing content, and watch progress
 */
export class LibraryService {
  private metadataService: MetadataService;
  private imageBaseUrl = 'https://image.tmdb.org/t/p';
  private posterSize = 'w500';
  private backdropSize = 'w1280';
  private videoExtensions = ['.mp4', '.mkv', '.avi', '.mov', '.wmv', '.flv', '.webm', '.m4v'];

  constructor(metadataService?: MetadataService) {
    this.metadataService = metadataService || new MetadataService();
  }

  /**
   * Get library items with filtering
   */
  async getLibraryItems(
    filters: LibraryFilters = {},
    profileId?: number
  ): Promise<{ items: LibraryItem[]; total: number }> {
    try {
      const {
        type,
        genre,
        search,
        sortBy = 'addedAt',
        sortOrder = 'DESC',
        limit = 50,
        offset = 0
      } = filters;

      // Build where clause
      const where: any = {};

      if (type) {
        where.type = type;
      }

      if (genre) {
        where.genres = {
          [Op.like]: `%${genre}%`
        };
      }

      if (search) {
        where[Op.or] = [
          { title: { [Op.like]: `%${search}%` } },
          { originalTitle: { [Op.like]: `%${search}%` } }
        ];
      }

      // Get total count
      const total = await Content.count({ where });

      // Get content items
      const contentItems = await Content.findAll({
        where,
        order: [[sortBy, sortOrder]],
        limit,
        offset
      });

      // Get watch progress for profile if provided
      let watchProgressMap = new Map<number, any>();
      if (profileId) {
        const watchHistory = await WatchHistory.findAll({
          where: {
            profileId,
            contentId: contentItems.map(c => c.id)
          }
        });
        watchProgressMap = new Map(
          watchHistory.map(wh => [
            wh.contentId,
            {
              progressSeconds: wh.progressSeconds,
              durationSeconds: wh.durationSeconds,
              completed: wh.completed,
              lastWatchedAt: wh.lastWatchedAt.toISOString()
            }
          ])
        );
      }

      // Convert to LibraryItem format
      const items: LibraryItem[] = await Promise.all(
        contentItems.map(async (content) => {
          const item: LibraryItem = {
            id: content.id,
            tmdbId: content.tmdbId,
            type: content.type,
            title: content.title,
            originalTitle: content.originalTitle,
            overview: content.overview,
            releaseDate: content.releaseDate instanceof Date ? content.releaseDate.toISOString() : content.releaseDate,
            posterUrl: getPosterUrl(content.posterPath, content.id),
            backdropUrl: getBackdropUrl(content.backdropPath, content.id),
            voteAverage: content.voteAverage ? parseFloat(content.voteAverage.toString()) : undefined,
            voteCount: content.voteCount,
            genres: content.genres ? JSON.parse(content.genres) : [],
            runtime: content.runtime,
            status: content.status,
            filePath: content.filePath,
            addedAt: content.addedAt.toISOString(),
            watchProgress: watchProgressMap.get(content.id)
          };

          // Get episodes for series
          if (content.type === 'series') {
            const episodes = await SeriesEpisode.findAll({
              where: { contentId: content.id },
              order: [['seasonNumber', 'ASC'], ['episodeNumber', 'ASC']]
            });

            // Get watched episodes if profileId provided
            let watchedEpisodes = new Set<number>();
            if (profileId) {
              const episodeHistory = await WatchHistory.findAll({
                where: {
                  profileId,
                  contentId: content.id,
                  completed: true
                }
              });
              watchedEpisodes = new Set(
                episodeHistory
                  .filter(wh => wh.episodeId !== null && wh.episodeId !== undefined)
                  .map(wh => wh.episodeId!)
              );
            }

            item.episodes = episodes.map(ep => ({
              id: ep.id,
              seasonNumber: ep.seasonNumber,
              episodeNumber: ep.episodeNumber,
              title: ep.title,
              overview: ep.overview,
              airDate: ep.airDate?.toISOString(),
              stillPath: ep.stillPath,
              filePath: ep.filePath,
              watched: watchedEpisodes.has(ep.id)
            }));
          }

          return item;
        })
      );

      logger.info(`Retrieved ${items.length} library items (total: ${total})`);
      return { items, total };
    } catch (error) {
      logger.error('Failed to get library items:', error);
      throw error;
    }
  }

  /**
   * Get a specific library item by ID
   */
  async getLibraryItem(id: number, profileId?: number): Promise<LibraryItem | null> {
    try {
      const content = await Content.findByPk(id);
      if (!content) {
        return null;
      }

      // Get watch progress
      let watchProgress;
      if (profileId) {
        const history = await WatchHistory.findOne({
          where: { profileId, contentId: id }
        });
        if (history) {
          watchProgress = {
            progressSeconds: history.progressSeconds,
            durationSeconds: history.durationSeconds,
            completed: history.completed,
            lastWatchedAt: history.lastWatchedAt.toISOString()
          };
        }
      }

      const item: LibraryItem = {
        id: content.id,
        tmdbId: content.tmdbId,
        type: content.type,
        title: content.title,
        originalTitle: content.originalTitle,
        overview: content.overview,
        releaseDate: content.releaseDate instanceof Date ? content.releaseDate.toISOString() : content.releaseDate,
        posterUrl: getPosterUrl(content.posterPath, content.id),
        backdropUrl: getBackdropUrl(content.backdropPath, content.id),
        voteAverage: content.voteAverage ? parseFloat(content.voteAverage.toString()) : undefined,
        voteCount: content.voteCount,
        genres: content.genres ? JSON.parse(content.genres) : [],
        runtime: content.runtime,
        status: content.status,
        filePath: content.filePath,
        addedAt: content.addedAt.toISOString(),
        watchProgress
      };

      // Get episodes for series
      if (content.type === 'series') {
        const episodes = await SeriesEpisode.findAll({
          where: { contentId: id },
          order: [['seasonNumber', 'ASC'], ['episodeNumber', 'ASC']]
        });

        // Get watched episodes
        let watchedEpisodes = new Set<number>();
        if (profileId) {
          const episodeHistory = await WatchHistory.findAll({
            where: {
              profileId,
              contentId: id,
              completed: true
            }
          });
          watchedEpisodes = new Set(
            episodeHistory
              .filter(wh => wh.episodeId !== null && wh.episodeId !== undefined)
              .map(wh => wh.episodeId!)
          );
        }

        item.episodes = episodes.map(ep => ({
          id: ep.id,
          seasonNumber: ep.seasonNumber,
          episodeNumber: ep.episodeNumber,
          title: ep.title,
          overview: ep.overview,
          airDate: ep.airDate?.toISOString(),
          stillPath: ep.stillPath,
          filePath: ep.filePath,
          watched: watchedEpisodes.has(ep.id)
        }));
      }

      return item;
    } catch (error) {
      logger.error(`Failed to get library item ${id}:`, error);
      throw error;
    }
  }

  /**
   * Add content to library
   */
  async addToLibrary(
    tmdbId: number,
    type: 'movie' | 'series',
    filePath: string
  ): Promise<Content> {
    try {
      logger.info(`Adding content to library: ${type} ${tmdbId}`);

      // Check if already in library
      const existing = await Content.findOne({
        where: { tmdbId, type }
      });

      if (existing) {
        logger.info(`Content already in library: ${existing.id}`);
        // Update file path if different
        if (existing.filePath !== filePath) {
          await existing.update({ filePath });
        }
        return existing;
      }

      // Fetch metadata
      const metadata = await this.metadataService.getMetadata(tmdbId, type);

      // Download images
      let posterPath: string | undefined;
      let backdropPath: string | undefined;

      if (metadata.posterPath) {
        try {
          posterPath = await this.metadataService.downloadPosterImage(
            metadata.posterPath,
            tmdbId
          );
        } catch (error) {
          logger.warn(`Failed to download poster for ${tmdbId}:`, error);
        }
      }

      if (metadata.backdropPath) {
        try {
          backdropPath = await this.metadataService.downloadBackdropImage(
            metadata.backdropPath,
            tmdbId
          );
        } catch (error) {
          logger.warn(`Failed to download backdrop for ${tmdbId}:`, error);
        }
      }

      // Create content entry
      const content = await Content.create({
        tmdbId,
        type,
        title: metadata.title,
        originalTitle: metadata.originalTitle,
        overview: metadata.overview,
        releaseDate: type === 'movie'
          ? new Date((metadata as any).releaseDate)
          : new Date((metadata as any).firstAirDate),
        posterPath,
        backdropPath,
        voteAverage: metadata.voteAverage,
        voteCount: metadata.voteCount,
        genres: JSON.stringify(metadata.genres),
        runtime: type === 'movie' ? (metadata as any).runtime : undefined,
        status: metadata.status,
        filePath
      });

      // Save metadata to media folder
      const mediaFolder = path.dirname(filePath);
      await this.metadataService.saveMetadataToMediaFolder(content.id, mediaFolder);

      logger.info(`Content added to library: ${content.id}`);
      return content;
    } catch (error) {
      logger.error(`Failed to add content to library (${type} ${tmdbId}):`, error);
      throw error;
    }
  }

  /**
   * Remove content from library
   */
  async removeFromLibrary(id: number, deleteFiles = false): Promise<void> {
    try {
      const content = await Content.findByPk(id);
      if (!content) {
        throw new Error(`Content not found: ${id}`);
      }

      logger.info(`Removing content from library: ${id}`);

      // Delete files if requested
      if (deleteFiles && content.filePath) {
        try {
          const mediaFolder = path.dirname(content.filePath);
          await fs.rm(mediaFolder, { recursive: true, force: true });
          logger.info(`Deleted media files: ${mediaFolder}`);
        } catch (error) {
          logger.warn(`Failed to delete media files for content ${id}:`, error);
        }
      }

      // Delete from database (cascades to related tables)
      await content.destroy();

      logger.info(`Content removed from library: ${id}`);
    } catch (error) {
      logger.error(`Failed to remove content from library (${id}):`, error);
      throw error;
    }
  }

  /**
   * Get recently added content
   */
  async getRecentlyAdded(limit = 20, profileId?: number): Promise<LibraryItem[]> {
    try {
      const result = await this.getLibraryItems(
        {
          sortBy: 'addedAt',
          sortOrder: 'DESC',
          limit
        },
        profileId
      );

      return result.items;
    } catch (error) {
      logger.error('Failed to get recently added content:', error);
      throw error;
    }
  }

  /**
   * Scan library folder for new media files
   */
  async scanLibraryFolder(): Promise<{ added: number; updated: number; errors: string[] }> {
    try {
      logger.info('Starting library scan');

      const mediaRoot = config.media.rootPath;
      const stats = { added: 0, updated: 0, errors: [] as string[] };

      // Try multiple possible folder names (case-insensitive)
      const moviesFolderNames = ['movies', 'Movies', 'MOVIES'];
      const seriesFolderNames = ['series', 'Series', 'SERIES', 'shows', 'Shows', 'SHOWS'];

      // Scan movies folder
      let moviesScanned = false;
      for (const folderName of moviesFolderNames) {
        const moviesPath = path.join(mediaRoot, folderName);
        try {
          await fs.access(moviesPath);
          await this.scanMoviesFolder(moviesPath, stats);
          moviesScanned = true;
          break;
        } catch (error) {
          // Folder doesn't exist, try next name
          continue;
        }
      }

      if (!moviesScanned) {
        logger.warn('No movies folder found in media root');
      }

      // Scan series folder
      let seriesScanned = false;
      for (const folderName of seriesFolderNames) {
        const seriesPath = path.join(mediaRoot, folderName);
        try {
          await fs.access(seriesPath);
          await this.scanSeriesFolder(seriesPath, stats);
          seriesScanned = true;
          break;
        } catch (error) {
          // Folder doesn't exist, try next name
          continue;
        }
      }

      if (!seriesScanned) {
        logger.warn('No series folder found in media root');
      }

      logger.info('Library scan completed', stats);
      return stats;
    } catch (error) {
      logger.error('Failed to scan library folder:', error);
      throw error;
    }
  }

  /**
   * Scan movies folder
   */
  private async scanMoviesFolder(
    moviesPath: string,
    stats: { added: number; updated: number; errors: string[] }
  ): Promise<void> {
    try {
      const entries = await fs.readdir(moviesPath, { withFileTypes: true });

      for (const entry of entries) {
        if (!entry.isDirectory()) continue;

        const movieFolder = path.join(moviesPath, entry.name);

        try {
          // Find video file
          const files = await fs.readdir(movieFolder);
          const videoFile = files.find(f =>
            this.videoExtensions.some(ext => f.toLowerCase().endsWith(ext))
          );

          if (!videoFile) {
            logger.debug(`No video file found in ${movieFolder}`);
            continue;
          }

          const filePath = path.join(movieFolder, videoFile);

          // Try to load metadata from folder
          const metadata = await this.metadataService.loadMetadataFromMediaFolder(movieFolder);

          if (metadata && metadata.tmdbId) {
            // Check if already in library
            const existing = await Content.findOne({
              where: { tmdbId: metadata.tmdbId, type: 'movie' }
            });

            if (existing) {
              // Update file path if changed
              if (existing.filePath !== filePath) {
                await existing.update({ filePath });
                stats.updated++;
              }
            } else {
              // Add to library
              await this.addToLibrary(metadata.tmdbId, 'movie', filePath);
              stats.added++;
            }
          } else {
            logger.warn(`No metadata found for movie in ${movieFolder}`);
            stats.errors.push(`No metadata: ${entry.name}`);
          }
        } catch (error) {
          logger.warn(`Failed to process movie folder ${entry.name}:`, error);
          stats.errors.push(`${entry.name}: ${(error as Error).message}`);
        }
      }
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== 'ENOENT') {
        throw error;
      }
      logger.debug(`Movies folder not found: ${moviesPath}`);
    }
  }

  /**
   * Scan series folder
   */
  private async scanSeriesFolder(
    seriesPath: string,
    stats: { added: number; updated: number; errors: string[] }
  ): Promise<void> {
    try {
      const entries = await fs.readdir(seriesPath, { withFileTypes: true });

      for (const entry of entries) {
        if (!entry.isDirectory()) continue;

        const seriesFolder = path.join(seriesPath, entry.name);

        try {
          // Load metadata from series folder
          const metadata = await this.metadataService.loadMetadataFromMediaFolder(seriesFolder);

          if (!metadata || !metadata.tmdbId) {
            logger.warn(`No metadata found for series in ${seriesFolder}`);
            stats.errors.push(`No metadata: ${entry.name}`);
            continue;
          }

          // Check if already in library
          let content = await Content.findOne({
            where: { tmdbId: metadata.tmdbId, type: 'series' }
          });

          if (!content) {
            // Add series to library (without specific file path for now)
            content = await this.addToLibrary(metadata.tmdbId, 'series', seriesFolder);
            stats.added++;
          }

          // Scan season folders for episodes
          const seasonFolders = await fs.readdir(seriesFolder, { withFileTypes: true });

          for (const seasonEntry of seasonFolders) {
            if (!seasonEntry.isDirectory() || !seasonEntry.name.startsWith('Season')) continue;

            const seasonFolder = path.join(seriesFolder, seasonEntry.name);
            const seasonMatch = seasonEntry.name.match(/Season\s+(\d+)/i);

            if (!seasonMatch) continue;

            const seasonNumber = parseInt(seasonMatch[1], 10);

            // Scan episode files
            const episodeFiles = await fs.readdir(seasonFolder);

            for (const episodeFile of episodeFiles) {
              if (!this.videoExtensions.some(ext => episodeFile.toLowerCase().endsWith(ext))) {
                continue;
              }

              // Parse episode number from filename (e.g., S01E01, 1x01, etc.)
              const episodeMatch = episodeFile.match(/[SE](\d+)[EX](\d+)/i) ||
                                   episodeFile.match(/(\d+)x(\d+)/);

              if (!episodeMatch) continue;

              const episodeNumber = parseInt(episodeMatch[2], 10);
              const filePath = path.join(seasonFolder, episodeFile);

              // Check if episode exists
              const existingEpisode = await SeriesEpisode.findOne({
                where: {
                  contentId: content.id,
                  seasonNumber,
                  episodeNumber
                }
              });

              if (existingEpisode) {
                // Update file path if changed
                if (existingEpisode.filePath !== filePath) {
                  await existingEpisode.update({ filePath });
                  stats.updated++;
                }
              } else {
                // Create episode entry
                await SeriesEpisode.create({
                  contentId: content.id,
                  seasonNumber,
                  episodeNumber,
                  filePath
                });
                stats.added++;
              }
            }
          }
        } catch (error) {
          logger.warn(`Failed to process series folder ${entry.name}:`, error);
          stats.errors.push(`${entry.name}: ${(error as Error).message}`);
        }
      }
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== 'ENOENT') {
        throw error;
      }
      logger.debug(`Series folder not found: ${seriesPath}`);
    }
  }
}

export default new LibraryService();
