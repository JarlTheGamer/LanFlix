import DeviceToken from '../models/DeviceToken';
import AutoDeleteSchedule from '../models/AutoDeleteSchedule';
import Content from '../models/Content';
import WatchHistory from '../models/WatchHistory';
import logger from '../utils/logger';
import { Op } from 'sequelize';

interface PushNotificationPayload {
  title: string;
  body: string;
  data?: Record<string, any>;
  actions?: Array<{
    action: string;
    title: string;
    icon?: string;
  }>;
}

interface KeepWatchingNotification {
  contentId: number;
  contentTitle: string;
  contentType: 'movie' | 'series';
  scheduledDeleteAt: string;
}

/**
 * Service for managing push notifications
 * Handles Firebase Cloud Messaging, Web Push API, and keep-watching notifications
 */
export class NotificationService {
  private fcmServerKey?: string;
  private webPushPublicKey?: string;
  private webPushPrivateKey?: string;

  constructor() {
    // Initialize with environment variables if available
    this.fcmServerKey = process.env.FCM_SERVER_KEY;
    this.webPushPublicKey = process.env.WEB_PUSH_PUBLIC_KEY;
    this.webPushPrivateKey = process.env.WEB_PUSH_PRIVATE_KEY;

    if (!this.fcmServerKey) {
      logger.warn('FCM server key not configured - push notifications will not work');
    }
  }

  /**
   * Register a device token for push notifications
   */
  async registerDeviceToken(
    profileId: number,
    deviceToken: string,
    platform: 'android' | 'android-tv' | 'web'
  ): Promise<DeviceToken> {
    try {
      logger.info(`Registering device token for profile ${profileId}, platform: ${platform}`);

      // Check if token already exists
      const existing = await DeviceToken.findOne({
        where: { deviceToken }
      });

      if (existing) {
        // Update last used time and profile if changed
        await existing.update({
          profileId,
          platform,
          lastUsedAt: new Date()
        });
        logger.info(`Device token updated: ${existing.id}`);
        return existing;
      }

      // Create new token
      const token = await DeviceToken.create({
        profileId,
        deviceToken,
        platform
      });

      logger.info(`Device token registered: ${token.id}`);
      return token;
    } catch (error) {
      logger.error('Failed to register device token:', error);
      throw error;
    }
  }

  /**
   * Unregister a device token
   */
  async unregisterDeviceToken(deviceToken: string): Promise<void> {
    try {
      const token = await DeviceToken.findOne({
        where: { deviceToken }
      });

      if (token) {
        await token.destroy();
        logger.info(`Device token unregistered: ${token.id}`);
      }
    } catch (error) {
      logger.error('Failed to unregister device token:', error);
      throw error;
    }
  }

  /**
   * Send push notification to a profile
   */
  async sendPushNotification(
    profileId: number,
    payload: PushNotificationPayload
  ): Promise<{ sent: number; failed: number }> {
    try {
      logger.info(`Sending push notification to profile ${profileId}`);

      // Get all device tokens for the profile
      const tokens = await DeviceToken.findAll({
        where: { profileId }
      });

      if (tokens.length === 0) {
        logger.warn(`No device tokens found for profile ${profileId}`);
        return { sent: 0, failed: 0 };
      }

      let sent = 0;
      let failed = 0;

      // Send to each device
      for (const token of tokens) {
        try {
          if (token.platform === 'web') {
            await this.sendWebPushNotification(token.deviceToken, payload);
          } else {
            await this.sendFCMNotification(token.deviceToken, payload);
          }
          
          // Update last used time
          await token.update({ lastUsedAt: new Date() });
          sent++;
        } catch (error) {
          logger.error(`Failed to send notification to device ${token.id}:`, error);
          failed++;
        }
      }

      logger.info(`Push notifications sent: ${sent} successful, ${failed} failed`);
      return { sent, failed };
    } catch (error) {
      logger.error('Failed to send push notifications:', error);
      throw error;
    }
  }

