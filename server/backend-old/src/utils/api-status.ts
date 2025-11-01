import { config } from '../config/env';
import logger from './logger';

interface ApiStatus {
  tmdb: boolean;
  radarr: boolean;
  sonarr: boolean;
  prowlarr: boolean;
}

class ApiStatusChecker {
  private status: ApiStatus = {
    tmdb: false,
    radarr: false,
    sonarr: false,
    prowlarr: false
  };

  private lastCheck: number = 0;
  private checkInterval = 60000; // Check every 60 seconds

  /**
   * Check if API keys are configured
   */
  checkApiConfiguration(): ApiStatus {
    const now = Date.now();
    
    // Only check once per minute to avoid overhead
    if (now - this.lastCheck < this.checkInterval) {
      return this.status;
    }

    this.status = {
      tmdb: !!config.externalServices.tmdb.apiKey,
      radarr: !!config.externalServices.radarr.apiKey,
      sonarr: !!config.externalServices.sonarr.apiKey,
      prowlarr: !!config.externalServices.prowlarr.apiKey
    };

    this.lastCheck = now;

    // Log warning if any APIs are not configured
    const missingApis = Object.entries(this.status)
      .filter(([_, configured]) => !configured)
      .map(([api]) => api.toUpperCase());

    if (missingApis.length > 0) {
      logger.warn(`API keys not configured: ${missingApis.join(', ')} - Running in limited mode`);
    }

    return this.status;
  }

  /**
   * Check if system is in offline/limited mode
   */
  isOfflineMode(): boolean {
    const status = this.checkApiConfiguration();
    return !status.tmdb; // TMDB is the primary API for content discovery
  }

  /**
   * Get status message for client
   */
  getStatusMessage(): string | null {
    if (this.isOfflineMode()) {
      return 'Server running in limited mode - API keys not configured. Library browsing available.';
    }
    return null;
  }
}

export const apiStatusChecker = new ApiStatusChecker();
