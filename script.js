const HEROES = {
  home: [
    {
      background:
        'url(https://image.tmdb.org/t/p/original/8rpDcsfLJypbO6vREc0547VKqEv.jpg)',
      tag: 'New Release • Sci-Fi',
      title: 'Avatar',
      meta: ['Movie', '2024', 'PG-13', '2h 46m'],
      description:
        'Paul Atreides unites with Chani and the Fremen while seeking revenge against the conspirators who destroyed his family.',
      secondary: 'Now streaming in 4K UHD',
    },
    {
      background:
        'url(https://www.hdwallpapers.in/download/the_boys_poster_4k_hd-3840x2160.jpg)',
      tag: 'Popular • Superhero',
      title: 'The Boys',
      meta: ['Series', '2019–', 'TV-MA', '4 Seasons'],
      description:
        'A group of vigilantes set out to take down corrupt superheroes who abuse their powers.',
      secondary: 'Season 4 now streaming',
    },
    {
      background:
        'url(https://image.tmdb.org/t/p/original/fYPiQewg7ogbzro2XcCTACSB2KC.jpg)',
      tag: 'Top Pick • Fantasy',
      title: 'House of the Dragon',
      meta: ['Series', '2022–', 'TV-MA', '2 Seasons'],
      description:
        'The Targaryen dynasty rules Westeros — and the seeds of civil war begin to take root 200 years before the events of Game of Thrones.',
      secondary: 'New season coming in 2025',
    },
  ],
  shows: [
    {
      background:
        'url(https://image.tmdb.org/t/p/original/oqP1qEZccq5AD9TVTIaO6IGUj7o.jpg)',
      tag: 'Hit Series • Thriller',
      title: 'Squid Game',
      meta: ['Series', '2021', 'TV-MA', '9 Episodes'],
      description:
        "'Hundreds of cash-strapped contestants accept an invitation to compete in children's games for a tempting prize, but the stakes are deadly.'",
      secondary: 'Season 2 premieres June 27',
    },
    {
      background:
        'url(https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg)',
      tag: 'New Series • Fantasy',
      title: 'Wednesday',
      meta: ['Series', '2022', 'TV-14', '8 Episodes'],
      description:
        'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
      secondary: 'Season 2 coming soon',
    },
  ],
  movies: [
    {
      background:
        'url(https://image.tmdb.org/t/p/original/4VujM9lbRv6j8N3w6JkYp1q5bZp.jpg)',
      tag: 'Exclusive Film',
      title: 'The Gray Man',
      meta: ['Movie', '2022', '2h 9m'],
      description:
        "'When a shadowy CIA agent uncovers damning agency secrets, he's hunted across the globe by a sociopathic rogue operative who's put a bounty on his head.'",
      secondary: 'Now streaming in Ultra HD',
    },
  ],
  games: [
    {
      background:
        'url(https://images.unsplash.com/photo-1528741386504-9040b037703b?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Mobile Game',
      title: 'Samurai Blade',
      meta: ['Game', 'Action', 'Rogue-lite'],
      description:
        'Slash through endless arenas filled with neon-soaked enemies and upgrade your blade in this stylish RipFlix exclusive game.',
      secondary: 'New season pass available',
    },
  ],
  my: [
    {
      background:
        'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Because you watched',
      title: 'Midnight Tales',
      meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
      description:
        'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
      secondary: 'Continue watching S3:E4',
    },
  ],
};

