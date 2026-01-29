import apiClient from './api-client.js';

/**
 * State Management Module
 * Handles data fetching, caching, and state persistence
 */

class StateManager {
  constructor() {
    this.loadState();
    this.clearLocalStorageCache();
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
   * Clear localStorage cache entries
   */
  clearLocalStorageCache() {
    try {
      const keysToRemove = [];
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith('cache_')) {
          keysToRemove.push(key);
        }
      }
      keysToRemove.forEach(key => localStorage.removeItem(key));
      if (keysToRemove.length > 0) {
        console.log(`🧹 Cleared ${keysToRemove.length} localStorage cache entries`);
      }
    } catch (error) {
      console.warn('Failed to clear localStorage cache:', error);
    }
  }

  /**
   * Clear all caches (no-op for compatibility)
   */
  clearCache() {
    // No-op - caching is disabled
  }

  /**
   * Get profiles from backend
   */
  async getProfiles(forceRefresh = false) {
    try {
      const response = await apiClient.getProfiles();
      // Handle both wrapped {profiles: []} and direct array responses
      const profiles = Array.isArray(response) ? response : (response.profiles || []);
      return profiles;
    } catch (error) {
      console.error('Failed to fetch profiles:', error);
      return [];
    }
  }

  /**
   * Get discover content from backend
   */
  async getDiscoverContent(profileId, forceRefresh = false) {
    try {
      const data = await apiClient.getDiscoverContent(profileId);
      return data;
    } catch (error) {
      console.error('Failed to fetch discover content:', error);
      return { trending: [], popular: { movies: [], series: [] } };
    }
  }

  /**
   * Get library movies from backend
   */
  async getLibraryMovies(filters = {}, forceRefresh = false) {
    try {
      const data = await apiClient.getLibraryMovies(filters);
      return data;
    } catch (error) {
      console.error('Failed to fetch library movies:', error);
      return { count: 0, items: [] };
    }
  }

  /**
   * Get library series from backend
   */
  async getLibrarySeries(filters = {}, forceRefresh = false) {
    try {
      const data = await apiClient.getLibrarySeries(filters);
      return data;
    } catch (error) {
      console.error('Failed to fetch library series:', error);
      return { count: 0, items: [] };
    }
  }

  /**
   * Get recently added content from backend
   */
  async getRecentlyAdded(limit = 20, forceRefresh = false) {
    try {
      const data = await apiClient.getRecentlyAdded(limit);
      return data;
    } catch (error) {
      console.error('Failed to fetch recently added:', error);
      return { count: 0, items: [] };
    }
  }

  /**
   * Get watchlist from backend
   */
  async getWatchlist(profileId, forceRefresh = false) {
    try {
      const data = await apiClient.getWatchlist(profileId);
      return data;
    } catch (error) {
      console.error('Failed to fetch watchlist:', error);
      return { count: 0, items: [] };
    }
  }

  /**
   * Get watch history from backend
   */
  async getWatchHistory(profileId, forceRefresh = false, limit = 50) {
    try {
      const data = await apiClient.getWatchHistory(profileId, limit);
      return data;
    } catch (error) {
      console.error('Failed to fetch watch history:', error);
      return [];
    }
  }

  /**
   * Get popular content
   */
  async getPopularContent(type, page = 1, profileId = null, forceRefresh = false) {
    try {
      const data = await apiClient.getPopularContent(type, page, profileId);
      return data;
    } catch (error) {
      console.error(`Failed to fetch popular ${type}:`, error);
      return [];
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


}

// Create singleton instance
const stateManager = new StateManager();

// Save state before page unload
window.addEventListener('beforeunload', () => {
  stateManager.saveState();
});

export default stateManager;

export const PROFILES = [];

