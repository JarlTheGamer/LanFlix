# Content Discovery

Browse and discover movies and TV series from external sources.

## Overview

Lanflix integrates with multiple external services to help you discover and add content to your library:
- **TMDB** - Metadata and trending content
- **Sonarr** - TV series management
- **Radarr** - Movie management
- **Prowlarr** - Indexer search

## Features

### Trending Content

View currently trending movies and TV series.

**How it works:**
1. Fetches trending data from TMDB
2. Checks if content is already in your library
3. Displays with "In Library" badge if available
4. Shows download option if not in library

**UI Location:** Home page, top section

### Popular Content

Browse popular movies and TV series by category.

**Categories:**
- Popular Movies
- Popular TV Series
- Top Rated
- Now Playing (Movies)
- On The Air (TV)

**Features:**
- Infinite scroll pagination
- Genre filtering
- Sort by popularity, rating, or release date

### Search

Search across all content sources.

**Search Types:**

1. **Library Search** - Search your local library
   - Fast, instant results
   - Includes movies and TV series
   - Shows file availability

2. **Discovery Search** - Search TMDB database
   - Millions of titles
   - Real-time results
   - Download integration

**Search Features:**
- Auto-complete suggestions
- Type filtering (movies/series/all)
- Debounced search (300ms delay)
- Search history

## Content Cards

Each content item displays:
- Poster image
- Title
- Release year
- Rating (TMDB score)
- Genre tags
- Library status badge

**Card Actions:**
- Click to view details
- Hover for quick info
- Add to My List
- Download (if not in library)

## Content Detail Modal

Detailed view when clicking a content card.

**Information Displayed:**
- Full poster and backdrop
- Title and original title
- Overview/plot summary
- Release date
- Runtime
- Genres
- Cast and crew
- Rating and vote count
- Similar content recommendations

**Actions Available:**
- Play (if in library)
- Add to My List
- Download
- View episodes (for TV series)
- Share

## TV Series Episodes

Browse and select episodes for TV series.

**Features:**
- Season selector
- Episode list with thumbnails
- Episode details (title, overview, air date)
- Watch progress indicators
- Download individual episodes or full seasons

**Episode Information:**
- Season and episode numbers
- Episode title
- Air date
- Runtime
- Still image
- Overview

## Download Integration

Request content downloads directly from discovery.

**Download Options:**

1. **Movies** - Download entire movie
2. **TV Series** - Download options:
   - Single episode
   - Full season
   - Entire series

**Download Process:**
1. Click download button
2. Content added to Sonarr/Radarr
3. Download queue updated
4. Notification when complete
5. Automatic library scan

## Filters and Sorting

### Genre Filters
- Action
- Comedy
- Drama
- Horror
- Sci-Fi
- Thriller
- And more...

### Sort Options
- Popularity
- Rating
- Release Date
- Title (A-Z)

### Type Filters
- Movies only
- TV Series only
- All content

## Personalization

Content discovery adapts to your profile:
- Watch history influences recommendations
- My List items highlighted
- Recently watched content
- Continue watching section

## External Service Integration

### TMDB Integration

**What it provides:**
- Content metadata
- Poster and backdrop images
- Trending and popular lists
- Search functionality
- Cast and crew information

**Configuration:**
```env
TMDB_API_KEY=your_api_key_here
```

### Sonarr Integration

**What it provides:**
- TV series management
- Episode tracking
- Download automation
- Quality profiles

**Configuration:**
```env
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_api_key
```

### Radarr Integration

**What it provides:**
- Movie management
- Download automation
- Quality profiles

**Configuration:**
```env
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_api_key
```

## API Usage

### Discover Content
```javascript
const response = await fetch('/api/content/discover?profileId=1');
const { trending, popular } = await response.json();
```

### Search Content
```javascript
const response = await fetch('/api/content/search?q=inception&type=movie');
const { results } = await response.json();
```

### Get Content Details
```javascript
const response = await fetch('/api/content/550?type=movie&profileId=1');
const content = await response.json();
```

### Get Episodes
```javascript
const response = await fetch('/api/content/1396/episodes?season=1');
const { season } = await response.json();
```

## Performance

### Caching

Content discovery uses multi-layer caching:
- **Redis** - API responses (1 hour TTL)
- **Memory** - Frequently accessed data
- **File** - Poster and backdrop images

### Lazy Loading

Images and content load progressively:
- Intersection Observer for viewport detection
- Placeholder images while loading
- Progressive image loading

### Debouncing

Search requests are debounced to reduce API calls:
- 300ms delay after typing stops
- Cancels previous requests
- Shows loading indicator

## User Interface

### Home Page Layout

```
┌─────────────────────────────────────┐
│  Search Bar                         │
├─────────────────────────────────────┤
│  Trending Now                       │
│  [Card] [Card] [Card] [Card]       │
├─────────────────────────────────────┤
│  Popular Movies                     │
│  [Card] [Card] [Card] [Card]       │
├─────────────────────────────────────┤
│  Popular TV Series                  │
│  [Card] [Card] [Card] [Card]       │
└─────────────────────────────────────┘
```

### Content Modal Layout

```
┌─────────────────────────────────────┐
│  [Backdrop Image]                   │
│  ┌──────┐                           │
│  │Poster│  Title                    │
│  │      │  Rating ★ 8.4             │
│  └──────┘  2010 • 148 min • Sci-Fi  │
├─────────────────────────────────────┤
│  [Play] [+ My List] [Download]     │
├─────────────────────────────────────┤
│  Overview                           │
│  A thief who steals corporate...   │
├─────────────────────────────────────┤
│  Cast                               │
│  [Actor] [Actor] [Actor]            │
└─────────────────────────────────────┘
```

## Keyboard Shortcuts

- `Esc` - Close modal
- `/` - Focus search
- `Enter` - Open selected content
- `Arrow Keys` - Navigate content cards

## Mobile Support

Content discovery is fully responsive:
- Touch-friendly cards
- Swipe gestures
- Mobile-optimized modals
- Adaptive grid layout

## Troubleshooting

### No Content Showing

**Check:**
1. TMDB API key configured
2. Internet connection active
3. External services running
4. Browser console for errors

### Images Not Loading

**Check:**
1. TMDB image URLs accessible
2. CORS configuration
3. Image cache directory writable
4. Network firewall settings

### Search Not Working

**Check:**
1. Search query length (min 2 characters)
2. API rate limits
3. TMDB service status
4. Browser console errors

## Next Steps

- [Download Management](./download-management.md) - Queue downloads
- [Multi-Profile Support](./multi-profile.md) - Personalized discovery
- [Content API](../api/content.md) - API reference

**Last Updated**: October 31, 2025
