# System Architecture Overview

Comprehensive overview of Lanflix's system architecture, design patterns, and technical decisions.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Pages   │  │ Modules  │  │  Styles  │  │  Assets  │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
│         │              │              │              │       │
│         └──────────────┴──────────────┴──────────────┘       │
│                         │                                     │
│                    HTTP/WebSocket                            │
│                         │                                     │
└─────────────────────────┼─────────────────────────────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────────┐
│                    Backend API                                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Routes  │→ │ Services │→ │  Models  │→ │ Database │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
│         │              │                                      │
│         │              ↓                                      │
│         │      ┌──────────────┐                             │
│         │      │   Clients    │                             │
│         │      └──────────────┘                             │
│         │              │                                      │
└─────────┼──────────────┼──────────────────────────────────────┘
          │              │
          │              ↓
          │      ┌──────────────────────────────────┐
          │      │    External Services             │
          │      │  ┌────────┐  ┌────────┐         │
          │      │  │  TMDB  │  │ Sonarr │         │
          │      │  └────────┘  └────────┘         │
          │      │  ┌────────┐  ┌────────┐         │
          │      │  │ Radarr │  │Prowlarr│         │
          │      │  └────────┘  └────────┘         │
          │      └──────────────────────────────────┘
          │
          ↓
    ┌──────────┐
    │  Media   │
    │  Files   │
    └──────────┘
```

## Architecture Layers

### 1. Presentation Layer (Frontend)

**Technology**: Vanilla JavaScript + Vite  
**Purpose**: User interface and interaction

**Components**:
- **Pages**: HTML entry points for different views
- **Modules**: Reusable JavaScript components
- **Styles**: CSS with custom properties
- **State Management**: Centralized application state

**Key Modules**:
- `video-player.js` - Video playback and controls
- `content-modal.js` - Content details modal
- `content-display.js` - Content grid/list rendering
- `navigation.js` - Menu and navigation
- `api-client.js` - Backend API communication
- `data.js` - State management

### 2. API Layer (Backend Routes)

**Technology**: Express.js  
**Purpose**: HTTP request handling and routing

**Route Groups**:
- `/api/content` - Content discovery and search
- `/api/library` - Media library management
- `/api/stream` - Video streaming
- `/api/profiles` - User profile management
- `/api/settings` - Application settings
- `/api/jobs` - Background job management

**Responsibilities**:
- Request validation
- Authentication/authorization
- Response formatting
- Error handling

### 3. Business Logic Layer (Services)

**Technology**: TypeScript classes  
**Purpose**: Core business logic and orchestration

**Services**:
- `content.service.ts` - Content discovery logic
- `library.service.ts` - Library management
- `metadata.service.ts` - Metadata fetching and caching
- `download-manager.service.ts` - Download queue management
- `notification.service.ts` - User notifications

**Responsibilities**:
- Business rule enforcement
- Data transformation
- External API orchestration
- Caching logic
- Error handling and retry logic

### 4. Data Access Layer (Models)

**Technology**: Sequelize ORM  
**Purpose**: Database abstraction and data modeling

**Models**:
- `Content` - Movies and TV shows
- `SeriesEpisode` - TV show episodes
- `Profile` - User profiles
- `WatchHistory` - Viewing progress
- `Watchlist` - User watchlist
- `DownloadQueue` - Download queue
- `Settings` - Application settings
- `AutoDeleteSchedule` - Auto-delete rules

**Responsibilities**:
- Data validation
- Relationships
- Queries
- Migrations

### 5. Integration Layer (Clients)

**Technology**: Axios HTTP client  
**Purpose**: External service integration

**Clients**:
- `tmdb.client.ts` - TMDB API integration
- `sonarr.client.ts` - Sonarr API integration
- `radarr.client.ts` - Radarr API integration
- `prowlarr.client.ts` - Prowlarr API integration

**Responsibilities**:
- API authentication
- Request/response transformation
- Error handling
- Rate limiting
- Retry logic

### 6. Infrastructure Layer

**Components**:
- **Database**: SQLite with WAL mode
- **Cache**: Redis + in-memory
- **Logging**: Winston
- **Jobs**: node-cron
- **File System**: Direct file access for streaming

## Design Patterns

### 1. Service-Oriented Architecture (SOA)

Each service is responsible for a specific domain:
```typescript
class ContentService {
  async discover() { /* ... */ }
  async search() { /* ... */ }
  async getDetails() { /* ... */ }
}
```

**Benefits**:
- Clear separation of concerns
- Easy to test
- Reusable across routes
- Maintainable

### 2. Repository Pattern

Models act as repositories for data access:
```typescript
const content = await Content.findByPk(id);
await content.update({ title: 'New Title' });
```

**Benefits**:
- Database abstraction
- Consistent data access
- Easy to mock for testing

### 3. Client Pattern

External services accessed through dedicated clients:
```typescript
const tmdbClient = new TMDBClient(apiKey);
const movie = await tmdbClient.getMovie(id);
```

**Benefits**:
- Centralized API logic
- Easy to swap implementations
- Consistent error handling

### 4. Middleware Pattern

Cross-cutting concerns handled by middleware:
```typescript
app.use(cors());
app.use(express.json());
app.use(injectApiStatus);
app.use(errorHandler);
```

**Benefits**:
- Reusable logic
- Clean route handlers
- Easy to add/remove features

### 5. Module Pattern (Frontend)

Frontend organized into self-contained modules:
```javascript
export class VideoPlayer {
  constructor() { /* ... */ }
  initialize() { /* ... */ }
  play() { /* ... */ }
}
```

**Benefits**:
- Encapsulation
- Reusability
- Clear dependencies

## Data Flow

### Content Discovery Flow

```
User → Frontend → API → Service → Client → TMDB
                                      ↓
                                   Cache
                                      ↓
                                  Database
                                      ↓
                                  Response
