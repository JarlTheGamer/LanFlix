import { Op } from 'sequelize';
import Content from '../models/Content';
import SeriesEpisode from '../models/SeriesEpisode';
import WatchHistory from '../models/WatchHistory';
import { MetadataService } from './metadata.service';
import { mediaConverterService } from './media-converter.service';
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
  available: boolean;
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
            posterUrl: getPosterUrl(content.posterPath, content.id, content.filePath),
            backdropUrl: getBackdropUrl(content.backdropPath, content.id, content.filePath),
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

            item.episodes = episodes.map(ep => {
              const { getEpisodeStillUrl } = require('../utils/image-url');
              return {
                id: ep.id,
                seasonNumber: ep.seasonNumber,
                episodeNumber: ep.episodeNumber,
                title: ep.title,
                overview: ep.overview,
                airDate: ep.airDate ? (ep.airDate instanceof Date ? ep.airDate.toISOString() : ep.airDate) : undefined,
                stillPath: getEpisodeStillUrl(ep.stillPath, content.filePath, ep.seasonNumber, ep.episodeNumber),
                filePath: ep.filePath,
                watched: watchedEpisodes.has(ep.id),
                available: !!ep.filePath // Episode is available if it has a file path
              };
            });
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
        posterUrl: getPosterUrl(content.posterPath, content.id, content.filePath),
        backdropUrl: getBackdropUrl(content.backdropPath, content.id, content.filePath),
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

        item.episodes = episodes.map(ep => {
          const { getEpisodeStillUrl } = require('../utils/image-url');
          return {
            id: ep.id,
            seasonNumber: ep.seasonNumber,
            episodeNumber: ep.episodeNumber,
            title: ep.title,
            overview: ep.overview,
            airDate: ep.airDate ? (ep.airDate instanceof Date ? ep.airDate.toISOString() : ep.airDate) : undefined,
            stillPath: getEpisodeStillUrl(ep.stillPath, content.filePath, ep.seasonNumber, ep.episodeNumber),
            filePath: ep.filePath,
            watched: watchedEpisodes.has(ep.id),
            available: !!ep.filePath // Episode is available if it has a file path
          };
        });
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

        // Save metadata to media folder even if content exists
        // For movies, use parent directory; for series, use the folder itself
        const mediaFolder = type === 'movie' ? path.dirname(filePath) : filePath;
        try {
          await this.metadataService.saveMetadataToMediaFolder(existing.id, mediaFolder);
          logger.info(`Saved metadata to ${mediaFolder} for existing content`);
        } catch (error) {
          logger.warn(`Failed to save metadata to media folder for existing content ${existing.id}:`, error);
        }

        return existing;
      }

      // Fetch metadata
      const metadata = await this.metadataService.getMetadata(tmdbId, type);

      // Store poster and backdrop paths (images will be saved to media folder)
      const posterPath = metadata.posterPath;
      const backdropPath = metadata.backdropPath;

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
      // For movies, use parent directory; for series, use the folder itself
      const mediaFolder = type === 'movie' ? path.dirname(filePath) : filePath;
      await this.metadataService.saveMetadataToMediaFolder(content.id, mediaFolder);

      // For series, fetch and store episode metadata
      if (type === 'series') {
        await this.fetchAndStoreEpisodeMetadata(content.id, tmdbId);
      }

      logger.info(`Content added to library: ${content.id}`);
      return content;
    } catch (error) {
      logger.error(`Failed to add content to library (${type} ${tmdbId}):`, error);
      throw error;
    }
  }

  /**
   * Fetch and store episode metadata for a series
   */
  async fetchAndStoreEpisodeMetadata(contentId: number, tmdbId: number): Promise<void> {
    try {
      logger.info(`Fetching episode metadata for series ${tmdbId}`);

      // Import tmdbClient to avoid circular dependency
      const { tmdbClient } = await import('../clients');

      // Get TV details to know how many seasons
      const tvDetails = await tmdbClient.getTVDetails(tmdbId);

      // Fetch all seasons
      for (const season of tvDetails.seasons) {
        // Skip season 0 (specials)
        if (season.season_number === 0) continue;

        try {
          const seasonDetails = await tmdbClient.getSeasonDetails(tmdbId, season.season_number);

          // Store each episode
          for (const episode of seasonDetails.episodes) {
            // Check if episode already exists
            const existing = await SeriesEpisode.findOne({
              where: {
                contentId,
                seasonNumber: episode.season_number,
                episodeNumber: episode.episode_number
              }
            });

            if (!existing) {
              await SeriesEpisode.create({
                contentId,
                seasonNumber: episode.season_number,
                episodeNumber: episode.episode_number,
                title: episode.name,
                overview: episode.overview,
                airDate: episode.air_date ? new Date(episode.air_date) : undefined,
                stillPath: episode.still_path || undefined
              });
            } else {
              // Update metadata if episode exists but metadata is missing
              await existing.update({
                title: episode.name,
                overview: episode.overview,
                airDate: episode.air_date ? new Date(episode.air_date) : undefined,
                stillPath: episode.still_path || undefined
              });
            }
          }

          logger.info(`Stored metadata for Season ${season.season_number} (${seasonDetails.episodes.length} episodes)`);

          // Small delay to avoid rate limiting
          await new Promise(resolve => setTimeout(resolve, 100));
        } catch (error) {
          logger.error(`Failed to fetch season ${season.season_number} for series ${tmdbId}:`, error);
        }
      }

      logger.info(`Episode metadata stored for series ${tmdbId}`);
    } catch (error) {
      logger.error(`Failed to fetch episode metadata for series ${tmdbId}:`, error);
      // Don't throw - allow content to be added even if episode metadata fails
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
  async scanLibraryFolder(): Promise<{ added: number; updated: number; removed: number; errors: string[] }> {
    try {
      logger.info('Starting library scan');

      const mediaRoot = config.media.rootPath;
      const stats = { added: 0, updated: 0, removed: 0, errors: [] as string[] };

      // Try multiple possible folder names (case-insensitive)
      const moviesFolderNames = ['movies', 'Movies', 'MOVIES'];
      const seriesFolderNames = ['series', 'Series', 'SERIES', 'shows', 'Shows', 'SHOWS'];

      // Track existing content folders
      const existingFolders = new Set<string>();

      // Scan movies folder
      let moviesScanned = false;
      for (const folderName of moviesFolderNames) {
        const moviesPath = path.join(mediaRoot, folderName);
        try {
          await fs.access(moviesPath);
          await this.scanMoviesFolder(moviesPath, stats, existingFolders);
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
          await this.scanSeriesFolder(seriesPath, stats, existingFolders);
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

      // Remove content from database if folder no longer exists
      await this.cleanupMissingContent(existingFolders, stats);

      logger.info('Library scan completed', stats);
      return stats;
    } catch (error) {
      logger.error('Failed to scan library folder:', error);
      throw error;
    }
  }

  /**
   * Remove content from database if the folder no longer exists
   */
  private async cleanupMissingContent(
    existingFolders: Set<string>,
    stats: { removed: number }
  ): Promise<void> {
    try {
      // Get all content from database
      const allContent = await Content.findAll();

      for (const content of allContent) {
        let shouldRemove = false;
        let reason = '';

        // Remove content with no filePath (orphaned entries)
        if (!content.filePath) {
          shouldRemove = true;
          reason = 'no file path';
        } else {
          // For movies, check if the file exists
          // For series, check if the folder exists
          try {
            await fs.access(content.filePath);
            // Path exists, keep it
          } catch (error) {
            // Path doesn't exist, remove from database
            shouldRemove = true;
            reason = `${content.type === 'movie' ? 'file' : 'folder'} no longer exists: ${content.filePath}`;
          }
        }

        if (shouldRemove) {
          logger.info(`Removing content ${content.id} (${content.title}) - ${reason}`);

          // Import models
          const Watchlist = (await import('../models/Watchlist')).default;
          const DownloadQueue = (await import('../models/DownloadQueue')).default;
          const AutoDeleteSchedule = (await import('../models/AutoDeleteSchedule')).default;

          // Delete related records first to avoid foreign key constraint errors
          if (content.type === 'series') {
            // Delete episodes
            await SeriesEpisode.destroy({ where: { contentId: content.id } });
          }

          // Delete all related records
          await WatchHistory.destroy({ where: { contentId: content.id } });
          await Watchlist.destroy({ where: { contentId: content.id } });
          await DownloadQueue.destroy({ where: { contentId: content.id } });
          await AutoDeleteSchedule.destroy({ where: { contentId: content.id } });

          // Now delete the content
          await content.destroy();
          stats.removed++;
        }
      }

      // Also check episodes for series
      const allEpisodes = await SeriesEpisode.findAll();

      for (const episode of allEpisodes) {
        // Skip episodes without filePath (metadata-only entries are OK)
        if (!episode.filePath) continue;

        try {
          await fs.access(episode.filePath);
          // File exists, keep it
        } catch (error) {
          // File doesn't exist, remove from database
          logger.info(`Removing episode ${episode.id} (S${episode.seasonNumber}E${episode.episodeNumber}) - file no longer exists: ${episode.filePath}`);
          await episode.destroy();
          stats.removed++;
        }
      }
    } catch (error) {
      logger.error('Failed to cleanup missing content:', error);
    }
  }

  /**
   * Scan movies folder
   */
  private async scanMoviesFolder(
    moviesPath: string,
    stats: { added: number; updated: number; errors: string[] },
    existingFolders: Set<string>
  ): Promise<void> {
    try {
      const entries = await fs.readdir(moviesPath, { withFileTypes: true });

      for (const entry of entries) {
        if (!entry.isDirectory()) continue;

        const movieFolder = path.join(moviesPath, entry.name);

        try {
          // Find video file - check for nested folder first (qBittorrent issue)
          let files = await fs.readdir(movieFolder);
          let videoFile = files.find(f =>
            this.videoExtensions.some(ext => f.toLowerCase().endsWith(ext)) &&
            !f.includes('.converting.')  // Skip incomplete conversion files
          );
          let actualMovieFolder = movieFolder;

          // If no video file found, check if there's a single subfolder with the same name
          if (!videoFile) {
            const subfolders = files.filter(f => {
              const fullPath = path.join(movieFolder, f);
              try {
                return require('fs').statSync(fullPath).isDirectory();
              } catch {
                return false;
              }
            });

            // If there's exactly one subfolder, check inside it
            if (subfolders.length === 1) {
              const subfolderPath = path.join(movieFolder, subfolders[0]);
              const subfolderFiles = await fs.readdir(subfolderPath);
              videoFile = subfolderFiles.find(f =>
                this.videoExtensions.some(ext => f.toLowerCase().endsWith(ext))
              );

              if (videoFile) {
                actualMovieFolder = subfolderPath;
                logger.info(`Found video in nested folder: ${subfolderPath}`);
              }
            }
          }

          if (!videoFile) {
            logger.debug(`No video file found in ${movieFolder}`);
            continue;
          }

          let filePath = path.join(actualMovieFolder, videoFile);

          // TODO: Auto-convert file if needed (offline transcoding not yet implemented)
          // mediaConverterService.ensureCompatible(filePath)
          //   .then((convertedPath) => {
          //     if (convertedPath !== filePath) {
          //       logger.info(`File auto-converted: ${filePath} -> ${convertedPath}`);
          //       // Update the database with new file path
          //       Content.update(
          //         { filePath: convertedPath },
          //         { where: { filePath } }
          //       ).catch(err => logger.error('Failed to update file path after conversion:', err));
          //     }
          //   })
          //   .catch((err) => {
          //     logger.error(`Failed to auto-convert file ${filePath}:`, err);
          //   });

          // Try to load metadata from the top-level folder first, then nested
          let metadata = await this.metadataService.loadMetadataFromMediaFolder(movieFolder);
          if (!metadata && actualMovieFolder !== movieFolder) {
            metadata = await this.metadataService.loadMetadataFromMediaFolder(actualMovieFolder);
          }

          if (metadata && metadata.tmdbId) {
            // Check if already in library
            const existing = await Content.findOne({
              where: { tmdbId: metadata.tmdbId, type: 'movie' }
            });

            if (existing) {
              // Update metadata if missing or file path changed
              const needsUpdate =
                !existing.overview ||
                !existing.posterPath ||
                !existing.backdropPath ||
                existing.filePath !== filePath;

              logger.debug(`Checking if ${metadata.title} needs update: overview=${!!existing.overview}, posterPath=${!!existing.posterPath}, backdropPath=${!!existing.backdropPath}, filePathMatch=${existing.filePath === filePath}`);

              if (needsUpdate) {
                logger.info(`Updating metadata for ${metadata.title} (id: ${existing.id})`);
                // Cast to MovieMetadata since we're in the movies folder
                const movieMetadata = metadata as any;
                await existing.update({
                  title: metadata.title,
                  originalTitle: metadata.originalTitle,
                  overview: metadata.overview,
                  releaseDate: new Date(movieMetadata.releaseDate),
                  posterPath: metadata.posterPath,
                  backdropPath: metadata.backdropPath,
                  voteAverage: metadata.voteAverage,
                  voteCount: metadata.voteCount,
                  genres: JSON.stringify(metadata.genres),
                  runtime: movieMetadata.runtime,
                  status: metadata.status,
                  filePath
                });
                stats.updated++;
                logger.info(`Successfully updated metadata for ${metadata.title}`);
              }
              // Track this folder as existing
              existingFolders.add(movieFolder);
            } else {
              // Add to library
              await this.addToLibrary(metadata.tmdbId, 'movie', filePath);
              stats.added++;
              // Track this folder as existing
              existingFolders.add(movieFolder);
            }
          } else {
            // Try to search for metadata using folder name
            logger.info(`No metadata file found for ${entry.name}, attempting to fetch from TMDB`);

            try {
              // Parse movie title and year from folder name
              const match = entry.name.match(/^(.+?)\s*\((\d{4})\)/);
              if (match) {
                const [, title, year] = match;

                // Import tmdbClient
                const { tmdbClient } = await import('../clients');

                // Search for movie
                const searchResults = await tmdbClient.searchMovie(title);
                const movieMatch = searchResults.results.find(m =>
                  m.release_date && m.release_date.startsWith(year)
                );

                if (movieMatch) {
                  logger.info(`Found TMDB match for ${title} (${year}): ${movieMatch.id}`);

                  // Add to library (this will fetch and save metadata)
                  await this.addToLibrary(movieMatch.id, 'movie', filePath);
                  stats.added++;
                  // Track this folder as existing
                  existingFolders.add(movieFolder);
                } else {
                  logger.warn(`No TMDB match found for ${title} (${year})`);
                  stats.errors.push(`No TMDB match: ${entry.name}`);
                }
              } else {
                logger.warn(`Could not parse movie title and year from folder name: ${entry.name}`);
                stats.errors.push(`Invalid folder name format: ${entry.name}`);
              }
            } catch (error) {
              logger.error(`Failed to fetch metadata for ${entry.name}:`, error);
              stats.errors.push(`Failed to fetch metadata: ${entry.name}`);
            }
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
    stats: { added: number; updated: number; errors: string[] },
    existingFolders: Set<string>
  ): Promise<void> {
    try {
      const entries = await fs.readdir(seriesPath, { withFileTypes: true });

      // Import sonarrClient to check for downloading series
      const { sonarrClient } = await import('../clients');
      let sonarrSeries: any[] = [];
      try {
        sonarrSeries = await sonarrClient.getSeries();
        logger.info(`Found ${sonarrSeries.length} series in Sonarr`);
      } catch (error) {
        logger.warn('Failed to get series from Sonarr:', error);
      }

      for (const entry of entries) {
        if (!entry.isDirectory()) continue;

        const seriesFolder = path.join(seriesPath, entry.name);

        try {
          // Load metadata from series folder
          let metadata = await this.metadataService.loadMetadataFromMediaFolder(seriesFolder);

          // If no metadata file exists, try to fetch from TMDB using folder name
          if (!metadata || !metadata.tmdbId) {
            logger.info(`No metadata found for series in ${seriesFolder}, attempting to fetch from TMDB`);

            try {
              // Try to parse series title from folder name
              const seriesTitle = entry.name;

              // Import tmdbClient
              const { tmdbClient } = await import('../clients');

              // Search for series
              const searchResults = await tmdbClient.searchTV(seriesTitle);

              if (searchResults.results.length > 0) {
                const seriesMatch = searchResults.results[0]; // Take first match
                logger.info(`Found TMDB match for ${seriesTitle}: ${seriesMatch.id}`);

                // Fetch full metadata
                metadata = await this.metadataService.getMetadata(seriesMatch.id, 'series');

                // Save metadata JSON and images to series folder directly
                const metadataPath = path.join(seriesFolder, 'metadata.json');
                await fs.writeFile(metadataPath, JSON.stringify(metadata, null, 2));
                logger.info(`Saved metadata JSON to ${metadataPath}`);

                // Download and save images
                if (metadata.posterPath) {
                  try {
                    const axios = (await import('axios')).default;
                    const posterUrl = `https://image.tmdb.org/t/p/w500${metadata.posterPath}`;
                    const posterPath = path.join(seriesFolder, 'poster.jpg');
                    const response = await axios.get(posterUrl, { responseType: 'arraybuffer' });
                    await fs.writeFile(posterPath, response.data);
                    logger.info(`Saved poster to ${posterPath}`);
                  } catch (error) {
                    logger.warn(`Failed to download poster for ${seriesTitle}:`, error);
                  }
                }

                if (metadata.backdropPath) {
                  try {
                    const axios = (await import('axios')).default;
                    const backdropUrl = `https://image.tmdb.org/t/p/w1280${metadata.backdropPath}`;
                    const backdropPath = path.join(seriesFolder, 'backdrop.jpg');
                    const response = await axios.get(backdropUrl, { responseType: 'arraybuffer' });
                    await fs.writeFile(backdropPath, response.data);
                    logger.info(`Saved backdrop to ${backdropPath}`);
                  } catch (error) {
                    logger.warn(`Failed to download backdrop for ${seriesTitle}:`, error);
                  }
                }
              } else {
                logger.warn(`No TMDB match found for ${seriesTitle}`);
                stats.errors.push(`No TMDB match: ${entry.name}`);
                continue;
              }
            } catch (error) {
              logger.error(`Failed to fetch metadata for ${entry.name}:`, error);
              stats.errors.push(`Failed to fetch metadata: ${entry.name}`);
              continue;
            }
          }

          // Check if already in library
          let content = await Content.findOne({
            where: { tmdbId: metadata.tmdbId, type: 'series' }
          });

          if (!content) {
            // Add series to library
            content = await this.addToLibrary(metadata.tmdbId, 'series', seriesFolder);
            stats.added++;
            // Track this folder as existing
            existingFolders.add(seriesFolder);

            // Check if this series is in Sonarr and create download queue entry if needed
            const sonarrMatch = sonarrSeries.find(s =>
              s.title.toLowerCase() === entry.name.toLowerCase() ||
              s.path.toLowerCase().includes(entry.name.toLowerCase())
            );

            if (sonarrMatch) {
              logger.info(`Series ${entry.name} found in Sonarr (id: ${sonarrMatch.id}), checking download status`);

              // Check if there's already a download queue entry
              const DownloadQueue = (await import('../models/DownloadQueue')).default;
              const existingQueue = await DownloadQueue.findOne({
                where: { contentId: content.id }
              });

              if (!existingQueue) {
                // Create download queue entry as "downloading"
                await DownloadQueue.create({
                  profileId: 1, // Default profile, adjust as needed
                  contentId: content.id,
                  type: 'series',
                  externalId: sonarrMatch.id,
                  status: 'downloading',
                  progressPercent: 0
                });
                logger.info(`Created download queue entry for ${entry.name}`);
              }
            }
          } else {
            // Update metadata if missing
            const needsUpdate =
              !content.overview ||
              !content.posterPath ||
              !content.backdropPath;

            if (needsUpdate) {
              const seriesMetadata = metadata as any;
              await content.update({
                title: metadata.title,
                originalTitle: metadata.originalTitle,
                overview: metadata.overview,
                releaseDate: new Date(seriesMetadata.firstAirDate),
                posterPath: metadata.posterPath,
                backdropPath: metadata.backdropPath,
                voteAverage: metadata.voteAverage,
                voteCount: metadata.voteCount,
                genres: JSON.stringify(metadata.genres),
                status: metadata.status
              });
              stats.updated++;
              logger.info(`Updated metadata for ${metadata.title}`);
            }

            // Track this folder as existing
            existingFolders.add(seriesFolder);
            // Check if episode metadata exists, if not fetch it
            const episodeCount = await SeriesEpisode.count({
              where: { contentId: content.id }
            });

            if (episodeCount === 0) {
              logger.info(`No episode metadata found for series ${content.id}, fetching from TMDB`);
              await this.fetchAndStoreEpisodeMetadata(content.id, metadata.tmdbId);
            }
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
              if (!this.videoExtensions.some(ext => episodeFile.toLowerCase().endsWith(ext)) ||
                  episodeFile.includes('.converting.')) {  // Skip incomplete conversion files
                continue;
              }

              // Parse episode number from filename (e.g., S01E01, 1x01, etc.)
              const episodeMatch = episodeFile.match(/[SE](\d+)[EX](\d+)/i) ||
                episodeFile.match(/(\d+)x(\d+)/);

              if (!episodeMatch) continue;

              const episodeNumber = parseInt(episodeMatch[2], 10);
              let filePath = path.join(seasonFolder, episodeFile);

              // TODO: Auto-convert file if needed (offline transcoding not yet implemented)
              // mediaConverterService.ensureCompatible(filePath)
              //   .then((convertedPath) => {
              //     if (convertedPath !== filePath) {
              //       logger.info(`Episode auto-converted: ${filePath} -> ${convertedPath}`);
              //       // Update the database with new file path
              //       SeriesEpisode.update(
              //         { filePath: convertedPath },
              //         { where: { filePath } }
              //       ).catch(err => logger.error('Failed to update episode file path after conversion:', err));
              //     }
              //   })
              //   .catch((err) => {
              //     logger.error(`Failed to auto-convert episode ${filePath}:`, err);
              //   });

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
                let needsUpdate = false;
                const updates: any = {};

                if (existingEpisode.filePath !== filePath) {
                  updates.filePath = filePath;
                  needsUpdate = true;
                }

                // Check if still image needs to be downloaded
                // If stillPath is a TMDB path (starts with /), download it locally
                if (existingEpisode.stillPath && existingEpisode.stillPath.startsWith('/')) {
                  const downloadedStill = await this.metadataService.downloadEpisodeStill(
                    existingEpisode.stillPath,
                    seasonFolder,
                    seasonNumber,
                    episodeNumber
                  );
                  if (downloadedStill) {
                    updates.stillPath = downloadedStill; // Update to local filename
                    needsUpdate = true;
                  }
                }

                if (needsUpdate) {
                  await existingEpisode.update(updates);
                  stats.updated++;
                }
              } else {
                // Create episode entry with file path
                // Try to fetch metadata from TMDB if not already stored
                const { tmdbClient } = await import('../clients');
                try {
                  const seasonDetails = await tmdbClient.getSeasonDetails(metadata.tmdbId, seasonNumber);
                  const episodeData = seasonDetails.episodes.find(
                    (ep: any) => ep.episode_number === episodeNumber
                  );

                  // Download episode still image to season folder
                  let localStillPath = episodeData?.still_path;
                  if (episodeData?.still_path) {
                    const downloadedStill = await this.metadataService.downloadEpisodeStill(
                      episodeData.still_path,
                      seasonFolder,
                      seasonNumber,
                      episodeNumber
                    );
                    if (downloadedStill) {
                      localStillPath = downloadedStill; // Store just the filename
                    }
                  }

                  await SeriesEpisode.create({
                    contentId: content.id,
                    seasonNumber,
                    episodeNumber,
                    title: episodeData?.name,
                    overview: episodeData?.overview,
                    airDate: episodeData?.air_date ? new Date(episodeData.air_date) : undefined,
                    stillPath: localStillPath || undefined,
                    filePath
                  });
                } catch (error) {
                  // If metadata fetch fails, create episode with just file path
                  logger.warn(`Failed to fetch metadata for S${seasonNumber}E${episodeNumber}:`, error);
                  await SeriesEpisode.create({
                    contentId: content.id,
                    seasonNumber,
                    episodeNumber,
                    filePath
                  });
                }
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
