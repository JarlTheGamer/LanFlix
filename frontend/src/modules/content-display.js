import { HEROES, MOVIES } from './data.js';
import stateManager from './data.js';
import apiClient from './api-client.js';

export class ContentDisplay {
  constructor(profileManager) {
    this.profileManager = profileManager;
    this.currentCategory = 'home';
    this.currentHeroIndex = 0;
    this.activeAmbilightLayer = 1;
    this.focusedHeroElement = null;
    this.contentData = {};
    this.isLoading = false;

    this.root = document.documentElement;
    this.heroCarouselTrack = document.getElementById('hero-carousel-track');
    this.heroAmbilight = document.getElementById('hero-ambilight');
    this.ambilightLayer1 = document.getElementById('ambilight-layer-1');
    this.ambilightLayer2 = document.getElementById('ambilight-layer-2');
    this.topNav = document.querySelector('.top-nav');
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
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    await this.renderCards('all');
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

  async loadContent() {
    if (this.isLoading) return;
    this.isLoading = true;

    try {
      const profileId = this.profileManager.selectedProfileId;

      switch (this.currentCategory) {
        case 'home':
          // Home shows downloaded content + small discovery carousel
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

          this.contentData = {
            recentlyAdded: recentlyAdded.items || [],
            discoverPreview: trendingItems
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
          this.contentData = {
            series: seriesData.items || []
          };
          break;

        case 'movies':
          // Movies page displays downloaded movies only
          const moviesData = await stateManager.getLibraryMovies({ limit: 100 });
          this.contentData = {
            movies: moviesData.items || []
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

    // For discovery page, check if offline first
    if (this.currentCategory === 'discover') {
      console.log('Discovery page - checking offline status:', {
        apiClientOffline: apiClient.isOffline,
        stateManagerOffline: stateManager.isOffline
      });
      
      if (apiClient.isOffline || stateManager.isOffline) {
        // Show offline hero for discovery
        console.log('Showing offline discovery hero');
        const offlineHero = this.createOfflineDiscoveryHero();
        this.heroCarouselTrack.appendChild(offlineHero);
      } else {
        console.log('Showing mock discovery heroes');
        const heroes = HEROES[this.currentCategory] || [];
        heroes.forEach((hero, index) => {
          const heroSection = this.createHeroFromMock(hero, index);
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
    if (this.focusedHeroElement) {
      this.focusedHeroElement.classList.add('focused');
    }
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

    const backdropUrl = item.backdropPath
      ? `https://image.tmdb.org/t/p/original${item.backdropPath}`
      : item.posterPath
        ? `https://image.tmdb.org/t/p/original${item.posterPath}`
        : 'https://via.placeholder.com/1920x1080?text=No+Image';

    const genres = Array.isArray(item.genres) ? item.genres.join(' • ') : (item.genre || 'Unknown');
    const year = item.releaseDate ? new Date(item.releaseDate).getFullYear() : (item.year || '');
    const duration = item.runtime ? `${Math.floor(item.runtime / 60)}h ${item.runtime % 60}m` : (item.duration || '');
    const rating = item.contentRating || item.rating || 'NR';
    const type = item.type === 'movie' ? 'Movie' : 'Series';

    const meta = [type, year, rating, duration].filter(Boolean);

    heroSection.innerHTML = `
      <div class="hero-background" style="background-image: url(${backdropUrl})"></div>
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
      playBtn.addEventListener('click', () => {
        window.location.href = `player.html?contentId=${item.id}&type=${item.type}`;
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

  /**
   * Create offline hero for discovery page
   */
  createOfflineDiscoveryHero() {
    const heroSection = document.createElement('section');
    heroSection.className = 'hero';
    heroSection.dataset.index = 0;

    heroSection.innerHTML = `
      <div class="hero-background" style="background: linear-gradient(135deg, #1a1a2a 0%, #2d1a1a 100%)"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">Connection Issue</div>
          <h1 class="hero-title">Uh Oh! Looks Like Your Connection Didn't Work</h1>
          <div class="hero-meta"><span>Offline</span><span>Discovery Unavailable</span></div>
          <p class="hero-description">Discovery features require an internet connection to browse new content. We'll automatically retry in 10 minutes, or you can try again now.</p>
          <div class="hero-actions">
            <button class="cta primary" id="hero-retry-btn">
              <span>🔄 Retry Connection</span>
            </button>
            <button class="cta ghost" onclick="document.querySelector('[data-hero=\\'home\\']').click();">
              <span>Go to Home</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Tip</span> Your downloaded content is still available in Home, Movies, and Series</div>
      </div>
    `;

    // Add retry button handler
    setTimeout(() => {
      const retryBtn = document.getElementById('hero-retry-btn');
      if (retryBtn) {
        retryBtn.addEventListener('click', async () => {
          retryBtn.innerHTML = '<span>⏳ Checking...</span>';
          retryBtn.disabled = true;

          const isOnline = await apiClient.checkConnection();

          if (isOnline) {
            await this.refreshContent();
          } else {
            retryBtn.innerHTML = '<span>❌ Still Offline - Try Again</span>';
            retryBtn.disabled = false;
          }
        });
      }
    }, 100);

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
      this.ambilightLayer2.style.backgroundImage = backgroundImage;
      this.ambilightLayer2.classList.add('active');
      this.ambilightLayer1.classList.remove('active');
      this.activeAmbilightLayer = 2;
    } else {
      this.ambilightLayer1.style.backgroundImage = backgroundImage;
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

    // Check if discovery page is offline - show simple message
    if (category === 'discover' && (apiClient.isOffline || stateManager.isOffline)) {
      this.showDiscoveryOfflinePage();
      return;
    }

    // Load content for new category
    await this.loadContent();

    // Show normal UI elements
    this.showNormalUI();

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    await this.renderCards('all');
  }

  async renderCards(filter) {
    const row = document.getElementById('spotlight-row');
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

    const posterUrl = item.posterPath
      ? `https://image.tmdb.org/t/p/w500${item.posterPath}`
      : item.image || 'https://via.placeholder.com/300x450?text=No+Image';

    const backdropUrl = item.backdropPath
      ? `https://image.tmdb.org/t/p/original${item.backdropPath}`
      : item.expandedImage || posterUrl;

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

    const posterUrl = item.posterPath
      ? `https://image.tmdb.org/t/p/w500${item.posterPath}`
      : item.image || 'https://via.placeholder.com/300x450?text=No+Image';

    const backdropUrl = item.backdropPath
      ? `https://image.tmdb.org/t/p/original${item.backdropPath}`
      : item.expandedImage || posterUrl;

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

    return movieCard;
  }

  /**
   * Setup lazy loading for images using Intersection Observer
   */
  setupLazyLoading() {
    const imageObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target;
          const src = img.dataset.src;

          if (src) {
            img.src = src;
            img.removeAttribute('data-src');
            observer.unobserve(img);
          }
        }
      });
    }, {
      rootMargin: '50px 0px',
      threshold: 0.01
    });

    // Observe all images with data-src attribute
    document.querySelectorAll('img[data-src]').forEach(img => {
      imageObserver.observe(img);
    });

    return imageObserver;
  }

  /**
   * Setup card click handlers for expansion
   */
  setupCardHandlers() {
    const cards = document.querySelectorAll('.movie-card');

    cards.forEach(card => {
      card.addEventListener('click', () => {
        this.handleCardClick(card);
      });
    });
  }

  /**
   * Handle card click for expansion and actions
   */
  handleCardClick(card) {
    const contentId = card.dataset.contentId;
    const contentType = card.dataset.contentType;

    // Toggle expansion
    const wasExpanded = card.classList.contains('expanded');

    // Collapse all other cards
    document.querySelectorAll('.movie-card').forEach(c => {
      c.classList.remove('expanded');
    });

    // Expand this card if it wasn't expanded
    if (!wasExpanded) {
      card.classList.add('expanded');

      // Add action buttons if not already present
      if (!card.querySelector('.card-actions')) {
        this.addCardActions(card, contentId, contentType);
      }
    }
  }

  /**
   * Add action buttons to expanded card
   */
  addCardActions(card, contentId, contentType) {
    const movieInfo = card.querySelector('.movie-info');
    if (!movieInfo) return;

    const isDiscoveryContent = card.dataset.isDiscovery === 'true';

    const actionsDiv = document.createElement('div');
    actionsDiv.className = 'card-actions';

    // Show different buttons based on whether it's discovery or library content
    if (isDiscoveryContent) {
      actionsDiv.innerHTML = `
        <button class="card-action-btn queue-btn" data-action="queue">
          <span>+</span> Watch in a bit
        </button>
        <button class="card-action-btn info-btn" data-action="info">
          <span>ℹ</span> More Info
        </button>
      `;
    } else {
      actionsDiv.innerHTML = `
        <button class="card-action-btn play-btn" data-action="play">
          <span>▶</span> Play
        </button>
        <button class="card-action-btn info-btn" data-action="info">
          <span>ℹ</span> More Info
        </button>
      `;
    }

    movieInfo.appendChild(actionsDiv);

    // Add event listeners
    actionsDiv.querySelector('.play-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.handlePlayAction(contentId, contentType);
    });

    actionsDiv.querySelector('.queue-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.handleQueueAction(contentId, contentType);
    });

    actionsDiv.querySelector('.info-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.handleInfoAction(contentId, contentType);
    });
  }

  /**
   * Handle play action
   */
  async handlePlayAction(contentId, contentType) {
    console.log('Play:', contentId, contentType);
    // This will be implemented in the video player module
    const streamUrl = apiClient.getStreamUrl(contentId);
    window.location.href = `player.html?contentId=${contentId}&type=${contentType}`;
  }

  /**
   * Handle queue action (Watch in a bit)
   */
  async handleQueueAction(contentId, contentType) {
    try {
      const profileId = this.profileManager.selectedProfileId;
      if (!profileId) {
        alert('Please select a profile first');
        return;
      }

      // Get content details first
      const content = await apiClient.getContentDetails(contentId, contentType, profileId);

      // Queue the download
      await apiClient.queueDownload(
        contentId,
        profileId,
        contentType,
        content.title,
        content.releaseDate ? new Date(content.releaseDate).getFullYear() : null
      );

      alert(`"${content.title}" has been added to your download queue!`);
    } catch (error) {
      console.error('Failed to queue download:', error);
      alert('Failed to add to download queue. Please try again.');
    }
  }

  /**
   * Handle info action
   */
  async handleInfoAction(contentId, contentType) {
    try {
      const profileId = this.profileManager.selectedProfileId;
      const content = await apiClient.getContentDetails(contentId, contentType, profileId);

      // Show detailed info modal (to be implemented)
      console.log('Show info for:', content);
      alert(`Title: ${content.title}\n\nOverview: ${content.overview || 'No description available.'}`);
    } catch (error) {
      console.error('Failed to get content details:', error);
      alert('Failed to load content details.');
    }
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
}
