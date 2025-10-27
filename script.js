const HEROES = {
  home: {
    background:
      'url(https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg)',
    poster: 'https://image.tmdb.org/t/p/w500/mBDlsOhNOVMu86A3Xmk0G2IKa6M.jpg',
    tag: 'New Series • Fantasy',
    title: 'Wednesday',
    meta: ['Series', '2022', 'TV-14', '8 Episodes'],
    description:
      'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
    secondary: 'Season 2 coming soon',
  },
  shows: {
    background:
      'url(https://image.tmdb.org/t/p/original/oqP1qEZccq5AD9TVTIaO6IGUj7o.jpg)',
    poster: 'https://image.tmdb.org/t/p/w500/dDlEmu3EZ0Pgg93K2SVNLCjCSvE.jpg',
    tag: 'Hit Series • Thriller',
    title: 'Squid Game',
    meta: ['Series', '2021', 'TV-MA', '9 Episodes'],
    description:
      'Hundreds of cash-strapped contestants accept an invitation to compete in children’s games for a tempting prize, but the stakes are deadly.',
    secondary: 'Season 2 premieres June 27',
  },
  movies: {
    background:
      'url(https://image.tmdb.org/t/p/original/4VujM9lbRv6j8N3w6JkYp1q5bZp.jpg)',
    poster: 'https://image.tmdb.org/t/p/w500/8cXbitsS6dWQ5gfMT0r1ZAGVIXz.jpg',
    tag: 'Exclusive Film',
    title: 'The Gray Man',
    meta: ['Movie', '2022', '2h 9m'],
    description:
      'When a shadowy CIA agent uncovers damning agency secrets, he’s hunted across the globe by a sociopathic rogue operative who’s put a bounty on his head.',
    secondary: 'Now streaming in Ultra HD',
  },
  games: {
    background:
      'url(https://images.unsplash.com/photo-1528741386504-9040b037703b?auto=format&fit=crop&w=1400&q=80)',
    poster: 'https://images.unsplash.com/photo-1542751371-adc38448a05e?auto=format&fit=crop&w=600&q=80',
    tag: 'Mobile Game',
    title: 'Samurai Blade',
    meta: ['Game', 'Action', 'Rogue-lite'],
    description:
      'Slash through endless arenas filled with neon-soaked enemies and upgrade your blade in this stylish RipFlix exclusive game.',
    secondary: 'New season pass available',
  },
  my: {
    background:
      'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
    poster: 'https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=600&q=80',
    tag: 'Because you watched',
    title: 'Midnight Tales',
    meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
    description:
      'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
    secondary: 'Continue watching S3:E4',
  },
};

const CARDS = [
  {
    title: 'Arcane',
    type: 'series',
    meta: 'Animated • 2021',
    image: 'https://image.tmdb.org/t/p/w500/8TRGKO5soNmp6QhZWc6Vz6E2kgg.jpg',
    match: '98% Match',
    tag: 'New Episodes',
  },
  {
    title: 'All of Us Are Dead',
    type: 'series',
    meta: 'Horror • 2022',
    image: 'https://image.tmdb.org/t/p/w500/mZjZgY6ObiKtVuKVDrnS9VnuNlE.jpg',
    match: '94% Match',
    tag: 'Trending Now',
  },
  {
    title: 'The Adam Project',
    type: 'movies',
    meta: 'Sci-Fi • 2022',
    image: 'https://image.tmdb.org/t/p/w500/wFjboE0aFZNbVOF05fzrka9Fqyx.jpg',
    match: '92% Match',
    tag: 'Family Pick',
  },
  {
    title: 'Bridgerton',
    type: 'series',
    meta: 'Drama • 2020',
    image: 'https://image.tmdb.org/t/p/w500/yYZTYdDbmblP60sGMgKkIYQ7oLD.jpg',
    match: '97% Match',
    tag: 'Watch Again',
  },
  {
    title: 'RRR',
    type: 'movies',
    meta: 'Action • 2022',
    image: 'https://image.tmdb.org/t/p/w500/dVq7m1yMaA3Qn2VLAHrVwzn4F9R.jpg',
    match: '95% Match',
    tag: 'Explosive',
  },
  {
    title: 'The Witcher',
    type: 'series',
    meta: 'Fantasy • 2021',
    image: 'https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg',
    match: '96% Match',
    tag: 'New Season',
  },
  {
    title: 'Extraction 2',
    type: 'movies',
    meta: 'Thriller • 2023',
    image: 'https://image.tmdb.org/t/p/w500/7gKI9hpEMcZUQpNgKrkDzJpbnNS.jpg',
    match: '93% Match',
    tag: 'Just Added',
  },
  {
    title: 'Cyberpunk: Edgerunners',
    type: 'series',
    meta: 'Anime • 2022',
    image: 'https://image.tmdb.org/t/p/w500/7PH3R6c0h2ZEDQji0fyQhuKIBjZ.jpg',
    match: '99% Match',
    tag: 'Critics Love',
  },
];

