/**
 * Search Module
 * Handles content search functionality
 */

import apiClient from './api-client.js';
import stateManager from './data.js';

class SearchModule {
  constructor() {
    this.searchInput = null;
    this.searchResults = null;
    this.searchOverlay = null;
    this.debounceTimer = null;
    this.debounceDelay = 300;
    this.isOpen = false;
  }

  /**
   * Initialize search module
   */
  initialize() {
    this.createSearchUI();
    this.attachEventListeners();
  }

  /**
   * Create search UI elements
   */
  createSearchUI() {
    // Create search overlay
    this.searchOverlay = document.createElement('div');
    this.searchOverlay.className = 'search-overlay';
    this.searchOverlay.innerHTML = `
      <div class="search-modal">
        <div class="search-header">
          <div class="search-input-wrapper">
            <svg class="search-icon" viewBox="0 0 24 24" width="20" height="20">
              <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
            </svg>
            <input type="text" class="search-input" placeholder="Search movies and TV shows..." autocomplete="off">
            <button class="search-clear-btn" style="display: none;">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
            </button>
          </div>
          <button class="search-close-btn">
            <svg viewBox="0 0 24 24" width="24" height="24">
              <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
        </div>
        <div class="search-results">
          <div class="search-empty">
            <svg viewBox="0 0 24 24" width="48" height="48">
              <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
            </svg>
            <p>Start typing to search...</p>
          </div>
        </div>
      </div>
    `;
    document.body.appendChild(this.searchOverlay);

    // Get references
    this.searchInput = this.searchOverlay.querySelector('.search-input');
    this.searchResults = this.searchOverlay.querySelector('.search-results');
  }

  /**
   * Attach event listeners
   */
  attachEventListeners() {
    // Search input
    this.searchInput.addEventListener('input', (e) => {
      this.handleSearchInput(e.target.value);
    });

    // Clear button
    this.searchOverlay.querySelector('.search-clear-btn').addEventListener('click', () => {
      this.clearSearch();
    });

    // Close button
    this.searchOverlay.querySelector('.search-close-btn').addEventListener('click', () => {
      this.close();
    });

    // Close on overlay click
    this.searchOverlay.addEventListener('click', (e) => {
      if (e.target === this.searchOverlay) {
        this.close();
      }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
      // Open search with '/' key
      if (e.key === '/' && !this.isOpen && document.activeElement.tagName !== 'INPUT') {
        e.preventDefault();
        this.open();
      }
      
      // Close search with Escape
      if (e.key === 'Escape' && this.isOpen) {
        this.close();
      }
    });
  }

  /**
   * Handle search input with debouncing
   */
  handleSearchInput(query) {
    const clearBtn = this.searchOverlay.querySelector('.search-clear-btn');
    
    // Show/hide clear button
    if (query.length > 0) {
      clearBtn.style.display = 'block';
    } else {
      clearBtn.style.display = 'none';
      this.showEmptyState();
      return;
    }

    // Debounce search
    clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.performSearch(query);
    }, this.debounceDelay);
  }

  /**
   * Perform search
   */
  async performSearch(query) {
    if (query.trim().length < 2) {
      this.showEmptyState('Please enter at least 2 characters');
      return;
    }

    try {
      this.showLoading();
      
      // Search TMDB for discovery content
      const response = await apiClient.searchTMDB(query);
      
      // Handle different response formats
      let results = [];
      if (Array.isArray(response)) {
        results = response;
      } else if (response && Array.isArray(response.results)) {
        results = response.results;
      } else if (response && response.data && Array.isArray(response.data)) {
        results = response.data;
      }
      
      if (results.length === 0) {
        this.showEmptyState('No results found');
      } else {
        this.displayResults(results);
      }
    } catch (error) {
      console.error('Search failed:', error);
      this.showEmptyState('Search failed. Please try again.');
    }
  }

  /**
   * Display search results
   */
  displayResults(results) {
    // Ensure results is an array
    if (!Array.isArray(results)) {
      console.error('Search results is not an array:', results);
      this.showEmptyState('Invalid search results');
      return;
    }

    this.searchResults.innerHTML = `
      <div class="search-results-grid">
        ${results.map(item => this.createResultCard(item)).join('')}
      </div>
    `;

    // Attach click handlers
    this.searchResults.querySelectorAll('.search-result-card').forEach((card, index) => {
      card.addEventListener('click', () => {
        this.handleResultClick(results[index]);
      });
    });
  }

  /**
   * Create result card HTML
   */
  createResultCard(item) {
    const posterUrl = item.posterPath 
      ? apiClient.getImageUrl(item.posterPath, 'w342')
      : 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="342" height="513" viewBox="0 0 342 513"%3E%3Crect fill="%23222" width="342" height="513"/%3E%3Ctext x="50%25" y="50%25" dominant-baseline="middle" text-anchor="middle" font-family="sans-serif" font-size="24" fill="%23666"%3ENo Image%3C/text%3E%3C/svg%3E';
    
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : '';
    const typeLabel = item.type === 'movie' ? 'Movie' : 'TV Show';
    const rating = item.voteAverage ? item.voteAverage.toFixed(1) : 'N/A';

    return `
      <div class="search-result-card" data-id="${item.id}">
        <div class="search-result-poster">
          <img src="${posterUrl}" alt="${item.title}" loading="lazy">
          <div class="search-result-overlay">
            <svg viewBox="0 0 24 24" width="48" height="48">
              <path fill="white" d="M8 5v14l11-7z"/>
            </svg>
          </div>
        </div>
        <div class="search-result-info">
          <h3 class="search-result-title">${item.title}</h3>
          <div class="search-result-meta">
            <span class="search-result-type">${typeLabel}</span>
            ${year ? `<span class="search-result-year">${year}</span>` : ''}
            <span class="search-result-rating">
              <svg viewBox="0 0 24 24" width="14" height="14">
                <path fill="currentColor" d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
              </svg>
              ${rating}
            </span>
          </div>
        </div>
      </div>
    `;
  }

  /**
   * Handle result click
   */
  handleResultClick(item) {
    this.close();
    
    // Import and show content modal
    import('./content-modal.js').then(module => {
      const ContentModal = module.default;
      // Create a minimal profileManager object with just the selectedProfileId
      const profileManager = {
        selectedProfileId: stateManager.currentProfileId
      };
      const contentModal = new ContentModal(profileManager);
      contentModal.show(item.id, item.type, true); // true = isDiscovery
    });
  }

  /**
   * Show loading state
   */
  showLoading() {
    this.searchResults.innerHTML = `
      <div class="search-loading">
        <div class="spinner"></div>
        <p>Searching...</p>
      </div>
    `;
  }

  /**
   * Show empty state
   */
  showEmptyState(message = 'Start typing to search...') {
    this.searchResults.innerHTML = `
      <div class="search-empty">
        <svg viewBox="0 0 24 24" width="48" height="48">
          <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
        </svg>
        <p>${message}</p>
      </div>
    `;
  }

  /**
   * Clear search
   */
  clearSearch() {
    this.searchInput.value = '';
    this.searchOverlay.querySelector('.search-clear-btn').style.display = 'none';
    this.showEmptyState();
    this.searchInput.focus();
  }

  /**
   * Open search
   */
  open() {
    this.searchOverlay.classList.add('active');
    this.isOpen = true;
    this.searchInput.focus();
  }

  /**
   * Close search
   */
  close() {
    this.searchOverlay.classList.remove('active');
    this.isOpen = false;
    this.clearSearch();
  }
}

// Create singleton instance
const searchModule = new SearchModule();

export default searchModule;
