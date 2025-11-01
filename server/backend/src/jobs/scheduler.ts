import cron from 'node-cron';
import logger from '../utils/logger';
import downloadManager from '../services/download-manager.service';
import libraryService from '../services/library.service';
import metadataService from '../services/metadata.service';
import notificationService from '../services/notification.service';
import { cacheManager } from '../utils/cache-manager';
import Content from '../models/Content';
import AutoDeleteSchedule from '../models/AutoDeleteSchedule';
import { Op } from 'sequelize';

/**
 * Job scheduler for background tasks
 * Manages scheduled jobs for downloads, auto-delete, metadata refresh, library scanning, and cache cleanup
 */
export class JobScheduler {
  private jobs: Map<string, cron.ScheduledTask>;
  private isRunning: boolean;

  constructor() {
    this.jobs = new Map();
    this.isRunning = false;
  }

  /**
   * Start all scheduled jobs
   */
  start(): void {
    if (this.isRunning) {
      logger.warn('Job scheduler already running');
      return;
    }

    logger.info('Starting job scheduler');

    // 1. Download queue polling job (every 60 seconds)
    this.scheduleDownloadQueuePolling();

    // 2. Auto-delete check job (daily at 2 AM)
    this.scheduleAutoDeleteCheck();

    // 3. Metadata refresh job (daily at 3 AM for stale content)
    this.scheduleMetadataRefresh();

    // 4. Library scan job (every 6 hours)
    this.scheduleLibraryScan();

    // 5. Cache cleanup job (every hour)
    this.scheduleCacheCleanup();

    // 6. Keep-watching notification check (daily at 10 AM)
    this.scheduleKeepWatchingNotifications();

    this.isRunning = true;
    logger.info('Job scheduler started successfully');
  }

  /**
   * Stop all scheduled jobs
   */
  stop(): void {
    if (!this.isRunning) {
      logger.warn('Job scheduler not running');
      return;
    }

    logger.info('Stopping job scheduler');

    for (const [name, job] of this.jobs.entries()) {
      job.stop();
      logger.debug(`Stopped job: ${name}`);
    }

    this.jobs.clear();
    this.isRunning = false;
    logger.info('Job scheduler stopped');
  }

