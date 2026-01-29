/**
 * API Client Module
 * Handles all backend communication with typed methods for all API endpoints
 */

class ApiClient {
  constructor(baseURL = null) {
    // Dynamic base URL - will be loaded from config
    this.baseURL = baseURL || this.getBaseURL();
    this.authToken = null;
    this.retryAttempts = 0; // No retries - fail fast
    this.retryDelay = 1000; // ms
    this.isOffline = false;
    this.offlineRetryInterval = 10 * 60 * 1000; // 10 minutes
    this.lastOfflineCheck = null;
    this.offlineCheckTimer = null;
  }

  /**
   * Get base URL from app config or default
   */
  getBaseURL() {
    try {
      const config = localStorage.getItem('lanflix_config');
      if (config) {
        const parsed = JSON.parse(config);
        if (parsed.backendUrl) {
          return `${parsed.backendUrl}/api`;
        }
      }
    } catch (e) {
      console.warn('Failed to load backend URL from config:', e);
    }
    // Default fallback - use relative URL for web, works with proxy
    // For native apps, this will be configured via app-config.html
    return '/api';
  }

  /**
   * Update base URL (call this when user changes backend URL in settings)
   */
  setBaseURL(url) {
    this.baseURL = `${url}/api`;
  }

  /**
   * Set authentication token
   */
  setAuthToken(token) {
    this.authToken = token;
    if (token) {
      localStorage.setItem('authToken', token);
    } else {
      localStorage.removeItem('authToken');
    }
  }

  /**
   * Load auth token from storage
   */
  loadAuthToken() {
    const token = localStorage.getItem('authToken');
    if (token) {
      this.authToken = token;
    }
  }

  /**
   * Make HTTP request with retry logic and error handling
   */
  async request(endpoint, options = {}, retryCount = 0) {
    const url = `${this.baseURL}${endpoint}`;

    const headers = {
      'Content-Type': 'application/json',
      ...options.headers
    };

    if (this.authToken) {
      headers['Authorization'] = `Bearer ${this.authToken}`;
    }

    const config = {
      ...options,
      headers
    };

    try {
      const response = await fetch(url, config);

      // Handle non-OK responses
      if (!response.ok) {
        let errorData = {};
        const contentType = response.headers.get('content-type');

        // Only try to parse JSON if the response is actually JSON
        if (contentType && contentType.includes('application/json')) {
          errorData = await response.json().catch(() => ({}));
        } else {
          // If we got HTML or other non-JSON response, it's likely a routing error
          const text = await response.text().catch(() => '');
          if (text.includes('<!DOCTYPE') || text.includes('<html')) {
            errorData = {
              error: {
                message: `API endpoint not found or returned HTML instead of JSON. Check server routing configuration.`,
                code: 'ROUTING_ERROR'
              }
            };
          }
        }

        const errorMessage = errorData.error?.message || `HTTP ${response.status}: ${response.statusText}`;
        const error = new Error(errorMessage);
        error.statusCode = response.status;
        error.code = errorData.error?.code || 'HTTP_ERROR';
        error.details = errorData.error?.details;

        // Log detailed error information for debugging
        console.error(`API Error [${endpoint}]:`, {
          status: response.status,
          message: errorMessage,
          code: error.code,
          details: error.details
        });

        throw error;
      }

      // Successful response - mark as online
      this.markOnline();

      // Parse JSON response
      const data = await response.json();

      // Check for server status message (offline mode)
      if (data._serverStatus && data._serverStatus.offlineMode) {
        console.warn('⚠️ Server Status:', data._serverStatus.message);

        // Dispatch event for UI to show notification
        window.dispatchEvent(new CustomEvent('server-limited-mode', {
          detail: { message: data._serverStatus.message }
        }));
      }

      // Return parsed JSON
      return data;
    } catch (error) {
      // Retry logic for network errors
      if (retryCount < this.retryAttempts && this.isRetryableError(error)) {
        await this.delay(this.retryDelay * Math.pow(2, retryCount));
        return this.request(endpoint, options, retryCount + 1);
      }

      // Mark as offline after all retries failed
      this.markOffline();

      // Log error
      console.error(`API Error [${endpoint}]:`, error);
      throw error;
    }
  }

