# API Overview

Complete REST API reference for Lanflix backend.

## Base URL

```
http://localhost:5037/api
```

## Authentication

Lanflix uses a hybrid authentication system:
- **JWT Bearer**: Used for API authorization and SignalR.
- **Legacy Tokens**: Supported for backward compatibility with older clients.
- **Profile-Based**: Access control via `ProfileId` headers for multi-user support.

## Response Format

### Success Response

```json
{
  "data": { /* response data */ },
  "meta": {
    "timestamp": "2025-10-30T12:00:00Z",
    "apiStatus": {
      "tmdb": "online",
      "sonarr": "online",
      "radarr": "online",
      "prowlarr": "offline"
    }
  }
}
```

### Error Response

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Content not found",
    "statusCode": 404,
    "timestamp": "2025-10-30T12:00:00Z"
  }
}
```

## Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created |
| 204 | No Content | Success with no response body |
| 206 | Partial Content | Range request (streaming) |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Access denied |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 502 | Bad Gateway | External service error |
| 503 | Service Unavailable | Service temporarily unavailable |

## Rate Limiting

- **Default**: 100 requests per 15 minutes per IP
- **Streaming**: No rate limit
- **Search**: 30 requests per minute

Rate limit headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1698672000
```

## Pagination

Endpoints supporting pagination use query parameters:

```
GET /api/library/movies?page=1&limit=20
```

**Parameters**:
- `page` - Page number (default: 1)
- `limit` - Items per page (default: 20, max: 100)

**Response**:
```json
{
  "data": [ /* items */ ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 150,
    "totalPages": 8,
    "hasNext": true,
    "hasPrev": false
  }
}
```

## Filtering & Sorting

### Filtering

```
GET /api/library/movies?genre=action&year=2024
```

Common filters:
- `genre` - Filter by genre
- `year` - Filter by release year
- `rating` - Minimum rating
- `type` - Content type (movie, series)

### Sorting

```
GET /api/library/movies?sort=title&order=asc
```

**Parameters**:
- `sort` - Field to sort by
- `order` - Sort order (`asc` or `desc`)

Common sort fields:
- `title` - Alphabetical
- `releaseDate` - Release date
- `rating` - Rating
- `addedAt` - Date added to library

## Search

```
GET /api/content/search?q=inception&type=movie
```

**Parameters**:
- `q` - Search query (required)
- `type` - Content type filter (optional)
- `year` - Year filter (optional)
- `page` - Page number (optional)

## API Endpoints

### Content API
- [Content Discovery & Search](./content.md)
- Trending content
- Search
- Content details
- Queue downloads

### Library API
- [Media Library Management](./library.md)
- List movies
- List TV series
- Recently added
- Library scanning

### Streaming API
- [Video Streaming](./streaming.md)
- Stream video
- Update watch progress
- Get subtitles

### Profile API
- [User Profiles](./profile.md)
- List profiles
- Create profile
- Get watchlist
- Watch history

### Settings API
- [Application Settings](./settings.md)
- Get settings
- Update settings
- API status
- External service configuration

## Common Headers

### Request Headers

```
Content-Type: application/json
Accept: application/json
```

### Response Headers

```
Content-Type: application/json
X-API-Version: 1.0
X-Response-Time: 45ms
```

## Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Invalid request parameters |
| `NOT_FOUND` | Resource not found |
| `ALREADY_EXISTS` | Resource already exists |
| `EXTERNAL_SERVICE_ERROR` | External API error |
| `DATABASE_ERROR` | Database operation failed |
| `FILE_NOT_FOUND` | Media file not found |
| `PERMISSION_DENIED` | Access denied |
| `RATE_LIMIT_EXCEEDED` | Too many requests |

## Webhooks (Planned)

Future support for webhooks:

```json
{
  "event": "download.completed",
  "data": {
    "contentId": 123,
    "title": "Movie Title",
    "timestamp": "2025-10-30T12:00:00Z"
  }
}
```

**Events**:
- `download.completed`
- `download.failed`
- `library.updated`
- `content.added`

## WebSocket API (Planned)

Real-time updates via WebSocket:

```javascript
const ws = new WebSocket('ws://localhost:3000/ws');

ws.on('message', (data) => {
  const event = JSON.parse(data);
  console.log(event.type, event.data);
});

// Subscribe to events
ws.send(JSON.stringify({
  action: 'subscribe',
  channel: 'downloads',
  profileId: 123
}));
```

## API Versioning

Current version: `v1` (implicit)

Future versions will use URL versioning:
```
/api/v1/content
/api/v2/content
```

## CORS

CORS is enabled for all origins in development:

```javascript
app.use(cors({
  origin: '*',
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization']
}));
```

Production should restrict origins:

```javascript
app.use(cors({
  origin: 'https://yourdomain.com',
  credentials: true
}));
```

## API Examples

### JavaScript (Fetch)

```javascript
// Get trending content
const response = await fetch('http://localhost:3000/api/content/discover');
const data = await response.json();

// Search for content
const searchResponse = await fetch(
  'http://localhost:3000/api/content/search?q=inception'
);
const searchData = await searchResponse.json();

// Queue download
await fetch('http://localhost:3000/api/content/123/queue', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ profileId: 1, quality: 'HD' })
});
```

### cURL

```bash
# Get trending content
curl http://localhost:3000/api/content/discover

# Search for content
curl "http://localhost:3000/api/content/search?q=inception"

# Queue download
curl -X POST http://localhost:3000/api/content/123/queue \
  -H "Content-Type: application/json" \
  -d '{"profileId": 1, "quality": "HD"}'

# Stream video
curl -H "Range: bytes=0-1000" \
  http://localhost:3000/api/stream/123
```

### Python

```python
import requests

# Get trending content
response = requests.get('http://localhost:3000/api/content/discover')
data = response.json()

# Search for content
response = requests.get(
    'http://localhost:3000/api/content/search',
    params={'q': 'inception'}
)
data = response.json()

# Queue download
response = requests.post(
    'http://localhost:3000/api/content/123/queue',
    json={'profileId': 1, 'quality': 'HD'}
)
```

## Testing the API

### Health Check

```bash
curl http://localhost:3000/health
```

Expected response:
```json
{
  "status": "ok",
  "timestamp": "2025-10-30T12:00:00Z"
}
```

### API Status

```bash
curl http://localhost:3000/api/settings/api-status
```

Expected response:
```json
{
  "tmdb": "online",
  "sonarr": "online",
  "radarr": "online",
  "prowlarr": "offline"
}
```

## API Client Libraries

### Official Clients (Planned)
- JavaScript/TypeScript
- Python
- Go
- PHP

### Community Clients
- Submit your client library!

## Rate Limit Best Practices

1. **Cache responses** when possible
2. **Implement exponential backoff** for retries
3. **Use webhooks** instead of polling (when available)
4. **Batch requests** when possible
5. **Monitor rate limit headers**

## API Changelog

### v1.0.0 (Current)
- Initial API release
- Content discovery
- Library management
- Video streaming
- Profile management

### v1.1.0 (Planned)
- WebSocket support
- Webhooks
- API authentication
- GraphQL endpoint

## Support

- [API Documentation](./overview.md)
- [GitHub Issues](https://github.com/yourusername/lanflix/issues)
- [Discussions](https://github.com/yourusername/lanflix/discussions)

**Last Updated**: October 30, 2025
