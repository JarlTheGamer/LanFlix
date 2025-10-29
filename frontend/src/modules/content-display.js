import { HEROES, MOVIES } from './data.js';

export class ContentDisplay {
  constructor() {
    this.currentCategory = 'home';
    this.currentHeroIndex = 0;
    this.activeAmbilightLayer = 1;
    this.focusedHeroElement = null;
    
    this.root = document.documentElement;
    this.heroCarouselTrack = document.getElementById('hero-carousel-track');
    this.heroAmbilight = document.getElementById('hero-ambilight');
    this.ambilightLayer1 = document.getElementById('ambilight-layer-1');
    this.ambilightLayer2 = document.getElementById('ambilight-layer-2');
    this.topNav = document.querySelector('.top-nav');
  }

  initialize() {
    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
    this.renderCards('all');
    this.setupScrollHandler();
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

  switchCategory(category) {
    this.currentCategory = category;
    this.currentHeroIndex = 0;

    this.createCarouselItems();
    this.updateCarouselPosition();
    this.updateAmbilightForCurrentSlide();
  }

  renderCards(filter) {
    const row = document.getElementById('spotlight-row');
    row.innerHTML = '';

    const movieHub = document.createElement('div');
    movieHub.className = 'movie-hub';

    const filteredMovies = MOVIES.filter((movie) => filter === 'all' || movie.type === filter);

    filteredMovies.forEach((movie, index) => {
      const movieCard = document.createElement('article');
      movieCard.className = 'movie-card';
      movieCard.dataset.index = index;

      movieCard.innerHTML = `
        <div class="movie-poster-container">
          <img src="${movie.image}" alt="${movie.title}" class="movie-poster movie-poster-regular" loading="lazy" />
          <img src="${movie.expandedImage || movie.image}" alt="${movie.title}" class="movie-poster movie-poster-expanded" loading="lazy" />
        </div>
        <div class="movie-overlay"></div>
        <div class="movie-compact-title">${movie.title}</div>
        <div class="movie-info">
          <h3 class="movie-title">${movie.title}</h3>
          <div class="movie-meta">
            <span>${movie.genre}</span>
            <span>${movie.year}</span>
            <span>${movie.duration}</span>
            <span>${movie.rating}</span>
          </div>
          <p class="movie-description">${movie.description}</p>
        </div>
      `;

      movieHub.appendChild(movieCard);
    });

    row.appendChild(movieHub);
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

  getFocusedHeroElement() {
    return this.focusedHeroElement;
  }
}