  /**
   * Schedule download queue polling job (every 60 seconds)
   */
  private scheduleDownloadQueuePolling(): void {
    const jobName = 'download-queue-polling';
    
    // Run every 60 seconds
    const job = cron.schedule('*/60 * * * * *', async () => {
      try {
        logger.debug('Running download queue polling job');
        await downloadManager.pollDownloadProgress();
      } catch (error) {
        logger.error('Download queue polling job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (every 60 seconds)`);

    // Run immediately on startup
    this.runJobImmediately(jobName, async () => {
      await downloadManager.pollDownloadProgress();
    });
  }

  /**
   * Schedule auto-delete check job (daily at 2 AM)
   */
  private scheduleAutoDeleteCheck(): void {
    const jobName = 'auto-delete-check';
    
    // Run daily at 2 AM
    const job = cron.schedule('0 2 * * *', async () => {
      try {
        logger.info('Running auto-delete check job');
        await this.processAutoDelete();
      } catch (error) {
        logger.error('Auto-delete check job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (daily at 2 AM)`);
  }

  /**
   * Schedule metadata refresh job (daily at 3 AM for stale content)
   */
  private scheduleMetadataRefresh(): void {
    const jobName = 'metadata-refresh';
    
    // Run daily at 3 AM
    const job = cron.schedule('0 3 * * *', async () => {
      try {
        logger.info('Running metadata refresh job');
        await this.refreshStaleMetadata();
      } catch (error) {
        logger.error('Metadata refresh job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (daily at 3 AM)`);
  }

  /**
   * Schedule library scan job (every 6 hours)
   */
  private scheduleLibraryScan(): void {
    const jobName = 'library-scan';
    
    // Run every 6 hours (at 0:00, 6:00, 12:00, 18:00)
    const job = cron.schedule('0 */6 * * *', async () => {
      try {
        logger.info('Running library scan job');
        await this.scanLibrary();
      } catch (error) {
        logger.error('Library scan job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (every 6 hours)`);

    // Run immediately on startup
    this.runJobImmediately(jobName, async () => {
      await this.scanLibrary();
    });
  }

  /**
   * Schedule cache cleanup job (every hour)
   */
  private scheduleCacheCleanup(): void {
    const jobName = 'cache-cleanup';
    
    // Run every hour
    const job = cron.schedule('0 * * * *', async () => {
      try {
        logger.debug('Running cache cleanup job');
        await this.cleanupCache();
      } catch (error) {
        logger.error('Cache cleanup job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (every hour)`);
  }

  /**
   * Schedule keep-watching notification check (daily at 10 AM)
   */
  private scheduleKeepWatchingNotifications(): void {
    const jobName = 'keep-watching-notifications';
    
    // Run daily at 10 AM
    const job = cron.schedule('0 10 * * *', async () => {
      try {
        logger.info('Running keep-watching notifications job');
        await notificationService.checkAndSendKeepWatchingNotifications();
      } catch (error) {
        logger.error('Keep-watching notifications job failed:', error);
      }
    });

    this.jobs.set(jobName, job);
    logger.info(`Scheduled job: ${jobName} (daily at 10 AM)`);
  }

  /**
   * Process auto-delete for content
   */
  private async processAutoDelete(): Promise<void> {
    try {
      const now = new Date();

      // Find content scheduled for deletion (no keep response)
      const schedules = await AutoDeleteSchedule.findAll({
        where: {
          deleted: false,
          scheduledDeleteAt: {
            [Op.lte]: now
          },
          userResponse: {
            [Op.ne]: 'keep'
          }
        },
        include: [{ model: Content, as: 'content' }]
      });

      if (schedules.length === 0) {
        logger.info('No content scheduled for deletion');
        return;
      }

      logger.info(`Processing ${schedules.length} content items for auto-delete`);

      let deleted = 0;
      let failed = 0;

      for (const schedule of schedules) {
        try {
          const content = (schedule as any).content;
          if (!content) {
            logger.warn(`Content not found for schedule ${schedule.id}`);
            continue;
          }

          logger.info(`Deleting content: ${content.title} (ID: ${content.id})`);

          // Remove from library (with file deletion)
          await libraryService.removeFromLibrary(content.id, true);

          // Mark as deleted in schedule
          await schedule.update({
            deleted: true,
            deletedAt: new Date()
          });

          deleted++;
          logger.info(`Content deleted: ${content.title}`);
        } catch (error) {
          logger.error(`Failed to delete content for schedule ${schedule.id}:`, error);
          failed++;
        }
      }

      logger.info(`Auto-delete completed: ${deleted} deleted, ${failed} failed`);
    } catch (error) {
      logger.error('Failed to process auto-delete:', error);
      throw error;
    }
  }

  /**
   * Refresh stale metadata (older than 7 days)
   */
  private async refreshStaleMetadata(): Promise<void> {
    try {
      const sevenDaysAgo = new Date();
      sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

      // Find content with stale metadata
      const staleContent = await Content.findAll({
        where: {
          updatedAt: {
            [Op.lt]: sevenDaysAgo
          }
        }
      });

      if (staleContent.length === 0) {
        logger.info('No stale metadata found');
        return;
      }

      logger.info(`Refreshing metadata for ${staleContent.length} content items`);

      let refreshed = 0;
      let failed = 0;

      for (const content of staleContent) {
        try {
          await metadataService.refreshMetadata(content.id);
          refreshed++;
        } catch (error) {
          logger.error(`Failed to refresh metadata for content ${content.id}:`, error);
          failed++;
        }
      }

      logger.info(`Metadata refresh completed: ${refreshed} refreshed, ${failed} failed`);
    } catch (error) {
      logger.error('Failed to refresh stale metadata:', error);
      throw error;
    }
  }

  /**
   * Scan library for new content
   */
  private async scanLibrary(): Promise<void> {
    try {
      const result = await libraryService.scanLibraryFolder();
      logger.info(`Library scan completed: ${result.added} added, ${result.updated} updated, ${result.errors.length} errors`);
      
      if (result.errors.length > 0) {
        logger.warn('Library scan errors:', result.errors);
      }
    } catch (error) {
      logger.error('Failed to scan library:', error);
      throw error;
    }
  }

  /**
   * Cleanup expired cache entries
   */
  private async cleanupCache(): Promise<void> {
    try {
      // The cache manager already has automatic cleanup for memory cache
      // This job can be used for additional cleanup tasks if needed
      
      const stats = cacheManager.getStats();
      logger.debug(`Cache stats: ${stats.memorySize} entries in memory, Redis connected: ${stats.redisConnected}`);

      // Clean up old device tokens (not used in 90 days)
      const deletedTokens = await notificationService.cleanupOldDeviceTokens();
      if (deletedTokens > 0) {
        logger.info(`Cleaned up ${deletedTokens} old device tokens`);
      }
    } catch (error) {
      logger.error('Failed to cleanup cache:', error);
      throw error;
    }
  }

  /**
   * Run a job immediately (used for startup jobs)
   */
  private runJobImmediately(jobName: string, jobFn: () => Promise<void>): void {
    logger.info(`Running ${jobName} immediately on startup`);
    
    // Run in background without blocking
    jobFn().catch(error => {
      logger.error(`Failed to run ${jobName} on startup:`, error);
    });
  }

  /**
   * Get job status
   */
  getStatus(): {
    isRunning: boolean;
    jobs: string[];
  } {
    return {
      isRunning: this.isRunning,
      jobs: Array.from(this.jobs.keys())
    };
  }

  /**
   * Manually trigger a specific job
   */
  async triggerJob(jobName: string): Promise<void> {
    logger.info(`Manually triggering job: ${jobName}`);

    switch (jobName) {
      case 'download-queue-polling':
        await downloadManager.pollDownloadProgress();
        break;
      case 'auto-delete-check':
        await this.processAutoDelete();
        break;
      case 'metadata-refresh':
        await this.refreshStaleMetadata();
        break;
      case 'library-scan':
        await this.scanLibrary();
        break;
      case 'cache-cleanup':
        await this.cleanupCache();
        break;
      case 'keep-watching-notifications':
        await notificationService.checkAndSendKeepWatchingNotifications();
        break;
      default:
        throw new Error(`Unknown job: ${jobName}`);
    }

    logger.info(`Job completed: ${jobName}`);
  }
}

// Export singleton instance
export const jobScheduler = new JobScheduler();

