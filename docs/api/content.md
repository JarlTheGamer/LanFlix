# Content API

API endpoints for content discovery, search, and management.

## Base URL

```
/api/content
```

## Endpoints

### Discover Content

Get trending and popular content from external sources.

```http
GET /api/content/discover
```

**Query Parameters:**
- `page` (optional) - Page number (default: 1)
- `profileId` (optional) - Profile ID for personalized results

**Response:**
```json
{
  "trending": [
    {
      "id": 123,
      "tmdbId": 550,
      "type": "movie",
      "title": "Fight Club",
      "overview": "An insomniac office worker...",
      "posterPath": "https://...",
      "backdropPath": "https://...",
      "voteAverage": 8.4,
      "releaseDate": "1999-10-15",
      "genres": ["Drama", "Thriller"]
    }
  ],
  "popular": {
    "movies": [...],
    "series": [...]
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/content/discover?profileId=1');
const data = await response.json();
```

### Get Popular Content

Get popular movies or TV series.

```http
GET /api/content/popular
```

**Query Parameters:**
- `type` (optional) - Content type: 'movie' or 'series' (default: 'movie')
- `page` (optional) - Page number (default: 1)
- `profileId` (optional) - Profile ID

**Response:**
```json
{
  "page": 1,
  "totalPages": 100,
  "totalResults": 2000,
  "results": [...]
}
```

**Example:**
```javascript
const response = await fetch('/api/content/popular?type=series&page=2');
```

### Search Content

Search for content in your library.

```http
GET /api/content/search
```

**Query Parameters:**
- `q` (required) - Search query
- `type` (optional) - Content type: 'movie', 'series', or 'all' (default: 'all')
- `profileId` (optional) - Profile ID

**Response:**
```json
{
  "query": "inception",
  "type": "all",
  "results": [
    {
      "id": 456,
      "tmdbId": 27205,
      "type": "movie",
      "title": "Inception",
      "overview": "A thief who steals corporate secrets...",
      "posterPath": "https://...",
      "voteAverage": 8.8,
      "releaseDate": "2010-07-16",
      "inLibrary": true
    }
  ]
}
```

**Example:**
```javascript
const response = await fetch('/api/content/search?q=inception&type=movie');
```

### Discovery Search

Search TMDB for content not in your library.

```http
GET /api/content/discovery/search
```

**Query Parameters:**
- `q` (required) - Search query
- `type` (optional) - Content type: 'movie', 'series', or 'all' (default: 'all')

**Response:**
```json
{
  "query": "breaking bad",
  "type": "series",
  "results": [...]
}
```

**Example:**
```javascript
const response = await fetch('/api/content/discovery/search?q=breaking+bad&type=series');
```

### Get Content Details

Get detailed information about specific content.

```http
GET /api/content/:id
```

**Path Parameters:**
- `id` (required) - TMDB ID

**Query Parameters:**
- `type` (required) - Content type: 'movie' or 'series'
- `profileId` (optional) - Profile ID for watch history

**Response:**
```json
{
  "id": 123,
  "tmdbId": 550,
  "type": "movie",
  "title": "Fight Club",
  "originalTitle": "Fight Club",
  "overview": "An insomniac office worker...",
  "releaseDate": "1999-10-15",
  "posterPath": "https://...",
  "backdropPath": "https://...",
  "voteAverage": 8.4,
  "voteCount": 25000,
  "genres": ["Drama", "Thriller", "Comedy"],
  "runtime": 139,
  "status": "available",
  "filePath": "/media/movies/Fight Club (1999)/Fight Club (1999).mkv",
  "inLibrary": true,
  "inWatchlist": false,
  "watchProgress": null
}
```

**Example:**
```javascript
const response = await fetch('/api/content/550?type=movie&profileId=1');
```

### Get Series Episodes

Get episodes for a TV series.

```http
GET /api/content/:id/episodes
```

**Path Parameters:**
- `id` (required) - TMDB ID of the series

**Query Parameters:**
- `season` (optional) - Specific season number

