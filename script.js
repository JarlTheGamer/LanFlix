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
  // Popular Movies
  {
    title: 'Avatar: The Way of Water',
    type: 'movies',
    genre: 'Sci-Fi',
    duration: '3h 12m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/t6HIqrRAclMCA60NsSmeqe9RmNV.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/8rpDcsfLJypbO6vREc0547VKqEv.jpg',
    description: 'Set more than a decade after the events of the first film, Avatar: The Way of Water begins to tell the story of the Sully family.',
  },
  {
    title: 'Top Gun: Maverick',
    type: 'movies',
    genre: 'Action',
    duration: '2h 11m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/62HCnUTziyWcpDaBO2i1DX17ljH.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/odJ4hx6g6vBt4lBWKFD1tI8WS4x.jpg',
    description: 'After thirty years, Maverick is still pushing the envelope as a top naval aviator, but must confront ghosts of his past.',
  },
  {
    title: 'Black Panther: Wakanda Forever',
    type: 'movies',
    genre: 'Action',
    duration: '2h 41m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/sv1xJUazXeYqALzczSZ3O6nkH75.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/yYrvN5WFeGYjJnRzhY0QXuo4Isw.jpg',
    description: 'Queen Ramonda, Shuri, M\'Baku, Okoye and the Dora Milaje fight to protect their nation from intervening world powers.',
  },
  {
    title: 'Spider-Man: No Way Home',
    type: 'movies',
    genre: 'Action',
    duration: '2h 28m',
    rating: 'PG-13',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/1g0dhYtq4irTY1GPXvft6k4YLjm.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/14QbnygCuTO0vl7CAFmPf1fgZfV.jpg',
    description: 'Peter Parker seeks help from Doctor Strange when his identity as Spider-Man is revealed, causing reality to fracture.',
  },
  {
    title: 'Dune',
    type: 'movies',
    genre: 'Sci-Fi',
    duration: '2h 35m',
    rating: 'PG-13',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/d5NXSklXo0qyIYkgV94XAgMIckC.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/s1FdOr2M7VwjGJdyJmSvZmgOLdI.jpg',
    description: 'Paul Atreides leads nomadic tribes in a revolt against the evil Harkonnen oppressors on the desert planet Arrakis.',
  },
  {
    title: 'The Batman',
    type: 'movies',
    genre: 'Action',
    duration: '2h 56m',
    rating: 'PG-13',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/74xTEgt7R36Fpooo50r9T25onhq.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/b0PlSFdDwbyK0cf5RxwDpaOJQvQ.jpg',
    description: 'Batman ventures into Gotham City\'s underworld when a sadistic killer leaves behind a trail of cryptic clues.',
  },
  {
    title: 'Everything Everywhere All at Once',
    type: 'movies',
    genre: 'Sci-Fi',
    duration: '2h 19m',
    rating: 'R',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/w3LxiVYdWWRvEVdn5RYq6jIqkb1.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/AaV1YIdWKnjAIAOe7UMlqNjwjsv.jpg',
    description: 'A Chinese-American woman gets swept up in an insane adventure where she alone can save existence.',
  },
  {
    title: 'John Wick: Chapter 4',
    type: 'movies',
    genre: 'Action',
    duration: '2h 49m',
    rating: 'R',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/vZloFAK7NmvMGKE7VkF5UHaz0I.jpg',
    description: 'John Wick uncovers a path to defeating The High Table. But before he can earn his freedom, he must face off against a new enemy.',
  },
  {
    title: 'Oppenheimer',
    type: 'movies',
    genre: 'Drama',
    duration: '3h 0m',
    rating: 'R',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg',
    description: 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.',
  },
  {
    title: 'Barbie',
    type: 'movies',
    genre: 'Comedy',
    duration: '1h 54m',
    rating: 'PG-13',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/iuFNMS8U5cb6xfzi51Dbkovj7vM.jpg',
    description: 'Barbie and Ken are having the time of their lives in the colorful and seemingly perfect world of Barbie Land.',
  },
  {
    title: 'Guardians of the Galaxy Vol. 3',
    type: 'movies',
    genre: 'Action',
    duration: '2h 30m',
    rating: 'PG-13',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/r2J02Z2OpNTctfOSN1Ydgii51I3.jpg',
    description: 'Peter Quill must rally his team around him to defend the universe and protect one of their own.',
  },
  {
    title: 'Fast X',
    type: 'movies',
    genre: 'Action',
    duration: '2h 21m',
    rating: 'PG-13',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/fiVW06jE7z9YnO4trhaMEdclSiC.jpg',
    description: 'Dom Toretto and his family are targeted by the vengeful son of drug kingpin Hernan Reyes.',
  },
  {
    title: 'Scream VI',
    type: 'movies',
    genre: 'Horror',
    duration: '2h 3m',
    rating: 'R',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/wDWwtvkRRlgTiUr6TyLSMX8FCuZ.jpg',
    description: 'The survivors of the Ghostface killings leave Woodsboro behind and start a fresh chapter in New York City.',
  },
  {
    title: 'Indiana Jones and the Dial of Destiny',
    type: 'movies',
    genre: 'Adventure',
    duration: '2h 34m',
    rating: 'PG-13',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/Af4bXE63pVsb2FtbW8uYIyPBadD.jpg',
    description: 'Aging archaeologist Indiana Jones races against time to retrieve a legendary artifact that can change the course of history.',
  },
  {
    title: 'Mission: Impossible – Dead Reckoning Part One',
    type: 'movies',
    genre: 'Action',
    duration: '2h 43m',
    rating: 'PG-13',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/NNxYkU70HPurnNCSiCjYAmacwm.jpg',
    description: 'Ethan Hunt and his IMF team embark on their most dangerous mission yet: to track down a terrifying new weapon.',
  },

  // Popular TV Series
  {
    title: 'Stranger Things',
    type: 'series',
    genre: 'Sci-Fi',
    duration: '4 Seasons',
    rating: 'TV-14',
    year: '2016',
    image: 'https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/56v2KjBlU4XaOv9rVYEQypROD7P.jpg',
    description: 'When a young boy vanishes, a small town uncovers a mystery involving secret experiments, terrifying supernatural forces, and one strange little girl.',
  },
  {
    title: 'Wednesday',
    type: 'series',
    genre: 'Mystery',
    duration: '8 Episodes',
    rating: 'TV-14',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/9PFonBhy4cQy7Jz20NpMygczOkv.jpg',
    expandedImage: 'https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg',
    description: 'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
  },
  {
    title: 'The Crown',
    type: 'series',
    genre: 'Drama',
    duration: '6 Seasons',
    rating: 'TV-MA',
    year: '2016',
    image: 'https://image.tmdb.org/t/p/w500/1M876KPjulVwppEpldhdc8V4o68.jpg',
    description: 'Follows the political rivalries and romance of Queen Elizabeth II\'s reign and the events that shaped the second half of the twentieth century.',
  },
  {
    title: 'House of the Dragon',
    type: 'series',
    genre: 'Fantasy',
    duration: '2 Seasons',
    rating: 'TV-MA',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/7QMsOTMUswlwxJP0rTTZfmz2tX2.jpg',
    description: 'The Targaryen dynasty is at the absolute apex of its power, with more than 15 dragons under their yoke. Most empires crumble from such heights.',
  },
  {
    title: 'The Last of Us',
    type: 'series',
    genre: 'Drama',
    duration: '9 Episodes',
    rating: 'TV-MA',
    year: '2023',
    image: 'https://image.tmdb.org/t/p/w500/uKvVjHNqB5VmOrdxqAt2F7J78ED.jpg',
    description: 'Twenty years after modern civilization has been destroyed, Joel must smuggle Ellie out of an oppressive quarantine zone.',
  },
  {
    title: 'The Bear',
    type: 'series',
    genre: 'Comedy',
    duration: '3 Seasons',
    rating: 'TV-MA',
    year: '2022',
    image: 'https://image.tmdb.org/t/p/w500/sHFlbKS3WLqMnp9t2ghADIJFnuQ.jpg',
    description: 'A young chef from the fine dining world comes home to Chicago to run his family sandwich shop.',
  },
  {
    title: 'Euphoria',
    type: 'series',
    genre: 'Drama',
    duration: '2 Seasons',
    rating: 'TV-MA',
    year: '2019',
    image: 'https://image.tmdb.org/t/p/w500/jtnfNzqZwN4E32FGGxx1YZaBWWf.jpg',
    description: 'A group of high school students navigate love and friendships in a world of drugs, sex, trauma and social media.',
  },
  {
    title: 'The White Lotus',
    type: 'series',
    genre: 'Comedy',
    duration: '2 Seasons',
    rating: 'TV-MA',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/gH5i3JbnLsyTvcImlofNvXtH3i5.jpg',
    description: 'The exploits of various guests and employees at an exclusive tropical resort over the span of a week.',
  },
  {
    title: 'Squid Game',
    type: 'series',
    genre: 'Thriller',
    duration: '9 Episodes',
    rating: 'TV-MA',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/dDlEmu3EZ0Pgg93K2SVNLCjCSvE.jpg',
    description: 'Hundreds of cash-strapped contestants accept an invitation to compete in children\'s games for a tempting prize, but the stakes are deadly.',
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
    title: 'Ozark',
    type: 'series',
    genre: 'Crime',
    duration: '4 Seasons',
    rating: 'TV-MA',
    year: '2017',
    image: 'https://image.tmdb.org/t/p/w500/m73QiJOFMQWPMEjONLuOLNTlbpK.jpg',
    description: 'A financial advisor drags his family from Chicago to the Missouri Ozarks, where he must launder money to appease a drug boss.',
  },
  {
    title: 'Money Heist',
    type: 'series',
    genre: 'Crime',
    duration: '5 Seasons',
    rating: 'TV-MA',
    year: '2017',
    image: 'https://image.tmdb.org/t/p/w500/reEMJA1uzscCbkpeRJeTT2bjqUp.jpg',
    description: 'An unusual group of robbers attempt to carry out the most perfect robbery in Spanish history - stealing 2.4 billion euros from the Royal Mint of Spain.',
  },
  {
    title: 'Breaking Bad',
    type: 'series',
    genre: 'Crime',
    duration: '5 Seasons',
    rating: 'TV-MA',
    year: '2008',
    image: 'https://image.tmdb.org/t/p/w500/3xnWaLQjelJDDF7LT1WBo6f4BRe.jpg',
    description: 'A high school chemistry teacher diagnosed with inoperable lung cancer turns to manufacturing and selling methamphetamine.',
  },
  {
    title: 'Better Call Saul',
    type: 'series',
    genre: 'Crime',
    duration: '6 Seasons',
    rating: 'TV-MA',
    year: '2015',
    image: 'https://image.tmdb.org/t/p/w500/fC2HDm5t0kHl7mTm7jxMR31cyEc.jpg',
    description: 'The trials and tribulations of criminal lawyer Jimmy McGill in the time before he established his strip-mall law office in Albuquerque.',
  },
  {
    title: 'Game of Thrones',
    type: 'series',
    genre: 'Fantasy',
    duration: '8 Seasons',
    rating: 'TV-MA',
    year: '2011',
    image: 'https://image.tmdb.org/t/p/w500/1XS1oqL89opfnbLl8WnZY1O1uJx.jpg',
    description: 'Nine noble families fight for control over the lands of Westeros, while an ancient enemy returns after being dormant for millennia.',
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
  {
    title: 'Arcane',
    type: 'series',
    genre: 'Animation',
    duration: '9 Episodes',
    rating: 'TV-14',
    year: '2021',
    image: 'https://image.tmdb.org/t/p/w500/fqldf2t8ztc9aiwn3k6mlX3tvRT.jpg',
    description: 'Set in utopian Piltover and the oppressed underground of Zaun, the story follows the origins of two iconic League champions.',
  },
  {
    title: 'Dark',
    type: 'series',
    genre: 'Sci-Fi',
    duration: '3 Seasons',
    rating: 'TV-MA',
    year: '2017',
    image: 'https://image.tmdb.org/t/p/w500/rrGO9jt7jLEmeABdJpIcbKd3sLo.jpg',
    description: 'A family saga with a supernatural twist, set in a German town, where the disappearance of two young children exposes the relationships among four families.',
  },
  {
    title: 'The Mandalorian',
    type: 'series',
    genre: 'Sci-Fi',
    duration: '3 Seasons',
    rating: 'TV-14',
    year: '2019',
    image: 'https://image.tmdb.org/t/p/w500/sWgBv7LV2PRoQgkxwlibdGXKz1S.jpg',
    description: 'The travels of a lone bounty hunter in the outer reaches of the galaxy, far from the authority of the New Republic.',
  },
  {
    title: 'Peaky Blinders',
    type: 'series',
    genre: 'Crime',
    duration: '6 Seasons',
    rating: 'TV-MA',
    year: '2013',
    image: 'https://image.tmdb.org/t/p/w500/vUUqzWa2LnHIVqkaKVlVGkVcZIW.jpg',
    description: 'A gangster family epic set in 1900s England, centering on a gang who sew razor blades in the peaks of their caps.',
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

    // Add hover event for search-home button to trigger ambilight
    if (button.classList.contains('search-home')) {
      button.addEventListener('mouseenter', () => {
        updateAmbilightForCurrentSlide();
      });
    }
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
  let focusedElement = 'menu'; // Start with menu (search-home button)
  const menuButtons = Array.from(document.querySelectorAll('.menu-item'));
  const tabs = Array.from(document.querySelectorAll('.tab'));
  const cards = () => Array.from(document.querySelectorAll('.movie-card'));
  const profileButton = document.querySelector('.profile');

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

  let focusedMenuIndex = 1; // Start with Home button (index 1)
  let focusedTabIndex = 0;
  let focusedCardIndex = 0;
  let lastMenuIndex = 1; // Track the last menu position

  function updateFocus() {
    const allHeros = document.querySelectorAll('.hero');
    allHeros.forEach(h => h.classList.remove('focused'));
    menuButtons.forEach((btn) => btn.classList.remove('focused'));
    tabs.forEach((tab) => tab.classList.remove('focused'));
    if (profileButton) profileButton.classList.remove('focused');

    // Remove focused and expanded from all cards
    const allCards = cards();
    allCards.forEach((card) => {
      card.classList.remove('focused');
      card.classList.remove('expanded');

      // Remove ambilight effect from title
      const title = card.querySelector('.movie-title');
      if (title) {
        title.style.textShadow = '';
      }
    });

    if (focusedElement === 'hero') {
      if (focusedHeroElement) {
        focusedHeroElement.classList.add('focused');
      }
    } else if (focusedElement === 'menu') {
      menuButtons[focusedMenuIndex].classList.add('focused');
      // Trigger ambilight update when focusing on search-home button
      if (focusedMenuIndex === 0 && menuButtons[0].classList.contains('search-home')) {
        updateAmbilightForCurrentSlide();
      }
    } else if (focusedElement === 'profile') {
      if (profileButton) profileButton.classList.add('focused');
    } else if (focusedElement === 'tabs') {
      tabs[focusedTabIndex].classList.add('focused');
    } else if (focusedElement === 'cards') {
      const cardElements = cards();
      if (cardElements[focusedCardIndex]) {
        const focusedCard = cardElements[focusedCardIndex];
        focusedCard.classList.add('focused');
        focusedCard.classList.add('expanded');

        // Add ambilight effect to title
        const title = focusedCard.querySelector('.movie-title');
        if (title) {
          title.style.textShadow = `
            0 0 20px rgba(255, 255, 255, 0.8),
            0 0 40px rgba(255, 255, 255, 0.6),
            0 0 60px rgba(255, 255, 255, 0.4)
          `;
        }

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
        focusedMenuIndex = lastMenuIndex; // Go back to where we last were
        updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        focusedElement = 'tabs';
        updateFocus();
      }
    } else if (focusedElement === 'menu') {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        if (focusedMenuIndex === 0) {
          // If on search-home button, go to profile
          focusedElement = 'profile';
          updateFocus();
        } else {
          focusedMenuIndex = focusedMenuIndex > 0 ? focusedMenuIndex - 1 : menuButtons.length - 1;
          // Automatically activate the focused menu item
          menuButtons.forEach((btn) => btn.classList.remove('active'));
          menuButtons[focusedMenuIndex].classList.add('active');
          switchCategory(menuButtons[focusedMenuIndex].dataset.hero);
          updateFocus();
        }
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
        lastMenuIndex = focusedMenuIndex; // Remember where we were
        focusedElement = 'hero';
        updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        lastMenuIndex = focusedMenuIndex; // Remember where we were
        focusedElement = 'hero';
        updateFocus();
      }
    } else if (focusedElement === 'profile') {
      if (e.key === 'ArrowRight') {
        e.preventDefault();
        focusedElement = 'menu';
        focusedMenuIndex = 0; // Go to search-home button
        updateFocus();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        focusedElement = 'hero';
        updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        // Handle profile menu action here
        console.log('Profile menu opened');
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

  // Initialize the correct active state
  menuButtons.forEach((btn) => btn.classList.remove('active'));
  menuButtons[focusedMenuIndex].classList.add('active');
  
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