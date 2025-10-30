export class Navigation {
  constructor(contentDisplay, profileManager) {
    this.contentDisplay = contentDisplay;
    this.profileManager = profileManager;

    this.focusedElement = 'menu';
    this.focusedMenuIndex = 1;
    this.focusedTabIndex = 0;
    this.focusedCardIndex = 0;
    this.focusedCarouselIndex = 0; // Track which carousel is focused
    this.lastMenuIndex = 1;

    // Page transition state
    this.isTransitioning = false;
    this.transitionDuration = 300; // ms
  }

  initialize() {
    this.setupMenu();
    this.setupTabs();
    this.setupKeyboardNavigation();
    this.setupServerStatusListener();
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
    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    menuButtons.forEach((btn) => btn.classList.remove('active'));
    menuButtons[this.focusedMenuIndex].classList.add('active');

    document.addEventListener('keydown', (e) => this.handleKeyboard(e));

    // Add support for Android TV remote control buttons
    this.setupRemoteControlSupport();

    this.updateFocus();
  }

  /**
   * Setup Android TV remote control support
   */
  setupRemoteControlSupport() {
    // Map remote control buttons to keyboard events
    const remoteButtonMap = {
      'MediaPlayPause': ' ', // Space for play/pause
      'MediaPlay': ' ',
      'MediaPause': ' ',
      'MediaStop': 'Escape',
      'MediaTrackNext': 'ArrowRight',
      'MediaTrackPrevious': 'ArrowLeft',
      'Back': 'Escape'
    };

    document.addEventListener('keydown', (e) => {
      if (remoteButtonMap[e.key]) {
        const mappedKey = remoteButtonMap[e.key];
        const syntheticEvent = new KeyboardEvent('keydown', {
          key: mappedKey,
          code: mappedKey,
          bubbles: true,
          cancelable: true
        });
        e.preventDefault();
        document.dispatchEvent(syntheticEvent);
      }
    });
  }

  handleKeyboard(e) {
    // Prevent default for arrow keys to avoid page scrolling
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
      e.preventDefault();
    }

    // If profile selection is active, let profile manager handle it
    if (this.profileManager.profileSelectionActive) {
      this.profileManager.handleKeyboard(e);
      return;
    }

    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    const tabs = Array.from(document.querySelectorAll('.tab'));
    const cards = () => Array.from(document.querySelectorAll('.movie-card'));

    if (this.focusedElement === 'hero') {
      if (e.key === 'ArrowLeft') {
        const newIndex = this.contentDisplay.currentHeroIndex > 0
          ? this.contentDisplay.currentHeroIndex - 1
          : this.contentDisplay.currentHeroIndex;
        this.contentDisplay.goToSlide(newIndex);
      } else if (e.key === 'ArrowRight') {
        const newIndex = this.contentDisplay.currentHeroIndex + 1;
        this.contentDisplay.goToSlide(newIndex);
      } else if (e.key === 'ArrowUp') {
        this.focusedElement = 'menu';
        this.focusedMenuIndex = this.lastMenuIndex;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        this.focusedElement = 'tabs';
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
        this.focusedCardIndex = this.focusedCardIndex > 0 ? this.focusedCardIndex - 1 : carouselCards.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        this.focusedCardIndex = this.focusedCardIndex < carouselCards.length - 1 ? this.focusedCardIndex + 1 : 0;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        // Move to previous carousel or tabs
        if (this.focusedCarouselIndex > 0) {
          this.focusedCarouselIndex--;
          this.focusedCardIndex = 0; // Reset to first card in new carousel
          this.updateFocus();
        } else {
          this.focusedElement = 'tabs';
          this.updateFocus();
        }
      } else if (e.key === 'ArrowDown') {
        // Move to next carousel
        if (this.focusedCarouselIndex < carousels.length - 1) {
          this.focusedCarouselIndex++;
          this.focusedCardIndex = 0; // Reset to first card in new carousel
          this.updateFocus();
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
    allHeros.forEach(h => h.classList.remove('focused'));
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

    if (this.focusedElement === 'hero') {
      const focusedHero = this.contentDisplay.getFocusedHeroElement();
      if (focusedHero) {
        focusedHero.classList.add('focused');
      }
    } else if (this.focusedElement === 'menu') {
      if (menuButtons[this.focusedMenuIndex]) {
        menuButtons[this.focusedMenuIndex].classList.add('focused');
      }
    } else if (this.focusedElement === 'profile') {
      if (profileButton) profileButton.classList.add('focused');
    } else if (this.focusedElement === 'settings') {
      if (settingsButton) settingsButton.classList.add('focused');
    } else if (this.focusedElement === 'tabs') {
      if (tabs[this.focusedTabIndex]) {
        tabs[this.focusedTabIndex].classList.add('focused');
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
}
