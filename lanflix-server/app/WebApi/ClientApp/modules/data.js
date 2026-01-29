import apiClient from './api-client.js';

/**
 * State Management Module
 * Handles data fetching, caching, and state persistence
 */

class StateManager {
  constructor() {
    this.cache = {
      profiles: null,
      discoverContent: null,
      libraryMovies: null,
      librarySeries: null,
      recentlyAdded: null,
      watchlist: null
    };

    this.cacheTimestamps = {};
    this.cacheDuration = 30 * 1000; // 30 seconds
    this.isOffline = false;

    // Load state from localStorage
    this.loadState();

    // Load cached data from localStorage
    this.loadCacheFromStorage();

    // Listen for API status changes
    window.addEventListener('api-offline', () => {
      this.isOffline = true;
      console.log('📦 Switching to offline mode - using cached data');
    });

    window.addEventListener('api-online', () => {
      this.isOffline = false;
      console.log('🌐 Back online - refreshing data');
      // Optionally refresh data when back online
      this.refreshAllData();
    });
  }

  /**
   * Save current state to localStorage
   */
  saveState() {
    const state = {
      currentPage: this.currentPage,
      currentProfileId: this.currentProfileId,
      scrollPosition: window.scrollY
    };
    localStorage.setItem('appState', JSON.stringify(state));
  }

  /**
   * Load state from localStorage
   */
  loadState() {
    const savedState = localStorage.getItem('appState');
    if (savedState) {
      const state = JSON.parse(savedState);
      // Always default to home page on load
      this.currentPage = 'home';
      this.currentProfileId = state.currentProfileId || null;
      this.scrollPosition = 0; // Reset scroll on page load
    } else {
      this.currentPage = 'home';
      this.currentProfileId = null;
      this.scrollPosition = 0;
    }
  }

  /**
   * Check if cache is valid
   */
  isCacheValid(key) {
    if (!this.cache[key]) return false;
    const timestamp = this.cacheTimestamps[key];
    if (!timestamp) return false;
    return Date.now() - timestamp < this.cacheDuration;
  }

  /**
   * Set cache with timestamp and persist to localStorage
   */
  setCache(key, data) {
    this.cache[key] = data;
    this.cacheTimestamps[key] = Date.now();

    // Persist to localStorage for offline access
    this.saveCacheToStorage(key, data);
  }

  /**
   * Save cache to localStorage
   */
  saveCacheToStorage(key, data) {
    try {
      const cacheData = {
        data: data,
        timestamp: Date.now()
      };
      localStorage.setItem(`cache_${key}`, JSON.stringify(cacheData));
    } catch (error) {
      console.warn('Failed to save cache to localStorage:', error);
    }
  }

  /**
   * Load cache from localStorage
   */
  loadCacheFromStorage() {
    try {
      Object.keys(this.cache).forEach(key => {
        const cached = localStorage.getItem(`cache_${key}`);
        if (cached) {
          const cacheData = JSON.parse(cached);
          this.cache[key] = cacheData.data;
          this.cacheTimestamps[key] = cacheData.timestamp;
        }
      });
      console.log('📦 Loaded cached data from localStorage');
    } catch (error) {
      console.warn('Failed to load cache from localStorage:', error);
    }
  }

  /**
   * Refresh all data when back online
   */
  async refreshAllData() {
    // Clear cache timestamps to force refresh
    this.cacheTimestamps = {};

    // Dispatch event for UI to refresh
    window.dispatchEvent(new CustomEvent('data-refresh-needed'));
  }

  /**
   * Get profiles from backend or cache
   */
  async getProfiles(forceRefresh = false) {
    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached profiles (offline mode)');
      return this.cache.profiles || [];
    }

    if (!forceRefresh && this.isCacheValid('profiles')) {
      return this.cache.profiles;
    }