const MOVIES = [
  {
    title: 'Hit Man',
    type: 'movies',
    genre: 'Comedy',
    duration: '1h 55m',
    rating: 'R',
    year: '2024',
    image: 'https://image.tmdb.org/t/p/w500/1xwBbFBCP9Z5StdyJMWbFBZw2Tc.jpg',
    description: 'A mild-mannered professor moonlighting as a fake hit man in police stings ignites a chain reaction of trouble when he falls for a potential client.',
  },
  {
    title: 'Our Little Secret',
    type: 'movies',
    genre: 'Romance',
    duration: '1h 39m',
    rating: 'PG-13',
    year: '2024',
    image: 'https://image.tmdb.org/t/p/w500/9p9Ed2gOJvKSLdHhiRhNaEdsKgr.jpg',
    description: 'Two ex-lovers discover they are both dating siblings and must keep their past relationship a secret during a chaotic family Christmas.',
  },
  {
    title: 'Good Grief',
    type: 'movies',
    genre: 'Drama',
    duration: '1h 40m',
    rating: 'R',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/kOVKoTQzKOhGNjp7dOHoqMhun8E.jpg',
    description: 'When his husband unexpectedly dies, Marc\'s world shatters, sending him and his two best friends on a soul-searching trip to Paris.',
  },
  {
    title: 'Good on Paper',
    type: 'movies',
    genre: 'Comedy',
    duration: '1h 32m',
    rating: 'R',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/8pgKccb5PfE8LZ4Wy3JlboCjhzc.jpg',
    description: 'After years of putting her career ahead of love, a stand-up comic meets a guy who seems perfect: smart, nice, successful... and possibly too good to be true.',
  },
  {
    title: 'Arcane',
    type: 'series',
    genre: 'Animation',
    duration: '9 Episodes',
    rating: 'TV-14',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/8TRGKO5soNmp6QhZWc6Vz6E2kgg.jpg',
    description: 'Set in utopian Piltover and the oppressed underground of Zaun, the story follows the origins of two iconic League champions-and the power that will tear them apart.',
  },
  {
    title: 'All of Us Are Dead',
    type: 'series',
    genre: 'Horror',
    duration: '12 Episodes',
    rating: 'TV-MA',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/mZjZgY6ObiKtVuKVDrnS9VnuNlE.jpg',
    description: 'A high school becomes ground zero for a zombie virus outbreak. Trapped students must fight their way out — or turn into one of the rabid infected.',
  },
  {
    title: 'The Adam Project',
    type: 'movies',
    genre: 'Sci-Fi',
    duration: '1h 46m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/wFjboE0aFZNbVOF05fzrka9Fqyx.jpg',
    description: 'After accidentally crash-landing in 2022, time-traveling fighter pilot Adam Reed teams up with his 12-year-old self on a mission to save the future.',
  },
  {
    title: 'Bridgerton',
    type: 'series',
    genre: 'Drama',
    duration: '3 Seasons',
    rating: 'TV-MA',
    year: '2020',
    image: 'https://image.tmdb.org/t/p/w500/yYZTYdDbmblP60sGMgKkIYQ7oLD.jpg',
    description: 'Wealth, lust, and betrayal set in the backdrop of Regency era England, seen through the eyes of the powerful Bridgerton family.',
  },
  {
    title: 'RRR',
    type: 'movies',
    genre: 'Action',
    duration: '3h 7m',
    rating: 'NR',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/dVq7m1yMaA3Qn2VLAHrVwzn4F9R.jpg',
    description: 'A fearless revolutionary and an officer in the British force, who once shared a deep bond, decide to join forces and chart out an inspirational path of freedom.',
  },
  {
    title: 'The Witcher',
    type: 'series',
    genre: 'Fantasy',
    duration: '3 Seasons',
    rating: 'TV-MA',
    year: '2019',
    image: 'https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg',
    description: 'Geralt of Rivia, a solitary monster hunter, struggles to find his place in a world where people often prove more wicked than beasts.',
  },
  {
    title: 'Extraction 2',
    type: 'movies',
    genre: 'Action',
    duration: '2h 2m',
    rating: 'R',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/7gKI9hpEMcZUQpNgKrkDzJpbnNS.jpg',
    description: 'Back from the brink of death, highly skilled commando Tyler Rake takes on another dangerous mission: saving the imprisoned family of a ruthless gangster.',
  },
  {
    title: 'Cyberpunk: Edgerunners',
    type: 'series',
    genre: 'Anime',
    duration: '10 Episodes',
    rating: 'TV-MA',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/7PH3R6c0h2ZEDQji0fyQhuKIBjZ.jpg',
    description: 'A street kid trying to survive in a technology and body modification-obsessed city of the future. Having everything to lose, he chooses to stay alive by becoming an edgerunner.',
  },
  {
    title: 'Wednesday',
    type: 'series',
    genre: 'Mystery',
    duration: '8 Episodes',
    rating: 'TV-14',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/9PFonBhy4cQy7Jz20NpMygczOkv.jpg',
    description: 'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
  },
  {
    title: 'Stranger Things',
    type: 'series',
    genre: 'Sci-Fi',
    duration: '4 Seasons',
    rating: 'TV-14',
    year: '2016',
    image: 'https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg',
    description: 'When a young boy vanishes, a small town uncovers a mystery involving secret experiments, terrifying supernatural forces, and one strange little girl.',
  },
  {
    title: 'The Queen\'s Gambit',
    type: 'series',
    genre: 'Drama',
    duration: '7 Episodes',
    rating: 'TV-MA',
    year: '2020',
    image: 'https://image.tmdb.org/t/p/w500/zU0htwkhNvBQdVSIKB9s6hgVeFK.jpg',
    description: 'In a 1950s orphanage, a young girl reveals an astonishing talent for chess and begins an unlikely journey to stardom while grappling with addiction.',
  },
];

