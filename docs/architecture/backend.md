# Backend Architecture

Deep dive into Lanflix backend structure and services.

## Technology Stack

- **Runtime**: Node.js 18+
- **Framework**: Express.js
- **Language**: TypeScript
- **Database**: SQLite with Sequelize ORM
- **Caching**: Redis (optional)
- **Real-time**: Socket.IO
- **Media Processing**: FFmpeg via fluent-ffmpeg
- **Scheduling**: node-cron
- **Logging**: Winston

## Project Structure

```
backend/src/
├── app.ts                      # Application entry point
├── clients/                    # External API clients
│   ├── sonarr.client.ts
│   ├── radarr.client.ts
│   ├── prowlarr.client.ts
│   └── tmdb.client.ts
├── config/                     # Configuration
│   ├── database.js
│   └── env.ts
├── jobs/                       # Scheduled tasks
│   ├── scheduler.ts
│   └── index.ts
├── middleware/                 # Express middleware
│   ├── error-handler.ts
│   ├── validation.ts
│   └── api-status.middleware.ts
├── migrations/                 # Database migrations
├── models/                     # Sequelize models
│   ├── Profile.ts
│   ├── Content.ts
│   ├── SeriesEpisode.ts
│   ├── WatchHistory.ts
│   ├── Watchlist.ts
│   ├── DownloadQueue.ts
│   ├── Settings.ts
│   ├── AutoDeleteSchedule.ts
│   └── DeviceToken.ts
├── routes/                     # API routes
│   ├── content.routes.ts
│   ├── library.routes.ts
│   ├── profile.routes.ts
│   ├── streaming.routes.ts
│   ├── settings.routes.ts
│   ├── notification.routes.ts
│   ├── jobs.routes.ts
│   ├── webhook.routes.ts
│   ├── transcode.routes.ts
│   └── index.ts
├── services/                   # Business logic
│   ├── content.service.ts
│   ├── library.service.ts
│   ├── metadata.service.ts
│   ├── media-converter.service.ts
│   ├── offline-transcoder.service.ts
│   ├── download-manager.service.ts
│   └── notification.service.ts
└── utils/                      # Utilities
    ├── logger.ts
    ├── cache-manager.ts
    ├── database.ts
    ├── ffmpeg.ts
    ├── rate-limiter.ts
    └── api-status.ts
```

## Core Components

### Application Entry (app.ts)

```typescript
// Initialize Express app
const app = express();

// Middleware stack
app.use(cors());
app.use(express.json());
app.use(rateLimiter);
app.use(apiStatusMiddleware);

// Mount routes
app.use('/api', routes);

// Error handling
app.use(errorHandler);

// Start server
app.listen(PORT);
```

### Database Layer

#### Models

**Profile Model**
- User profiles with preferences
- Watch history tracking
- Watchlist management

**Content Model**
- Movies and TV series metadata
- File paths and media info
- TMDB integration

**SeriesEpisode Model**
- Episode-level data
- Season organization
- Episode file tracking

**WatchHistory Model**
- Playback progress
- Resume points
- Watch timestamps

**DownloadQueue Model**
- Download requests
- Queue management
- Status tracking

**Settings Model**
- Key-value configuration
- Application settings
- User preferences

#### Relationships

```typescript
// Profile relationships
Profile.hasMany(WatchHistory);
Profile.hasMany(Watchlist);

// Content relationships
Content.hasMany(SeriesEpisode);
Content.hasMany(WatchHistory);
Content.hasMany(Watchlist);

// Episode relationships
SeriesEpisode.belongsTo(Content);
SeriesEpisode.hasMany(WatchHistory);
```

### Service Layer

#### Content Service
```typescript
class ContentService {
  // Discover content from external sources
  async discoverContent(type, filters)
  
  // Search across all sources
  async searchContent(query, type)
  
  // Get content details
  async getContentDetails(id)
  
  // Sync with external services
  async syncWithSonarr()
  async syncWithRadarr()
}
```