    try {
      const response = await apiClient.getProfiles();
      // Handle both wrapped {profiles: []} and direct array responses
      const profiles = Array.isArray(response) ? response : (response.profiles || []);
      this.setCache('profiles', profiles);
      this.isOffline = false;
      return profiles;
    } catch (error) {
      console.error('Failed to fetch profiles:', error);
      this.isOffline = true;
      // Return cached data if available
      const cachedData = this.cache.profiles || [];
      console.log('📦 Using cached profiles due to error');
      return cachedData;
    }
  }

  /**
   * Get discover content from backend or cache
   */
  async getDiscoverContent(profileId, forceRefresh = false) {
    const cacheKey = `discover_${profileId}`;

    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached discover content (offline mode)');
      return this.cache[cacheKey] || { trending: [], popular: { movies: [], series: [] } };
    }

    if (!forceRefresh && this.cache[cacheKey] && this.isCacheValid('discoverContent')) {
      return this.cache[cacheKey];
    }

    try {
      const data = await apiClient.getDiscoverContent(profileId);
      this.cache[cacheKey] = data;
      this.setCache('discoverContent', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch discover content:', error);
      this.isOffline = true;
      // Return cached data if available
      const cachedData = this.cache[cacheKey] || { trending: [], popular: { movies: [], series: [] } };
      console.log('📦 Using cached discover content due to error');
      return cachedData;
    }
  }

  /**
   * Get library movies from backend or cache
   */
  async getLibraryMovies(filters = {}, forceRefresh = false) {
    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached library movies (offline mode)');
      return this.cache.libraryMovies || { count: 0, items: [] };
    }

    if (!forceRefresh && this.isCacheValid('libraryMovies')) {
      return this.cache.libraryMovies;
    }

    try {
      const data = await apiClient.getLibraryMovies(filters);
      this.setCache('libraryMovies', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch library movies:', error);
      this.isOffline = true;
      const cachedData = this.cache.libraryMovies || { count: 0, items: [] };
      console.log('📦 Using cached library movies due to error');
      return cachedData;
    }
  }

  /**
   * Get library series from backend or cache
   */
  async getLibrarySeries(filters = {}, forceRefresh = false) {
    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached library series (offline mode)');
      return this.cache.librarySeries || { count: 0, items: [] };
    }

    if (!forceRefresh && this.isCacheValid('librarySeries')) {
      return this.cache.librarySeries;
    }

    try {
      const data = await apiClient.getLibrarySeries(filters);
      this.setCache('librarySeries', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch library series:', error);
      this.isOffline = true;
      const cachedData = this.cache.librarySeries || { count: 0, items: [] };
      console.log('📦 Using cached library series due to error');
      return cachedData;
    }
  }

  /**
   * Get recently added content from backend or cache
   */
  async getRecentlyAdded(limit = 20, forceRefresh = false) {
    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached recently added (offline mode)');
      return this.cache.recentlyAdded || { count: 0, items: [] };
    }

    if (!forceRefresh && this.isCacheValid('recentlyAdded')) {
      return this.cache.recentlyAdded;
    }

    try {
      const data = await apiClient.getRecentlyAdded(limit);
      this.setCache('recentlyAdded', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch recently added:', error);
      this.isOffline = true;
      const cachedData = this.cache.recentlyAdded || { count: 0, items: [] };
      console.log('📦 Using cached recently added due to error');
      return cachedData;
    }
  }

  /**
   * Get watchlist from backend or cache
   */
  async getWatchlist(profileId, forceRefresh = false) {
    const cacheKey = `watchlist_${profileId}`;

    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached watchlist (offline mode)');
      return this.cache[cacheKey] || { count: 0, items: [] };
    }

    if (!forceRefresh && this.cache[cacheKey] && this.isCacheValid('watchlist')) {
      return this.cache[cacheKey];
    }

    try {
      const data = await apiClient.getWatchlist(profileId);
      this.cache[cacheKey] = data;
      this.setCache('watchlist', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch watchlist:', error);
      this.isOffline = true;
      const cachedData = this.cache[cacheKey] || { count: 0, items: [] };
      console.log('📦 Using cached watchlist due to error');
      return cachedData;
    }
  }

  /**
   * Get watch history from backend or cache
   */
  async getWatchHistory(profileId, forceRefresh = false, limit = 50) {
    const cacheKey = `history_${profileId}`;

    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log('📦 Using cached watch history (offline mode)');
      return this.cache[cacheKey] || [];
    }

    if (!forceRefresh && this.cache[cacheKey] && this.isCacheValid('watchHistory')) {
      return this.cache[cacheKey];
    }

    try {
      const data = await apiClient.getWatchHistory(profileId, limit);
      this.cache[cacheKey] = data;
      this.setCache('watchHistory', data);
      this.isOffline = false;
      return data;
    } catch (error) {
      console.error('Failed to fetch watch history:', error);
      this.isOffline = true;
      const cachedData = this.cache[cacheKey] || [];
      console.log('📦 Using cached watch history due to error');
      return cachedData;
    }
  }

  /**
   * Get popular content
   */
  async getPopularContent(type, page = 1, profileId = null, forceRefresh = false) {
    const cacheKey = `popular_${type}_${page}`;

    // If offline, return cached data immediately
    if (apiClient.isOffline && !forceRefresh) {
      console.log(`📦 Using cached popular ${type} (offline mode)`);
      return this.cache[cacheKey] || [];
    }

    try {
      const data = await apiClient.getPopularContent(type, page, profileId);
      this.cache[cacheKey] = data;
      this.setCache(`popular_${type}`, data);
      return data;
    } catch (error) {
      console.error(`Failed to fetch popular ${type}:`, error);
      const cachedData = this.cache[cacheKey] || [];
      console.log(`📦 Using cached popular ${type} due to error`);
      return cachedData;
    }
  }

  /**
   * Search content
   */
  async searchContent(query, type = 'all', profileId = null) {
    try {
      return await apiClient.searchContent(query, type, profileId);
    } catch (error) {
      console.error('Failed to search content:', error);
      return { query, type, results: [] };
    }
  }

  /**
   * Clear all caches
   */
  clearCache() {
    this.cache = {
      profiles: null,
      discoverContent: null,
      libraryMovies: null,
      librarySeries: null,
      recentlyAdded: null,
      watchlist: null
    };
    this.cacheTimestamps = {};
  }
}

// Create singleton instance
const stateManager = new StateManager();

// Save state before page unload
window.addEventListener('beforeunload', () => {
  stateManager.saveState();
});

export default stateManager;

export const PROFILES = [];

