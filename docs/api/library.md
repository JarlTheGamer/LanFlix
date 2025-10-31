# Library API

API endpoints for managing your local media library.

## Base URL

```
/api/library
```

## Endpoints

### Get Movies

Get all movies in your library with optional filtering.

```http
GET /api/library/movies
```

**Query Parameters:**
- `genre` (optional) - Filter by genre
- `sortBy` (optional) - Sort field: 'addedAt', 'title', 'releaseDate', 'voteAverage'
- `sortOrder` (optional) - Sort order: 'ASC' or 'DESC'
- `limit` (optional) - Number of results
- `offset` (optional) - Pagination offset

**Response:**
```json
{
  "type": "movie",
  "count": 150,
  "items": [
    {
      "id": 1,
      "tmdbId": 550,
      "type": "movie",
      "title": "Fight Club",
      "overview": "An insomniac office worker...",
      "releaseDate": "1999-10-15",
      "posterPath": "https://...",
      "backdropPath": "https://...",
      "voteAverage": 8.4,
      "genres": ["Drama", "Thriller"],
      "runtime": 139,
      "filePath": "/media/movies/Fight Club (1999)/Fight Club (1999).mkv",
      "addedAt": "2025-10-01T10:00:00.000Z"
    }
  ]
}
```

**Example:**
```javascript
const response = await fetch('/api/library/movies?sortBy=addedAt&sortOrder=DESC&limit=20');
```

### Get TV Series

Get all TV series in your library.

```http
GET /api/library/series
```

**Query Parameters:** Same as movies endpoint

**Response:**
```json
{
  "type": "series",
  "count": 45,
  "items": [...]
}
```

### Get Recently Added

Get recently added content across all types.

```http
GET /api/library/recent
```

**Query Parameters:**
- `limit` (optional) - Number of results (default: 20)

**Response:**
```json
{
  "count": 20,
  "items": [...]
}
```

**Example:**
```javascript
const response = await fetch('/api/library/recent?limit=10');
```

### Get Library Item

Get detailed information about a specific library item.

```http
GET /api/library/:id
```

**Path Parameters:**
- `id` (required) - Content ID

**Query Parameters:**
- `profileId` (optional) - Include watch progress for profile

**Response:**
```json
{
  "id": 1,
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
  "addedAt": "2025-10-01T10:00:00.000Z",
  "watchProgress": {
    "progressSeconds": 3600,
    "durationSeconds": 8340,
    "completed": false,
    "lastWatched": "2025-10-30T20:00:00.000Z"
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/library/1?profileId=1');
```

### Add to Library

Manually add content to library.

```http
POST /api/library
```

**Request Body:**
```json
{
  "tmdbId": 550,
  "type": "movie",
  "filePath": "/media/movies/Fight Club (1999)/Fight Club (1999).mkv"
}
```

**Response:**
```json
{
  "message": "Content added to library",
  "content": {
    "id": 1,
    "tmdbId": 550,
    "type": "movie",
    "title": "Fight Club",
    ...
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/library', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    tmdbId: 550,
    type: 'movie',
    filePath: '/media/movies/Fight Club (1999)/Fight Club (1999).mkv'
  })
});
```

### Remove from Library

Remove content from library.

```http
DELETE /api/library/:id
```

**Path Parameters:**
- `id` (required) - Content ID

**Response:**
```json
{
  "message": "Item removed from library",
  "id": 1
}
```

**Example:**
```javascript
const response = await fetch('/api/library/1', {
  method: 'DELETE'
});
```

## Error Responses

### 400 Bad Request
```json
{
  "error": "Missing required fields: tmdbId, type, filePath",
  "code": "VALIDATION_ERROR"
}
```

### 404 Not Found
```json
{
  "error": "Library item not found",
  "code": "NOT_FOUND"
}
```

## Filtering & Sorting

### Genre Filtering
```javascript
// Get action movies
const response = await fetch('/api/library/movies?genre=Action');
```

### Sorting
```javascript
// Sort by rating (highest first)
const response = await fetch('/api/library/movies?sortBy=voteAverage&sortOrder=DESC');

// Sort by recently added
const response = await fetch('/api/library/movies?sortBy=addedAt&sortOrder=DESC');
```

### Pagination
```javascript
// Get page 2 (20 items per page)
const response = await fetch('/api/library/movies?limit=20&offset=20');
```

## Next Steps

- [Content API](./content.md) - Content discovery
- [Streaming API](./streaming.md) - Video streaming
- [Settings API](./settings.md) - Library settings

**Last Updated**: October 31, 2025