  /**
   * Mark API as offline and schedule retry
   */
  markOffline() {
    if (!this.isOffline) {
      this.isOffline = true;
      this.lastOfflineCheck = Date.now();
      console.warn('🔴 API is offline - switching to local mode');

      // Dispatch event for UI to react
      window.dispatchEvent(new CustomEvent('api-offline'));

      // Schedule automatic retry
      this.scheduleOfflineRetry();
    }
  }

  /**
   * Mark API as online and clear retry timer
   */
  markOnline() {
    if (this.isOffline) {
      this.isOffline = false;
      console.log('🟢 API is back online');

      // Dispatch event for UI to react
      window.dispatchEvent(new CustomEvent('api-online'));

      // Clear retry timer
      if (this.offlineCheckTimer) {
        clearTimeout(this.offlineCheckTimer);
        this.offlineCheckTimer = null;
      }
    }
  }

  /**
   * Schedule automatic retry when offline
   */
  scheduleOfflineRetry() {
    // Clear existing timer
    if (this.offlineCheckTimer) {
      clearTimeout(this.offlineCheckTimer);
    }

    // Schedule next check
    this.offlineCheckTimer = setTimeout(async () => {
      console.log('🔄 Attempting to reconnect to API...');
      await this.checkConnection();
    }, this.offlineRetryInterval);
  }

  /**
   * Check if API is available
   */
  async checkConnection() {
    try {
      // Try a lightweight endpoint - use GET instead of HEAD since SettingsController doesn't support HEAD
      const response = await fetch(`${this.baseURL}/settings/debug/count`, {
        method: 'GET',
        headers: this.authToken ? { 'Authorization': `Bearer ${this.authToken}` } : {}
      });

      if (response.ok) {
        this.markOnline();
        return true;
      } else {
        this.scheduleOfflineRetry();
        return false;
      }
    } catch (error) {
      this.scheduleOfflineRetry();
      return false;
    }
  }

  /**
   * Check if error is retryable
   */
  isRetryableError(error) {
    // Retry on network errors or 5xx server errors
    return !error.statusCode || (error.statusCode >= 500 && error.statusCode < 600);
  }

  /**
   * Delay helper for retry logic
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Convenience method for GET requests
   */
  async get(endpoint, options = {}) {
    return this.request(endpoint, { ...options, method: 'GET' });
  }

  /**
   * Convenience method for POST requests
   */
  async post(endpoint, data = null, options = {}) {
    const requestOptions = { ...options, method: 'POST' };
    if (data) {
      requestOptions.body = JSON.stringify(data);
    }
    return this.request(endpoint, requestOptions);
  }

  /**
   * Convenience method for PUT requests
   */
  async put(endpoint, data = null, options = {}) {
    const requestOptions = { ...options, method: 'PUT' };
    if (data) {
      requestOptions.body = JSON.stringify(data);
    }
    return this.request(endpoint, requestOptions);
  }

  /**
   * Convenience method for DELETE requests
   */
  async delete(endpoint, options = {}) {
    return this.request(endpoint, { ...options, method: 'DELETE' });
  }

  /**
   * Get TMDB image URL
   */
  getImageUrl(path, size = 'w500') {
    if (!path) return null;
    // Remove leading slash if present
    const cleanPath = path.startsWith('/') ? path.substring(1) : path;
    return `https://image.tmdb.org/t/p/${size}/${cleanPath}`;
  }

  // ==================== CONTENT ENDPOINTS ====================

  /**
   * GET /api/content/discover
   * Get trending and popular content
   */
  async getDiscoverContent(profileId = null, page = 1) {
    const params = new URLSearchParams();
    if (profileId) params.append('profileId', profileId);
    params.append('page', page);

    return this.request(`/content/discover?${params.toString()}`);
  }

  /**
   * GET /api/content/search
   * Search for content in library
   */
  async searchContent(query, type = 'all', profileId = null) {
    const params = new URLSearchParams();
    params.append('q', query);
    params.append('type', type);
    if (profileId) params.append('profileId', profileId);

    return this.request(`/content/search?${params.toString()}`);
  }

  /**
   * GET /api/content/discovery/search
   * Search TMDB for discovery content
   */
  async searchTMDB(query, type = 'all') {
    const params = new URLSearchParams();
    params.append('q', query);
    params.append('type', type);

    return this.request(`/content/discovery/search?${params.toString()}`);
  }

  /**
   * GET /api/content/popular
   * Get popular content
   */
  async getPopularContent(type, page = 1, profileId = null) {
    const params = new URLSearchParams();
    params.append('type', type);
    params.append('page', page);
    if (profileId) params.append('profileId', profileId);

    return this.request(`/content/popular?${params.toString()}`);
  }

