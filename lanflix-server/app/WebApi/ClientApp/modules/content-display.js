import stateManager from './data.js';
import apiClient from './api-client.js';
import ContentModal from './content-modal.js';

export class ContentDisplay {
  constructor(profileManager, navigation = null) {
    this.profileManager = profileManager;
    this.contentModal = new ContentModal(profileManager, navigation);
    this.currentCategory = 'home';
    this.currentHeroIndex = 0;
    this.activeAmbilightLayer = 1;
    this.focusedHeroElement = null;
    this.contentData = {};
    this.isLoading = false;
    this.imageObserver = null;

    this.root = document.documentElement;
    this.heroCarouselTrack = document.getElementById('hero-carousel-track');
    this.heroAmbilight = document.getElementById('hero-ambilight');
    this.ambilightLayer1 = document.getElementById('ambilight-layer-1');
    this.ambilightLayer2 = document.getElementById('ambilight-layer-2');
    this.topNav = document.querySelector('.top-nav');

    // Swipe functionality properties
    this.swipeStartX = 0;
    this.swipeStartY = 0;
    this.swipeEndX = 0;
    this.swipeEndY = 0;
    this.isSwipeActive = false;
    this.swipeThreshold = 50; // Minimum distance for a swipe
    this.swipeTimeout = null;

    // Refresh content when page becomes visible (e.g., returning from player)
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden) {
        this.refreshContent();
      }
    });
  }

  async initialize() {
    // Always start on home page
    this.currentCategory = 'home';

    // Set home menu item as active
    const menuButtons = document.querySelectorAll('.menu-item');
    menuButtons.forEach((btn) => btn.classList.remove('active'));
    const homeButton = document.querySelector('.menu-item[data-hero="home"]');
    if (homeButton) {
      homeButton.classList.add('active');
    }

    await this.loadContent();

    // Render home page with unique layout
    await this.renderHomePage();

    this.setupScrollHandler();
    this.setupOfflineHandlers();
  }

  /**
   * Setup handlers for offline/online events
   */
  setupOfflineHandlers() {
    // Listen for API status changes
    window.addEventListener('api-offline', () => {
      this.showOfflineNotification();
    });

    window.addEventListener('api-online', () => {
      this.hideOfflineNotification();
      // Refresh content when back online
      this.refreshContent();
    });

    // Listen for data refresh events
    window.addEventListener('data-refresh-needed', () => {
      this.refreshContent();
    });
  }

  /**
   * Show offline notification banner
   */
  showOfflineNotification() {
    // Remove existing notification if any
    this.hideOfflineNotification();

    const notification = document.createElement('div');
    notification.id = 'offline-notification';
    notification.style.cssText = `
      position: fixed;
      top: 60px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(255, 152, 0, 0.95);
      color: #000;
      padding: 12px 24px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      z-index: 10000;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `;
    notification.innerHTML = `
      <span style="margin-right: 8px;">🔴</span>
      Discovery features are offline. Your downloaded content is still available.
      <span style="margin-left: 8px; font-size: 12px; opacity: 0.8;">Retrying in 10 minutes...</span>
    `;

    document.body.appendChild(notification);

    // Add animation
    const style = document.createElement('style');
    style.textContent = `
      @keyframes slideDown {
        from {
          opacity: 0;
          transform: translateX(-50%) translateY(-20px);
        }
        to {
          opacity: 1;
          transform: translateX(-50%) translateY(0);
        }
      }
    `;
    document.head.appendChild(style);
  }

  /**
   * Hide offline notification banner
   */
  hideOfflineNotification() {
    const notification = document.getElementById('offline-notification');
    if (notification) {
      notification.style.animation = 'slideUp 0.3s ease-out';
      setTimeout(() => notification.remove(), 300);
    }
  }

  /**
   * Merge watch progress data with content items
   */
  mergeWatchProgress(items, watchHistory) {
    if (!watchHistory || watchHistory.length === 0) return items;

    // Create a map of contentId -> watch progress
    const progressMap = new Map();
    watchHistory.forEach(historyItem => {
      const contentId = historyItem.contentId || (historyItem.content && historyItem.content.id);
      if (contentId) {
        // Convert PositionTicks to seconds
        const progressSeconds = historyItem.positionTicks
          ? Math.floor(historyItem.positionTicks / 10_000_000)
          : 0;

        const currentActivity = historyItem.lastWatchedAt ? new Date(historyItem.lastWatchedAt).getTime() : 0;
        const existingProgress = progressMap.get(contentId);

        // Only update if no existing progress OR current item is newer
        if (!existingProgress || currentActivity > existingProgress.lastActivity) {
          progressMap.set(contentId, {
            progressSeconds: progressSeconds,
            durationSeconds: historyItem.content?.runtime ? historyItem.content.runtime * 60 : null,
            watchedPercentage: historyItem.watchedPercentage || 0,
            completed: historyItem.isCompleted || false,
            lastActivity: currentActivity
          });
        }
      }
    });

    // Merge progress into items
    return items.map(item => {
      const progress = progressMap.get(item.id);
      if (progress) {
        return {
          ...item,
          watchProgress: progress
        };
      }
      return item;
    });
  }

  /**
   * Refresh content (called when page becomes visible)
   */
  async refreshContent() {
    await this.loadContent();
    
    // Re-render the current view
    if (this.currentCategory === 'home') {
      await this.renderHomePage();
    } else if (this.currentCategory === 'continue') {
      await this.renderContinueWatching();
    } else {
      this.renderContentGrid();
    }
  }

  async loadContent() {
    if (this.isLoading) return;
    this.isLoading = true;

    try {
      const profileId = this.profileManager.selectedProfileId;

      // Always fetch minimal watch history for "Continue Watching" row (except on My List)
      let watchHistory = [];
      if (this.currentCategory !== 'my' && !apiClient.isOffline && !stateManager.isOffline) {
        watchHistory = await stateManager.getWatchHistory(profileId, false, 20).catch(() => []);
      }

      switch (this.currentCategory) {
        case 'home':
          // Home shows downloaded content + small discovery carousel + watch history
          const recentlyAdded = await stateManager.getRecentlyAdded(20);

          // Only fetch discovery if online
          let discoverPreview = { trending: { movies: [], series: [] } };
          if (!apiClient.isOffline && !stateManager.isOffline) {
            discoverPreview = await stateManager.getDiscoverContent(profileId).catch(() => ({
              trending: { movies: [], series: [] }
            }));
          }

          // Combine movies and series for preview
          const trendingItems = [
            ...(discoverPreview.trending?.movies || []),
            ...(discoverPreview.trending?.series || [])
          ].slice(0, 10);

          // Merge watch progress with recently added items
          const recentlyAddedWithProgress = this.mergeWatchProgress(recentlyAdded.items || [], watchHistory);

          this.contentData = {
            recentlyAdded: recentlyAddedWithProgress,
            discoverPreview: trendingItems,
            watchHistory: watchHistory
          };
          break;

        case 'discover':
          // Discovery shows only online content for downloading
          if (apiClient.isOffline || stateManager.isOffline) {
            // If offline, show empty discovery
            this.contentData = {
              trending: { movies: [], series: [] },
              popularMovies: [],
              popularSeries: []
            };
          } else {
            try {
              const [discoverData, popularMovies, popularSeries] = await Promise.all([
                stateManager.getDiscoverContent(profileId),
                stateManager.getPopularContent('movie', 1, profileId),
                stateManager.getPopularContent('series', 1, profileId)
              ]);

              this.contentData = {
                trending: discoverData.trending || { movies: [], series: [] },
                popularMovies: Array.isArray(popularMovies) ? popularMovies : (popularMovies?.items || []),
                popularSeries: Array.isArray(popularSeries) ? popularSeries : (popularSeries?.items || [])
              };
            } catch (error) {
              console.error('Failed to load discovery content:', error);
              this.contentData = {
                trending: { movies: [], series: [] },
                popularMovies: [],
                popularSeries: []
              };
            }
          }
          break;

        case 'shows':
          // Shows page displays downloaded series only
          const seriesData = await stateManager.getLibrarySeries({ limit: 100 });
          // Filter history for Series type only
          const seriesHistory = watchHistory.filter(h =>
            (h.type === 'episode' || h.type === 'series') ||
            (h.content && (h.content.type === 'episode' || h.content.type === 'series'))
          );

          // Merge watch progress with series items
          const seriesWithProgress = this.mergeWatchProgress(seriesData.items || [], watchHistory);

          this.contentData = {
            series: seriesWithProgress,
            watchHistory: seriesHistory
          };
          break;

        case 'movies':
          // Movies page displays downloaded movies only
          const moviesData = await stateManager.getLibraryMovies({ limit: 100 });
          // Filter history for Movie type only
          const movieHistory = watchHistory.filter(h =>
            h.type === 'movie' || (h.content && h.content.type === 'movie')
          );

          // Merge watch progress with movie items
          const moviesWithProgress = this.mergeWatchProgress(moviesData.items || [], watchHistory);

          this.contentData = {
            movies: moviesWithProgress,
            watchHistory: movieHistory
          };
          break;

        case 'my':
          // My List shows downloaded content from watchlist
          const watchlist = await stateManager.getWatchlist(profileId);
          this.contentData = {
            watchlist: watchlist.items?.map(item => item.content) || []
          };
          break;
      }
    } catch (error) {
      console.error('Failed to load content:', error);
      this.contentData = {};
    } finally {
      this.isLoading = false;
    }
  }

  createCarouselItems() {
    this.heroCarouselTrack.innerHTML = '';

    // For discovery page, use trending content
    if (this.currentCategory === 'discover') {
      const trending = this.contentData.trending || { movies: [], series: [] };
      const trendingItems = [
        ...(trending.movies || []),
        ...(trending.series || [])
      ].slice(0, 5);

      if (trendingItems.length === 0) {
        const emptyHero = this.createEmptyHero();
        this.heroCarouselTrack.appendChild(emptyHero);
      } else {
        trendingItems.forEach((item, index) => {
          const heroSection = this.createDiscoveryHero(item, index);
          this.heroCarouselTrack.appendChild(heroSection);
        });
      }
    } else {
      // For all other pages, use LOCAL downloaded content
      const localContent = this.getLocalContentForHero();

      if (localContent.length === 0) {
        // Show empty state
        const emptyHero = this.createEmptyHero();
        this.heroCarouselTrack.appendChild(emptyHero);
      } else {
        localContent.forEach((item, index) => {
          const heroSection = this.createHeroFromContent(item, index);
          this.heroCarouselTrack.appendChild(heroSection);
        });
      }
    }

    this.focusedHeroElement = this.heroCarouselTrack.querySelector('.hero');
  }

  /**
   * Get local content for hero carousel based on current category
   */
  getLocalContentForHero() {
    switch (this.currentCategory) {
      case 'home':
        // Show recently added content
        return (this.contentData.recentlyAdded || []).slice(0, 5);
      case 'movies':
        // Show downloaded movies
        return (this.contentData.movies || []).slice(0, 5);
      case 'shows':
        // Show downloaded series
        return (this.contentData.series || []).slice(0, 5);
      case 'my':
        // Show watchlist
        return (this.contentData.watchlist || []).slice(0, 5);
      default:
        return [];
    }
  }

  /**
   * Create hero section from local content
   */
  createHeroFromContent(item, index) {
    const heroSection = document.createElement('section');
    heroSection.className = 'hero';
    heroSection.dataset.index = index;
    heroSection.dataset.contentId = item.id;
    heroSection.dataset.contentType = item.type;

    const backdropUrl = item.backdropUrl
      || item.posterUrl
      || 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080"%3E%3Crect fill="%23222" width="1920" height="1080"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="48" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E';

    // Handle genres - they can be strings or objects with name property
    const genres = Array.isArray(item.genres)
      ? item.genres.map(g => typeof g === 'string' ? g : g.name || g.Name).filter(Boolean).join(' • ')
      : (item.genre || 'Unknown');
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : (item.year || '');
    const duration = item.runtime ? `${Math.floor(item.runtime / 60)}h ${item.runtime % 60}m` : (item.duration || '');
    const rating = item.contentRating || item.rating || 'NR';
    const type = item.type === 'movie' ? 'Movie' : 'Series';

    const meta = [type, year, rating, duration].filter(Boolean);

    heroSection.innerHTML = `
      <div class="hero-background" style="background-image: url('${backdropUrl}')"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">Your Library • ${genres}</div>
          <h1 class="hero-title">${item.title}</h1>
          <div class="hero-meta">${meta.map((item) => `<span>${item}</span>`).join('')}</div>
          <p class="hero-description">${item.overview || item.description || 'No description available.'}</p>
          <div class="hero-actions">
            <button class="cta primary" data-action="play">
              <span>▶ Play</span>
            </button>
            <button class="cta ghost" data-action="info">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Downloaded</span> Ready to watch</div>
      </div>
    `;

    // Add event listeners to buttons
    const playBtn = heroSection.querySelector('[data-action="play"]');
    const infoBtn = heroSection.querySelector('[data-action="info"]');

    if (playBtn) {
      playBtn.addEventListener('click', (e) => {
        e.preventDefault();
        this.handlePlayAction(item.id, item.type, item);
      });
    }

    if (infoBtn) {
      infoBtn.addEventListener('click', () => {
        this.handleInfoAction(item.id, item.type);
      });
    }

    return heroSection;
  }

  /**
   * Create hero section for discovery page from trending content
   */
  createDiscoveryHero(item, index) {
    const heroSection = document.createElement('section');
    heroSection.className = 'hero';
    heroSection.dataset.index = index;
    heroSection.dataset.contentId = item.tmdbId || item.id;
    heroSection.dataset.contentType = item.type;

    const backdropUrl = item.backdropUrl
      || item.posterUrl
      || 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080"%3E%3Crect fill="%23222" width="1920" height="1080"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="48" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E';

    // Handle genres - they can be strings or objects with name property
    const genres = Array.isArray(item.genres) && item.genres.length > 0
      ? item.genres.map(g => typeof g === 'string' ? g : g.name || g.Name).filter(Boolean).join(' • ')
      : 'Trending';
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : '';
    const rating = item.voteAverage ? `★ ${item.voteAverage.toFixed(1)}` : '';
    const type = item.type === 'movie' ? 'Movie' : 'Series';

    const meta = [type, year, rating].filter(Boolean);

    heroSection.innerHTML = `
      <div class="hero-background" style="background-image: url('${backdropUrl}')"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">🔥 Trending • ${genres}</div>
          <h1 class="hero-title">${item.title}</h1>
          <div class="hero-meta">${meta.map((m) => `<span>${m}</span>`).join('')}</div>
          <p class="hero-description">${item.overview || 'No description available.'}</p>
          <div class="hero-actions">
            <button class="cta primary" data-action="queue">
              <span>+ Add to Queue</span>
            </button>
            <button class="cta ghost" data-action="info">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Discover</span> Available to download</div>
      </div>
    `;

    // Add event listeners
    const queueBtn = heroSection.querySelector('[data-action="queue"]');
    const infoBtn = heroSection.querySelector('[data-action="info"]');

    if (queueBtn) {
      queueBtn.addEventListener('click', () => {
        this.handleQueueAction(item.tmdbId || item.id, item.type);
      });
    }

    if (infoBtn) {
      infoBtn.addEventListener('click', () => {
        this.handleInfoAction(item.tmdbId || item.id, item.type);
      });
    }

    return heroSection;
  }

  /**
   * Create hero section from mock data (for discovery page)
   */
  createHeroFromMock(hero, index) {
    const heroSection = document.createElement('section');
    heroSection.className = 'hero';
    heroSection.dataset.index = index;

    heroSection.innerHTML = `
      <div class="hero-background" style="background-image: ${hero.background}"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">${hero.tag}</div>
          <h1 class="hero-title">${hero.title}</h1>
          <div class="hero-meta">${hero.meta.map((item) => `<span>${item}</span>`).join('')}</div>
          <p class="hero-description">${hero.description}</p>
          <div class="hero-actions">
            <button class="cta primary">
              <span>Remind Me</span>
            </button>
            <button class="cta ghost">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>New</span> ${hero.secondary}</div>
      </div>
    `;

    return heroSection;
  }

  /**
   * Create empty hero when no content available
   */
  createEmptyHero() {
    const heroSection = document.createElement('section');
    heroSection.className = 'hero';
    heroSection.dataset.index = 0;

    heroSection.innerHTML = `
      <div class="hero-background" style="background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%)"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">Your Library</div>
          <h1 class="hero-title">No Content Yet</h1>
          <div class="hero-meta"><span>Empty Library</span></div>
          <p class="hero-description">Your library is empty. Go to Discovery to find and download content to watch!</p>
          <div class="hero-actions">
            <button class="cta primary" onclick="window.location.href='#'; document.querySelector('[data-hero=\\'discover\\']').click();">
              <span>Browse Discovery</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Tip</span> Download content to start watching</div>
      </div>
    `;

    return heroSection;
  }



  updateCarouselPosition() {
    const heroes = this.heroCarouselTrack.querySelectorAll('.hero');
    heroes.forEach((hero, index) => {
      const offset = (index - this.currentHeroIndex) * 100;
      hero.style.transform = `translateX(${offset}%)`;
      hero.style.opacity = index === this.currentHeroIndex ? '1' : '0';
      hero.style.scale = index === this.currentHeroIndex ? '1' : '0.9';
      hero.style.zIndex = index === this.currentHeroIndex ? '2' : '0';
    });
  }

  goToSlide(index) {
    const heroCount = this.heroCarouselTrack.querySelectorAll('.hero').length;

    if (index < 0) {
      this.currentHeroIndex = heroCount - 1;
    } else if (index >= heroCount) {
      this.currentHeroIndex = 0;
    } else {
      this.currentHeroIndex = index;
    }

    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    this.updateFocusedHero();
  }

  updateAmbilightForCurrentSlide() {
    const heroes = this.heroCarouselTrack.querySelectorAll('.hero');
    const currentHero = heroes[this.currentHeroIndex];

    if (!currentHero) return;

    const heroBackground = currentHero.querySelector('.hero-background');
    const backgroundImage = heroBackground ? heroBackground.style.backgroundImage : '';

    if (this.root) {
      this.root.style.setProperty('--hero-bg-image', backgroundImage);
    }

    if (this.activeAmbilightLayer === 1) {
      // Set the background image on the inactive layer first
      this.ambilightLayer2.style.backgroundImage = backgroundImage;

      // Force a reflow to ensure the background is set before transition
      void this.ambilightLayer2.offsetWidth;

      // Now trigger the transition
      this.ambilightLayer2.classList.add('active');
      this.ambilightLayer1.classList.remove('active');
      this.activeAmbilightLayer = 2;
    } else {
      // Set the background image on the inactive layer first
      this.ambilightLayer1.style.backgroundImage = backgroundImage;

      // Force a reflow to ensure the background is set before transition
      void this.ambilightLayer1.offsetWidth;

      // Now trigger the transition
      this.ambilightLayer1.classList.add('active');
      this.ambilightLayer2.classList.remove('active');
      this.activeAmbilightLayer = 1;
    }
  }

  updateFocusedHero() {
    const allHeroes = this.heroCarouselTrack.querySelectorAll('.hero');
    allHeroes.forEach((hero, index) => {
      hero.classList.toggle('focused', index === this.currentHeroIndex);
    });
    this.focusedHeroElement = allHeroes[this.currentHeroIndex];
  }

  async switchCategory(category) {
    this.currentCategory = category;
    this.currentHeroIndex = 0;

    // Save current page to state
    stateManager.currentPage = category;
    stateManager.saveState();

    // For discovery page, check connection status
    if (category === 'discover') {
      // Check if we can reach the API
      const isOnline = await apiClient.checkConnection();

      if (!isOnline) {
        console.log('Discovery page - API is offline, showing offline message');
        this.showDiscoveryOfflinePage();
        return;
      }
    }

    // Load content for new category
    await this.loadContent();

    // Render page based on category with unique layouts
    switch (category) {
      case 'home':
        await this.renderHomePage();
        break;
      case 'discover':
        await this.renderDiscoverPage();
        break;
      case 'movies':
        await this.renderMoviesPage();
        break;
      case 'shows':
        await this.renderShowsPage();
        break;
      case 'my':
        await this.renderMyListPage();
        break;
      default:
        await this.renderHomePage();
    }
  }

  async renderCards(filter) {
    const row = document.getElementById('spotlight-row');
    if (!row) return; // Element doesn't exist on this page
    row.innerHTML = '';

    // Check if we're offline and on discovery page
    if (this.currentCategory === 'discover' && (stateManager.isOffline || apiClient.isOffline)) {
      // Create empty movie hub to maintain structure
      const movieHub = document.createElement('div');
      movieHub.className = 'movie-hub';

      const offlineMessage = document.createElement('div');
      offlineMessage.style.cssText = 'text-align: center; padding: 60px 20px; color: #999; width: 100%;';
      offlineMessage.innerHTML = `
        <div style="font-size: 48px; margin-bottom: 20px;">📡</div>
        <h2 style="color: #fff; margin-bottom: 20px;">Discovery Features Offline</h2>
        <p style="font-size: 18px; margin-bottom: 10px;">Discovery features require an internet connection.</p>
        <p style="font-size: 16px; margin-bottom: 30px;">We'll automatically retry in 10 minutes.</p>
        <button id="retry-connection-btn" style="
          background: #e50914;
          color: white;
          border: none;
          padding: 12px 32px;
          font-size: 16px;
          border-radius: 4px;
          cursor: pointer;
          font-weight: 600;
          transition: background 0.2s;
        " onmouseover="this.style.background='#f40612'" onmouseout="this.style.background='#e50914'">
          Retry Now
        </button>
        <p style="font-size: 14px; margin-top: 40px; color: #666;">
          Your downloaded content is still available in Home, Movies, Series, and My List.
        </p>
      `;

      movieHub.appendChild(offlineMessage);
      row.appendChild(movieHub);

      // Add retry button handler
      document.getElementById('retry-connection-btn')?.addEventListener('click', async () => {
        const btn = document.getElementById('retry-connection-btn');
        if (btn) {
          btn.textContent = 'Checking...';
          btn.disabled = true;
        }

        const isOnline = await apiClient.checkConnection();

        if (isOnline) {
          await this.refreshContent();
        } else {
          if (btn) {
            btn.textContent = 'Still Offline - Try Again';
            btn.disabled = false;
          }
        }
      });

      return;
    }

    const movieHub = document.createElement('div');
    movieHub.className = 'movie-hub';

    // Inject "Continue Watching" row if history exists
    const history = this.contentData.watchHistory;
    if (history && history.length > 0) {
      const historySection = document.createElement('div');
      historySection.className = 'history-carousel-section';
      historySection.innerHTML = `
        <h2 style="color: #fff; margin: 20px 0 10px 0; font-size: 24px;">Continue Watching</h2>
      `;

      const historyHub = document.createElement('div');
      historyHub.className = 'movie-hub';

      history.forEach((historyItem, index) => {
        // historyItem is WatchHistoryDto: { content: ..., positionTicks: ..., watchedPercentage: ... }
        let item = historyItem.content || historyItem;

        // Convert PositionTicks to seconds (1 tick = 100 nanoseconds, so 10,000,000 ticks = 1 second)
        const progressSeconds = historyItem.positionTicks
          ? Math.floor(historyItem.positionTicks / 10_000_000)
          : 0;

        // Ensure progress is attached to the item for the card to render
        if (!item.watchProgress) {
          item.watchProgress = {
            progressSeconds: progressSeconds,
            durationSeconds: item.runtime ? item.runtime * 60 : null,
            watchedPercentage: historyItem.watchedPercentage || 0,
            completed: historyItem.isCompleted || false
          };
        }

        const card = this.createContentCard(item, index, false);
        historyHub.appendChild(card);
      });

      historySection.appendChild(historyHub);
      row.appendChild(historySection);
    }

    let contentItems = [];
    let showDiscoveryCarousel = false;

    // Get content based on current category
    switch (this.currentCategory) {
      case 'home':
        // Home shows downloaded content
        contentItems = this.contentData.recentlyAdded || [];
        showDiscoveryCarousel = true;
        break;
      case 'discover':
        // Discovery shows online content for downloading - NO DUPLICATES
        // Combine all discovery content but remove duplicates by ID
        const trendingMovies = this.contentData.trending?.movies || [];
        const trendingSeries = this.contentData.trending?.series || [];

        // Handle both array and object with items property
        const popularMovies = Array.isArray(this.contentData.popularMovies)
          ? this.contentData.popularMovies
          : (this.contentData.popularMovies?.items || []);
        const popularSeries = Array.isArray(this.contentData.popularSeries)
          ? this.contentData.popularSeries
          : (this.contentData.popularSeries?.items || []);

        const allDiscovery = [
          ...trendingMovies,
          ...trendingSeries,
          ...popularMovies,
          ...popularSeries
        ];

        // Remove duplicates based on tmdbId
        const seenIds = new Set();
        contentItems = allDiscovery.filter(item => {
          const id = item.tmdbId || item.id;
          if (seenIds.has(id)) {
            return false;
          }
          seenIds.add(id);
          return true;
        });
        break;
      case 'shows':
        // Shows page - downloaded series only
        contentItems = this.contentData.series || [];
        break;
      case 'movies':
        // Movies page - downloaded movies only
        contentItems = this.contentData.movies || [];
        break;
      case 'my':
        // My List - downloaded content from watchlist
        contentItems = this.contentData.watchlist || [];
        break;
      default:
        contentItems = [];
    }

    // Filter by type if needed
    const filteredContent = filter === 'all'
      ? contentItems
      : contentItems.filter(item => item.type === filter);

    // Show discovery carousel on home page if available and online
    if (showDiscoveryCarousel && this.contentData.discoverPreview?.length > 0 && !stateManager.isOffline && !apiClient.isOffline) {
      const discoverySection = document.createElement('div');
      discoverySection.className = 'discovery-carousel-section';
      discoverySection.innerHTML = `
        <h2 style="color: #fff; margin: 20px 0 10px 0; font-size: 24px;">Discover New Content</h2>
      `;

      const discoveryHub = document.createElement('div');
      discoveryHub.className = 'movie-hub';

      this.contentData.discoverPreview.forEach((item, index) => {
        const card = this.createContentCard(item, index, true);
        discoveryHub.appendChild(card);
      });

      discoverySection.appendChild(discoveryHub);
      row.appendChild(discoverySection);

      // Add separator
      const separator = document.createElement('h2');
      separator.style.cssText = 'color: #fff; margin: 40px 0 10px 0; font-size: 24px;';
      separator.textContent = 'Your Library';
      row.appendChild(separator);
    }

    // Show main content
    if (filteredContent.length === 0) {
      const emptyMessage = document.createElement('div');
      emptyMessage.style.cssText = 'text-align: center; padding: 60px 20px; color: #999;';

      if (this.currentCategory === 'home') {
        emptyMessage.innerHTML = `
          <h2 style="color: #fff; margin-bottom: 20px;">Your Library is Empty</h2>
          <p style="font-size: 18px;">Go to Discovery to find and download content!</p>
        `;
      } else if (this.currentCategory === 'my') {
        emptyMessage.innerHTML = `
          <h2 style="color: #fff; margin-bottom: 20px;">Your List is Empty</h2>
          <p style="font-size: 18px;">Add content to your list to see it here.</p>
        `;
      } else {
        emptyMessage.innerHTML = `
          <h2 style="color: #fff; margin-bottom: 20px;">No Content Found</h2>
          <p style="font-size: 18px;">Download some content to see it here!</p>
        `;
      }

      row.appendChild(emptyMessage);
      return;
    }

    filteredContent.forEach((item, index) => {
      const card = this.createContentCard(item, index, this.currentCategory === 'discover');
      movieHub.appendChild(card);
    });

    row.appendChild(movieHub);

    // Setup lazy loading and card handlers
    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Create a content card element
   */
  createContentCard(item, index, isDiscoveryContent = false) {
    const movieCard = document.createElement('article');
    movieCard.className = 'movie-card';
    movieCard.dataset.index = index;
    movieCard.dataset.contentId = item.id || item.tmdbId;
    movieCard.dataset.contentType = item.type;

    const posterUrl = item.posterUrl
      || item.image
      || 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450"%3E%3Crect fill="%23222" width="300" height="450"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="24" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E';

    const backdropUrl = item.backdropUrl
      || item.expandedImage
      || posterUrl;

    const genres = Array.isArray(item.genres) ? item.genres.join(', ') : (item.genre || 'Unknown');
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : (item.year || 'N/A');
    const duration = item.runtime ? `${item.runtime}m` : (item.duration || 'N/A');
    const rating = item.voteAverage ? `★ ${item.voteAverage.toFixed(1)}` : (item.rating || 'N/A');

    movieCard.innerHTML = `
        <div class="movie-poster-container">
          <img data-src="${posterUrl}" alt="${item.title}" class="movie-poster movie-poster-regular" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 450'%3E%3Crect fill='%23333' width='300' height='450'/%3E%3C/svg%3E" />
          <img data-src="${backdropUrl}" alt="${item.title}" class="movie-poster movie-poster-expanded" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1920 1080'%3E%3Crect fill='%23333' width='1920' height='1080'/%3E%3C/svg%3E" />
        </div>
        <div class="movie-overlay"></div>
        <div class="movie-compact-title">${item.title}</div>
        <div class="movie-info">
          <h3 class="movie-title">${item.title}</h3>
          <div class="movie-meta">
            <span>${genres}</span>
            <span>${year}</span>
            <span>${duration}</span>
            <span>${rating}</span>
          </div>
          <p class="movie-description">${item.overview || item.description || 'No description available.'}</p>
        </div>
      `;

    movieHub.appendChild(movieCard);
    ;

    row.appendChild(movieHub);

    // Setup lazy loading and card handlers
    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  setupScrollHandler() {
    const handleScroll = () => {
      if (!this.topNav) return;

      const threshold = 640 * 0.45;
      if (window.scrollY > threshold) {
        this.topNav.classList.add('is-solid');
      } else {
        this.topNav.classList.remove('is-solid');
      }
    };

    handleScroll();
    window.addEventListener('scroll', handleScroll, { passive: true });
  }

  /**
   * Create a content card element
   */
  createContentCard(item, index, isDiscoveryContent = false) {
    const movieCard = document.createElement('article');
    movieCard.className = 'movie-card';
    movieCard.dataset.index = index;
    movieCard.dataset.contentId = item.id || item.tmdbId;
    movieCard.dataset.contentType = item.type;
    movieCard.dataset.isDiscovery = isDiscoveryContent;

    const posterUrl = item.posterUrl
      || item.image
      || 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450"%3E%3Crect fill="%23222" width="300" height="450"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="24" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E';

    const backdropUrl = item.backdropUrl
      || item.expandedImage
      || posterUrl;

    // Handle genres - they can be strings or objects with name property
    const genres = Array.isArray(item.genres) && item.genres.length > 0
      ? item.genres.slice(0, 2).map(g => typeof g === 'string' ? g : g.name || g.Name).filter(Boolean).join(', ')
      : (item.genre || '');
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : (item.year || '');
    const duration = item.runtime ? `${item.runtime}m` : (item.duration || '');
    const rating = item.voteAverage ? `★ ${item.voteAverage.toFixed(1)}` : (item.rating || '');

    // Build meta array with only non-empty values
    const metaItems = [genres, year, duration, rating].filter(Boolean);

    // Build progress bar HTML if watch progress exists
    const progressBarHtml = item.watchProgress && item.watchProgress.progressSeconds > 0 && !item.watchProgress.completed
      ? `<div class="watch-progress-bar">
           <div class="watch-progress-fill" style="width: ${Math.min(item.watchProgress.watchedPercentage || 0, 100)}%"></div>
         </div>`
      : '';

    movieCard.innerHTML = `
      <div class="movie-poster-container">
        <img data-src="${posterUrl}" alt="${item.title}" class="movie-poster movie-poster-regular" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 450'%3E%3Crect fill='%23333' width='300' height='450'/%3E%3C/svg%3E" />
        <img data-src="${backdropUrl}" alt="${item.title}" class="movie-poster movie-poster-expanded" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1920 1080'%3E%3Crect fill='%23333' width='1920' height='1080'/%3E%3C/svg%3E" />
        ${progressBarHtml}
      </div>
      <div class="movie-overlay"></div>
      <div class="movie-compact-title">${item.title}</div>
      <div class="movie-info">
        <h3 class="movie-title">${item.title}</h3>
        <div class="movie-meta">
          ${metaItems.map(item => `<span>${item}</span>`).join('')}
        </div>
        <p class="movie-description">${item.overview || item.description || 'No description available.'}</p>
      </div>
    `;

    return movieCard;
  }

  /**
   * Setup lazy loading for images using Intersection Observer
   */
  setupLazyLoading() {
    // Disconnect existing observer if any
    if (this.imageObserver) {
      this.imageObserver.disconnect();
    }

    this.imageObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target;
          const src = img.dataset.src;

          if (src) {
            // Load the image
            img.src = src;
            img.removeAttribute('data-src');
            observer.unobserve(img);
          }
        }
      });
    }, {
      rootMargin: '200px 200px',
      threshold: 0.01
    });

    // Observe all images with data-src attribute
    const images = document.querySelectorAll('img[data-src]');
    images.forEach(img => {
      this.imageObserver.observe(img);
    });

    return this.imageObserver;
  }

  /**
   * Setup card click handlers for expansion
   */
  setupCardHandlers() {
    const cards = document.querySelectorAll('.movie-card');

    cards.forEach((card, index) => {
      // Make cards focusable
      card.setAttribute('tabindex', '0');
      card.dataset.cardIndex = index;

      // Click handler - open modal directly
      card.addEventListener('click', () => {
        const contentId = card.dataset.contentId;
        const contentType = card.dataset.contentType;
        const isDiscovery = card.dataset.isDiscovery === 'true';
        this.contentModal.show(contentId, contentType, isDiscovery);
      });

      // Enter key handler
      card.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          const contentId = card.dataset.contentId;
          const contentType = card.dataset.contentType;
          const isDiscovery = card.dataset.isDiscovery === 'true';
          this.contentModal.show(contentId, contentType, isDiscovery);
        }
      });
    });
  }

  /**
   * Handle card click for expansion and actions
   * @deprecated - Now opens modal directly instead of expanding
   */
  handleCardClick(card) {
    // This method is deprecated - cards now open modal directly
    const contentId = card.dataset.contentId;
    const contentType = card.dataset.contentType;
    const isDiscovery = card.dataset.isDiscovery === 'true';
    this.contentModal.show(contentId, contentType, isDiscovery);
  }

  /**
   * Add action buttons to expanded card
   * @deprecated - Cards now open modal directly, no expansion needed
   */
  addCardActions(card, contentId, contentType) {
    // This method is deprecated - cards now open modal directly
  }

  /**
   * Handle play action
   */
  async handlePlayAction(contentId, contentType, itemData = null) {
    console.log('Play action:', contentId, contentType);
    const type = (contentType || itemData?.type || 'movie').toLowerCase();

    if (type === 'series' || type === 'tv') {
      try {
        const profile = this.profileManager?.getCurrentProfile();
        const profileId = profile?.id;
        
        // Fetch series details to find available episodes
        const seriesData = await apiClient.getLibraryItem(contentId, profileId);
        const episodes = seriesData?.episodes || itemData?.episodes || [];

        let episodeToPlay = null;
        if (episodes.length > 0) {
          // Find first episode with a file or available
          episodeToPlay = episodes.find(e => e.hasFile || e.available || e.filePath) || episodes[0];
        }

        if (episodeToPlay) {
          const episodeId = episodeToPlay.id || episodeToPlay.episodeId || episodeToPlay.tmdbId;
          let playerUrl = `player.html?contentId=${contentId}&type=series&episodeId=${episodeId}`;
          if (episodeToPlay.watchProgress && episodeToPlay.watchProgress.progressSeconds > 30 && !episodeToPlay.watchProgress.completed) {
            playerUrl += `&startTime=${episodeToPlay.watchProgress.progressSeconds}`;
          }
          window.location.href = playerUrl;
          return;
        }
      } catch (err) {
        console.error('Failed to resolve series episode for play action:', err);
      }

      // If no episode found or fetch fails, open modal so user can view/select episodes
      await this.contentModal.show(contentId, type, false);
      return;
    }

    // For movies
    window.location.href = `player.html?contentId=${contentId}&type=${type}`;
  }

  /**
   * Handle queue action (Watch in a bit)
   */
  async handleQueueAction(contentId, contentType) {
    // Show modal instead of directly queuing
    await this.contentModal.show(contentId, contentType, true);
  }

  /**
   * Handle info action
   */
  async handleInfoAction(contentId, contentType) {
    // Determine if this is discovery content or library content
    // Check if we're on the discover page - hero content there is always discovery
    const isDiscovery = this.currentCategory === 'discover';

    await this.contentModal.show(contentId, contentType, isDiscovery);
  }

  getFocusedHeroElement() {
    return this.focusedHeroElement;
  }

  /**
   * Refresh content from backend
   */
  async refreshContent() {
    stateManager.clearCache();
    await this.loadContent();
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    await this.renderCards('all');
  }

  /**
   * Get content data for current category
   */
  getContentData() {
    return this.contentData;
  }

  /**
   * Show discovery offline page - simple white text message
   */
  showDiscoveryOfflinePage() {
    // Hide hero carousel
    const heroStage = document.querySelector('.hero-stage');
    if (heroStage) {
      heroStage.style.display = 'none';
    }

    // Hide spotlight section (tabs and cards)
    const spotlight = document.querySelector('.spotlight');
    if (spotlight) {
      spotlight.style.display = 'none';
    }

    // Show simple offline message
    const contentShell = document.querySelector('.content-shell');
    if (contentShell) {
      contentShell.innerHTML = `
        <div style="
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          min-height: 60vh;
          text-align: center;
          padding: 40px 20px;
        ">
          <h1 style="
            color: #fff;
            font-size: 48px;
            font-weight: 600;
            margin-bottom: 20px;
          ">Uh Oh!</h1>
          <p style="
            color: #fff;
            font-size: 24px;
            font-weight: 400;
            max-width: 600px;
            line-height: 1.5;
          ">Make sure you connected your server to the internet.</p>
          <button id="retry-discovery-btn" style="
            margin-top: 40px;
            background: #e50914;
            color: white;
            border: none;
            padding: 16px 40px;
            font-size: 18px;
            font-weight: 600;
            border-radius: 4px;
            cursor: pointer;
            transition: background 0.2s;
          " onmouseover="this.style.background='#f40612'" onmouseout="this.style.background='#e50914'">
            Retry Connection
          </button>
        </div>
      `;

      // Add retry button handler
      setTimeout(() => {
        const retryBtn = document.getElementById('retry-discovery-btn');
        if (retryBtn) {
          retryBtn.addEventListener('click', async () => {
            retryBtn.textContent = 'Checking...';
            retryBtn.disabled = true;

            const isOnline = await apiClient.checkConnection();

            if (isOnline) {
              // Reload the page
              await this.switchCategory('discover');
            } else {
              retryBtn.textContent = 'Still Offline - Try Again';
              retryBtn.disabled = false;
            }
          });
        }
      }, 100);
    }
  }

  /**
   * Show normal UI elements (hero, tabs, cards)
   */
  showNormalUI() {
    // Show hero carousel
    const heroStage = document.querySelector('.hero-stage');
    if (heroStage) {
      heroStage.style.display = '';
    }

    // Show spotlight section
    const spotlight = document.querySelector('.spotlight');
    if (spotlight) {
      spotlight.style.display = '';
    }

    // Restore content shell structure if needed
    const contentShell = document.querySelector('.content-shell');
    if (contentShell && !contentShell.querySelector('.spotlight')) {
      contentShell.innerHTML = `
        <section class="spotlight">
          <div class="spotlight-header">
            <h2>Your Next Watch</h2>
            <div class="spotlight-tabs" role="tablist">
              <button class="tab active" data-tab="all">All</button>
              <button class="tab" data-tab="series">Series</button>
              <button class="tab" data-tab="movies">Movies</button>
            </div>
          </div>
          <div class="spotlight-row" id="spotlight-row"></div>
        </section>
      `;

      // Re-setup tabs
      this.setupTabs();
    }
  }

  /**
   * Setup tabs (needed after restoring UI)
   */
  setupTabs() {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach((tab) => {
      tab.addEventListener('click', async () => {
        tabs.forEach((item) => item.classList.remove('active'));
        tab.classList.add('active');
        await this.renderCards(tab.dataset.tab);
      });
    });
  }

  // ==================== UNIQUE PAGE LAYOUTS ====================

  /**
   * Render HOME page - Hero carousel + Recently Added + Discovery Preview
   */
  async renderHomePage() {
    const heroStage = document.querySelector('.hero-stage');
    const contentShell = document.querySelector('.content-shell');

    heroStage.style.display = '';

    // Create hero carousel
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();

    // Home page layout
    const recentlyAdded = this.contentData.recentlyAdded || [];
    const discoverPreview = this.contentData.discoverPreview || [];

    contentShell.innerHTML = `
      ${recentlyAdded.length > 0 ? `
        <section class="spotlight" style="margin-top: 80px;">
          <div class="spotlight-header">
            <h2>Recently Added</h2>
          </div>
          <div class="spotlight-row">
            <div class="movie-hub" id="recently-added-hub"></div>
          </div>
        </section>
      ` : ''}
      ${discoverPreview.length > 0 ? `
        <section class="spotlight" style="margin-top: 80px;">
          <div class="spotlight-header" style="justify-content: space-between;">
            <h2>Discover New Content</h2>
            <button class="browse-all-btn" onclick="document.querySelector('[data-hero=\\'discover\\']').click()">
              Browse All →
            </button>
          </div>
          <div class="spotlight-row">
            <div class="movie-hub" id="discover-preview-hub"></div>
          </div>
        </section>
      ` : ''}
      ${recentlyAdded.length === 0 && discoverPreview.length === 0 ? `
        <div style="text-align: center; padding: 60px 20px; color: #999;">
          <h2 style="color: #fff; margin-bottom: 20px;">Your Library is Empty</h2>
          <p style="font-size: 18px;">Go to Discovery to find and download content!</p>
        </div>
      ` : ''}
    `;

    // Render recently added
    if (recentlyAdded.length > 0) {
      this.renderCarouselHub('recently-added-hub', recentlyAdded, false);
    }

    // Render discovery preview
    if (discoverPreview.length > 0) {
      this.renderCarouselHub('discover-preview-hub', discoverPreview, true);
    }

    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Render DISCOVER page - Hero carousel + Horizontal carousels with categories
   */
  async renderDiscoverPage() {
    const heroStage = document.querySelector('.hero-stage');
    const contentShell = document.querySelector('.content-shell');

    // Show hero carousel with trending content
    heroStage.style.display = '';

    // Create hero carousel from trending content
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();

    const trending = this.contentData.trending || { movies: [], series: [] };
    const popularMovies = Array.isArray(this.contentData.popularMovies)
      ? this.contentData.popularMovies
      : (this.contentData.popularMovies?.items || []);
    const popularSeries = Array.isArray(this.contentData.popularSeries)
      ? this.contentData.popularSeries
      : (this.contentData.popularSeries?.items || []);

    contentShell.innerHTML = `
      <div style="margin-top: 80px;">

        ${trending.movies.length > 0 ? `
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>🔥 Trending Movies</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="trending-movies-hub"></div>
            </div>
          </section>
        ` : ''}

        ${trending.series.length > 0 ? `
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>📺 Trending Series</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="trending-series-hub"></div>
            </div>
          </section>
        ` : ''}

        ${popularMovies.length > 0 ? `
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>⭐ Popular Movies</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="popular-movies-hub"></div>
            </div>
          </section>
        ` : ''}

        ${popularSeries.length > 0 ? `
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>🎬 Popular Series</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="popular-series-hub"></div>
            </div>
          </section>
        ` : ''}
      </div>
    `;

    // Render carousels
    this.renderCarouselHub('trending-movies-hub', trending.movies, true);
    this.renderCarouselHub('trending-series-hub', trending.series, true);
    this.renderCarouselHub('popular-movies-hub', popularMovies, true);
    this.renderCarouselHub('popular-series-hub', popularSeries, true);

    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Render MOVIES page - Horizontal carousel
   */
  async renderMoviesPage() {
    const heroStage = document.querySelector('.hero-stage');
    const contentShell = document.querySelector('.content-shell');

    heroStage.style.display = '';

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();

    const movies = this.contentData.movies || [];

    contentShell.innerHTML = `
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">Your Movies</h2>
          <div style="color: #999; font-size: 16px;">${movies.length} movies in your library</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="movies-hub"></div>
        </div>
      </section>
    `;

    this.renderCarouselHub('movies-hub', movies, false);

    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Render SHOWS page - Horizontal carousel
   */
  async renderShowsPage() {
    const heroStage = document.querySelector('.hero-stage');
    const contentShell = document.querySelector('.content-shell');

    heroStage.style.display = '';

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();

    const series = this.contentData.series || [];

    contentShell.innerHTML = `
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">Your Series</h2>
          <div style="color: #999; font-size: 16px;">${series.length} series in your library</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="shows-hub"></div>
        </div>
      </section>
    `;

    this.renderCarouselHub('shows-hub', series, false);

    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Render MY LIST page - Horizontal carousel
   */
  async renderMyListPage() {
    const heroStage = document.querySelector('.hero-stage');
    const contentShell = document.querySelector('.content-shell');

    heroStage.style.display = '';

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();

    const watchlist = this.contentData.watchlist || [];

    contentShell.innerHTML = `
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">My List</h2>
          <div style="color: #999; font-size: 16px;">${watchlist.length} items in your list</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="mylist-hub"></div>
        </div>
      </section>
    `;

    this.renderCarouselHub('mylist-hub', watchlist, false);

    this.setupLazyLoading();
    this.setupCardHandlers();
  }

  /**
   * Helper to render carousel hub
   */
  renderCarouselHub(hubId, items, isDiscoveryContent = false) {
    const hub = document.getElementById(hubId);
    if (!hub || !items || items.length === 0) return;

    // Clear existing content
    hub.innerHTML = '';

    items.forEach((item, index) => {
      const card = this.createContentCard(item, index, isDiscoveryContent);
      hub.appendChild(card);
    });
  }
}
