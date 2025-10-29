# Design Document

## Overview

The Lanflix streaming media application consists of two main components: a Node.js backend server and an enhanced HTML/CSS/JavaScript frontend. The backend provides RESTful APIs for content discovery, library management, and media streaming, while integrating with Sonarr, Radarr, and Prowlarr for automated content acquisition. The existing frontend UI will be refactored into modular components and packaged for multiple platforms using Electron (PC) and Capacitor (Android/Android TV).

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend Applications                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Electron   │  │  Capacitor   │  │  Capacitor   │     │
│  │   (PC/Mac)   │  │  (Android)   │  │ (Android TV) │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│         │                  │                  │             │
│         └──────────────────┴──────────────────┘             │
│                            │                                │
│                   Existing Lanflix UI                       │
│              (HTML/CSS/JS - Refactored)                     │
└─────────────────────────────────────────────────────────────┘
                             │
                    HTTP/WebSocket
                             │
┌─────────────────────────────────────────────────────────────┐
│                    Backend Server (Node.js)                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Express.js REST API                      │  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │  │
│  │  │Content │ │Library │ │Profile │ │Streaming│        │  │
│  │  │Routes  │ │Routes  │ │Routes  │ │Routes   │        │  │
│  │  └────────┘ └────────┘ └────────┘ └────────┘        │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Business Logic Layer                     │  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │  │
│  │  │Content │ │Library │ │Download│ │Metadata│        │  │
│  │  │Service │ │Service │ │Manager │ │Service │        │  │
│  │  └────────┘ └────────┘ └────────┘ └────────┘        │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           External Service Integrations               │  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │  │
│  │  │Sonarr  │ │Radarr  │ │Prowlarr│ │  TMDB  │        │  │
│  │  │Client  │ │Client  │ │Client  │ │ Client │        │  │
│  │  └────────┘ └────────┘ └────────┘ └────────┘        │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                 Data Layer                            │  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐                   │  │
│  │  │SQLite  │ │File    │ │Cache   │                   │  │
│  │  │Database│ │Storage │ │(Redis) │                   │  │
│  │  └────────┘ └────────┘ └────────┘                   │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             │
                             │
┌─────────────────────────────────────────────────────────────┐
│              External Services (User-Managed)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                 │
│  │  Sonarr  │  │  Radarr  │  │ Prowlarr │                 │
│  └──────────┘  └──────────┘  └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack

**Backend:**
- Runtime: Node.js 18+
- Framework: Express.js
- Database: SQLite (with Sequelize ORM)
- Caching: Redis (optional, in-memory fallback)
- Media Streaming: fluent-ffmpeg
- API Clients: axios
- WebSocket: socket.io (for real-time updates)

**Frontend:**
- Core: Existing HTML/CSS/JavaScript
- Bundler: Webpack or Vite
- PC Packaging: Electron
- Mobile Packaging: Capacitor
- HTTP Client: axios or fetch API
- Video Player: Video.js or Plyr

## Components and Interfaces

### Backend Components

#### 1. API Routes Layer

**Content Routes** (`/api/content`)
- `GET /api/content/discover` - Get trending and popular content
- `GET /api/content/search?q={query}` - Search for content
- `GET /api/content/:id` - Get detailed content information
- `POST /api/content/:id/queue` - Add content to download queue

**Library Routes** (`/api/library`)
- `GET /api/library/movies` - Get all movies in library
- `GET /api/library/series` - Get all TV series in library
- `GET /api/library/recent` - Get recently added content
- `GET /api/library/:id` - Get specific library item details
- `DELETE /api/library/:id` - Remove item from library

**Profile Routes** (`/api/profiles`)
- `GET /api/profiles` - Get all profiles
- `POST /api/profiles` - Create new profile
- `GET /api/profiles/:id` - Get profile details
- `PUT /api/profiles/:id` - Update profile
- `DELETE /api/profiles/:id` - Delete profile
- `GET /api/profiles/:id/watchlist` - Get profile's My List
- `POST /api/profiles/:id/watchlist/:contentId` - Add to My List
- `DELETE /api/profiles/:id/watchlist/:contentId` - Remove from My List

