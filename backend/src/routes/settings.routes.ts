import { Router, Request, Response, NextFunction } from 'express';
import { Settings } from '../models';
import { validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';
import { SonarrClient, RadarrClient, ProwlarrClient, TMDBClient, sonarrClient, radarrClient, prowlarrClient, tmdbClient } from '../clients';
import logger from '../utils/logger';

const router = Router();

/**
 * GET /api/settings
 * Retrieve all settings
 */
router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const settings = await Settings.findAll();

    // Convert to key-value object
    const settingsObject: Record<string, any> = {};
    settings.forEach(setting => {
      try {
        // Try to parse JSON values
        settingsObject[setting.key] = JSON.parse(setting.value);
      } catch {
        // If not JSON, use as string
        settingsObject[setting.key] = setting.value;
      }
    });

    res.json({
      settings: settingsObject
    });
  } catch (error) {
    next(error);
  }
});

/**
 * PUT /api/settings
 * Update settings
 */
router.put('/', validateBody(['settings']), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { settings } = req.body;

    if (typeof settings !== 'object' || Array.isArray(settings)) {
      const error: ApiError = new Error('Settings must be an object');
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }

    // Validate specific settings
    const validKeys = [
      'language',
      'timezone',
      'region',
      'videoQuality',
      'dataSaverMode',
      'audioLanguage',
      'theme',
      'visualEffects',
      'autoplay',
      'subtitlesEnabled',
      'subtitleLanguage'
    ];

    const invalidKeys = Object.keys(settings).filter(key => !validKeys.includes(key));
    if (invalidKeys.length > 0) {
      logger.warn(`Invalid setting keys provided: ${invalidKeys.join(', ')}`);
    }

    // Update or create settings
    const updatePromises = Object.entries(settings).map(async ([key, value]) => {
      if (!validKeys.includes(key)) return;

      const stringValue = typeof value === 'string' ? value : JSON.stringify(value);

      const [setting] = await Settings.findOrCreate({
        where: { key },
        defaults: { key, value: stringValue }
      });

      if (setting.value !== stringValue) {
        setting.value = stringValue;
        setting.updatedAt = new Date();
        await setting.save();
      }
    });

    await Promise.all(updatePromises);

    res.json({
      message: 'Settings updated successfully'
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/settings/services
 * Get external service connection status
 */
router.get('/services', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const services = {
      sonarr: { connected: false, error: null as string | null },
      radarr: { connected: false, error: null as string | null },
      prowlarr: { connected: false, error: null as string | null },
      tmdb: { connected: false, error: null as string | null }
    };

    // Test Sonarr connection
    try {
      await sonarrClient.testConnection();
      services.sonarr.connected = true;
    } catch (error: any) {
      services.sonarr.error = error.message;
      logger.error('Sonarr connection test failed:', error);
    }

    // Test Radarr connection
    try {
      await radarrClient.testConnection();
      services.radarr.connected = true;
    } catch (error: any) {
      services.radarr.error = error.message;
      logger.error('Radarr connection test failed:', error);
    }

    // Test Prowlarr connection
    try {
      await prowlarrClient.testConnection();
      services.prowlarr.connected = true;
    } catch (error: any) {
      services.prowlarr.error = error.message;
      logger.error('Prowlarr connection test failed:', error);
    }

    // Test TMDB connection
    try {
      await tmdbClient.testConnection();
      services.tmdb.connected = true;
    } catch (error: any) {
      services.tmdb.error = error.message;
      logger.error('TMDB connection test failed:', error);
    }

    res.json({
      services
    });
  } catch (error) {
    next(error);
  }
});

export default router;