```

1. User searches for content
2. Frontend calls `/api/content/search`
3. Route validates request
4. Service checks cache
5. If not cached, client calls TMDB
6. Response cached and stored
7. Data returned to frontend
8. Frontend renders results

### Video Streaming Flow

```
User → Frontend → API → File System → Stream
                   ↓
              Watch History
```

1. User clicks play
2. Frontend requests `/api/stream/:id`
3. Route validates content exists
4. File path retrieved from database
5. File streamed with range support
6. Progress tracked in watch history

### Download Queue Flow

```
User → Frontend → API → Service → Client → Sonarr/Radarr
                           ↓
                      Download Queue
                           ↓
                      Background Job
                           ↓
                        Library
```

1. User queues content
2. Frontend calls `/api/content/:id/queue`
3. Service adds to download queue
4. Client sends request to Sonarr/Radarr
5. Background job monitors progress
6. On completion, library updated
7. User notified

## Caching Strategy

### Multi-Layer Cache

```
Request → Memory Cache → Redis Cache → Database → External API
            (Fast)         (Shared)     (Persistent)  (Slow)
```

### Cache Layers

**1. Memory Cache**
- **TTL**: 5-15 minutes
- **Use**: Frequently accessed data
- **Examples**: Trending content, popular searches

**2. Redis Cache**
- **TTL**: 1-24 hours
- **Use**: Shared across instances
- **Examples**: Metadata, API responses

**3. Database Cache**
- **TTL**: Permanent until updated
- **Use**: Persistent storage
- **Examples**: Content details, watch history

**4. File System Cache**
- **TTL**: Permanent
- **Use**: Images and media
- **Examples**: Posters, backdrops

### Cache Invalidation

**Time-Based**:
- Trending content: 15 minutes
- Search results: 1 hour
- Metadata: 24 hours

**Event-Based**:
- Library scan: Clear library cache
- Content update: Clear content cache
- Profile change: Clear profile cache

## Error Handling

### Error Flow

```
Error → Service → Route → Middleware → Response
          ↓
        Logger