  /**
   * GET /api/content/:id
   * Get detailed content information
   */
  async getContentDetails(id, type, profileId = null) {
    const params = new URLSearchParams();
    params.append('type', type);
    if (profileId) params.append('profileId', profileId);

    return this.request(`/content/${id}?${params.toString()}`);
  }

  /**
   * GET /api/content/:id/episodes
   * Get episodes for a TV series (all seasons metadata)
   */
  async getSeriesEpisodes(tmdbId) {
    return this.request(`/content/${tmdbId}/episodes`);
  }

  /**
   * GET /api/content/:id/episodes?season=X
   * Get episodes for a specific season
   */
  async getSeasonEpisodes(tmdbId, seasonNumber) {
    return this.request(`/content/${tmdbId}/episodes?season=${seasonNumber}`);
  }

  /**
   * GET /api/series/:id/seasons/:seasonNumber/episodes
   * Get episodes for a specific season of a library series
   */
  async getLibrarySeasonEpisodes(seriesId, seasonNumber) {
    return this.request(`/series/${seriesId}/seasons/${seasonNumber}/episodes`);
  }

  /**
   * GET /api/series/:id/seasons
   * Get all seasons for a library series
   */
  async getLibrarySeriesSeasons(seriesId) {
    return this.request(`/series/${seriesId}/seasons`);
  }

  /**
   * POST /api/content/:id/queue
   * Add content to download queue
   */
  async queueDownload(id, profileId, type, title, year = null) {
    return this.request(`/content/${id}/queue`, {
      method: 'POST',
      body: JSON.stringify({
        ProfileId: profileId,
        Type: type,
        Title: title,
        Year: year
      })
    });
  }

  /**
   * POST /api/content/:id/queue/episode
   * Add specific episode to download queue (currently uses main queue endpoint)
   */
  async queueEpisodeDownload(tmdbId, profileId, title, seasonNumber, episodeNumber, year = null) {
    // For now, queue the entire series since individual episode queuing isn't implemented
    return this.queueDownload(tmdbId, profileId, 'series', title, year);
  }

  /**
   * POST /api/content/:id/queue/season
   * Add entire season to download queue (currently uses main queue endpoint)
   */
  async queueSeasonDownload(tmdbId, profileId, title, seasonNumber, year = null) {
    // For now, queue the entire series since individual season queuing isn't implemented
    return this.queueDownload(tmdbId, profileId, 'series', title, year);
  }

  // ==================== LIBRARY ENDPOINTS ====================

  /**
   * GET /api/library/movies
   * Get all movies in library
   */
  async getLibraryMovies(filters = {}) {
    const params = new URLSearchParams();
    if (filters.genre) params.append('genre', filters.genre);
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);
    if (filters.limit) params.append('limit', filters.limit);
    if (filters.offset) params.append('offset', filters.offset);

