import { Router, Request, Response, NextFunction } from 'express';
import { DeviceToken, Profile, AutoDeleteSchedule, Content } from '../models';
import { validatePathParam, validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';
import logger from '../utils/logger';

const router = Router();

/**
 * POST /api/notifications/register
 * Register device for push notifications
 */
router.post(
  '/register',
  validateBody(['profileId', 'deviceToken', 'platform']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const { profileId, deviceToken, platform } = req.body;

      // Verify profile exists
      const profile = await Profile.findByPk(profileId);
      if (!profile) {
        const error: ApiError = new Error('Profile not found');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }

      // Validate platform
      if (!['android', 'android-tv', 'web'].includes(platform)) {
        const error: ApiError = new Error('Platform must be one of: android, android-tv, web');
        error.statusCode = 400;
        error.code = 'VALIDATION_ERROR';
        return next(error);
      }

      // Check if device token already exists
      let device = await DeviceToken.findOne({
        where: { deviceToken }
      });

      if (device) {
        // Update existing device token
        device.profileId = profileId;
        device.platform = platform;
        device.lastUsedAt = new Date();
        await device.save();

        return res.json({
          message: 'Device token updated',
          device: {
            id: device.id,
            profileId: device.profileId,
            platform: device.platform,
            registeredAt: device.registeredAt,
            lastUsedAt: device.lastUsedAt
          }
        });
      }

      // Create new device token
      device = await DeviceToken.create({
        profileId,
        deviceToken,
        platform
      });

      res.status(201).json({
        message: 'Device registered successfully',
        device: {
          id: device.id,
          profileId: device.profileId,
          platform: device.platform,
          registeredAt: device.registeredAt,
          lastUsedAt: device.lastUsedAt
        }
      });
    } catch (error) {
      next(error);
    }
  }
);

/**
 * POST /api/notifications/:id/respond
 * Respond to keep-watching notification
 */
router.post(
  '/:id/respond',
  validatePathParam('id'),
  validateBody(['response']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const scheduleId = parseInt(req.params.id, 10);
      const { response } = req.body;

      // Validate response
      if (!['keep', 'delete'].includes(response)) {
        const error: ApiError = new Error('Response must be either "keep" or "delete"');
        error.statusCode = 400;
        error.code = 'VALIDATION_ERROR';
        return next(error);
      }

      // Find the auto-delete schedule
      const schedule = await AutoDeleteSchedule.findByPk(scheduleId, {
        include: [{
          model: Content,
          as: 'content'
        }]
      });

      if (!schedule) {
        const error: ApiError = new Error('Auto-delete schedule not found');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }

      // Update the schedule with user response
      schedule.userResponse = response as 'keep' | 'delete';
      schedule.responseAt = new Date();
      await schedule.save();

      logger.info(`User responded to keep-watching notification for content ${schedule.contentId}: ${response}`);

      res.json({
        message: 'Response recorded successfully',
        schedule: {
          id: schedule.id,
          contentId: schedule.contentId,
          userResponse: schedule.userResponse,
          responseAt: schedule.responseAt
        }
      });
    } catch (error) {
      next(error);
    }
  }
);

/**
 * GET /api/notifications/:profileId
 * Get notification history for a profile
 */
router.get('/:profileId', validatePathParam('profileId'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const profileId = parseInt(req.params.profileId, 10);

    // Verify profile exists
    const profile = await Profile.findByPk(profileId);
    if (!profile) {
      const error: ApiError = new Error('Profile not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    // Get all auto-delete schedules where notification was sent
    // This is a simplified version - in a real app, you'd have a separate notifications table
    const schedules = await AutoDeleteSchedule.findAll({
      where: {
        notificationSent: true
      },
      include: [{
        model: Content,
        as: 'content',
        required: true
      }],
      order: [['notificationSentAt', 'DESC']],
      limit: 50
    });

    res.json({
      profileId,
      count: schedules.length,
      notifications: schedules.map(schedule => ({
        id: schedule.id,
        type: 'keep_watching',
        contentId: schedule.contentId,
        contentTitle: (schedule as any).content?.title,
        scheduledDeleteAt: schedule.scheduledDeleteAt,
        notificationSentAt: schedule.notificationSentAt,
        userResponse: schedule.userResponse,
        responseAt: schedule.responseAt
      }))
    });
  } catch (error) {
    next(error);
  }
});

export default router;