const root = document.documentElement;
const heroBg = document.getElementById('hero-bg');
const heroTag = document.getElementById('hero-tag');
const heroTitle = document.getElementById('hero-title');
const heroMeta = document.getElementById('hero-meta');
const heroDescription = document.getElementById('hero-description');
const heroSecondary = document.getElementById('hero-secondary');
const heroAmbilight = document.getElementById('hero-ambilight');
const heroSection = document.querySelector('.hero');
const heroTray = document.getElementById('hero-tray');
const heroNavButtons = document.querySelectorAll('.hero-nav');
const menuButtons = document.querySelectorAll('.menu-item');
const topNav = document.querySelector('.top-nav');
const spotlightTrack = document.getElementById('spotlight-track');
const spotlightNavButtons = document.querySelectorAll('.spotlight-nav');
const heroKeys = Object.keys(HEROES);
let currentHeroIndex = 0;
let isScrollingSpotlight = false;

function extractPoster(hero) {
  if (hero.poster) return hero.poster;
  const match = /url\((['"]?)(.*?)\1\)/.exec(hero.background);
  return match ? match[2] : '';
}

function updateMenuState(activeKey) {
  menuButtons.forEach((button) => {
    const isActive = button.dataset.hero === activeKey;
    button.classList.toggle('active', isActive);
  });
}

function updateHeroTrayState() {
  if (!heroTray) return;
  const buttons = heroTray.querySelectorAll('.hero-thumb');
  buttons.forEach((button, index) => {
    const isActive = index === currentHeroIndex;
    button.classList.toggle('is-active', isActive);
    button.setAttribute('aria-pressed', String(isActive));
    if (isActive) {
      button.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    }
  });
}

function applyHero(key) {
  const hero = HEROES[key] ?? HEROES.home;
  currentHeroIndex = Math.max(heroKeys.indexOf(key), 0);
  if (root) {
    root.style.setProperty('--hero-bg-image', hero.background);
  }
  heroBg.style.backgroundImage = hero.background;
  heroBg.style.opacity = 0;
  if (heroAmbilight) {
    heroAmbilight.style.backgroundImage = hero.background;
    heroAmbilight.style.opacity = '0';
  }
  requestAnimationFrame(() => {
    heroBg.style.transition = 'none';
    heroBg.offsetHeight;
    heroBg.style.transition = '';
    heroBg.style.opacity = 1;
    if (heroAmbilight) {
      heroAmbilight.style.transition = 'none';
      heroAmbilight.offsetHeight;
      heroAmbilight.style.transition = '';
      heroAmbilight.style.opacity = '';
    }
  });

  heroTag.textContent = hero.tag;
  heroTitle.textContent = hero.title;
  heroMeta.innerHTML = hero.meta.map((item) => `<span>${item}</span>`).join('');
  heroDescription.textContent = hero.description;
  heroSecondary.innerHTML = `<span>New</span> ${hero.secondary}`;

  updateMenuState(key);
  updateHeroTrayState();
}

function updateHeroByIndex(index) {
  const normalized = (index + heroKeys.length) % heroKeys.length;
  applyHero(heroKeys[normalized]);
}

function handleScroll() {
  if (!topNav || !heroSection) return;
  const threshold = heroSection.offsetHeight * 0.45;
  if (window.scrollY > threshold) {
    topNav.classList.add('is-solid');
  } else {
    topNav.classList.remove('is-solid');
  }
}

function renderCards(filter) {
  if (!spotlightTrack) return;
  spotlightTrack.innerHTML = '';

  CARDS.filter((card) => filter === 'all' || card.type === filter).forEach((card, index) => {
    const cardElement = document.createElement('article');
    cardElement.className = 'card';
    cardElement.innerHTML = `
      <div class="card-poster">
        <img src="${card.image}" alt="${card.title}" loading="lazy" />
        <div class="card-overlay">
          <button class="card-play" type="button" aria-label="Play ${card.title}">
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M8 5v14l11-7z" />
            </svg>
          </button>
          <div class="card-meta-block">
            <span class="card-match">${card.match}</span>
            <span class="card-title">${card.title}</span>
            <span class="card-meta">${card.meta}</span>
            <span class="card-tag">${card.tag}</span>
          </div>
        </div>
        <span class="card-rank">${index + 1}</span>
      </div>
    `;
    spotlightTrack.appendChild(cardElement);
  });

  spotlightTrack.scrollLeft = 0;
  updateSpotlightNavState();
}

function setupMenu() {
  menuButtons.forEach((button) => {
    button.addEventListener('click', () => {
      applyHero(button.dataset.hero);
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

function buildHeroTray() {
  if (!heroTray) return;
  heroTray.innerHTML = '';
  heroKeys.forEach((key, index) => {
    const hero = HEROES[key];
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'hero-thumb';
    button.dataset.index = index;
    button.setAttribute('aria-label', hero.title);
    const poster = extractPoster(hero);
    button.innerHTML = `
      <span class="hero-thumb__image" style="background-image: url('${poster}')"></span>
      <span class="hero-thumb__title">${hero.title}</span>
    `;
    button.addEventListener('click', () => updateHeroByIndex(index));
    button.addEventListener('focus', () => button.classList.add('is-focused'));
    button.addEventListener('blur', () => button.classList.remove('is-focused'));
    heroTray.appendChild(button);
  });
  updateHeroTrayState();
}

function setupHeroNavigation() {
  heroNavButtons.forEach((button) => {
    button.addEventListener('click', () => {
      const direction = button.dataset.direction === 'next' ? 1 : -1;
      updateHeroByIndex(currentHeroIndex + direction);
    });
  });
}

function updateSpotlightNavState() {
  if (!spotlightTrack) return;
  const maxScroll = spotlightTrack.scrollWidth - spotlightTrack.clientWidth - 1;
  const atStart = spotlightTrack.scrollLeft <= 0;
  const atEnd = spotlightTrack.scrollLeft >= maxScroll;
  spotlightNavButtons.forEach((button) => {
    if (button.dataset.direction === 'prev') {
      button.disabled = atStart;
      button.classList.toggle('is-disabled', atStart);
    } else {
      button.disabled = atEnd;
      button.classList.toggle('is-disabled', atEnd);
    }
  });
}

function setupSpotlightNavigation() {
  if (!spotlightTrack) return;
  spotlightNavButtons.forEach((button) => {
    button.addEventListener('click', () => {
      const direction = button.dataset.direction === 'next' ? 1 : -1;
      const card = spotlightTrack.querySelector('.card');
      if (!card) return;
      const computedStyle = getComputedStyle(spotlightTrack);
      const gap = parseFloat(computedStyle.columnGap || computedStyle.gap || '24');
      const distance = card.offsetWidth + gap;
      spotlightTrack.scrollBy({ left: distance * direction, behavior: 'smooth' });
    });
  });

  spotlightTrack.addEventListener('scroll', () => {
    if (isScrollingSpotlight) return;
    isScrollingSpotlight = true;
    requestAnimationFrame(() => {
      updateSpotlightNavState();
      isScrollingSpotlight = false;
    });
  });
}

buildHeroTray();
setupHeroNavigation();
setupMenu();
setupTabs();
applyHero('home');
renderCards('all');
setupSpotlightNavigation();
handleScroll();
window.addEventListener('scroll', handleScroll, { passive: true });