#### Library Service
```typescript
class LibraryService {
  // Scan media directories
  async scanLibrary()
  
  // Match files to metadata
  async matchFiles()
  
  // Update content metadata
  async refreshMetadata(contentId)
  
  // Clean orphaned entries
  async cleanOrphaned()
}
```

#### Media Converter Service
```typescript
class MediaConverterService {
  // Progressive transcoding
  async startProgressiveTranscode(filePath, options)
  
  // Get media info
  async getMediaInfo(filePath)
  
  // Check codec compatibility
  async needsTranscoding(filePath)
  
  // Generate HLS playlist
  async generateHLS(filePath)
}
```

#### Offline Transcoder Service
```typescript
class OfflineTranscoderService {
  // Pre-transcode files
  async transcodeFile(contentId, options)
  
  // Queue management
  async addToQueue(contentId)
  async getQueueStatus()
  
  // Cleanup
  async cleanupTranscoded()
}
```

#### Metadata Service
```typescript
class MetadataService {
  // Fetch from TMDB
  async fetchMovieMetadata(tmdbId)
  async fetchSeriesMetadata(tmdbId)
  
  // Cache management
  async cacheMetadata(data)
  async getCachedMetadata(id)
  
  // Image handling
  async downloadPoster(url)
  async downloadBackdrop(url)
}
```

#### Download Manager Service
```typescript
class DownloadManagerService {
  // Queue operations
  async addToQueue(contentId, profileId)
  async removeFromQueue(queueId)
  
  // Download execution
  async processQueue()
  async downloadContent(queueItem)
  
  // Status tracking
  async getQueueStatus()
  async updateProgress(queueId, progress)
}
```

#### Notification Service
```typescript
class NotificationService {
  // Send notifications
  async sendNotification(profileId, message)
  
  // Push notifications
  async sendPushNotification(deviceToken, data)
  
  // Event handlers
  async onDownloadComplete(contentId)
  async onNewContent(contentId)
}
```

### External API Clients

#### Sonarr Client
```typescript
class SonarrClient {
  async getSeries()
  async getEpisodes(seriesId)
  async searchSeries(query)
  async addSeries(tvdbId, options)
  async getQueue()
}
```

#### Radarr Client
```typescript
class RadarrClient {
  async getMovies()
  async searchMovies(query)
  async addMovie(tmdbId, options)
  async getQueue()
}
```

#### Prowlarr Client
```typescript
class ProwlarrClient {
  async search(query, categories)
  async getIndexers()
  async testIndexer(indexerId)
}
```

#### TMDB Client
```typescript
class TMDBClient {
  async searchMovie(query)
  async searchTV(query)
  async getMovieDetails(id)
  async getTVDetails(id)
  async getSeasonDetails(id, season)
}
```

### Middleware

#### Error Handler
```typescript
function errorHandler(err, req, res, next) {
  logger.error(err);
  res.status(err.status || 500).json({
    error: err.message,
    stack: NODE_ENV === 'development' ? err.stack : undefined
  });
}
```

#### API Status Middleware
```typescript
function apiStatusMiddleware(req, res, next) {
  // Check external service availability
  // Add status to response headers
  next();
}
```

#### Validation Middleware
```typescript
function validateRequest(schema) {
  return (req, res, next) => {
    const { error } = schema.validate(req.body);
    if (error) {
      return res.status(400).json({ error: error.message });
    }
    next();
  };
}
```

### Utilities

#### Cache Manager
```typescript
class CacheManager {
  async get(key)
  async set(key, value, ttl)
  async del(key)
  async flush()
}
```

#### Logger
```typescript
const logger = winston.createLogger({
  level: LOG_LEVEL,
  format: winston.format.json(),
  transports: [
    new winston.transports.File({ filename: 'error.log', level: 'error' }),
    new winston.transports.File({ filename: 'combined.log' })
  ]
});
```