const root = document.documentElement;
const heroCarouselTrack = document.getElementById('hero-carousel-track');
const heroAmbilight = document.getElementById('hero-ambilight');
const ambilightLayer1 = document.getElementById('ambilight-layer-1');
const ambilightLayer2 = document.getElementById('ambilight-layer-2');
const topNav = document.querySelector('.top-nav');
let focusedHeroElement = null;

let currentCategory = 'home';
let currentHeroIndex = 0;
let activeAmbilightLayer = 1;

function createCarouselItems() {
  heroCarouselTrack.innerHTML = '';
  const heroes = HEROES[currentCategory];
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

    heroCarouselTrack.appendChild(heroSection);
  });

  focusedHeroElement = heroCarouselTrack.querySelector('.hero');
  if (focusedHeroElement) {
    focusedHeroElement.classList.add('focused');
  }
}

function updateCarouselPosition() {
  const heroes = heroCarouselTrack.querySelectorAll('.hero');
  heroes.forEach((hero, index) => {
    const offset = (index - currentHeroIndex) * 100;
    hero.style.transform = `translateX(${offset}%)`;
    hero.style.opacity = index === currentHeroIndex ? '1' : '0';
    hero.style.scale = index === currentHeroIndex ? '1' : '0.9';
    hero.style.zIndex = index === currentHeroIndex ? '2' : '0';
  });
}


function goToSlide(index) {
  const heroes = HEROES[currentCategory];
  if (index < 0) {
    currentHeroIndex = heroes.length - 1;
  } else if (index >= heroes.length) {
    currentHeroIndex = 0;
  } else {
    currentHeroIndex = index;
  }

  updateCarouselPosition();
  updateAmbilightForCurrentSlide();
  updateFocusedHero();
}



function updateAmbilightForCurrentSlide() {
  const heroes = HEROES[currentCategory];
  const hero = heroes[currentHeroIndex];
  if (root) {
    root.style.setProperty('--hero-bg-image', hero.background);
  }

  // Crossfade between two layers for smooth transition
  if (activeAmbilightLayer === 1) {
    ambilightLayer2.style.backgroundImage = hero.background;
    ambilightLayer2.classList.add('active');
    ambilightLayer1.classList.remove('active');
    activeAmbilightLayer = 2;
  } else {
    ambilightLayer1.style.backgroundImage = hero.background;
    ambilightLayer1.classList.add('active');
    ambilightLayer2.classList.remove('active');
    activeAmbilightLayer = 1;
  }
}

function updateFocusedHero() {
  const allHeroes = heroCarouselTrack.querySelectorAll('.hero');
  allHeroes.forEach((hero, index) => {
    hero.classList.toggle('focused', index === currentHeroIndex);
  });
  focusedHeroElement = allHeroes[currentHeroIndex];
}

function switchCategory(category) {
  currentCategory = category;
  currentHeroIndex = 0;

  createCarouselItems();
  updateCarouselPosition();
  updateAmbilightForCurrentSlide();
}

function handleScroll() {
  if (!topNav) return;

  const threshold = 640 * 0.45;
  if (window.scrollY > threshold) {
    topNav.classList.add('is-solid');
  } else {
    topNav.classList.remove('is-solid');
  }
}

function renderCards(filter) {
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
      <img src="${movie.image}" alt="${movie.title}" class="movie-poster" loading="lazy" />
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

    // No hover events for TV UI - expansion happens on focus via keyboard navigation

    movieHub.appendChild(movieCard);
  });

  row.appendChild(movieHub);
}

function setupMenu() {
  const menuButtons = document.querySelectorAll('.menu-item');
  menuButtons.forEach((button) => {
    button.addEventListener('click', () => {
      menuButtons.forEach((btn) => btn.classList.remove('active'));
      button.classList.add('active');
      switchCategory(button.dataset.hero);
    });
  });
}

function setupTabs() {
  const tabs = document.querySelectorAll('.tab');
  tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      tabs.forEach((item) => item.classList.remove('active'));
      tab.classList.add('active');
      renderCards(tab.dataset.tab);
    });
  });
}

