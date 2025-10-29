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
    this.cacheDuration = 5 * 60 * 1000; // 5 minutes
    this.isOffline = false;
    
    // Load state from localStorage
    this.loadState();
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
      this.currentPage = state.currentPage || 'home';
      this.currentProfileId = state.currentProfileId || null;
      this.scrollPosition = state.scrollPosition || 0;
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
   * Set cache with timestamp
   */
  setCache(key, data) {
    this.cache[key] = data;
    this.cacheTimestamps[key] = Date.now();
  }

  /**
   * Get profiles from backend or cache
   */
  async getProfiles(forceRefresh = false) {
    if (!forceRefresh && this.isCacheValid('profiles')) {
      return this.cache.profiles;
    }

    try {
      const response = await apiClient.getProfiles();
      this.setCache('profiles', response.profiles);
      this.isOffline = false;
      return response.profiles;
    } catch (error) {
      console.error('Failed to fetch profiles:', error);
      this.isOffline = true;
      // Return cached data if available
      return this.cache.profiles || [];
    }
  }

  /**
   * Get discover content from backend or cache
   */
  async getDiscoverContent(profileId, forceRefresh = false) {
    const cacheKey = `discover_${profileId}`;
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
      return this.cache[cacheKey] || { trending: [], popular: { movies: [], series: [] } };
    }
  }

  /**
   * Get library movies from backend or cache
   */
  async getLibraryMovies(filters = {}, forceRefresh = false) {
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
      return this.cache.libraryMovies || { count: 0, items: [] };
    }
  }

  /**
   * Get library series from backend or cache
   */
  async getLibrarySeries(filters = {}, forceRefresh = false) {
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
      return this.cache.librarySeries || { count: 0, items: [] };
    }
  }

  /**
   * Get recently added content from backend or cache
   */
  async getRecentlyAdded(limit = 20, forceRefresh = false) {
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
      return this.cache.recentlyAdded || { count: 0, items: [] };
    }
  }

  /**
   * Get watchlist from backend or cache
   */
  async getWatchlist(profileId, forceRefresh = false) {
    const cacheKey = `watchlist_${profileId}`;
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
      return this.cache[cacheKey] || { count: 0, items: [] };
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

// Legacy exports for backward compatibility (mock data)
export const PROFILES = [];

// Mock hero data for fallback
export const HEROES = {
  home: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/8rpDcsfLJypbO6vREc0547VKqEv.jpg)',
      tag: 'New Release • Sci-Fi',
      title: 'Avatar',
      meta: ['Movie', '2024', 'PG-13', '2h 46m'],
      description: 'Paul Atreides unites with Chani and the Fremen while seeking revenge against the conspirators who destroyed his family.',
      secondary: 'Now streaming in 4K UHD',
    },
    {
      background: 'url(https://www.hdwallpapers.in/download/the_boys_poster_4k_hd-3840x2160.jpg)',
      tag: 'Popular • Superhero',
      title: 'The Boys',
      meta: ['Series', '2019–', 'TV-MA', '4 Seasons'],
      description: 'A group of vigilantes set out to take down corrupt superheroes who abuse their powers.',
      secondary: 'Season 4 now streaming',
    },
    {
      background: 'url(https://image.tmdb.org/t/p/original/fYPiQewg7ogbzro2XcCTACSB2KC.jpg)',
      tag: 'Top Pick • Fantasy',
      title: 'House of the Dragon',
      meta: ['Series', '2022–', 'TV-MA', '2 Seasons'],
      description: 'The Targaryen dynasty rules Westeros — and the seeds of civil war begin to take root 200 years before the events of Game of Thrones.',
      secondary: 'New season coming in 2025',
    },
  ],
  discover: [
    {
      background: 'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Because you watched',
      title: 'Midnight Tales',
      meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
      description: 'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
      secondary: 'Continue watching S3:E4',
    },
  ],
  shows: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/oqP1qEZccq5AD9TVTIaO6IGUj7o.jpg)',
      tag: 'Hit Series • Thriller',
      title: 'Squid Game',
      meta: ['Series', '2021', 'TV-MA', '9 Episodes'],
      description: 'Hundreds of cash-strapped contestants accept an invitation to compete in children\'s games for a tempting prize, but the stakes are deadly.',
      secondary: 'Season 2 premieres June 27',
    },
    {
      background: 'url(https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg)',
      tag: 'New Series • Fantasy',
      title: 'Wednesday',
      meta: ['Series', '2022', 'TV-14', '8 Episodes'],
      description: 'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
      secondary: 'Season 2 coming soon',
    },
  ],
  movies: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/4VujM9lbRv6j8N3w6JkYp1q5bZp.jpg)',
      tag: 'Exclusive Film',
      title: 'The Gray Man',
      meta: ['Movie', '2022', '2h 9m'],
      description: 'When a shadowy CIA agent uncovers damning agency secrets, he\'s hunted across the globe by a sociopathic rogue operative who\'s put a bounty on his head.',
      secondary: 'Now streaming in Ultra HD',
    },
  ],
  my: [
    {
      background: 'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Because you watched',
      title: 'Midnight Tales',
      meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
      description: 'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
      secondary: 'Continue watching S3:E4',
    },
  ],
};

// Mock movie data for fallback
export const MOVIES = [
  {
    title: 'Avatar: The Way of Water',
    type: 'movies',
    genre: 'Sci-Fi',
    duration: '3h 12m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/t6HIqrRAclMCA60NsSmeqe9RmNV.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/8rpDcsfLJypbO6vREc0547VKqEv.jpg',
    description: 'Set more than a decade after the events of the first film, Avatar: The Way of Water begins to tell the story of the Sully family.',
  },
  {
    title: 'Stranger Things',
    type: 'series',
    genre: 'Sci-Fi',
    duration: '4 Seasons',
    rating: 'TV-14',
    year: '2016',
    image: 'https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/56v2KjBlU4XaOv9rVYEQypROD7P.jpg',
    description: 'When a young boy vanishes, a small town uncovers a mystery involving secret experiments, terrifying supernatural forces, and one strange little girl.',
  },
  // Add more movies/series as needed
];
