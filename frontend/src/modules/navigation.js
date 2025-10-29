export class Navigation {
  constructor(contentDisplay, profileManager) {
    this.contentDisplay = contentDisplay;
    this.profileManager = profileManager;
    
    this.focusedElement = 'menu';
    this.focusedMenuIndex = 1;
    this.focusedTabIndex = 0;
    this.focusedCardIndex = 0;
    this.lastMenuIndex = 1;
  }

  initialize() {
    this.setupMenu();
    this.setupTabs();
    this.setupKeyboardNavigation();
  }

  setupMenu() {
    const menuButtons = document.querySelectorAll('.menu-item');
    menuButtons.forEach((button) => {
      button.addEventListener('click', () => {
        menuButtons.forEach((btn) => btn.classList.remove('active'));
        button.classList.add('active');
        this.contentDisplay.switchCategory(button.dataset.hero);
      });
    });
  }

  setupTabs() {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        tabs.forEach((item) => item.classList.remove('active'));
        tab.classList.add('active');
        this.contentDisplay.renderCards(tab.dataset.tab);
      });
    });
  }

  setupKeyboardNavigation() {
    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    menuButtons.forEach((btn) => btn.classList.remove('active'));
    menuButtons[this.focusedMenuIndex].classList.add('active');

    document.addEventListener('keydown', (e) => this.handleKeyboard(e));
    this.updateFocus();
  }

  handleKeyboard(e) {
    // If profile selection is active, let profile manager handle it
    if (this.profileManager.profileSelectionActive) {
      this.profileManager.handleKeyboard(e);
      return;
    }

    const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
    const tabs = Array.from(document.querySelectorAll('.tab'));
    const cards = () => Array.from(document.querySelectorAll('.movie-card'));
    const profileButton = document.querySelector('.profile');

    if (this.focusedElement === 'hero') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        const newIndex = this.contentDisplay.currentHeroIndex > 0 
          ? this.contentDisplay.currentHeroIndex - 1 
          : this.contentDisplay.currentHeroIndex;
        this.contentDisplay.goToSlide(newIndex);
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        const newIndex = this.contentDisplay.currentHeroIndex + 1;
        this.contentDisplay.goToSlide(newIndex);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        this.focusedElement = 'menu';
        this.focusedMenuIndex = this.lastMenuIndex;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedElement = 'tabs';
        this.updateFocus();
      }
    } else if (this.focusedElement === 'menu') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        if (this.focusedMenuIndex === 0) {
          this.focusedElement = 'profile';
          this.updateFocus();
        } else {
          this.focusedMenuIndex = this.focusedMenuIndex > 0 ? this.focusedMenuIndex - 1 : menuButtons.length - 1;
          menuButtons.forEach((btn) => btn.classList.remove('active'));
          menuButtons[this.focusedMenuIndex].classList.add('active');
          this.contentDisplay.switchCategory(menuButtons[this.focusedMenuIndex].dataset.hero);
          this.updateFocus();
        }
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        if (this.focusedMenuIndex === menuButtons.length - 1) {
          this.focusedElement = 'settings';
          this.updateFocus();
        } else {
          this.focusedMenuIndex = this.focusedMenuIndex < menuButtons.length - 1 ? this.focusedMenuIndex + 1 : 0;
          menuButtons.forEach((btn) => btn.classList.remove('active'));
          menuButtons[this.focusedMenuIndex].classList.add('active');
          this.contentDisplay.switchCategory(menuButtons[this.focusedMenuIndex].dataset.hero);
          this.updateFocus();
        }
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.lastMenuIndex = this.focusedMenuIndex;
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        this.lastMenuIndex = this.focusedMenuIndex;
        this.focusedElement = 'hero';
        this.updateFocus();
      }
    } else if (this.focusedElement === 'profile') {
      if (e.key === 'ArrowRight') {
        e.preventDefault();
        this.focusedElement = 'menu';
        this.focusedMenuIndex = 0;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        this.profileManager.show();
      }
    } else if (this.focusedElement === 'settings') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        this.focusedElement = 'menu';
        this.focusedMenuIndex = menuButtons.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        window.location.href = 'settings.html';
      }
    } else if (this.focusedElement === 'tabs') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        this.focusedTabIndex = this.focusedTabIndex > 0 ? this.focusedTabIndex - 1 : tabs.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        this.focusedTabIndex = this.focusedTabIndex < tabs.length - 1 ? this.focusedTabIndex + 1 : 0;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        this.focusedElement = 'hero';
        this.updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedElement = 'cards';
        this.focusedCardIndex = 0;
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        tabs[this.focusedTabIndex].click();
        this.updateFocus();
      }
    } else if (this.focusedElement === 'cards') {
      const cardElements = cards();
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        this.focusedCardIndex = this.focusedCardIndex > 0 ? this.focusedCardIndex - 1 : cardElements.length - 1;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        this.focusedCardIndex = this.focusedCardIndex < cardElements.length - 1 ? this.focusedCardIndex + 1 : 0;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        this.focusedElement = 'tabs';
        this.updateFocus();
      }
    }
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
      tabs[this.focusedTabIndex].classList.add('focused');
    } else if (this.focusedElement === 'cards') {
      const cardElements = cards();
      if (cardElements[this.focusedCardIndex]) {
        const focusedCard = cardElements[this.focusedCardIndex];
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

        this.updateMovieCarousel();
      }
    }
  }

  updateMovieCarousel() {
    const movieHub = document.querySelector('.movie-hub');
    const cards = () => Array.from(document.querySelectorAll('.movie-card'));
    const cardElements = cards();

    if (movieHub && cardElements.length > 0) {
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

      movieHub.style.transform = `translateX(-${offset}px)`;
    }
  }
}
