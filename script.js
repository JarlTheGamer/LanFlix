const HEROES = {
  home: {
    background:
      'url(https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg)',
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
  },
  {
    title: 'All of Us Are Dead',
    type: 'series',
    meta: 'Horror • 2022',
    image: 'https://image.tmdb.org/t/p/w500/mZjZgY6ObiKtVuKVDrnS9VnuNlE.jpg',
  },
  {
    title: 'The Adam Project',
    type: 'movies',
    meta: 'Sci-Fi • 2022',
    image: 'https://image.tmdb.org/t/p/w500/wFjboE0aFZNbVOF05fzrka9Fqyx.jpg',
  },
  {
    title: 'Bridgerton',
    type: 'series',
    meta: 'Drama • 2020',
    image: 'https://image.tmdb.org/t/p/w500/yYZTYdDbmblP60sGMgKkIYQ7oLD.jpg',
  },
  {
    title: 'RRR',
    type: 'movies',
    meta: 'Action • 2022',
    image: 'https://image.tmdb.org/t/p/w500/dVq7m1yMaA3Qn2VLAHrVwzn4F9R.jpg',
  },
  {
    title: 'The Witcher',
    type: 'series',
    meta: 'Fantasy • 2021',
    image: 'https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg',
  },
  {
    title: 'Extraction 2',
    type: 'movies',
    meta: 'Thriller • 2023',
    image: 'https://image.tmdb.org/t/p/w500/7gKI9hpEMcZUQpNgKrkDzJpbnNS.jpg',
  },
  {
    title: 'Cyberpunk: Edgerunners',
    type: 'series',
    meta: 'Anime • 2022',
    image: 'https://image.tmdb.org/t/p/w500/7PH3R6c0h2ZEDQji0fyQhuKIBjZ.jpg',
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
const topNav = document.querySelector('.top-nav');
const heroSection = document.querySelector('.hero');

function updateHero(key) {
  const hero = HEROES[key] ?? HEROES.home;
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
  const row = document.getElementById('spotlight-row');
  row.innerHTML = '';

  CARDS.filter((card) => filter === 'all' || card.type === filter).forEach((card) => {
    const cardElement = document.createElement('article');
    cardElement.className = 'card';
    cardElement.innerHTML = `
      <img src="${card.image}" alt="${card.title}" loading="lazy" />
      <div class="card-info">
        <span class="card-title">${card.title}</span>
        <span class="card-meta">${card.meta}</span>
      </div>
    `;
    row.appendChild(cardElement);
  });
}

function setupMenu() {
  const menuButtons = document.querySelectorAll('.menu-item');
  menuButtons.forEach((button) => {
    button.addEventListener('click', () => {
      menuButtons.forEach((btn) => btn.classList.remove('active'));
      button.classList.add('active');
      updateHero(button.dataset.hero);
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

updateHero('home');
renderCards('all');
setupMenu();
setupTabs();
handleScroll();
window.addEventListener('scroll', handleScroll, { passive: true });
