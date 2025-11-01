export { TMDBClient } from './tmdb.client';
export { SonarrClient } from './sonarr.client';
export { RadarrClient } from './radarr.client';
export { ProwlarrClient } from './prowlarr.client';

import { TMDBClient } from './tmdb.client';
import { SonarrClient } from './sonarr.client';
import { RadarrClient } from './radarr.client';
import { ProwlarrClient } from './prowlarr.client';
import { Settings } from '../models';
import logger from '../utils/logger';

// Create singleton instances
export const tmdbClient = new TMDBClient();
export const sonarrClient = new SonarrClient();
export const radarrClient = new RadarrClient();
export const prowlarrClient = new ProwlarrClient();

/**
 * Load API keys from database and update clients
 */
export async function loadApiKeysFromDatabase(): Promise<void> {
  try {
    const settings = await Settings.findAll();
    const settingsMap = new Map<string, string>();
    
    settings.forEach(setting => {
      settingsMap.set(setting.key, setting.value);
    });

    // Update TMDB API key if found in database
    const tmdbApiKey = settingsMap.get('tmdbApiKey');
    if (tmdbApiKey) {
      const trimmedKey = tmdbApiKey.trim();
      if (trimmedKey.length === 0) {
        logger.warn('TMDB API key in database is empty');
      } else if (trimmedKey.length < 20) {
        logger.warn(`TMDB API key in database appears invalid (length: ${trimmedKey.length})`);
      } else {
        tmdbClient.updateApiKey(trimmedKey);
        logger.info(`Loaded TMDB API key from database (length: ${trimmedKey.length})`);
      }
    }

    // Update Sonarr configuration
    const sonarrUrl = settingsMap.get('sonarrUrl');
    const sonarrApiKey = settingsMap.get('sonarrApiKey');
    if (sonarrUrl || sonarrApiKey) {
      sonarrClient.updateConfig(
        sonarrUrl?.trim() || undefined,
        sonarrApiKey?.trim() || undefined
      );
      logger.info('Loaded Sonarr configuration from database');
    }

    // Update Radarr configuration
    const radarrUrl = settingsMap.get('radarrUrl');
    const radarrApiKey = settingsMap.get('radarrApiKey');
    if (radarrUrl || radarrApiKey) {
      radarrClient.updateConfig(
        radarrUrl?.trim() || undefined,
        radarrApiKey?.trim() || undefined
      );
      logger.info('Loaded Radarr configuration from database');
    }

    // Update Prowlarr configuration
    const prowlarrUrl = settingsMap.get('prowlarrUrl');
    const prowlarrApiKey = settingsMap.get('prowlarrApiKey');
    if (prowlarrUrl || prowlarrApiKey) {
      prowlarrClient.updateConfig(
        prowlarrUrl?.trim() || undefined,
        prowlarrApiKey?.trim() || undefined
      );
      logger.info('Loaded Prowlarr configuration from database');
    }
  } catch (error) {
    logger.error('Failed to load API keys from database:', error);
  }
}