  /**
   * Send Firebase Cloud Messaging notification
   */
  private async sendFCMNotification(
    deviceToken: string,
    payload: PushNotificationPayload
  ): Promise<void> {
    if (!this.fcmServerKey) {
      throw new Error('FCM server key not configured');
    }

    // Note: In production, you would use the official Firebase Admin SDK
    // This is a simplified implementation showing the structure
    logger.info('FCM notification would be sent here', {
      deviceToken: deviceToken.substring(0, 20) + '...',
      payload
    });

    // Example FCM payload structure:
    const fcmPayload = {
      to: deviceToken,
      notification: {
        title: payload.title,
        body: payload.body
      },
      data: payload.data || {},
      android: {
        priority: 'high',
        notification: {
          sound: 'default',
          click_action: 'FLUTTER_NOTIFICATION_CLICK'
        }
      }
    };

    // In production, send via FCM API:
    // await axios.post('https://fcm.googleapis.com/fcm/send', fcmPayload, {
    //   headers: {
    //     'Authorization': `key=${this.fcmServerKey}`,
    //     'Content-Type': 'application/json'
    //   }
    // });

    logger.debug('FCM notification prepared', fcmPayload);
  }

  /**
   * Send Web Push notification
   */
  private async sendWebPushNotification(
    subscription: string,
    payload: PushNotificationPayload
  ): Promise<void> {
    if (!this.webPushPublicKey || !this.webPushPrivateKey) {
      throw new Error('Web Push keys not configured');
    }

    // Note: In production, you would use the web-push library
    // This is a simplified implementation showing the structure
    logger.info('Web Push notification would be sent here', {
      subscription: subscription.substring(0, 20) + '...',
      payload
    });

    // Example Web Push implementation:
    // const webpush = require('web-push');
    // webpush.setVapidDetails(
    //   'mailto:your-email@example.com',
    //   this.webPushPublicKey,
    //   this.webPushPrivateKey
    // );
    // await webpush.sendNotification(
    //   JSON.parse(subscription),
    //   JSON.stringify(payload)
    // );

    logger.debug('Web Push notification prepared', payload);
  }