**Streaming Routes** (`/api/stream`)
- `GET /api/stream/:id` - Stream media file
- `POST /api/stream/:id/progress` - Update watch progress
- `GET /api/stream/:id/subtitles` - Get available subtitles

**Notification Routes** (`/api/notifications`)
- `POST /api/notifications/register` - Register device for push notifications
- `POST /api/notifications/:id/respond` - Respond to keep-watching notification
- `GET /api/notifications/:profileId` - Get notification history

**Settings Routes** (`/api/settings`)
- `GET /api/settings` - Get application settings
- `PUT /api/settings` - Update application settings
- `GET /api/settings/services` - Get external service connection status

#### 2. Service Layer

**ContentService**
```javascript
class ContentService {
  async searchContent(query, type = 'all')
  async getContentDetails(id, type)
  async getTrendingContent(type, page = 1)
  async getPopularContent(type, page = 1)
}
```

**LibraryService**
```javascript
class LibraryService {
  async getLibraryItems(type, filters)
  async getLibraryItem(id)
  async addToLibrary(contentId, metadata, filePath)
  async removeFromLibrary(id)
  async scanLibraryFolder()
  async getRecentlyAdded(limit = 20)
}
```

**DownloadManager**
```javascript
class DownloadManager {
  async queueDownload(contentId, type, profileId)
  async getDownloadStatus(contentId)
  async cancelDownload(contentId)
  async pollDownloadProgress()
  async handleDownloadComplete(contentId)
  async scheduleAutoDelete(contentId, daysUntilDelete = 30)
  async sendKeepWatchingNotification(contentId, profileId)
  async handleKeepWatchingResponse(contentId, profileId, keepContent)
}
```

**MetadataService**
```javascript
class MetadataService {
  async fetchMovieMetadata(tmdbId)
  async fetchSeriesMetadata(tmdbId)
  async downloadPosterImage(url, contentId)
  async downloadBackdropImage(url, contentId)
  async refreshMetadata(contentId)
  async saveMetadataToMediaFolder(contentId, mediaFolderPath)
  async loadMetadataFromMediaFolder(mediaFolderPath)
}
```

**NotificationService**
```javascript
class NotificationService {
  async sendPushNotification(profileId, title, message, data)
  async sendKeepWatchingPrompt(profileId, contentId, contentTitle)
  async registerDeviceToken(profileId, deviceToken, platform)
  async unregisterDeviceToken(deviceToken)
}
```

#### 3. External Service Clients

**SonarrClient**
```javascript
class SonarrClient {
  constructor(baseUrl, apiKey)
  async searchSeries(query)
  async addSeries(tvdbId, qualityProfileId, rootFolder)
  async getSeries()
  async getSeriesById(id)
  async getQueue()
  async deleteSeries(id)
}
```

**RadarrClient**
```javascript
class RadarrClient {
  constructor(baseUrl, apiKey)
  async searchMovies(query)
  async addMovie(tmdbId, qualityProfileId, rootFolder)
  async getMovies()
  async getMovieById(id)
  async getQueue()
  async deleteMovie(id)
}
```

**ProwlarrClient**
```javascript
class ProwlarrClient {
  constructor(baseUrl, apiKey)
  async search(query, type = 'all')
  async getIndexers()
  async testIndexer(id)
}
```

**TMDBClient**
```javascript
class TMDBClient {
  constructor(apiKey)
  async searchMovie(query)
  async searchTV(query)
  async getMovieDetails(id)
  async getTVDetails(id)
  async getTrending(mediaType, timeWindow = 'week')
  async getPopular(mediaType)
}
```

### Frontend Components

