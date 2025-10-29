// Mock data - will be replaced with API calls later
export const PROFILES = [
  {
    id: 1,
    name: 'Alex',
    avatar: { primary: '#ff6b6b', secondary: '#ee5a24' },
    watchedShows: [
      { title: 'Stranger Things', image: 'https://image.tmdb.org/t/p/original/49WJfeN0moxb9IPfGn8AIqMGskD.jpg', meta: 'S4 • Sci-Fi' },
      { title: 'The Crown', image: 'https://image.tmdb.org/t/p/original/1M876KPjulVwppEpldhdc8V4o68.jpg', meta: 'S6 • Drama' },
      { title: 'Wednesday', image: 'https://m.media-amazon.com/images/M/MV5BNjkxNzlhMTAtZGQ3Mi00NDNmLWJkMWEtMWQ3ZjNiMWRjOGVlXkEyXkFqcGdeQWRvb2xpbmhk._V1_.jpg', meta: 'S1 • Mystery' },
      { title: 'The Witcher', image: 'https://image.tmdb.org/t/p/original/7vjaCdMw15FEbXyLQTVa04URsPm.jpg', meta: 'S3 • Fantasy' },
      { title: 'Arcane', image: 'https://image.tmdb.org/t/p/original/fqldf2t8ztc9aiwn3k6mlX3tvRT.jpg', meta: 'S1 • Animation' },
      { title: 'The Mandalorian', image: 'https://image.tmdb.org/t/p/original/sWgBv7LV2PRoQgkxwlibdGXKz1S.jpg', meta: 'S3 • Sci-Fi' },
    ]
  },
  {
    id: 2,
    name: 'Sarah',
    avatar: { primary: '#4ecdc4', secondary: '#26d0ce' },
    watchedShows: [
      { title: 'The Queen\'s Gambit', image: 'https://image.tmdb.org/t/p/original/zU0htwkhNvBQdVSIKB9s6hgVeFK.jpg', meta: 'Limited • Drama' },
      { title: 'Euphoria', image: 'https://image.tmdb.org/t/p/original/jtnfNzqZwN4E32FGGxx1YZaBWWf.jpg', meta: 'S2 • Drama' }
    ]
  },
  {
    id: 3,
    name: 'Marcus',
    avatar: { primary: '#a55eea', secondary: '#8854d0' },
    watchedShows: [
      { title: 'Breaking Bad', image: 'https://image.tmdb.org/t/p/original/3xnWaLQjelJDDF7LT1WBo6f4BRe.jpg', meta: 'S5 • Crime' },
      { title: 'The Bear', image: 'https://image.tmdb.org/t/p/original/sHFlbKS3WLqMnp9t2ghADIJFnuQ.jpg', meta: 'S3 • Comedy' }
    ]
  },
  {
    id: 4,
    name: 'Kids',
    avatar: { primary: '#ffa726', secondary: '#ff9800' },
    watchedShows: [
      { title: 'Arcane', image: 'https://image.tmdb.org/t/p/original/fqldf2t8ztc9aiwn3k6mlX3tvRT.jpg', meta: 'S1 • Animation' },
      { title: 'The Mandalorian', image: 'https://image.tmdb.org/t/p/original/sWgBv7LV2PRoQgkxwlibdGXKz1S.jpg', meta: 'S3 • Sci-Fi' },
      { title: 'Avatar: The Last Airbender', image: 'https://image.tmdb.org/t/p/original/cMD9Ygz11zjJzAovURpO75Qg7rT.jpg', meta: 'S3 • Animation' },
    ]
  }
];

export const HEROES = {
  home: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/8rpDcsfLJypbO6vREc0547VKqEv.jpg)',
      tag: 'New Release • Sci-Fi',
      title: 'Avatar',
      meta: ['Movie', '2024', 'PG-13', '2h 46m'],
      description: 'Paul Atreides unites with Chani and the Fremen while seeking revenge against the conspirators who destroyed his family.',
      secondary: 'Now streaming in 4K UHD',
    },
    {
      background: 'url(https://www.hdwallpapers.in/download/the_boys_poster_4k_hd-3840x2160.jpg)',
      tag: 'Popular • Superhero',
      title: 'The Boys',
      meta: ['Series', '2019–', 'TV-MA', '4 Seasons'],
      description: 'A group of vigilantes set out to take down corrupt superheroes who abuse their powers.',
      secondary: 'Season 4 now streaming',
    },
    {
      background: 'url(https://image.tmdb.org/t/p/original/fYPiQewg7ogbzro2XcCTACSB2KC.jpg)',
      tag: 'Top Pick • Fantasy',
      title: 'House of the Dragon',
      meta: ['Series', '2022–', 'TV-MA', '2 Seasons'],
      description: 'The Targaryen dynasty rules Westeros — and the seeds of civil war begin to take root 200 years before the events of Game of Thrones.',
      secondary: 'New season coming in 2025',
    },
  ],
  discover: [
    {
      background: 'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Because you watched',
      title: 'Midnight Tales',
      meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
      description: 'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
      secondary: 'Continue watching S3:E4',
    },
  ],
  shows: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/oqP1qEZccq5AD9TVTIaO6IGUj7o.jpg)',
      tag: 'Hit Series • Thriller',
      title: 'Squid Game',
      meta: ['Series', '2021', 'TV-MA', '9 Episodes'],
      description: 'Hundreds of cash-strapped contestants accept an invitation to compete in children\'s games for a tempting prize, but the stakes are deadly.',
      secondary: 'Season 2 premieres June 27',
    },
    {
      background: 'url(https://image.tmdb.org/t/p/original/qYeg0MP1LpPD5r5h9wxR83DMnyE.jpg)',
      tag: 'New Series • Fantasy',
      title: 'Wednesday',
      meta: ['Series', '2022', 'TV-14', '8 Episodes'],
      description: 'Smart, sarcastic and a little dead inside, Wednesday Addams investigates a murder spree while making new friends — and foes — at Nevermore Academy.',
      secondary: 'Season 2 coming soon',
    },
  ],
  movies: [
    {
      background: 'url(https://image.tmdb.org/t/p/original/4VujM9lbRv6j8N3w6JkYp1q5bZp.jpg)',
      tag: 'Exclusive Film',
      title: 'The Gray Man',
      meta: ['Movie', '2022', '2h 9m'],
      description: 'When a shadowy CIA agent uncovers damning agency secrets, he\'s hunted across the globe by a sociopathic rogue operative who\'s put a bounty on his head.',
      secondary: 'Now streaming in Ultra HD',
    },
  ],
  my: [
    {
      background: 'url(https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=1400&q=80)',
      tag: 'Because you watched',
      title: 'Midnight Tales',
      meta: ['Series', '2020', 'TV-MA', '3 Seasons'],
      description: 'Dive back into the anthology of haunting stories where every episode unlocks a new mystery, curated from your personal watch history.',
      secondary: 'Continue watching S3:E4',
    },
  ],
};

export const MOVIES = [
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
  // Add more movies/series as needed
];