#### FFmpeg Utilities
```typescript
async function getVideoInfo(filePath)
async function extractSubtitles(filePath)
async function generateThumbnail(filePath, timestamp)
async function detectHDR(filePath)
```

### Scheduled Jobs

```typescript
// Library scan - every 6 hours
cron.schedule('0 */6 * * *', async () => {
  await libraryService.scanLibrary();
});

// Metadata refresh - daily at 2 AM
cron.schedule('0 2 * * *', async () => {
  await metadataService.refreshAll();
});

// Cleanup temp files - daily at 3 AM
cron.schedule('0 3 * * *', async () => {
  await cleanupTempFiles();
});

// Auto-delete old content - daily at 4 AM
cron.schedule('0 4 * * *', async () => {
  await processAutoDelete();
});
```

## API Routes

### Content Routes (`/api/content`)
- `GET /discover` - Discover content
- `GET /search` - Search content
- `GET /:id` - Get content details
- `GET /:id/episodes` - Get series episodes

### Library Routes (`/api/library`)
- `GET /` - Get library content
- `POST /scan` - Trigger library scan
- `POST /refresh/:id` - Refresh metadata
- `DELETE /:id` - Remove from library

### Profile Routes (`/api/profiles`)
- `GET /` - List profiles
- `POST /` - Create profile
- `PUT /:id` - Update profile
- `DELETE /:id` - Delete profile

### Streaming Routes (`/api/stream`)
- `GET /:id` - Stream content
- `GET /:id/info` - Get stream info
- `POST /:id/progress` - Update progress

### Transcode Routes (`/api/transcode`)
- `POST /start` - Start transcoding
- `GET /status/:id` - Get transcode status
- `DELETE /cancel/:id` - Cancel transcoding

### Settings Routes (`/api/settings`)
- `GET /` - Get all settings
- `GET /:key` - Get setting
- `PUT /:key` - Update setting

### Webhook Routes (`/api/webhook`)
- `POST /sonarr` - Sonarr webhook
- `POST /radarr` - Radarr webhook

## Data Flow

### Content Discovery Flow
```
User Request → Content Routes → Content Service
    ↓
External API Clients (Sonarr/Radarr/TMDB)
    ↓
Metadata Service → Cache → Database
    ↓
Response to User
```

### Streaming Flow
```
User Request → Streaming Routes
    ↓
Check if transcoding needed
    ↓
Media Converter Service → FFmpeg
    ↓
Progressive stream chunks → User
```

### Library Scan Flow
```
Scheduled Job → Library Service
    ↓
Scan media directories
    ↓
Match files to metadata (TMDB)
    ↓
Update database
    ↓
Notify users of new content
```

## Performance Optimizations

### Caching Strategy
- **Redis**: API responses, metadata
- **Memory**: Frequently accessed data
- **File**: Poster/backdrop images

### Database Optimization
- Indexes on frequently queried fields
- WAL mode for better concurrency
- Connection pooling

### Streaming Optimization
- Chunked transfer encoding
- Range request support
- Progressive transcoding

## Security

### Rate Limiting
```typescript
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 100
});
```

### Input Validation
- Request body validation
- File path sanitization
- SQL injection prevention (Sequelize)

### CORS Configuration
```typescript
app.use(cors({
  origin: ALLOWED_ORIGINS,
  credentials: true
}));
```

## Monitoring

### Health Checks
```typescript
app.get('/api/health', (req, res) => {
  res.json({
    status: 'ok',
    uptime: process.uptime(),
    timestamp: new Date()
  });
});
```

### Logging
- Request/response logging
- Error tracking
- Performance metrics

## Next Steps

- [Frontend Architecture](./frontend.md)
- [Database Schema](./database.md)
- [API Documentation](../api/overview.md)

**Last Updated**: October 31, 2025