#### Refactored Module Structure

**modules/api-client.js**
- Handles all HTTP requests to backend
- Manages authentication tokens
- Implements request/response interceptors
- Provides typed API methods

**modules/navigation.js**
- Manages menu navigation and routing
- Handles keyboard/remote control input
- Updates active menu states
- Manages page transitions

**modules/content-display.js**
- Renders content carousels
- Handles card expansion/collapse
- Manages hero carousel
- Updates content metadata displays

**modules/profile-manager.js**
- Handles profile selection
- Manages profile CRUD operations
- Stores active profile state
- Syncs profile data with backend

**modules/video-player.js**
- Initializes video player
- Handles playback controls
- Tracks watch progress
- Manages subtitle selection

**modules/settings-manager.js**
- Handles settings UI interactions
- Syncs settings with backend
- Manages form validation
- Updates application configuration

## Data Models

### Database Schema

**Profiles Table**
```sql
CREATE TABLE profiles (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name VARCHAR(255) NOT NULL,
  avatar_color_primary VARCHAR(7) NOT NULL,
  avatar_color_secondary VARCHAR(7) NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**Content Table**
```sql
CREATE TABLE content (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  tmdb_id INTEGER UNIQUE NOT NULL,
  type VARCHAR(20) NOT NULL, -- 'movie' or 'series'
  title VARCHAR(255) NOT NULL,
  original_title VARCHAR(255),
  overview TEXT,
  release_date DATE,
  poster_path VARCHAR(255),
  backdrop_path VARCHAR(255),
  vote_average DECIMAL(3,1),
  vote_count INTEGER,
  genres TEXT, -- JSON array
  runtime INTEGER,
  status VARCHAR(50),
  file_path VARCHAR(500),
  added_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**Series_Episodes Table**
```sql
CREATE TABLE series_episodes (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content_id INTEGER NOT NULL,
  season_number INTEGER NOT NULL,
  episode_number INTEGER NOT NULL,
  title VARCHAR(255),
  overview TEXT,
  air_date DATE,
  still_path VARCHAR(255),
  file_path VARCHAR(500),
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE
);
```

**Watch_History Table**
```sql
CREATE TABLE watch_history (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  content_id INTEGER NOT NULL,
  episode_id INTEGER, -- NULL for movies
  progress_seconds INTEGER DEFAULT 0,
  duration_seconds INTEGER,
  completed BOOLEAN DEFAULT FALSE,
  last_watched_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE,
  FOREIGN KEY (episode_id) REFERENCES series_episodes(id) ON DELETE CASCADE
);
```

**Watchlist Table**
```sql
CREATE TABLE watchlist (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  content_id INTEGER NOT NULL,
  added_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE,
  UNIQUE(profile_id, content_id)
);
```

**Download_Queue Table**
```sql
CREATE TABLE download_queue (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  content_id INTEGER NOT NULL,
  type VARCHAR(20) NOT NULL,
  external_id INTEGER, -- Sonarr/Radarr ID
  status VARCHAR(50) DEFAULT 'queued', -- queued, downloading, completed, failed
  progress_percent INTEGER DEFAULT 0,
  error_message TEXT,
  queued_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  completed_at DATETIME,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE
);
```

**Settings Table**
```sql
CREATE TABLE settings (
  key VARCHAR(255) PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**Auto_Delete_Schedule Table**
```sql
CREATE TABLE auto_delete_schedule (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content_id INTEGER NOT NULL,
  scheduled_delete_at DATETIME NOT NULL,
  notification_sent BOOLEAN DEFAULT FALSE,
  notification_sent_at DATETIME,
  user_response VARCHAR(20), -- 'keep', 'delete', or NULL
  response_at DATETIME,
  deleted BOOLEAN DEFAULT FALSE,
  deleted_at DATETIME,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE
);
```

**Device_Tokens Table**
```sql
CREATE TABLE device_tokens (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  device_token VARCHAR(500) NOT NULL UNIQUE,
  platform VARCHAR(50) NOT NULL, -- 'android', 'android-tv', 'web'
  registered_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  last_used_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE
);
```

### API Response Models

**Content Response**
```typescript
interface ContentResponse {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  originalTitle: string;
  overview: string;
  releaseDate: string;
  posterUrl: string;
  backdropUrl: string;
  voteAverage: number;
  voteCount: number;
  genres: string[];
  runtime?: number; // minutes, for movies
  status: string;
  inLibrary: boolean;
  inWatchlist: boolean;
}
```

**Library Item Response**
```typescript
interface LibraryItemResponse extends ContentResponse {
  filePath: string;
  addedAt: string;
  watchProgress?: {
    progressSeconds: number;
    durationSeconds: number;
    completed: boolean;
    lastWatchedAt: string;
  };
  episodes?: EpisodeResponse[]; // for series
}
```

**Episode Response**
```typescript
interface EpisodeResponse {
  id: number;
  seasonNumber: number;
  episodeNumber: number;
  title: string;
  overview: string;
  airDate: string;
  stillUrl: string;
  filePath?: string;
  watched: boolean;
}
```

**Profile Response**
```typescript
interface ProfileResponse {
  id: number;
  name: string;
  avatarColorPrimary: string;
  avatarColorSecondary: string;
  createdAt: string;
}
```

**Download Status Response**
```typescript
interface DownloadStatusResponse {
  id: number;
  contentId: number;
  status: 'queued' | 'downloading' | 'completed' | 'failed';
  progressPercent: number;
  errorMessage?: string;
  queuedAt: string;
  completedAt?: string;
}
```

## Error Handling

### Backend Error Handling Strategy

1. **API Error Responses**
   - Use consistent error response format
   - Include error codes for client handling
   - Log all errors with context

```typescript
interface ErrorResponse {
  error: {
    code: string;
    message: string;
    details?: any;
  };
}
```

2. **External Service Failures**
   - Implement circuit breaker pattern
   - Return cached data when available
   - Provide meaningful error messages to users

3. **Database Errors**
   - Wrap all database operations in try-catch
   - Implement transaction rollback
   - Log errors with query context

4. **File System Errors**
   - Validate file paths before operations
   - Handle permission errors gracefully
   - Implement retry logic for transient failures

### Frontend Error Handling

1. **Network Errors**
   - Display user-friendly error messages
   - Implement automatic retry for failed requests
   - Show offline indicator when backend unavailable

2. **Playback Errors**
   - Detect and report codec issues
   - Fallback to alternative quality streams
   - Provide clear error messages with solutions

## Testing Strategy

### Backend Testing

**Unit Tests**
- Test individual service methods
- Mock external API clients
- Test database models and queries
- Coverage target: 80%

**Integration Tests**
- Test API endpoints end-to-end
- Test external service integrations
- Test database operations
- Use test database instance

**Performance Tests**
- Load test streaming endpoints
- Test concurrent user scenarios
- Measure API response times
- Test database query performance

### Frontend Testing

**Unit Tests**
- Test individual module functions
- Mock API client responses
- Test UI state management
- Coverage target: 70%

**E2E Tests**
- Test critical user flows
- Test navigation and routing
- Test video playback
- Test across different platforms

**Manual Testing**
- Test on actual Android TV devices
- Test on various Android phones
- Test on Windows/Mac/Linux
- Test with different screen sizes

## Deployment and Configuration

### Backend Deployment

**Environment Variables**
```
# Server Configuration
PORT=3000
NODE_ENV=production
LOG_LEVEL=info

# Database
DATABASE_PATH=./data/lanflix.db

# Media Storage
MEDIA_ROOT_PATH=/path/to/media
POSTER_CACHE_PATH=./data/posters
BACKDROP_CACHE_PATH=./data/backdrops

# External Services
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_sonarr_api_key
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_radarr_api_key
PROWLARR_URL=http://localhost:9696
PROWLARR_API_KEY=your_prowlarr_api_key
TMDB_API_KEY=your_tmdb_api_key

# Optional
REDIS_URL=redis://localhost:6379
```

**Directory Structure**
```
lanflix-server/
├── src/
│   ├── routes/
│   │   ├── content.routes.js
│   │   ├── library.routes.js
│   │   ├── profile.routes.js
│   │   ├── streaming.routes.js
│   │   └── settings.routes.js
│   ├── services/
│   │   ├── content.service.js
│   │   ├── library.service.js
│   │   ├── download-manager.service.js
│   │   └── metadata.service.js
│   ├── clients/
│   │   ├── sonarr.client.js
│   │   ├── radarr.client.js
│   │   ├── prowlarr.client.js
│   │   └── tmdb.client.js
│   ├── models/
│   │   ├── profile.model.js
│   │   ├── content.model.js
│   │   ├── episode.model.js
│   │   ├── watch-history.model.js
│   │   ├── watchlist.model.js
│   │   └── download-queue.model.js
│   ├── middleware/
│   │   ├── error-handler.js
│   │   ├── logger.js
│   │   └── validation.js
│   ├── utils/
│   │   ├── database.js
│   │   ├── cache.js
│   │   └── file-utils.js
│   └── app.js
├── data/
│   ├── lanflix.db
│   ├── posters/
│   └── backdrops/
├── logs/
├── package.json
└── .env
```

### Frontend Deployment

**Build Configuration**

*Electron (PC)*
```json
{
  "build": {
    "appId": "com.lanflix.app",
    "productName": "Lanflix",
    "directories": {
      "output": "dist"
    },
    "files": [
      "build/**/*",
      "node_modules/**/*"
    ],
    "win": {
      "target": "nsis"
    },
    "mac": {
      "target": "dmg"
    },
    "linux": {
      "target": "AppImage"
    }
  }
}
```

*Capacitor (Android/Android TV)*
```json
{
  "appId": "com.lanflix.app",
  "appName": "Lanflix",
  "webDir": "build",
  "bundledWebRuntime": false,
  "plugins": {
    "SplashScreen": {
      "launchShowDuration": 0
    }
  }
}
```

**Frontend Directory Structure**
```
lanflix-ui/
├── src/
│   ├── modules/
│   │   ├── api-client.js
│   │   ├── navigation.js
│   │   ├── content-display.js
│   │   ├── profile-manager.js
│   │   ├── video-player.js
│   │   └── settings-manager.js
│   ├── pages/
│   │   ├── index.html
│   │   └── settings.html
│   ├── styles/
│   │   ├── styles.css
│   │   └── settings.css
│   ├── assets/
│   │   └── images/
│   └── main.js
├── electron/
│   └── main.js
├── android/
├── package.json
└── capacitor.config.json
```

## Security Considerations

1. **API Security**
   - Implement rate limiting on all endpoints
   - Validate and sanitize all user inputs
   - Use HTTPS in production
   - Implement CORS properly

2. **File Access**
   - Validate file paths to prevent directory traversal
   - Restrict media streaming to authorized content
   - Implement file access logging

3. **External Service Credentials**
   - Store API keys in environment variables
   - Never expose credentials in frontend
   - Implement credential rotation support

4. **User Data**
   - Encrypt sensitive profile data
   - Implement data retention policies
   - Provide data export functionality

## Metadata Storage Strategy

### Server-Side Metadata Storage

Metadata is stored on the server in two locations:

1. **Database** - For fast queries and filtering
2. **Media Folder** - For backup and portability

### Media Folder Structure

When content is downloaded, metadata will be stored alongside the media files on the server:

```
/server/media/
├── movies/
│   └── Avatar (2009)/
│       ├── Avatar (2009).mkv
│       ├── metadata.json          # TMDB metadata (server-side)
│       ├── poster.jpg              # Movie poster (server-side)
│       ├── backdrop.jpg            # Backdrop image (server-side)
│       └── subtitles/
│           ├── Avatar.en.srt
│           └── Avatar.es.srt
└── series/
    └── Breaking Bad/
        ├── metadata.json           # Series metadata (server-side)
        ├── poster.jpg
        ├── backdrop.jpg
        └── Season 01/
            ├── Breaking Bad - S01E01.mkv
            ├── Breaking Bad - S01E01.json  # Episode metadata (server-side)
            └── Breaking Bad - S01E01.jpg   # Episode still (server-side)
```

**Note:** All metadata files are stored on the server. Clients fetch this data via API calls.

### Metadata JSON Format

**Movie metadata.json**
```json
{
  "tmdbId": 19995,
  "title": "Avatar",
  "originalTitle": "Avatar",
  "overview": "In the 22nd century...",
  "releaseDate": "2009-12-18",
  "runtime": 162,
  "voteAverage": 7.6,
  "voteCount": 28000,
  "genres": ["Action", "Adventure", "Fantasy", "Science Fiction"],
  "cast": [
    {"name": "Sam Worthington", "character": "Jake Sully"},
    {"name": "Zoe Saldana", "character": "Neytiri"}
  ],
  "director": "James Cameron",
  "posterPath": "poster.jpg",
  "backdropPath": "backdrop.jpg",
  "fetchedAt": "2024-10-29T12:00:00Z"
}
```

**Series metadata.json**
```json
{
  "tmdbId": 1396,
  "title": "Breaking Bad",
  "overview": "A high school chemistry teacher...",
  "firstAirDate": "2008-01-20",
  "lastAirDate": "2013-09-29",
  "numberOfSeasons": 5,
  "numberOfEpisodes": 62,
  "genres": ["Drama", "Crime"],
  "voteAverage": 8.9,
  "posterPath": "poster.jpg",
  "backdropPath": "backdrop.jpg",
  "seasons": [
    {
      "seasonNumber": 1,
      "episodeCount": 7,
      "airDate": "2008-01-20"
    }
  ],
  "fetchedAt": "2024-10-29T12:00:00Z"
}
```

## Auto-Delete and Keep-Watching System

### Auto-Delete Workflow

1. **Content Download Complete**
   - Schedule auto-delete for 30 days from completion
   - Store schedule in `auto_delete_schedule` table

2. **7 Days Before Deletion**
   - Send push notification to all profiles that have watched the content
   - Notification: "Hey! 'Avatar' will be deleted in 7 days. Want to keep watching?"
   - Include thumbs up/down buttons

3. **User Response**
   - Thumbs up: Cancel auto-delete, mark content as "keep"
   - Thumbs down or no response: Proceed with deletion
   - Update `auto_delete_schedule` table with response

4. **Deletion Day**
   - If no "keep" response received, delete media files and metadata
   - Remove from library database
   - Log deletion event

### Push Notification Implementation

**Technology Stack**
- Android/Android TV: Firebase Cloud Messaging (FCM)
- Web: Web Push API with service workers
- Fallback: In-app notifications

**Notification Payload**
```json
{
  "title": "Keep watching Avatar?",
  "body": "This movie will be deleted in 7 days. Tap to keep it.",
  "data": {
    "type": "keep_watching",
    "contentId": 123,
    "contentTitle": "Avatar",
    "contentType": "movie",
    "scheduledDeleteAt": "2024-11-05T00:00:00Z"
  },
  "actions": [
    {
      "action": "keep",
      "title": "👍 Keep",
      "icon": "thumbs_up"
    },
    {
      "action": "delete",
      "title": "👎 Delete",
      "icon": "thumbs_down"
    }
  ]
}
```

## Performance Optimization

### 1. Server-Side Caching Strategy

**Multi-Layer Cache System**

```
Request → Memory Cache → Redis Cache → Database → External API
           (instant)      (< 5ms)       (< 50ms)    (200-2000ms)
```

**TMDB API Caching**
- Cache search results for 6 hours (trending content changes slowly)
- Cache content details for 7 days (metadata rarely changes)
- Cache images indefinitely (images never change)
- Implement cache warming for popular content
- Rate limit: Max 40 requests per 10 seconds to TMDB

**Sonarr/Radarr API Caching**
- Cache series/movie lists for 5 minutes
- Cache queue status for 30 seconds
- Don't cache add/delete operations
- Rate limit: Max 10 requests per second per service

**Prowlarr API Caching**
- Cache search results for 1 hour (indexer results are relatively stable)
- Cache indexer list for 24 hours
- Rate limit: Max 5 searches per minute per user

**Cache Implementation**
```javascript
class CacheManager {
  constructor() {
    this.memoryCache = new Map(); // In-memory for hot data
    this.redisClient = redis.createClient(); // Distributed cache
  }

  async get(key, fetchFn, ttl) {
    // Check memory cache first
    if (this.memoryCache.has(key)) {
      const cached = this.memoryCache.get(key);
      if (cached.expiresAt > Date.now()) {
        return cached.data;
      }
    }

    // Check Redis cache
    const redisData = await this.redisClient.get(key);
    if (redisData) {
      const parsed = JSON.parse(redisData);
      this.memoryCache.set(key, parsed); // Promote to memory
      return parsed.data;
    }

    // Fetch from source
    const data = await fetchFn();
    const cacheEntry = {
      data,
      expiresAt: Date.now() + ttl
    };

    // Store in both caches
    this.memoryCache.set(key, cacheEntry);
    await this.redisClient.setex(key, ttl / 1000, JSON.stringify(cacheEntry));

    return data;
  }
}
```

**API Rate Limiting**
```javascript
class RateLimiter {
  constructor(maxRequests, windowMs) {
    this.maxRequests = maxRequests;
    this.windowMs = windowMs;
    this.requests = new Map();
  }

  async checkLimit(key) {
    const now = Date.now();
    const userRequests = this.requests.get(key) || [];
    
    // Remove old requests outside window
    const validRequests = userRequests.filter(
      time => now - time < this.windowMs
    );

    if (validRequests.length >= this.maxRequests) {
      throw new Error('Rate limit exceeded');
    }

    validRequests.push(now);
    this.requests.set(key, validRequests);
  }
}
```

### 2. Database Optimization
- Index frequently queried columns (tmdb_id, type, profile_id)
- Implement database connection pooling (max 10 connections)
- Use prepared statements for all queries
- Regular VACUUM operations for SQLite (weekly)
- Implement read replicas for heavy read operations

### 3. Streaming Optimization
- Implement adaptive bitrate streaming (HLS/DASH)
- Use HTTP range requests for seeking
- Transcode media on-the-fly if needed (using ffmpeg)
- Implement video thumbnail generation (cached)
- Pre-buffer 30 seconds of content

### 4. Frontend Optimization
- Lazy load images with intersection observer
- Implement virtual scrolling for large lists
- Bundle and minify JavaScript
- Use service workers for offline support
- Cache API responses in IndexedDB (client-side)

### 5. Metadata Loading Priority
1. Load from database (fastest, < 50ms)
2. Load from media folder JSON if database missing
3. Fetch from TMDB only if:
   - Content not in database
   - Metadata is stale (> 7 days old)
   - User explicitly requests refresh

### 6. Background Jobs
- Metadata refresh: Run daily at 3 AM for content older than 7 days
- Download queue polling: Every 60 seconds
- Auto-delete check: Run daily at 2 AM
- Cache cleanup: Remove expired entries every hour
- Library scan: Run on startup and every 6 hours