    return this.request(`/library/movies?${params.toString()}`);
  }

  /**
   * GET /api/library/series
   * Get all TV series in library
   */
  async getLibrarySeries(filters = {}) {
    const params = new URLSearchParams();
    if (filters.genre) params.append('genre', filters.genre);
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);
    if (filters.limit) params.append('limit', filters.limit);
    if (filters.offset) params.append('offset', filters.offset);

    return this.request(`/library/series?${params.toString()}`);
  }

  /**
   * GET /api/library/recent
   * Get recently added content
   */
  async getRecentlyAdded(limit = 20) {
    return this.request(`/library/recent?limit=${limit}`);
  }

  /**
   * GET /api/library/:id
   * Get specific library item details
   */
  async getLibraryItem(id, profileId = null) {
    const params = new URLSearchParams();
    if (profileId) params.append('profileId', profileId);

    const queryString = params.toString();
    return this.request(`/library/${id}${queryString ? '?' + queryString : ''}`);
  }

  /**
   * DELETE /api/library/:id
   * Remove item from library
   */
  async removeFromLibrary(id) {
    return this.request(`/library/${id}`, {
      method: 'DELETE'
    });
  }

  // ==================== PROFILE ENDPOINTS ====================

  /**
   * GET /api/profiles
   * List all profiles
   */
  async getProfiles() {
    return this.request('/profiles');
  }

  /**
   * POST /api/profiles
   * Create new profile
   */
  async createProfile(name, avatarColorPrimary, avatarColorSecondary) {
    return this.request('/profiles', {
      method: 'POST',
      body: JSON.stringify({ name, avatarColorPrimary, avatarColorSecondary })
    });
  }

  /**
   * GET /api/profiles/:id
   * Get profile details
   */
  async getProfile(id) {
    return this.request(`/profiles/${id}`);
  }

  /**
   * PUT /api/profiles/:id
   * Update profile
   */
  async updateProfile(id, updates) {
    return this.request(`/profiles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(updates)
    });
  }

  /**
   * DELETE /api/profiles/:id
   * Delete profile
   */
  async deleteProfile(id) {
    return this.request(`/profiles/${id}`, {
      method: 'DELETE'
    });
  }

  /**
   * GET /api/profiles/:id/watchlist
   * Get profile's My List (watchlist)
   */
  async getWatchlist(profileId) {
    return this.request(`/profiles/${profileId}/watchlist`);
  }

  /**
   * POST /api/profiles/:id/watchlist/:contentId
   * Add content to My List
   */
  async addToWatchlist(profileId, contentId) {
    return this.request(`/profiles/${profileId}/watchlist/${contentId}`, {
      method: 'POST'
    });
  }

  /**
   * DELETE /api/profiles/:id/watchlist/:contentId
   * Remove content from My List
   */
  async removeFromWatchlist(profileId, contentId) {
    return this.request(`/profiles/${profileId}/watchlist/${contentId}`, {
      method: 'DELETE'
    });
  }

  /**
   * GET /api/profiles/:id/history
   * Get profile's watch history
   */
  async getWatchHistory(profileId, limit = 50) {
    const params = new URLSearchParams();
    if (limit) params.append('limit', limit);
    return this.request(`/profiles/${profileId}/history?${params.toString()}`);
  }

  // ==================== CONTENT ENDPOINTS ====================

  /**
   * GET /api/content/:id
   * Get content details (movie or series)
   */
  async getContentDetails(contentId, contentType, profileId = null) {
    const params = new URLSearchParams();
    params.append('type', contentType);
    if (profileId) params.append('profileId', profileId);

    return this.request(`/content/${contentId}?${params.toString()}`);
  }

  /**
   * Get next episode for a series
   */
  async getNextEpisode(seriesId, currentEpisodeId) {
    try {
      // Get all episodes for the series
      const episodes = await this.getSeriesEpisodes(seriesId);
      if (!episodes || !episodes.length) return null;

      // Flatten episodes from all seasons
      let allEpisodes = [];
      episodes.forEach(season => {
        if (season.episodes && season.episodes.length) {
          allEpisodes.push(...season.episodes);
        }
      });

      // Sort by season and episode number
      allEpisodes.sort((a, b) => {
        if (a.seasonNumber !== b.seasonNumber) {
          return a.seasonNumber - b.seasonNumber;
        }
        return a.episodeNumber - b.episodeNumber;
      });

      // Find current episode index
      const currentIndex = allEpisodes.findIndex(e => e.id == currentEpisodeId || e.tmdbId == currentEpisodeId);

      if (currentIndex !== -1 && currentIndex < allEpisodes.length - 1) {
        return allEpisodes[currentIndex + 1];
      }

      return null;
    } catch (error) {
      console.error('Failed to get next episode:', error);
      return null;
    }
  }

  // ==================== STREAMING ENDPOINTS ====================

  /**
   * GET /api/transcoding/stream/:id
   * Get streaming URL for content
   */
  getStreamUrl(contentId, episodeId = null, profileId = null, startTime = null) {
    const params = new URLSearchParams();
    if (episodeId) params.append('episodeId', episodeId);
    if (profileId) params.append('profileId', profileId);
    if (startTime) params.append('startTime', startTime.toString());

    return `${this.baseURL}/transcoding/stream/${contentId}?${params.toString()}`;
  }

  /**
   * POST /api/transcoding/stream/:id/progress
   * Update watch progress
   */
  async updateWatchProgress(contentId, profileId, progressSeconds, durationSeconds = null, episodeId = null) {
    const body = {
      profileId,
      progressSeconds,
      durationSeconds
    };

    if (episodeId) {
      body.episodeId = episodeId;
    }

    return this.request(`/transcoding/stream/${contentId}/progress`, {
      method: 'POST',
      body: JSON.stringify(body)
    });
  }

  /**
   * GET /api/transcoding/stream/:id/subtitles
   * List available subtitles
   */
  async getSubtitles(contentId, episodeId = null) {
    const params = new URLSearchParams();
    if (episodeId) params.append('episodeId', episodeId);

    return this.request(`/transcoding/stream/${contentId}/subtitles?${params.toString()}`);
  }

  /**
   * GET /api/transcoding/stream/:id/info
   * Get media file information (codecs, duration, etc.)
   */
  async getMediaInfo(contentId, episodeId = null) {
    const params = new URLSearchParams();
    if (episodeId) params.append('episodeId', episodeId);

    return this.request(`/transcoding/stream/${contentId}/info?${params.toString()}`);
  }

  // ==================== SETTINGS ENDPOINTS ====================

  /**
   * GET /api/settings
   * Get application settings
   */
  async getSettings() {
    return this.request('/settings');
  }

  /**
   * PUT /api/settings
   * Update application settings
   */
  async updateSettings(settings) {
    return this.request('/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  /**
   * PUT /api/settings/streaming/:profileId
   * Update streaming preferences for a profile
   */
  async updateStreamingPreferences(profileId, preferences) {
    return this.request(`/settings/streaming/${profileId}`, {
      method: 'PUT',
      body: JSON.stringify({ streamingPreferences: preferences })
    });
  }

  /**
   * GET /api/settings/services
   * Get external service connection status
   */
  async getServiceStatus() {
    return this.request('/settings/services');
  }

  /**
   * POST /api/settings/test-connection
   * Test connection to external service
   */
  async testServiceConnection(service) {
    return this.request('/settings/test-connection', {
      method: 'POST',
      body: JSON.stringify({ service })
    });
  }

  /**
   * GET /api/settings/radarr/root-folders
   * Get available root folders from Radarr
   */
  async getRadarrRootFolders() {
    return this.request('/settings/radarr/root-folders');
  }

  /**
   * GET /api/settings/sonarr/root-folders
   * Get available root folders from Sonarr
   */
  async getSonarrRootFolders() {
    return this.request('/settings/sonarr/root-folders');
  }

  /**
   * GET /api/settings/custom/:key
   * Get a custom setting
   */
  async getCustomSetting(key) {
    return this.request(`/settings/custom/${key}`);
  }

  /**
   * PUT /api/settings/custom/:key
   * Save a custom setting
   */
  async saveCustomSetting(key, value) {
    return this.request(`/settings/custom/${key}`, {
      method: 'PUT',
      body: JSON.stringify({ value })
    });
  }

  // ==================== NOTIFICATION ENDPOINTS ====================

  /**
   * POST /api/notifications/register
   * Register device for push notifications
   */
  async registerDevice(profileId, deviceToken, platform) {
    return this.request('/notifications/register', {
      method: 'POST',
      body: JSON.stringify({ profileId, deviceToken, platform })
    });
  }

  /**
   * POST /api/notifications/:id/respond
   * Respond to keep-watching notification
   */
  async respondToNotification(notificationId, response) {
    return this.request(`/notifications/${notificationId}/respond`, {
      method: 'POST',
      body: JSON.stringify({ response })
    });
  }

  /**
   * GET /api/notifications/:profileId
   * Get notification history
   */
  async getNotifications(profileId) {
    return this.request(`/notifications/${profileId}`);
  }

  // ==================== DOWNLOAD QUEUE ENDPOINTS ====================

  /**
   * GET /api/downloads/queue
   * Get current download queue from Radarr and Sonarr
   */
  async getDownloadQueue() {
    return this.request('/downloads/queue');
  }

  /**
   * DELETE /api/downloads/queue/:id
   * Cancel/remove download from queue
   */
  async cancelDownload(downloadId, service) {
    return this.request(`/downloads/queue/${downloadId}`, {
      method: 'DELETE',
      body: JSON.stringify({ service })
    });
  }

  // ==================== JOBS ENDPOINTS ====================

  /**
   * GET /api/jobs/status
   * Get background job status
   */
  async getJobsStatus() {
    return this.request('/jobs/status');
  }

  /**
   * POST /api/jobs/:jobName/trigger
   * Trigger a background job
   */
  async triggerJob(jobName) {
    return this.request(`/jobs/${jobName}/trigger`, {
      method: 'POST'
    });
  }
}

// Create singleton instance
const apiClient = new ApiClient();

// Load auth token on initialization
apiClient.loadAuthToken();

export default apiClient;
