import { SonarrClient, RadarrClient, sonarrClient, radarrClient } from '../clients';
import { LibraryService } from './library.service';
import Content from '../models/Content';
import DownloadQueue from '../models/DownloadQueue';
import AutoDeleteSchedule from '../models/AutoDeleteSchedule';
import logger from '../utils/logger';

interface QueueDownloadOptions {
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  year?: number;
  profileId: number;
}

interface DownloadStatus {
  id: number;
  contentId: number;
  status: 'queued' | 'downloading' | 'completed' | 'failed';
  progressPercent: number;
  errorMessage?: string;
  queuedAt: string;
  completedAt?: string;
}

/**
 * Service for managing downloads via Sonarr and Radarr
 * Handles queueing, status tracking, completion, and auto-delete scheduling
 */
export class DownloadManager {
  private sonarrClient: SonarrClient;
  private radarrClient: RadarrClient;
  private libraryService: LibraryService;
  private pollingInterval: NodeJS.Timeout | null = null;
  private isPolling = false;

  constructor(
    sonarr?: SonarrClient,
    radarr?: RadarrClient,
    libraryService?: LibraryService
  ) {
    this.sonarrClient = sonarr || sonarrClient;
    this.radarrClient = radarr || radarrClient;
    this.libraryService = libraryService || new LibraryService();
  }

  /**
   * Queue a download via Sonarr or Radarr
   */
  async queueDownload(options: QueueDownloadOptions): Promise<DownloadQueue> {
    try {
      logger.info(`Queueing download: ${options.type} ${options.title}`);

      // Check if content already exists in library
      const existingContent = await Content.findOne({
        where: { tmdbId: options.tmdbId, type: options.type }
      });

      let contentId: number;

      if (existingContent) {
        contentId = existingContent.id;
        logger.info(`Content already in library: ${contentId}`);
      } else {
        // Create placeholder content entry
        const content = await Content.create({
          tmdbId: options.tmdbId,
          type: options.type,
          title: options.title,
          releaseDate: options.year ? new Date(options.year, 0, 1) : undefined
        });
        contentId = content.id;
      }

      // Check if already in download queue
      const existingQueue = await DownloadQueue.findOne({
        where: {
          contentId,
          status: ['queued', 'downloading']
        }
      });

      if (existingQueue) {
        logger.info(`Download already queued: ${existingQueue.id}`);
        return existingQueue;
      }

      // Add to Sonarr or Radarr
      let externalId: number | undefined;

      if (options.type === 'series') {
        try {
          // Get root folder and quality profile
          const [rootFolders, qualityProfiles] = await Promise.all([
            this.sonarrClient.getRootFolders(),
            this.sonarrClient.getQualityProfiles()
          ]);

          if (rootFolders.length === 0 || qualityProfiles.length === 0) {
            const missingItems = [];
            if (rootFolders.length === 0) missingItems.push('root folder');
            if (qualityProfiles.length === 0) missingItems.push('quality profile');
            
            throw new Error(
              `Sonarr is not properly configured. Please set up ${missingItems.join(' and ')} in Sonarr settings (http://localhost:8989/settings/mediamanagement for root folders, http://localhost:8989/settings/profiles for quality profiles)`
            );
          }

          // Search for series to get TVDB ID
          const searchResults = await this.sonarrClient.searchSeries(options.title);
          const match = searchResults.find(s => s.title === options.title);

          if (!match) {
            throw new Error(`Series not found in Sonarr: ${options.title}`);
          }

          // Add series to Sonarr
          const series = await this.sonarrClient.addSeries({
            tvdbId: match.tvdbId,
            title: options.title,
            qualityProfileId: qualityProfiles[0].id,
            rootFolderPath: rootFolders[0].path,
            searchForMissingEpisodes: true
          });

          externalId = series.id;
          logger.info(`Series added to Sonarr: ${externalId}`);
        } catch (error) {
          logger.error('Failed to add series to Sonarr:', error);
          throw error;
        }
      } else {
        try {
          // Get root folder and quality profile
          const [rootFolders, qualityProfiles] = await Promise.all([
            this.radarrClient.getRootFolders(),
            this.radarrClient.getQualityProfiles()
          ]);

          if (rootFolders.length === 0 || qualityProfiles.length === 0) {
            const missingItems = [];
            if (rootFolders.length === 0) missingItems.push('root folder');
            if (qualityProfiles.length === 0) missingItems.push('quality profile');
            
            throw new Error(
              `Radarr is not properly configured. Please set up ${missingItems.join(' and ')} in Radarr settings (http://localhost:7878/settings/mediamanagement for root folders, http://localhost:7878/settings/profiles for quality profiles)`
            );
          }

          // Add movie to Radarr
          const movie = await this.radarrClient.addMovie({
            tmdbId: options.tmdbId,
            title: options.title,
            year: options.year || new Date().getFullYear(),
            qualityProfileId: qualityProfiles[0].id,
            rootFolderPath: rootFolders[0].path,
            searchForMovie: true
          });

          externalId = movie.id;
          logger.info(`Movie added to Radarr: ${externalId}`);
        } catch (error) {
          logger.error('Failed to add movie to Radarr:', error);
          throw error;
        }
      }

      // Create download queue entry
      const queueEntry = await DownloadQueue.create({
        profileId: options.profileId,
        contentId,
        type: options.type,
        externalId,
        status: 'queued',
        progressPercent: 0
      });

      logger.info(`Download queued: ${queueEntry.id}`);
      return queueEntry;
    } catch (error) {
      logger.error('Failed to queue download:', error);
      throw error;
    }
  }