  /**
   * Send keep-watching notification (7 days before deletion)
   */
  async sendKeepWatchingPrompt(
    profileId: number,
    contentId: number,
    contentTitle: string
  ): Promise<void> {
    try {
      logger.info(`Sending keep-watching prompt for content ${contentId} to profile ${profileId}`);

      const content = await Content.findByPk(contentId);
      if (!content) {
        throw new Error(`Content not found: ${contentId}`);
      }

      const schedule = await AutoDeleteSchedule.findOne({
        where: { contentId, deleted: false }
      });

      if (!schedule) {
        throw new Error(`No auto-delete schedule found for content ${contentId}`);
      }

      const payload: PushNotificationPayload = {
        title: 'Keep watching?',
        body: `"${contentTitle}" will be deleted in 7 days. Tap to keep it.`,
        data: {
          type: 'keep_watching',
          contentId,
          contentTitle,
          contentType: content.type,
          scheduledDeleteAt: schedule.scheduledDeleteAt.toISOString()
        },
        actions: [
          {
            action: 'keep',
            title: '👍 Keep',
            icon: 'thumbs_up'
          },
          {
            action: 'delete',
            title: '👎 Delete',
            icon: 'thumbs_down'
          }
        ]
      };

      await this.sendPushNotification(profileId, payload);

      // Mark notification as sent
      await schedule.update({
        notificationSent: true,
        notificationSentAt: new Date()
      });

      logger.info(`Keep-watching prompt sent for content ${contentId}`);
    } catch (error) {
      logger.error(`Failed to send keep-watching prompt for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Handle keep-watching notification response
   */
  async handleKeepWatchingResponse(
    contentId: number,
    profileId: number,
    keepContent: boolean
  ): Promise<void> {
    try {
      logger.info(`Handling keep-watching response for content ${contentId}: ${keepContent ? 'keep' : 'delete'}`);

      const schedule = await AutoDeleteSchedule.findOne({
        where: { contentId, deleted: false }
      });

      if (!schedule) {
        throw new Error(`No auto-delete schedule found for content ${contentId}`);
      }

      if (keepContent) {
        // User wants to keep the content - cancel auto-delete
        await schedule.update({
          userResponse: 'keep',
          responseAt: new Date()
        });
        logger.info(`Auto-delete cancelled for content ${contentId}`);
      } else {
        // User wants to delete - mark for immediate deletion
        await schedule.update({
          userResponse: 'delete',
          responseAt: new Date(),
          scheduledDeleteAt: new Date() // Delete immediately
        });
        logger.info(`Content ${contentId} marked for immediate deletion`);
      }
    } catch (error) {
      logger.error(`Failed to handle keep-watching response for content ${contentId}:`, error);
      throw error;
    }
  }

  /**
   * Check and send keep-watching notifications (7 days before deletion)
   */
  async checkAndSendKeepWatchingNotifications(): Promise<number> {
    try {
      logger.info('Checking for keep-watching notifications to send');

      const now = new Date();
      const sevenDaysFromNow = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

      // Find schedules that need notification (7 days before deletion)
      const schedules = await AutoDeleteSchedule.findAll({
        where: {
          deleted: false,
          notificationSent: false,
          scheduledDeleteAt: {
            [Op.lte]: sevenDaysFromNow,
            [Op.gt]: now
          }
        },
        include: [{ model: Content, as: 'content' }]
      });

      if (schedules.length === 0) {
        logger.info('No keep-watching notifications to send');
        return 0;
      }

      let sent = 0;

      for (const schedule of schedules) {
        try {
          const content = (schedule as any).content;
          if (!content) {
            logger.warn(`Content not found for schedule ${schedule.id}`);
            continue;
          }

          // Find profiles that have watched this content
          const watchHistory = await WatchHistory.findAll({
            where: { contentId: content.id },
            attributes: ['profileId'],
            group: ['profileId']
          });

          const profileIds = watchHistory.map(wh => wh.profileId);

          if (profileIds.length === 0) {
            logger.debug(`No profiles have watched content ${content.id}`);
            continue;
          }

          // Send notification to each profile
          for (const profileId of profileIds) {
            try {
              await this.sendKeepWatchingPrompt(profileId, content.id, content.title);
              sent++;
            } catch (error) {
              logger.error(`Failed to send keep-watching prompt to profile ${profileId}:`, error);
            }
          }
        } catch (error) {
          logger.error(`Failed to process schedule ${schedule.id}:`, error);
        }
      }

      logger.info(`Keep-watching notifications sent: ${sent}`);
      return sent;
    } catch (error) {
      logger.error('Failed to check and send keep-watching notifications:', error);
      throw error;
    }
  }

  /**
   * Get notification history for a profile
   */
  async getNotificationHistory(
    profileId: number,
    limit = 50
  ): Promise<KeepWatchingNotification[]> {
    try {
      const schedules = await AutoDeleteSchedule.findAll({
        where: {
          notificationSent: true
        },
        include: [
          {
            model: Content,
            as: 'content',
            required: true,
            include: [
              {
                model: WatchHistory,
                as: 'watchHistory',
                where: { profileId },
                required: true
              }
            ]
          }
        ],
        order: [['notificationSentAt', 'DESC']],
        limit
      });

      return schedules.map(schedule => {
        const content = (schedule as any).content;
        return {
          contentId: content.id,
          contentTitle: content.title,
          contentType: content.type,
          scheduledDeleteAt: schedule.scheduledDeleteAt.toISOString()
        };
      });
    } catch (error) {
      logger.error(`Failed to get notification history for profile ${profileId}:`, error);
      throw error;
    }
  }

  /**
   * Clean up old device tokens (not used in 90 days)
   */
  async cleanupOldDeviceTokens(): Promise<number> {
    try {
      const ninetyDaysAgo = new Date();
      ninetyDaysAgo.setDate(ninetyDaysAgo.getDate() - 90);

      const deleted = await DeviceToken.destroy({
        where: {
          lastUsedAt: {
            [Op.lt]: ninetyDaysAgo
          }
        }
      });

      if (deleted > 0) {
        logger.info(`Cleaned up ${deleted} old device tokens`);
      }

      return deleted;
    } catch (error) {
      logger.error('Failed to cleanup old device tokens:', error);
      throw error;
    }
  }
}

export default new NotificationService();