function setupKeyboardNavigation() {
  let focusedElement = 'hero';
  const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
  const tabs = Array.from(document.querySelectorAll('.tab'));
  const cards = () => Array.from(document.querySelectorAll('.movie-card'));

  function updateMovieCarousel() {
    const movieHub = document.querySelector('.movie-hub');
    const cardElements = cards();
    
    if (movieHub && cardElements.length > 0) {
      // Get responsive card dimensions
      const isTablet = window.innerWidth <= 768;
      const isMobile = window.innerWidth <= 480;
      
      const cardWidth = isMobile ? 120 : isTablet ? 140 : 180;
      const expandedCardWidth = isMobile ? 320 : isTablet ? 380 : 480;
      const gap = isMobile ? 12 : 16;
      
      // Calculate total offset needed to position focused card at left
      let offset = 0;
      for (let i = 0; i < focusedCardIndex; i++) {
        const card = cardElements[i];
        const isExpanded = card.classList.contains('expanded');
        offset += (isExpanded ? expandedCardWidth : cardWidth) + gap;
      }
      
      // Apply transform to move the entire row
      movieHub.style.transform = `translateX(-${offset}px)`;
    }
  }

  let focusedMenuIndex = 0;
  let focusedTabIndex = 0;
  let focusedCardIndex = 0;

  function updateFocus() {
    const allHeros = document.querySelectorAll('.hero');
    allHeros.forEach(h => h.classList.remove('focused'));
    menuButtons.forEach((btn) => btn.classList.remove('focused'));
    tabs.forEach((tab) => tab.classList.remove('focused'));
    
    // Remove focused and expanded from all cards
    const allCards = cards();
    allCards.forEach((card) => {
      card.classList.remove('focused');
      card.classList.remove('expanded');
    });

    if (focusedElement === 'hero') {
      if (focusedHeroElement) {
        focusedHeroElement.classList.add('focused');
      }
    } else if (focusedElement === 'menu') {
      menuButtons[focusedMenuIndex].classList.add('focused');
    } else if (focusedElement === 'tabs') {
      tabs[focusedTabIndex].classList.add('focused');
    } else if (focusedElement === 'cards') {
      const cardElements = cards();
      if (cardElements[focusedCardIndex]) {
        const focusedCard = cardElements[focusedCardIndex];
        focusedCard.classList.add('focused');
        focusedCard.classList.add('expanded');
        
        // Update the carousel position to keep focused card at left
        updateMovieCarousel();
      }
    }
  }

  document.addEventListener('keydown', (e) => {
    if (focusedElement === 'hero') {
      const heroes = HEROES[currentCategory];
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        const newIndex = currentHeroIndex > 0 ? currentHeroIndex - 1 : heroes.length - 1;
        goToSlide(newIndex);
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        const newIndex = currentHeroIndex < heroes.length - 1 ? currentHeroIndex + 1 : 0;
        goToSlide(newIndex);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        focusedElement = 'menu';
        updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        focusedElement = 'tabs';
        updateFocus();
      }
    } else if (focusedElement === 'menu') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        focusedMenuIndex = focusedMenuIndex > 0 ? focusedMenuIndex - 1 : menuButtons.length - 1;
        // Automatically activate the focused menu item
        menuButtons.forEach((btn) => btn.classList.remove('active'));
        menuButtons[focusedMenuIndex].classList.add('active');
        switchCategory(menuButtons[focusedMenuIndex].dataset.hero);
        updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        focusedMenuIndex = focusedMenuIndex < menuButtons.length - 1 ? focusedMenuIndex + 1 : 0;
        // Automatically activate the focused menu item
        menuButtons.forEach((btn) => btn.classList.remove('active'));
        menuButtons[focusedMenuIndex].classList.add('active');
        switchCategory(menuButtons[focusedMenuIndex].dataset.hero);
        updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        focusedElement = 'hero';
        updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        focusedElement = 'hero';
        updateFocus();
      }
    } else if (focusedElement === 'tabs') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        focusedTabIndex = focusedTabIndex > 0 ? focusedTabIndex - 1 : tabs.length - 1;
        updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        focusedTabIndex = focusedTabIndex < tabs.length - 1 ? focusedTabIndex + 1 : 0;
        updateFocus();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        focusedElement = 'hero';
        updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        focusedElement = 'cards';
        focusedCardIndex = 0;
        updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        tabs[focusedTabIndex].click();
        updateFocus();
      }
    } else if (focusedElement === 'cards') {
      const cardElements = cards();

      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        focusedCardIndex = focusedCardIndex > 0 ? focusedCardIndex - 1 : cardElements.length - 1;
        updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        focusedCardIndex = focusedCardIndex < cardElements.length - 1 ? focusedCardIndex + 1 : 0;
        updateFocus();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        focusedElement = 'tabs';
        updateFocus();
      }
    }
  });

  updateFocus();
}

createCarouselItems();
updateCarouselPosition();
updateAmbilightForCurrentSlide();
renderCards('all');
setupMenu();
setupTabs();
setupKeyboardNavigation();

handleScroll();
window.addEventListener('scroll', handleScroll, { passive: true });