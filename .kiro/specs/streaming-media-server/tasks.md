# Implementation Plan

- [x] 1. Set up project structure and core infrastructure



  - Create backend and frontend directory structure with proper separation
  - Initialize Node.js backend with Express.js, TypeScript configuration, and essential dependencies
  - Set up SQLite database with Sequelize ORM and create migration system
  - Configure environment variables and create .env.example template
  - Set up logging system with Winston for structured logging
  - _Requirements: 14.1, 14.2, 14.6, 14.7_

- [x] 2. Implement database models and migrations





  - Create Sequelize models for Profiles, Content, Series_Episodes, Watch_History, Watchlist tables
  - Create Sequelize models for Download_Queue, Settings, Auto_Delete_Schedule, Device_Tokens tables
  - Write database migration scripts for all tables with proper indexes
  - Implement database seeding for initial settings and test data
  - _Requirements: 1.3, 8.1, 8.4, 8.5_
-

- [x] 3. Build external service API clients




- [x] 3.1 Implement TMDB API client


  - Create TMDBClient class with methods for search, movie details, TV details, trending, and popular content
  - Implement error handling and retry logic with exponential backoff
  - Add request rate limiting (40 requests per 10 seconds)
  - _Requirements: 12.1, 13.5_

- [x] 3.2 Implement Sonarr API client


  - Create SonarrClient class with methods for search, add series, get series, get queue, delete series
  - Implement authentication with API key
  - Add connection testing and health check methods
  - _Requirements: 2.1, 2.5, 2.6_

- [x] 3.3 Implement Radarr API client


  - Create RadarrClient class with methods for search, add movie, get movies, get queue, delete movie
  - Implement authentication with API key
  - Add connection testing and health check methods
  - _Requirements: 2.2, 2.5, 2.6_

- [x] 3.4 Implement Prowlarr API client


  - Create ProwlarrClient class with methods for search and get indexers
  - Implement authentication with API key
  - Add search result parsing and normalization
  - _Requirements: 2.3, 2.4_

- [x] 4. Implement caching system





  - Create CacheManager class with memory and Redis cache layers
  - Implement cache key generation and TTL management
  - Add cache warming for popular content
  - Implement cache invalidation strategies
  - Create RateLimiter class for API rate limiting
  - _Requirements: 13.2, 13.4_

- [x] 5. Build core backend services




- [x] 5.1 Implement MetadataService


  - Create methods to fetch movie and series metadata from TMDB
  - Implement poster and backdrop image downloading
  - Add metadata saving to media folder as JSON files
  - Implement metadata loading from media folder JSON files
  - Add metadata refresh logic with staleness check (7 days)
  - _Requirements: 12.1, 12.2, 12.3, 12.5_

- [x] 5.2 Implement ContentService


  - Create methods for content search using Prowlarr and TMDB
  - Implement trending and popular content fetching with caching
  - Add content details retrieval with metadata enrichment
  - Implement content type detection (movie vs series)
  - _Requirements: 3.2, 3.3, 3.4, 3.6_


- [x] 5.3 Implement LibraryService

  - Create methods to get library items with filtering by type
  - Implement library scanning to detect new media files
  - Add methods to add and remove content from library
  - Implement recently added content retrieval
  - Add watch progress integration for library items
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 5.4 Implement DownloadManager


  - Create methods to queue downloads via Sonarr/Radarr
  - Implement download status tracking and progress polling
  - Add download completion handler to update library
  - Implement auto-delete scheduling (30 days after completion)
  - Add methods to cancel downloads
  - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

- [x] 5.5 Implement NotificationService


  - Create push notification sending using Firebase Cloud Messaging
  - Implement keep-watching notification generation (7 days before deletion)
  - Add device token registration and management
  - Implement notification response handling (keep/delete)
  - Add Web Push API support for browser notifications
  - _Requirements: 4.6_

- [ ] 6. Build REST API routes




- [x] 6.1 Implement Content routes


  - Create GET /api/content/discover endpoint with trending content
  - Create GET /api/content/search endpoint with query parameter
  - Create GET /api/content/:id endpoint for content details
  - Create POST /api/content/:id/queue endpoint to add to download queue
  - Add request validation middleware
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.2_

- [x] 6.2 Implement Library routes


  - Create GET /api/library/movies endpoint with filtering
  - Create GET /api/library/series endpoint with filtering
  - Create GET /api/library/recent endpoint for recently added
  - Create GET /api/library/:id endpoint for library item details
  - Create DELETE /api/library/:id endpoint to remove from library
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 6.3 Implement Profile routes


  - Create GET /api/profiles endpoint to list all profiles
  - Create POST /api/profiles endpoint to create new profile
  - Create GET /api/profiles/:id endpoint for profile details
  - Create PUT /api/profiles/:id endpoint to update profile
  - Create DELETE /api/profiles/:id endpoint to delete profile
  - Create GET /api/profiles/:id/watchlist endpoint for My List
  - Create POST /api/profiles/:id/watchlist/:contentId endpoint to add to My List
  - Create DELETE /api/profiles/:id/watchlist/:contentId endpoint to remove from My List
  - _Requirements: 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.6_

- [x] 6.4 Implement Streaming routes


  - Create GET /api/stream/:id endpoint with HTTP range request support
  - Create POST /api/stream/:id/progress endpoint to update watch progress
  - Create GET /api/stream/:id/subtitles endpoint to list available subtitles
  - Implement adaptive bitrate streaming support
  - Add video transcoding on-the-fly if needed
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_


- [x] 6.5 Implement Settings routes

  - Create GET /api/settings endpoint to retrieve all settings
  - Create PUT /api/settings endpoint to update settings
  - Create GET /api/settings/services endpoint for external service status
  - Add settings validation
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