**Response (All Seasons):**
```json
{
  "tmdbId": 1396,
  "title": "Breaking Bad",
  "numberOfSeasons": 5,
  "numberOfEpisodes": 62,
  "seasons": [
    {
      "seasonNumber": 1,
      "episodeCount": 7,
      "airDate": "2008-01-20",
      "episodes": []
    },
    {
      "seasonNumber": 2,
      "episodeCount": 13,
      "airDate": "2009-03-08",
      "episodes": []
    }
  ]
}
```

**Response (Specific Season):**
```json
{
  "tmdbId": 1396,
  "title": "Breaking Bad",
  "numberOfSeasons": 5,
  "numberOfEpisodes": 62,
  "season": {
    "seasonNumber": 1,
    "episodeCount": 7,
    "airDate": "2008-01-20",
    "episodes": [
      {
        "id": 1,
        "seasonNumber": 1,
        "episodeNumber": 1,
        "title": "Pilot",
        "overview": "When an unassuming high school chemistry teacher...",
        "airDate": "2008-01-20",
        "stillPath": "https://...",
        "runtime": 58
      }
    ]
  }
}
```

**Example:**
```javascript
// Get all seasons
const response = await fetch('/api/content/1396/episodes');

// Get specific season
const response = await fetch('/api/content/1396/episodes?season=1');
```

### Queue Download

Add content to download queue.

```http
POST /api/content/:id/queue
```

**Path Parameters:**
- `id` (required) - TMDB ID

**Request Body:**
```json
{
  "profileId": 1,
  "type": "movie",
  "title": "Inception",
  "year": 2010
}
```

**Response:**
```json
{
  "message": "Content added to download queue",
  "queueItem": {
    "id": 1,
    "contentId": null,
    "profileId": 1,
    "status": "pending",
    "progress": 0,
    "addedAt": "2025-10-31T12:00:00.000Z"
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/content/27205/queue', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    type: 'movie',
    title: 'Inception',
    year: 2010
  })
});
```

### Queue Episode Download

Add specific episode to download queue.

```http
POST /api/content/:id/queue/episode
```

**Path Parameters:**
- `id` (required) - TMDB ID of the series

**Request Body:**
```json
{
  "profileId": 1,
  "title": "Breaking Bad",
  "seasonNumber": 1,
  "episodeNumber": 1,
  "year": 2008
}
```

**Response:**
```json
{
  "message": "Episode S1E1 added to download queue",
  "queueItem": {...}
}
```

**Example:**
```javascript
const response = await fetch('/api/content/1396/queue/episode', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    title: 'Breaking Bad',
    seasonNumber: 1,
    episodeNumber: 1,
    year: 2008
  })
});
```

### Queue Season Download

Add entire season to download queue.

```http
POST /api/content/:id/queue/season
```

**Path Parameters:**
- `id` (required) - TMDB ID of the series

**Request Body:**
```json
{
  "profileId": 1,
  "title": "Breaking Bad",
  "seasonNumber": 1,
  "year": 2008
}
```

**Response:**
```json
{
  "message": "Season 1 added to download queue",
  "queueItem": {...}
}
```

**Example:**
```javascript
const response = await fetch('/api/content/1396/queue/season', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    title: 'Breaking Bad',
    seasonNumber: 1,
    year: 2008
  })
});
```

## Error Responses

### 400 Bad Request
```json
{
  "error": "Query parameter \"type\" is required (movie or series)",
  "code": "VALIDATION_ERROR"
}
```

### 404 Not Found
```json
{
  "error": "Content not found",
  "code": "NOT_FOUND"
}
```

### 500 Internal Server Error
```json
{
  "error": "Internal server error",
  "code": "INTERNAL_ERROR"
}
```

## Rate Limiting

- 100 requests per 15 minutes per IP
- Exceeding limit returns 429 Too Many Requests

## Notes

- Content discovery requires external service configuration (Sonarr/Radarr/TMDB)
- Search results include both library content and external sources
- Download queue requires Sonarr/Radarr integration
- Episode data is cached for performance

## Next Steps

- [Library API](./library.md) - Library management
- [Streaming API](./streaming.md) - Video streaming
- [Profile API](./profile.md) - Profile management

**Last Updated**: October 31, 2025