  /**
   * Get download status
   */
  async getDownloadStatus(contentId: number): Promise<DownloadStatus | null> {
    try {
      const queueEntry = await DownloadQueue.findOne({
        where: { contentId },
        order: [['queuedAt', 'DESC']]
      });

      if (!queueEntry) {
        return null;
      }

      return {
        id: queueEntry.id,
        contentId: queueEntry.contentId,
        status: queueEntry.status,
        progressPercent: queueEntry.progressPercent,
        errorMessage: queueEntry.errorMessage,
        queuedAt: queueEntry.queuedAt.toISOString(),
        completedAt: queueEntry.completedAt?.toISOString()
      };
    } catch (error) {
      logger.error(`Failed to get download status for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Cancel a download
   */
  async cancelDownload(contentId: number): Promise<void> {
    try {
      const queueEntry = await DownloadQueue.findOne({
        where: {
          contentId,
          status: ['queued', 'downloading']
        }
      });

      if (!queueEntry) {
        throw new Error(`No active download found for content ${contentId}`);
      }

      logger.info(`Cancelling download: ${queueEntry.id}`);

      // Remove from Sonarr/Radarr if external ID exists
      if (queueEntry.externalId) {
        try {
          if (queueEntry.type === 'series') {
            await this.sonarrClient.deleteSeries(queueEntry.externalId, false);
          } else {
            await this.radarrClient.deleteMovie(queueEntry.externalId, false);
          }
        } catch (error) {
          logger.warn(`Failed to remove from ${queueEntry.type === 'series' ? 'Sonarr' : 'Radarr'}:`, error);
        }
      }

      // Update queue entry
      await queueEntry.update({
        status: 'failed',
        errorMessage: 'Cancelled by user'
      });

      logger.info(`Download cancelled: ${queueEntry.id}`);
    } catch (error) {
      logger.error(`Failed to cancel download for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Poll download progress from Sonarr and Radarr
   */
  async pollDownloadProgress(): Promise<void> {
    if (this.isPolling) {
      logger.debug('Download polling already in progress, skipping');
      return;
    }

    this.isPolling = true;

    try {
      logger.debug('Polling download progress');

      // Get active downloads
      const activeDownloads = await DownloadQueue.findAll({
        where: {
          status: ['queued', 'downloading']
        }
      });

      if (activeDownloads.length === 0) {
        logger.debug('No active downloads to poll');
        return;
      }

      // Poll Sonarr queue
      const seriesDownloads = activeDownloads.filter(d => d.type === 'series');
      if (seriesDownloads.length > 0) {
        try {
          const sonarrQueue = await this.sonarrClient.getQueue(1, 100);
          await this.updateDownloadProgress(seriesDownloads, sonarrQueue.records, 'series');
        } catch (error) {
          logger.error('Failed to poll Sonarr queue:', error);
        }
      }

      // Poll Radarr queue
      const movieDownloads = activeDownloads.filter(d => d.type === 'movie');
      if (movieDownloads.length > 0) {
        try {
          const radarrQueue = await this.radarrClient.getQueue(1, 100);
          await this.updateDownloadProgress(movieDownloads, radarrQueue.records, 'movie');
        } catch (error) {
          logger.error('Failed to poll Radarr queue:', error);
        }
      }

      // Check for completed downloads (not in queue anymore)
      await this.checkCompletedDownloads(activeDownloads);
    } catch (error) {
      logger.error('Failed to poll download progress:', error);
    } finally {
      this.isPolling = false;
    }
  }

  /**
   * Update download progress from queue items
   */
  private async updateDownloadProgress(
    downloads: DownloadQueue[],
    queueItems: any[],
    type: 'movie' | 'series'
  ): Promise<void> {
    const queueMap = new Map(
      queueItems.map(item => [
        type === 'series' ? item.seriesId : item.movieId,
        item
      ])
    );

    for (const download of downloads) {
      if (!download.externalId) continue;

      const queueItem = queueMap.get(download.externalId);

      if (queueItem) {
        // Calculate progress
        const progress = queueItem.size > 0
          ? Math.round(((queueItem.size - queueItem.sizeleft) / queueItem.size) * 100)
          : 0;

        // Update status
        const status = queueItem.status === 'completed' ? 'completed' : 'downloading';

        await download.update({
          status,
          progressPercent: progress
        });

        logger.debug(`Updated download progress: ${download.id} - ${progress}%`);
      }
    }
  }

  /**
   * Check for completed downloads
   */
  private async checkCompletedDownloads(activeDownloads: DownloadQueue[]): Promise<void> {
    for (const download of activeDownloads) {
      if (!download.externalId) continue;

      try {
        // Check if content exists in Sonarr/Radarr with files
        let hasFiles = false;

        if (download.type === 'series') {
          const series = await this.sonarrClient.getSeriesById(download.externalId);
          // Check if any season has episodes with files
          hasFiles = series.seasons.some(s => s.seasonNumber > 0);
        } else {
          const movie = await this.radarrClient.getMovieById(download.externalId);
          hasFiles = movie.hasFile;
        }

        if (hasFiles) {
          await this.handleDownloadComplete(download.contentId);
        }
      } catch (error) {
        // If item not found in Sonarr/Radarr, it might have been removed
        logger.warn(`Failed to check completion for download ${download.id}:`, error);
      }
    }
  }

  /**
   * Handle download completion
   */
  async handleDownloadComplete(contentId: number): Promise<void> {
    try {
      logger.info(`Handling download completion for content ${contentId}`);

      // Update download queue
      const queueEntry = await DownloadQueue.findOne({
        where: {
          contentId,
          status: ['queued', 'downloading']
        }
      });

      if (!queueEntry) {
        logger.warn(`No active download found for content ${contentId}`);
        return;
      }

      await queueEntry.update({
        status: 'completed',
        progressPercent: 100,
        completedAt: new Date()
      });

      // Scan library to add the new content
      await this.libraryService.scanLibraryFolder();

      // Schedule auto-delete (30 days from now)
      await this.scheduleAutoDelete(contentId, 30);

      logger.info(`Download completed for content ${contentId}`);
    } catch (error) {
      logger.error(`Failed to handle download completion for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Schedule auto-delete for content
   */
  async scheduleAutoDelete(contentId: number, daysUntilDelete = 30): Promise<void> {
    try {
      const scheduledDeleteAt = new Date();
      scheduledDeleteAt.setDate(scheduledDeleteAt.getDate() + daysUntilDelete);

      // Check if already scheduled
      const existing = await AutoDeleteSchedule.findOne({
        where: { contentId, deleted: false }
      });

      if (existing) {
        logger.info(`Auto-delete already scheduled for content ${contentId}`);
        return;
      }

      await AutoDeleteSchedule.create({
        contentId,
        scheduledDeleteAt
      });

      logger.info(`Auto-delete scheduled for content ${contentId} at ${scheduledDeleteAt.toISOString()}`);
    } catch (error) {
      logger.error(`Failed to schedule auto-delete for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Start polling for download progress
   */
  startPolling(intervalMs = 60000): void {
    if (this.pollingInterval) {
      logger.warn('Download polling already started');
      return;
    }

    logger.info(`Starting download polling (interval: ${intervalMs}ms)`);

    // Poll immediately
    this.pollDownloadProgress();

    // Then poll at interval
    this.pollingInterval = setInterval(() => {
      this.pollDownloadProgress();
    }, intervalMs);
  }

  /**
   * Stop polling for download progress
   */
  stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
      logger.info('Download polling stopped');
    }
  }

  /**
   * Get all active downloads
   */
  async getActiveDownloads(): Promise<DownloadStatus[]> {
    try {
      const activeDownloads = await DownloadQueue.findAll({
        where: {
          status: ['queued', 'downloading']
        },
        order: [['queuedAt', 'DESC']]
      });

      return activeDownloads.map(d => ({
        id: d.id,
        contentId: d.contentId,
        status: d.status,
        progressPercent: d.progressPercent,
        errorMessage: d.errorMessage,
        queuedAt: d.queuedAt.toISOString(),
        completedAt: d.completedAt?.toISOString()
      }));
    } catch (error) {
      logger.error('Failed to get active downloads:', error);
      throw error;
    }
  }
}

export default new DownloadManager();