- [x] 6.6 Implement Notification routes


  - Create POST /api/notifications/register endpoint for device token registration
  - Create POST /api/notifications/:id/respond endpoint for keep-watching responses
  - Create GET /api/notifications/:profileId endpoint for notification history
  - _Requirements: 4.6_

- [x] 7. Implement background jobs and scheduled tasks




  - Create job scheduler using node-cron
  - Implement download queue polling job (every 60 seconds)
  - Implement auto-delete check job (daily at 2 AM)
  - Implement metadata refresh job (daily at 3 AM for stale content)
  - Implement library scan job (on startup and every 6 hours)
  - Implement cache cleanup job (every hour)
  - Add job monitoring and error handling
  - _Requirements: 2.6, 2.7, 12.6_

- [ ] 8. Refactor frontend JavaScript into modules

- [ ] 8.1 Create API client module
  - Extract all backend communication into api-client.js module
  - Implement typed methods for all API endpoints
  - Add request/response interceptors for error handling
  - Implement authentication token management
  - Add request retry logic for failed requests
  - Make all the frontend work. if you reload it doesnt refresh everything, rather it will save stuff and you open the same page, use the your next watch as a template for all the carousels you can scroll through.
  - _Requirements: 14.4, 14.5_

- [ ] 8.2 Create navigation module
  - Extract navigation logic from script.js into navigation.js
  - Implement menu navigation and routing
  - Add keyboard and remote control input handling
  - Implement page transition animations
  - _Requirements: 14.4_

- [ ] 8.3 Create content display module
  - Extract content rendering logic into content-display.js
  - Implement carousel rendering and management
  - Add card expansion/collapse functionality
  - Implement hero carousel with ambilight effects
  - Add lazy loading for images
  - _Requirements: 14.4_

- [ ] 8.4 Create profile manager module
  - Extract profile logic into profile-manager.js
  - Implement profile selection UI
  - Add profile CRUD operations
  - Implement active profile state management
  - Add profile data synchronization with backend
  - _Requirements: 14.4_

- [ ] 8.5 Create video player module
  - Create video-player.js module with Video.js or Plyr integration
  - Implement playback controls (play, pause, seek, volume, fullscreen)
  - Add watch progress tracking (update every 10 seconds)
  - Implement subtitle selection and display
  - Add resume playback from saved position
  - _Requirements: 11.4, 11.5, 11.6, 11.7_

- [ ] 8.6 Create settings manager module
  - Extract settings logic into settings-manager.js
  - Implement settings form handling and validation
  - Add settings synchronization with backend
  - Implement profile management UI
  - _Requirements: 14.4_

- [ ] 9. Integrate frontend with backend APIs
  - Update Home page to fetch library content from backend
  - Update Discover page to fetch trending content from backend
  - Update Series page to fetch TV shows from backend
  - Update Movies page to fetch movies from backend
  - Update My List page to fetch watchlist from backend
  - Implement "Watch in a bit" button functionality
  - Add download progress indicators
  - Implement real-time updates using WebSocket
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.6, 5.1, 5.2, 5.5, 6.1, 6.2, 7.1, 7.2, 7.3_

- [ ] 10. Implement push notification handling in frontend
  - Integrate Firebase Cloud Messaging SDK for Android/Android TV
  - Implement Web Push API for browser notifications
  - Add device token registration on app startup
  - Implement notification click handlers for keep-watching prompts
  - Add in-app notification display as fallback
  - _Requirements: 4.6_

- [ ] 11. Package frontend for multiple platforms
- [ ] 11.1 Set up Electron for PC deployment
  - Initialize Electron project with main and renderer processes
  - Configure Electron builder for Windows, Mac, and Linux
  - Implement native menu and window management
  - Add auto-update functionality
  - _Requirements: 10.1, 10.4, 10.5_

- [ ] 11.2 Set up Capacitor for Android deployment
  - Initialize Capacitor project for Android
  - Configure Android manifest for phone and TV support
  - Implement touch navigation for phones
  - Add Firebase Cloud Messaging plugin
  - Build APK for Android phones
  - _Requirements: 10.2, 10.3, 10.4, 10.5_

- [ ] 11.3 Set up Capacitor for Android TV deployment
  - Configure Android TV manifest and leanback launcher
  - Implement D-pad navigation for remote control
  - Add TV-specific UI optimizations
  - Build APK for Android TV
  - _Requirements: 10.2, 10.4, 10.5_

- [ ] 12. Implement error handling and logging
  - Add global error handler middleware in Express
  - Implement structured error responses with error codes
  - Add comprehensive logging for all operations
  - Implement error tracking and monitoring
  - Add user-friendly error messages in frontend
  - _Requirements: 13.3_

- [ ] 13. Add configuration and setup wizard
  - Create initial setup wizard for first-time configuration
  - Add UI for configuring Sonarr, Radarr, Prowlarr connections
  - Implement connection testing for external services
  - Add media folder path configuration
  - Create configuration validation
  - _Requirements: 2.1, 2.2, 2.3, 14.7_

- [ ] 14. Implement security measures
  - Add API rate limiting middleware
  - Implement input validation and sanitization
  - Add file path validation to prevent directory traversal
  - Implement CORS configuration
  - Add HTTPS support for production
  - Implement API key rotation support
  - _Requirements: 13.1, 13.2_

- [ ] 15. Create documentation and deployment guides
  - Write README with installation instructions
  - Create API documentation
  - Write deployment guide for backend server
  - Create user guide for frontend applications
  - Document configuration options
  - Add troubleshooting guide
  - _Requirements: 1.1, 1.2_
