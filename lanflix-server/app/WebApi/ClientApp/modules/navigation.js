export class Navigation {
  constructor(contentDisplay, profileManager) {
    this.contentDisplay = contentDisplay;
    this.profileManager = profileManager;

    this.focusedElement = 'menu';
    this.focusedMenuIndex = 1;
    this.heroButtonIndex = 0; // 0 = Play, 1 = More Info
    this.focusedTabIndex = 0;
    this.focusedCardIndex = 0;
    this.focusedCarouselIndex = 0; // Track which carousel is focused
    this.lastMenuIndex = 1;

    // Modal navigation state
    this.modalFocusedElement = 'actions'; // 'actions', 'seasons', 'episodes'
    this.modalFocusedActionIndex = 0;
    this.modalFocusedSeasonIndex = 0;
    this.modalFocusedEpisodeIndex = 0;

    // Page transition state
    this.isTransitioning = false;
    this.transitionDuration = 300; // ms

    // Track if user has initiated keyboard/remote navigation
    this.hasUserNavigated = false;

    // Detect if device has touch capability
    this.isTouchDevice = this.detectTouchDevice();

    // Detect if running on Android TV
    this.isAndroidTV = this.detectAndroidTV();
  }

  /**
   * Detect if device is touch-capable (mobile/tablet)
   */
  detectTouchDevice() {
    return (('ontouchstart' in window) ||
      (navigator.maxTouchPoints > 0) ||
      (navigator.msMaxTouchPoints > 0)) &&
      window.innerWidth < 1024; // Exclude large touch screens like Surface
  }

  /**
   * Detect if running on Android TV or other TV platforms
   */
  detectAndroidTV() {
    const userAgent = navigator.userAgent.toLowerCase();

    // Fire TV specific detection
    const isFireTV = userAgent.includes('aftm') || userAgent.includes('aftb') || userAgent.includes('afts') || userAgent.includes('aftkmst12') || userAgent.includes('firetv');

    // Strict TV detection - DO NOT use loose 'tv' because it matches 'native' (naTVe)!
    const isTV = (
      isFireTV ||
      userAgent.includes('googletv') ||
      userAgent.includes('androidtv') ||
      userAgent.includes('smarttv') ||
      userAgent.includes('web0s') || // LG webOS
      userAgent.includes('tizen') || // Samsung Tizen
      userAgent.includes('netcast') || // LG NetCast
      userAgent.includes('leanback')
    );

    console.log('TV Detection:', { userAgent, isTV, isFireTV });

    // Store Fire TV detection for later use
    this.isFireTV = isFireTV;

    return isTV;
  }

  initialize() {
    this.setupMenu();
    this.setupTabs();
    this.setupKeyboardNavigation();
    this.setupServerStatusListener();

    // Initialize TV navigation if on TV platform
    if (this.isAndroidTV) {
      this.initializeTVNavigation();
    }
  }

  /**
   * Initialize TV-specific navigation features
   */
  initializeTVNavigation() {
    console.log('🎮 TV platform detected - enabling remote control navigation');

    // Add TV mode class for styling
    document.body.classList.add('tv-mode');

    // Setup remote control event listeners
    this.setupRemoteControlListeners();

    // Force focus to be visible immediately
    this.focusedElement = 'menu';
    this.focusedMenuIndex = 1; // Start on Home
    this.updateFocus();

    // Ensure the active menu item is properly set
    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    menuButtons.forEach((btn) => btn.classList.remove('active'));
    if (menuButtons[this.focusedMenuIndex]) {
      menuButtons[this.focusedMenuIndex].classList.add('active');
    }
  }

  /**
   * Setup remote control event listeners for TV platforms
   */
  setupRemoteControlListeners() {
    // Listen for all key events and map remote control buttons
    document.addEventListener('keydown', (e) => this.handleRemoteControl(e), true);
  }

  /**
   * Handle remote control input and map to standard keyboard events
   */
  handleRemoteControl(e) {
    // Fire TV specific key mappings
    const fireTVKeyMap = {
      'KEYCODE_DPAD_UP': 'ArrowUp',
      'KEYCODE_DPAD_DOWN': 'ArrowDown',
      'KEYCODE_DPAD_LEFT': 'ArrowLeft',
      'KEYCODE_DPAD_RIGHT': 'ArrowRight',
      'KEYCODE_DPAD_CENTER': 'Enter',
      'KEYCODE_BACK': 'Escape',
      'KEYCODE_MENU': 'm',
      'KEYCODE_MEDIA_PLAY': ' ',
      'KEYCODE_MEDIA_PAUSE': ' ',
      'KEYCODE_MEDIA_PLAY_PAUSE': ' ',
      'KEYCODE_MEDIA_STOP': 'Escape',
      'KEYCODE_MEDIA_FAST_FORWARD': 'ArrowRight',
      'KEYCODE_MEDIA_REWIND': 'ArrowLeft'
    };

    // Standard remote control mappings
    const remoteKeyMap = {
      'ArrowUp': 'ArrowUp',
      'ArrowDown': 'ArrowDown',
      'ArrowLeft': 'ArrowLeft',
      'ArrowRight': 'ArrowRight',
      'Enter': 'Enter',
      'Select': 'Enter',
      'OK': 'Enter',
      'Back': 'Escape',
      'Escape': 'Escape',
      'Backspace': 'Escape',
      'MediaPlay': ' ',
      'MediaPause': ' ',
      'MediaPlayPause': ' ',
      'MediaStop': 'Escape',
      'MediaTrackNext': 'ArrowRight',
      'MediaTrackPrevious': 'ArrowLeft',
      'Menu': 'm',
      'Options': 'o',
      'Info': 'i',
      'Guide': 'g',
      'Home': 'h'
    };

    // Try Fire TV specific mapping first if on Fire TV
    let mappedKey = null;
    if (this.isFireTV) {
      mappedKey = fireTVKeyMap[e.code] || fireTVKeyMap[e.key];
    }

    // Fall back to standard mapping
    if (!mappedKey) {
      mappedKey = remoteKeyMap[e.key] || remoteKeyMap[e.code];
    }

    if (mappedKey && mappedKey !== e.key) {
      // Prevent the original event
      e.preventDefault();
      e.stopPropagation();

      console.log(`🎮 Remote control: ${e.key}/${e.code} -> ${mappedKey}`);

      // Create a synthetic keyboard event
      const syntheticEvent = new KeyboardEvent('keydown', {
        key: mappedKey,
        code: mappedKey,
        bubbles: true,
        cancelable: true,
        composed: true
      });

      // Dispatch to the existing navigation system
      setTimeout(() => {
        document.dispatchEvent(syntheticEvent);
      }, 0);
    }
  }



  /**
   * Setup listener for server status messages
   */
  setupServerStatusListener() {
    let notificationShown = false;

    window.addEventListener('server-limited-mode', (event) => {
      // Only show notification once per session
      if (notificationShown) return;
      notificationShown = true;

      const message = event.detail.message;
      this.showNotification(message, 'warning', 8000);
    });
  }

  /**
   * Show a notification banner
   */
  showNotification(message, type = 'info', duration = 5000) {
    // Remove existing notification if any
    const existing = document.querySelector('.server-notification');
    if (existing) {
      existing.remove();
    }

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `server-notification ${type}`;
    notification.innerHTML = `
      <div class="notification-content">
        <span class="notification-icon">${type === 'warning' ? '⚠️' : 'ℹ️'}</span>
        <span class="notification-message">${message}</span>
      </div>
    `;

    // Add styles
    notification.style.cssText = `
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: ${type === 'warning' ? 'rgba(255, 193, 7, 0.95)' : 'rgba(33, 150, 243, 0.95)'};
      color: ${type === 'warning' ? '#000' : '#fff'};
      padding: 16px 24px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      z-index: 10000;
      font-size: 14px;
      max-width: 600px;
      animation: slideDown 0.3s ease-out;
    `;

    // Add animation keyframes
    if (!document.querySelector('#notification-styles')) {
      const style = document.createElement('style');
      style.id = 'notification-styles';
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
        @keyframes slideUp {
          from {
            opacity: 1;
            transform: translateX(-50%) translateY(0);
          }
          to {
            opacity: 0;
            transform: translateX(-50%) translateY(-20px);
          }
        }
        .notification-content {
          display: flex;
          align-items: center;
          gap: 12px;
        }
        .notification-icon {
          font-size: 20px;
        }
        .notification-message {
          flex: 1;
        }
      `;
      document.head.appendChild(style);
    }

    document.body.appendChild(notification);

    // Auto-remove after duration
    if (duration > 0) {
      setTimeout(() => {
        notification.style.animation = 'slideUp 0.3s ease-out';
        setTimeout(() => notification.remove(), 300);
      }, duration);
    }
  }

  setupMenu() {
    const menuButtons = document.querySelectorAll('.menu-item');
    menuButtons.forEach((button) => {
      // Skip search button - it's handled by search module
      if (button.id === 'search-btn' || button.classList.contains('search-home')) {
        return;
      }

      button.addEventListener('click', async () => {
        if (this.isTransitioning) return;

        menuButtons.forEach((btn) => btn.classList.remove('active'));
        button.classList.add('active');
        await this.navigateToPage(button.dataset.hero);
      });
    });

    // Setup profile button click handler
    const profileButton = document.querySelector('.profile');
    if (profileButton) {
      profileButton.addEventListener('click', () => {
        window.location.href = 'profiles.html';
      });
    }

    // Setup settings button click handler (backup for inline onclick)
    const settingsButton = document.querySelector('.settings-btn');
    if (settingsButton) {
      settingsButton.addEventListener('click', () => {
        window.location.href = 'settings.html';
      });
    }
  }

  /**
   * Navigate to a page with transition animation
   */
  async navigateToPage(category) {
    if (this.isTransitioning) return;

    this.isTransitioning = true;
    const main = document.querySelector('main');

    try {
      // Fade out
      if (main) {
        main.style.transition = `opacity ${this.transitionDuration}ms ease-out`;
        main.style.opacity = '0';
      }

      // Wait for fade out
      await this.delay(this.transitionDuration);

      // Switch content
      await this.contentDisplay.switchCategory(category);

      // Fade in
      if (main) {
        main.style.opacity = '1';
      }

      // Reset transition state
      await this.delay(this.transitionDuration);
    } catch (error) {
      console.error('Navigation error:', error);
      // Ensure main is visible even if navigation fails
      if (main) {
        main.style.opacity = '1';
      }
    } finally {
      // Always reset transition state
      this.isTransitioning = false;
    }
  }

  /**
   * Delay helper
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  setupTabs() {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach((tab) => {
      tab.addEventListener('click', async () => {
        tabs.forEach((item) => item.classList.remove('active'));
        tab.classList.add('active');
        await this.contentDisplay.renderCards(tab.dataset.tab);
      });
    });
  }

  setupKeyboardNavigation() {
    // Enable keyboard navigation for non-touch devices or TV platforms
    if (!this.isTouchDevice || this.isAndroidTV) {
      console.log('Setting up keyboard navigation for TV/desktop');

      const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
      menuButtons.forEach((btn) => btn.classList.remove('active'));
      if (menuButtons[this.focusedMenuIndex]) {
        menuButtons[this.focusedMenuIndex].classList.add('active');
      }

      document.addEventListener('keydown', (e) => this.handleKeyboard(e));

      // Force initial focus update for TV platforms
      if (this.isAndroidTV) {
        // Delay to ensure DOM is ready
        setTimeout(() => {
          this.updateFocus();
        }, 100);
      } else {
        this.updateFocus();
      }
    } else {
      // For touch devices, ensure touch interactions work smoothly
      this.setupTouchNavigation();
    }
  }

  /**
   * Setup touch-friendly navigation for mobile devices
   */
  setupTouchNavigation() {
    // Disable focus styles on touch devices
    document.body.classList.add('touch-device');

    // Add CSS to hide focus indicators on touch devices
    const style = document.createElement('style');
    style.textContent = `
      .touch-device .focused {
        outline: none !important;
        box-shadow: none !important;
        transform: none !important;
      }
      .touch-device .movie-card.expanded {
        transform: none !important;
      }
    `;
    document.head.appendChild(style);
  }


  /**
   * Get the current navigation bar height
   */
  getNavigationHeight() {
    const nav = document.querySelector('.top-nav');
    return nav ? nav.offsetHeight : 0;
  }

  /**
   * Smoothly scroll the current page to ensure an element is visible.
   * On TV platforms we keep focused elements in view while navigating.
   *
   * @param {Element} element - The element that should be visible
   * @param {Object} options
   * @param {'start'|'center'} [options.align='start'] - Preferred alignment within the viewport
   * @param {number} [options.offset=24] - Additional offset applied after alignment
   * @param {'auto'|'smooth'} [options.behavior='smooth'] - Scroll behavior
   */
  scrollElementIntoView(element, { align = 'start', offset = 24, behavior = 'smooth' } = {}) {
    if (!element) {
      return;
    }

    const rects = element.getClientRects();
    if (!rects.length) {
      return;
    }

    const navHeight = this.getNavigationHeight();
    const rect = rects[0];
    const scrollContainer = document.scrollingElement || document.documentElement || document.body;
    const currentScroll = window.pageYOffset || scrollContainer.scrollTop || 0;
    let targetScrollTop = currentScroll;

    if (align === 'center') {
      const viewportHeight = Math.max(window.innerHeight - navHeight, 1);
      const elementCenter = rect.top + currentScroll + rect.height / 2;
      const targetCenter = currentScroll + navHeight + (viewportHeight / 2) - offset;
      targetScrollTop = elementCenter - targetCenter + currentScroll;
    } else {
      targetScrollTop = rect.top + currentScroll - navHeight - offset;
    }

    const maxScroll = (scrollContainer.scrollHeight || document.body.scrollHeight) - window.innerHeight;
    targetScrollTop = Math.max(0, Math.min(targetScrollTop, maxScroll));

    if (Math.abs(targetScrollTop - currentScroll) < 4) {
      return;
    }

    try {
      window.scrollTo({ top: targetScrollTop, behavior });
    } catch (err) {
      window.scrollTo(0, targetScrollTop);
    }
  }


  handleKeyboard(e) {
    // Skip keyboard navigation on touch devices (unless Android TV)
    if (this.isTouchDevice && !this.isAndroidTV) {
      return;
    }

    // Enable navigation mode on first key press
    this.hasUserNavigated = true;

    // Prevent default for arrow keys to avoid page scrolling
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
      e.preventDefault();
    }

    // If profile selection is active, let profile manager handle it
    if (this.profileManager.profileSelectionActive) {
      this.profileManager.handleKeyboard(e);
      return;
    }

    // Check if modal is open and handle modal navigation
    const modal = document.getElementById('content-modal');
    if (modal && modal.classList.contains('visible')) {
      this.handleModalKeyboard(e);
      return;
    }

    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    const tabs = Array.from(document.querySelectorAll('.tab'));
    const cards = () => Array.from(document.querySelectorAll('.movie-card'));

    if (this.focusedElement === 'hero') {
      const focusedHero = this.contentDisplay.getFocusedHeroElement();
      const actionBtns = focusedHero ? Array.from(focusedHero.querySelectorAll('.hero-actions button, .cta')) : [];

      if (e.key === 'ArrowRight') {
        if (this.heroButtonIndex < actionBtns.length - 1) {
          this.heroButtonIndex++;
          this.updateFocus();
        } else {
          // Double press right on last button scrolls to next hero slide (wrapping around)
          const totalHeroSlides = this.contentDisplay.heroCarouselTrack?.children.length || 1;
          const newIndex = (this.contentDisplay.currentHeroIndex + 1) % totalHeroSlides;
          this.contentDisplay.goToSlide(newIndex);
          this.heroButtonIndex = 0;
          this.updateFocus();
        }
      } else if (e.key === 'ArrowLeft') {
        if (this.heroButtonIndex > 0) {
          this.heroButtonIndex--;
          this.updateFocus();
        } else {
          // Double press left on first button scrolls to previous hero slide (wrapping around to end)
          const totalHeroSlides = this.contentDisplay.heroCarouselTrack?.children.length || 1;
          const newIndex = (this.contentDisplay.currentHeroIndex - 1 + totalHeroSlides) % totalHeroSlides;
          this.contentDisplay.goToSlide(newIndex);
          this.heroButtonIndex = 0;
          this.updateFocus();
        }
      } else if (e.key === 'Enter') {
        if (actionBtns[this.heroButtonIndex]) {
          actionBtns[this.heroButtonIndex].click();
        }
      } else if (e.key === 'ArrowUp') {
        this.focusedElement = 'menu';
        this.focusedMenuIndex = this.lastMenuIndex;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        this.focusedElement = 'cards';
        this.focusedCardIndex = 0;
        this.focusedCarouselIndex = 0;
        this.updateFocus();
      }
    } else if (this.focusedElement === 'menu') {
      if (e.key === 'ArrowLeft') {
        if (this.focusedMenuIndex === 0) {
          this.focusedElement = 'profile';
          this.updateFocus();
        } else {
          this.focusedMenuIndex = this.focusedMenuIndex > 0 ? this.focusedMenuIndex - 1 : menuButtons.length - 1;
          menuButtons.forEach((btn) => btn.classList.remove('active'));
          menuButtons[this.focusedMenuIndex].classList.add('active');
          this.navigateToPage(menuButtons[this.focusedMenuIndex].dataset.hero).catch(console.error);
          this.updateFocus();
        }
      } else if (e.key === 'ArrowRight') {
        if (this.focusedMenuIndex === menuButtons.length - 1) {
          this.focusedElement = 'settings';
          this.updateFocus();
        } else {
          this.focusedMenuIndex = this.focusedMenuIndex < menuButtons.length - 1 ? this.focusedMenuIndex + 1 : 0;
          menuButtons.forEach((btn) => btn.classList.remove('active'));
          menuButtons[this.focusedMenuIndex].classList.add('active');
          this.navigateToPage(menuButtons[this.focusedMenuIndex].dataset.hero).catch(console.error);
          this.updateFocus();
        }
      } else if (e.key === 'ArrowDown') {
        this.lastMenuIndex = this.focusedMenuIndex;
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        this.lastMenuIndex = this.focusedMenuIndex;
        this.focusedElement = 'hero';
        this.updateFocus();
      }
    } else if (this.focusedElement === 'profile') {
      if (e.key === 'ArrowRight') {
        this.focusedElement = 'menu';
        this.focusedMenuIndex = 0;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        window.location.href = 'profiles.html';
      }
    } else if (this.focusedElement === 'settings') {
      if (e.key === 'ArrowLeft') {
        this.focusedElement = 'menu';
        this.focusedMenuIndex = menuButtons.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        window.location.href = 'settings.html';
      }
    } else if (this.focusedElement === 'tabs') {
      if (e.key === 'ArrowLeft') {
        this.focusedTabIndex = this.focusedTabIndex > 0 ? this.focusedTabIndex - 1 : tabs.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        this.focusedTabIndex = this.focusedTabIndex < tabs.length - 1 ? this.focusedTabIndex + 1 : 0;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        this.focusedElement = 'cards';
        this.focusedCardIndex = 0;
        this.focusedCarouselIndex = 0; // Start at first carousel
        this.updateFocus();
      } else if (e.key === 'Enter') {
        tabs[this.focusedTabIndex].click();
        this.updateFocus();
      }
    } else if (this.focusedElement === 'cards') {
      const carousels = this.getCarousels();
      const currentCarousel = carousels[this.focusedCarouselIndex];

      if (!currentCarousel) return;

      const carouselCards = Array.from(currentCarousel.querySelectorAll('.movie-card'));

      if (e.key === 'ArrowLeft') {
        this.focusedCardIndex = (this.focusedCardIndex > 0)
          ? this.focusedCardIndex - 1
          : carouselCards.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        this.focusedCardIndex = (this.focusedCardIndex < carouselCards.length - 1)
          ? this.focusedCardIndex + 1
          : 0;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        // Move to previous carousel or hero
        if (this.focusedCarouselIndex > 0) {
          this.focusedCarouselIndex--;
          this.focusedCardIndex = 0;
          this.updateFocus();
        } else {
          this.focusedElement = 'hero';
          this.updateFocus();
        }
      } else if (e.key === 'ArrowDown') {
        // Move to next carousel
        if (this.focusedCarouselIndex < carousels.length - 1) {
          this.focusedCarouselIndex++;
          this.focusedCardIndex = 0; // Reset to first card in new carousel
          this.updateFocus();
        }
      } else if (e.key === 'Enter') {
        // Open modal for focused card
        const currentCarousel = carousels[this.focusedCarouselIndex];
        if (currentCarousel) {
          const carouselCards = Array.from(currentCarousel.querySelectorAll('.movie-card'));
          const focusedCard = carouselCards[this.focusedCardIndex];
          if (focusedCard) {
            const contentId = focusedCard.dataset.contentId;
            const contentType = focusedCard.dataset.contentType;
            const isDiscovery = focusedCard.dataset.isDiscovery === 'true';
            this.contentDisplay.contentModal.show(contentId, contentType, isDiscovery);
          }
        }
      }
    }
  }

  getCarousels() {
    // Get all movie-hub elements (carousels)
    return Array.from(document.querySelectorAll('.movie-hub'));
  }

  updateFocus() {
    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    const tabs = Array.from(document.querySelectorAll('.tab'));
    const cards = () => Array.from(document.querySelectorAll('.movie-card'));
    const profileButton = document.querySelector('.profile');
    const settingsButton = document.querySelector('.settings-btn');

    const allHeros = document.querySelectorAll('.hero');
    allHeros.forEach(h => {
      h.classList.remove('focused');
      h.querySelectorAll('.hero-actions button, .cta').forEach(b => b.classList.remove('focused', 'active-focus'));
    });
    menuButtons.forEach((btn) => btn.classList.remove('focused'));
    tabs.forEach((tab) => tab.classList.remove('focused'));
    if (profileButton) profileButton.classList.remove('focused');
    if (settingsButton) settingsButton.classList.remove('focused');

    const allCards = cards();
    allCards.forEach((card) => {
      card.classList.remove('focused');
      card.classList.remove('expanded');
      const title = card.querySelector('.movie-title');
      if (title) {
        title.style.textShadow = '';
      }
    });

    // Highlight initial Nav Bar item ("Home") on load
    if (!this.hasUserNavigated) {
      if (this.focusedElement === 'menu' && menuButtons[this.focusedMenuIndex]) {
        menuButtons[this.focusedMenuIndex].classList.add('focused');
      }
      return;
    }

    if (this.focusedElement === 'hero') {
      const focusedHero = this.contentDisplay.getFocusedHeroElement();
      if (focusedHero) {
        focusedHero.classList.add('focused');
        const actionBtns = Array.from(focusedHero.querySelectorAll('.hero-actions button, .cta'));
        actionBtns.forEach(b => b.classList.remove('focused', 'active-focus'));
        if (actionBtns.length > 0) {
          const btnToFocus = actionBtns[this.heroButtonIndex] || actionBtns[0];
          btnToFocus.classList.add('focused', 'active-focus');
        }
      }

      const heroStage = document.querySelector('.hero-stage');
      if (heroStage) {
        this.scrollElementIntoView(heroStage, { align: 'start', offset: 32 });
      }
    } else if (this.focusedElement === 'menu') {
      if (menuButtons[this.focusedMenuIndex]) {
        menuButtons[this.focusedMenuIndex].classList.add('focused');
        this.scrollElementIntoView(menuButtons[this.focusedMenuIndex], { align: 'start', offset: 10 });
      }
    } else if (this.focusedElement === 'profile') {
      if (profileButton) profileButton.classList.add('focused');
    } else if (this.focusedElement === 'settings') {
      if (settingsButton) settingsButton.classList.add('focused');
    } else if (this.focusedElement === 'tabs') {
      if (tabs[this.focusedTabIndex]) {
        tabs[this.focusedTabIndex].classList.add('focused');
        const tabSection = tabs[this.focusedTabIndex].closest('.spotlight') || tabs[this.focusedTabIndex].closest('section');
        this.scrollElementIntoView(tabSection || tabs[this.focusedTabIndex], { align: 'start', offset: 32 });
      }
    } else if (this.focusedElement === 'cards') {
      const carousels = this.getCarousels();
      const currentCarousel = carousels[this.focusedCarouselIndex];

      if (currentCarousel) {
        const carouselCards = Array.from(currentCarousel.querySelectorAll('.movie-card'));

        if (carouselCards[this.focusedCardIndex]) {
          const focusedCard = carouselCards[this.focusedCardIndex];
          focusedCard.classList.add('focused');
          focusedCard.classList.add('expanded');

          const title = focusedCard.querySelector('.movie-title');
          if (title) {
            title.style.textShadow = `
              0 0 20px rgba(255, 255, 255, 0.8),
              0 0 40px rgba(255, 255, 255, 0.6),
              0 0 60px rgba(255, 255, 255, 0.4)
            `;
          }

          this.updateMovieCarousel(currentCarousel);

          try {
            focusedCard.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
          } catch (e) {}

          const section = focusedCard.closest('.spotlight') || focusedCard.closest('section') || currentCarousel;
          this.scrollElementIntoView(section, { align: 'center', offset: 40 });
        }
      }
    }
  }

  updateMovieCarousel(carousel) {
    if (!carousel) return;

    const cardElements = Array.from(carousel.querySelectorAll('.movie-card'));

    if (cardElements.length > 0) {
      const isTablet = window.innerWidth <= 768;
      const isMobile = window.innerWidth <= 480;

      const cardWidth = isMobile ? 120 : isTablet ? 140 : 180;
      const expandedCardWidth = isMobile ? 320 : isTablet ? 380 : 480;
      const gap = isMobile ? 12 : 16;

      let offset = 0;
      for (let i = 0; i < this.focusedCardIndex; i++) {
        const card = cardElements[i];
        const isExpanded = card.classList.contains('expanded');
        offset += (isExpanded ? expandedCardWidth : cardWidth) + gap;
      }

      carousel.style.transform = `translateX(-${offset}px)`;
    }
  }

  /**
   * Handle keyboard navigation within the modal
   */
  handleModalKeyboard(e) {
    const modal = document.getElementById('content-modal');
    if (!modal) return;

    // Close modal on Escape
    if (e.key === 'Escape') {
      const closeBtn = modal.querySelector('.modal-close');
      if (closeBtn) {
        closeBtn.click();
      }
      return;
    }

    // Get modal elements
    const actionButtons = Array.from(modal.querySelectorAll('.modal-actions .modal-btn'));
    const seasonTabs = Array.from(modal.querySelectorAll('.season-tab'));
    const episodeCards = Array.from(modal.querySelectorAll('.season-episodes.active .episode-card-horizontal'));

    // Handle navigation based on current focused element
    if (this.modalFocusedElement === 'actions') {
      if (e.key === 'ArrowLeft') {
        this.modalFocusedActionIndex = this.modalFocusedActionIndex > 0 
          ? this.modalFocusedActionIndex - 1 
          : actionButtons.length - 1;
        this.updateModalFocus();
      } else if (e.key === 'ArrowRight') {
        this.modalFocusedActionIndex = this.modalFocusedActionIndex < actionButtons.length - 1 
          ? this.modalFocusedActionIndex + 1 
          : 0;
        this.updateModalFocus();
      } else if (e.key === 'ArrowDown') {
        // Move to seasons if available, otherwise episodes
        if (seasonTabs.length > 0) {
          this.modalFocusedElement = 'seasons';
          this.modalFocusedSeasonIndex = 0;
        } else if (episodeCards.length > 0) {
          this.modalFocusedElement = 'episodes';
          this.modalFocusedEpisodeIndex = 0;
        }
        this.updateModalFocus();
      } else if (e.key === 'Enter') {
        if (actionButtons[this.modalFocusedActionIndex]) {
          actionButtons[this.modalFocusedActionIndex].click();
        }
      }
    } else if (this.modalFocusedElement === 'seasons') {
      if (e.key === 'ArrowUp') {
        this.modalFocusedElement = 'actions';
        this.updateModalFocus();
      } else if (e.key === 'ArrowDown') {
        if (episodeCards.length > 0) {
          this.modalFocusedElement = 'episodes';
          this.modalFocusedEpisodeIndex = 0;
          this.updateModalFocus();
        }
      } else if (e.key === 'ArrowLeft') {
        this.modalFocusedSeasonIndex = this.modalFocusedSeasonIndex > 0 
          ? this.modalFocusedSeasonIndex - 1 
          : seasonTabs.length - 1;
        this.updateModalFocus();
      } else if (e.key === 'ArrowRight') {
        this.modalFocusedSeasonIndex = this.modalFocusedSeasonIndex < seasonTabs.length - 1 
          ? this.modalFocusedSeasonIndex + 1 
          : 0;
        this.updateModalFocus();
      } else if (e.key === 'Enter') {
        if (seasonTabs[this.modalFocusedSeasonIndex]) {
          seasonTabs[this.modalFocusedSeasonIndex].click();
          // After clicking season tab, focus on first episode
          setTimeout(() => {
            this.modalFocusedElement = 'episodes';
            this.modalFocusedEpisodeIndex = 0;
            this.updateModalFocus();
          }, 100);
        }
      }
    } else if (this.modalFocusedElement === 'episodes') {
      if (e.key === 'ArrowUp') {
        if (this.modalFocusedEpisodeIndex > 0) {
          this.modalFocusedEpisodeIndex--;
          this.updateModalFocus();
        } else if (seasonTabs.length > 0) {
          this.modalFocusedElement = 'seasons';
          this.updateModalFocus();
        } else {
          this.modalFocusedElement = 'actions';
          this.updateModalFocus();
        }
      } else if (e.key === 'ArrowDown') {
        if (this.modalFocusedEpisodeIndex < episodeCards.length - 1) {
          this.modalFocusedEpisodeIndex++;
          this.updateModalFocus();
        }
      } else if (e.key === 'Enter') {
        const focusedEpisode = episodeCards[this.modalFocusedEpisodeIndex];
        if (focusedEpisode) {
          // Try to click play button first, then download button
          const playBtn = focusedEpisode.querySelector('.episode-play-btn');
          const downloadBtn = focusedEpisode.querySelector('.episode-download-btn');
          
          if (playBtn) {
            playBtn.click();
          } else if (downloadBtn) {
            downloadBtn.click();
          }
        }
      }
    }
  }

  /**
   * Update focus styling in modal
   */
  updateModalFocus() {
    const modal = document.getElementById('content-modal');
    if (!modal) return;

    // Remove all existing focus classes
    modal.querySelectorAll('.modal-focused').forEach(el => {
      el.classList.remove('modal-focused');
    });

    // Add focus to current element
    if (this.modalFocusedElement === 'actions') {
      const actionButtons = modal.querySelectorAll('.modal-actions .modal-btn');
      if (actionButtons[this.modalFocusedActionIndex]) {
        actionButtons[this.modalFocusedActionIndex].classList.add('modal-focused');
      }
    } else if (this.modalFocusedElement === 'seasons') {
      const seasonTabs = modal.querySelectorAll('.season-tab');
      if (seasonTabs[this.modalFocusedSeasonIndex]) {
        seasonTabs[this.modalFocusedSeasonIndex].classList.add('modal-focused');
      }
    } else if (this.modalFocusedElement === 'episodes') {
      const episodeCards = modal.querySelectorAll('.season-episodes.active .episode-card-horizontal');
      if (episodeCards[this.modalFocusedEpisodeIndex]) {
        episodeCards[this.modalFocusedEpisodeIndex].classList.add('modal-focused');
        
        // Scroll episode into view if needed
        const episodesContainer = modal.querySelector('.episodes-list-vertical');
        if (episodesContainer) {
          const focusedEpisode = episodeCards[this.modalFocusedEpisodeIndex];
          const containerRect = episodesContainer.getBoundingClientRect();
          const episodeRect = focusedEpisode.getBoundingClientRect();
          
          if (episodeRect.bottom > containerRect.bottom) {
            focusedEpisode.scrollIntoView({ behavior: 'smooth', block: 'end' });
          } else if (episodeRect.top < containerRect.top) {
            focusedEpisode.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        }
      }
    }
  }

  /**
   * Initialize modal navigation when modal opens
   */
  initializeModalNavigation() {
    this.modalFocusedElement = 'actions';
    this.modalFocusedActionIndex = 0;
    this.modalFocusedSeasonIndex = 0;
    this.modalFocusedEpisodeIndex = 0;
    
    // Set initial focus after a short delay to ensure modal is rendered
    setTimeout(() => {
      this.updateModalFocus();
    }, 100);
  }
}