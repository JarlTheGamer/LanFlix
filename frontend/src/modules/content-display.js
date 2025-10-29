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
    // Restore saved page state
    const savedPage = stateManager.currentPage;
    if (savedPage) {
      this.currentCategory = savedPage;
    }

    await this.loadContent();
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    await this.renderCards('all');
    this.setupScrollHandler();

    // Restore scroll position
    if (stateManager.scrollPosition) {
      setTimeout(() => {
        window.scrollTo(0, stateManager.scrollPosition);
      }, 100);
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
          const [recentlyAdded, discoverPreview] = await Promise.all([
            stateManager.getRecentlyAdded(20),
            stateManager.getDiscoverContent(profileId).catch(() => ({ trending: [] }))
          ]);
          this.contentData = {
            recentlyAdded: recentlyAdded.items || [],
            discoverPreview: (discoverPreview.trending || []).slice(0, 10) // Only 10 items for preview
          };
          break;

        case 'discover':
          // Discovery shows only online content for downloading
          const discoverData = await stateManager.getDiscoverContent(profileId);
          this.contentData = {
            trending: discoverData.trending || [],
            popularMovies: discoverData.popular?.movies || [],
            popularSeries: discoverData.popular?.series || []
          };
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
    const heroes = HEROES[this.currentCategory];
    
    heroes.forEach((hero, index) => {
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

      this.heroCarouselTrack.appendChild(heroSection);
    });

    this.focusedHeroElement = this.heroCarouselTrack.querySelector('.hero');
    if (this.focusedHeroElement) {
      this.focusedHeroElement.classList.add('focused');
    }
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
    const heroes = HEROES[this.currentCategory];
    if (index < 0) {
      this.currentHeroIndex = heroes.length - 1;
    } else if (index >= heroes.length) {
      this.currentHeroIndex = 0;
    } else {
      this.currentHeroIndex = index;
    }

    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    this.updateFocusedHero();
  }

  updateAmbilightForCurrentSlide() {
    const heroes = HEROES[this.currentCategory];
    const hero = heroes[this.currentHeroIndex];
    
    if (this.root) {
      this.root.style.setProperty('--hero-bg-image', hero.background);
    }

    if (this.activeAmbilightLayer === 1) {
      this.ambilightLayer2.style.backgroundImage = hero.background;
      this.ambilightLayer2.classList.add('active');
      this.ambilightLayer1.classList.remove('active');
      this.activeAmbilightLayer = 2;
    } else {
      this.ambilightLayer1.style.backgroundImage = hero.background;
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

    // Load content for new category
    await this.loadContent();

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    await this.renderCards('all');
  }

  async renderCards(filter) {
    const row = document.getElementById('spotlight-row');
    row.innerHTML = '';

    // Check if we're offline and on discovery page
    if (this.currentCategory === 'discover' && stateManager.isOffline) {
      row.innerHTML = `
        <div style="text-align: center; padding: 60px 20px; color: #999;">
          <h2 style="color: #fff; margin-bottom: 20px;">We're Currently Offline</h2>
          <p style="font-size: 18px; margin-bottom: 10px;">Discovery features require an internet connection.</p>
          <p style="font-size: 16px;">Check back later to browse and download new content.</p>
          <p style="font-size: 14px; margin-top: 30px;">Your downloaded content is still available in Home, Movies, Series, and My List.</p>
        </div>
      `;
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
        // Discovery shows online content for downloading
        contentItems = [
          ...(this.contentData.trending || []),
          ...(this.contentData.popularMovies || []),
          ...(this.contentData.popularSeries || [])
        ];
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

    // Show discovery carousel on home page if available
    if (showDiscoveryCarousel && this.contentData.discoverPreview?.length > 0 && !stateManager.isOffline) {
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
}