```

### Error Types

**1. Validation Errors** (400)
```typescript
if (!req.body.title) {
  throw new ValidationError('Title is required');
}
```

**2. Not Found Errors** (404)
```typescript
if (!content) {
  throw new NotFoundError('Content not found');
}
```

**3. External API Errors** (502/503)
```typescript
try {
  await tmdbClient.getMovie(id);
} catch (error) {
  throw new ExternalServiceError('TMDB unavailable');
}
```

**4. Internal Errors** (500)
```typescript
try {
  await processData();
} catch (error) {
  logger.error('Unexpected error', error);
  throw new InternalError('Something went wrong');
}
```

### Error Response Format

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

## Security Considerations

### Input Validation
- All user input validated
- SQL injection prevention (ORM)
- XSS prevention (sanitization)
- Path traversal prevention

### Authentication
- Profile-based access control
- API key validation for external services
- Session management (planned)

### File Access
- Whitelist allowed file paths
- Validate file existence
- Check file permissions
- Prevent directory traversal

### Rate Limiting
- Per-IP rate limiting
- Per-user rate limiting (planned)
- External API rate limiting

## Performance Optimizations

### Backend
- Database connection pooling
- Query optimization with indexes
- Response compression
- Efficient caching
- Lazy loading

### Frontend
- Code splitting
- Lazy image loading
- Virtual scrolling (planned)
- Debounced search
- Optimistic UI updates

### Streaming
- Range request support
- Efficient file streaming
- Proper buffer sizes
- Connection keep-alive

## Scalability Considerations

### Current Architecture
- Single server deployment
- SQLite database
- Local file storage
- In-memory + Redis cache

### Future Scalability

**Horizontal Scaling**:
- Load balancer
- Multiple backend instances
- Shared Redis cache
- Distributed file storage

**Database Scaling**:
- PostgreSQL migration
- Read replicas
- Connection pooling
- Query optimization

**Storage Scaling**:
- NAS/SAN integration
- Cloud storage (S3)
- CDN for images
- Distributed file system

## Monitoring & Observability

### Logging
- Winston logger
- Log levels: error, warn, info, debug
- Log rotation
- Structured logging

### Metrics (Planned)
- Request count
- Response time
- Error rate
- Cache hit rate
- Active users

### Health Checks
- `/health` endpoint
- Database connectivity
- External service status
- Disk space monitoring

## Deployment Architecture

### Development
```
Frontend (Vite Dev Server) → Backend (ts-node-dev)
                                ↓
                            SQLite
```

### Production
```
Frontend (Static Files) → Nginx → Backend (Node.js)
                                      ↓
                                  SQLite/PostgreSQL
                                      ↓
                                    Redis
```

## Technology Decisions

### Why SQLite?
- ✅ Zero configuration
- ✅ Embedded database
- ✅ Perfect for single-server
- ✅ ACID compliant
- ⚠️ Limited concurrency
- ⚠️ Not for distributed systems

### Why Vanilla JavaScript?
- ✅ No framework overhead
- ✅ Fast load times
- ✅ Easy to understand
- ✅ Full control
- ⚠️ More boilerplate
- ⚠️ Manual state management

### Why TypeScript (Backend)?
- ✅ Type safety
- ✅ Better IDE support
- ✅ Catch errors early
- ✅ Self-documenting
- ⚠️ Build step required

### Why Express.js?
- ✅ Mature and stable
- ✅ Large ecosystem
- ✅ Flexible
- ✅ Well-documented
- ⚠️ Callback-heavy (mitigated with async/await)

## Next Steps

- [Backend Architecture](./backend.md) - Detailed backend structure
- [Frontend Architecture](./frontend.md) - Detailed frontend structure
- [Database Schema](./database.md) - Database design
- [Caching Strategy](./caching.md) - Caching implementation

**Last Updated**: October 30, 2025
