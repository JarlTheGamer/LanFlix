/**
 * API Client Module
 * Handles all backend communication with typed methods for all API endpoints
 */

class ApiClient {
  constructor(baseURL = 'http://localhost:3000/api') {
    this.baseURL = baseURL;
    this.authToken = null;
    this.retryAttempts = 0; // No retries - fail fast
    this.retryDelay = 1000; // ms
    this.isOffline = false;
    this.offlineRetryInterval = 10 * 60 * 1000; // 10 minutes
    this.lastOfflineCheck = null;
    this.offlineCheckTimer = null;
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
        const errorData = await response.json().catch(() => ({}));
        const error = new Error(errorData.error?.message || `HTTP ${response.status}: ${response.statusText}`);
        error.statusCode = response.status;
        error.code = errorData.error?.code || 'HTTP_ERROR';
        error.details = errorData.error?.details;
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
      // Try a lightweight endpoint
      const response = await fetch(`${this.baseURL}/settings`, {
        method: 'HEAD',
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
   * Search for content
   */
  async searchContent(query, type = 'all', profileId = null) {
    const params = new URLSearchParams();
    params.append('q', query);
    params.append('type', type);
    if (profileId) params.append('profileId', profileId);

    return this.request(`/content/search?${params.toString()}`);
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
   * POST /api/content/:id/queue
   * Add content to download queue
   */
  async queueDownload(id, profileId, type, title, year = null) {
    return this.request(`/content/${id}/queue`, {
      method: 'POST',
      body: JSON.stringify({ profileId, type, title, year })
    });
  }

  /**
   * POST /api/content/:id/queue/episode
   * Add specific episode to download queue
   */
  async queueEpisodeDownload(tmdbId, profileId, title, seasonNumber, episodeNumber, year = null) {
    return this.request(`/content/${tmdbId}/queue/episode`, {
      method: 'POST',
      body: JSON.stringify({ profileId, title, seasonNumber, episodeNumber, year })
    });
  }

  /**
   * POST /api/content/:id/queue/season
   * Add entire season to download queue
   */
  async queueSeasonDownload(tmdbId, profileId, title, seasonNumber, year = null) {
    return this.request(`/content/${tmdbId}/queue/season`, {
      method: 'POST',
      body: JSON.stringify({ profileId, title, seasonNumber, year })
    });
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

  // ==================== STREAMING ENDPOINTS ====================

  /**
   * GET /api/stream/:id
   * Get streaming URL for content
   */
  getStreamUrl(contentId, episodeId = null) {
    const params = new URLSearchParams();
    if (episodeId) params.append('episodeId', episodeId);

    return `${this.baseURL}/stream/${contentId}?${params.toString()}`;
  }

  /**
   * POST /api/stream/:id/progress
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

    return this.request(`/stream/${contentId}/progress`, {
      method: 'POST',
      body: JSON.stringify(body)
    });
  }

  /**
   * GET /api/stream/:id/subtitles
   * List available subtitles
   */
  async getSubtitles(contentId, episodeId = null) {
    const params = new URLSearchParams();
    if (episodeId) params.append('episodeId', episodeId);

    return this.request(`/stream/${contentId}/subtitles?${params.toString()}`);
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
      body: JSON.stringify({ settings })
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
}

// Create singleton instance
const apiClient = new ApiClient();

// Load auth token on initialization
apiClient.loadAuthToken();

export default apiClient;
