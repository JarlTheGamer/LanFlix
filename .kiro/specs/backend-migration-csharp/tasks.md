# Implementation Plan

- [x] 1. Set up C# project structure with Clean Architecture






  - Create solution with Domain, Application, Infrastructure, and WebApi projects
  - Configure project references and dependencies
  - Set up NuGet packages (EF Core, MediatR, FluentValidation, FFmpeg.NET, etc.)
  - _Requirements: All requirements depend on proper project structure_

- [x] 2. Implement Domain layer entities and value objects





  - [x] 2.1 Create base entity classes and common interfaces


    - Write BaseEntity abstract class with Id, CreatedAt, UpdatedAt properties
    - Create IAuditableEntity and ISoftDelete interfaces
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 7.1, 7.2_
  
  - [x] 2.2 Implement Content entity and related types


    - Write Content entity with all properties (TmdbId, Title, FilePath, etc.)
    - Create ContentType enum (Movie, Series)
    - Implement Episode entity for series content
    - _Requirements: 1.1, 1.2, 4.1, 4.2_
  
  - [x] 2.3 Create MediaInfo value object hierarchy


    - Write MediaInfo record with Video, Audio, Subtitles properties
    - Implement VideoStream record (codec, resolution, bitrate, HDR)
    - Implement AudioStream record (codec, channels, language)
    - Implement SubtitleStream record (format, language)
    - _Requirements: 3.1, 3.2, 4.2, 14.7_
  


  - [x] 2.4 Implement Profile and WatchHistory entities




    - Write Profile entity with preferences
    - Create UserPreferences value object
    - Write WatchHistory entity with position tracking
    - Create Watchlist entity


    - _Requirements: 1.3, 1.4_
  
  - [x] 2.5 Create StreamSession entity





    - Write StreamSession entity with session tracking
    - Create StreamingMode enum (DirectPlay, DirectStream, TranscodeVideo, FullTranscode)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 8.1, 8.2_

- [x] 3. Implement Application layer with CQRS pattern





  - [x] 3.1 Set up MediatR and common interfaces


    - Configure MediatR in DependencyInjection
    - Create IApplicationDbContext interface
    - Create ICacheService interface
    - Create common DTOs (ContentDto, ProfileDto, etc.)
    - _Requirements: All requirements use CQRS pattern_
  
  - [x] 3.2 Implement MediatR pipeline behaviors


    - Write LoggingBehavior for request/response logging
    - Write ValidationBehavior using FluentValidation
    - Write PerformanceBehavior for slow query detection
    - Write CachingBehavior for query result caching
    - _Requirements: 5.4, 9.1, 9.2, 14.4_
  
  - [x] 3.3 Create Library feature commands and queries


    - Write GetLibraryItemsQuery with handler (pagination, filtering, search)
    - Write GetContentDetailsQuery with handler
    - Write ScanLibraryCommand with handler
    - Write AddContentCommand with handler and validator
    - Write RemoveContentCommand with handler
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 3.4 Create Streaming feature commands and queries


    - Write StartStreamCommand with handler
    - Write GetStreamInfoQuery with handler
    - Write UpdateProgressCommand with handler
    - Write StopStreamCommand with handler
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6, 8.1, 8.2_
  
  - [x] 3.5 Create Profile feature commands and queries


    - Write GetProfilesQuery with handler
    - Write CreateProfileCommand with handler and validator
    - Write UpdateProfileCommand with handler
    - Write GetWatchHistoryQuery with handler
    - _Requirements: 1.3, 1.4_

- [x] 4. Implement Infrastructure layer - Database





  - [x] 4.1 Set up EF Core DbContext and configurations


    - Create ApplicationDbContext implementing IApplicationDbContext
    - Write ContentConfiguration with indexes and JSON columns
    - Write ProfileConfiguration
    - Write WatchHistoryConfiguration
    - Write StreamSessionConfiguration
    - Configure soft delete query filters
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_
  
  - [x] 4.2 Create database migrations


    - Generate initial migration for all entities
    - Test migration on SQLite
    - Test migration on PostgreSQL
    - _Requirements: 7.6, 7.7_
  
  - [x] 4.3 Implement repository pattern (if needed)


    - Create generic repository for complex queries
    - Implement ContentRepository with Dapper for performance-critical queries
    - _Requirements: 7.3, 14.3_
  
  - [x] 4.4 Create compiled queries for performance


    - Write compiled queries for frequently accessed data
    - Implement query result caching
    - _Requirements: 5.4, 14.3_

- [x] 5. Implement Infrastructure layer - FFmpeg Integration





  - [x] 5.1 Create MediaAnalyzer service


    - Write MediaAnalyzer implementing IMediaAnalyzer
    - Use FFprobe to extract video stream information
    - Extract audio streams with language detection
    - Extract subtitle streams
    - Detect HDR content (HDR10, Dolby Vision)
    - _Requirements: 4.2, 4.4_
  
  - [x] 5.2 Implement HardwareAccelerationDetector


    - Test for NVIDIA NVENC availability
    - Test for Intel QuickSync availability
    - Test for AMD AMF availability
    - Test for VAAPI availability (Linux)
    - Determine preferred acceleration method
    - _Requirements: 3.5, 14.6_
  
  - [x] 5.3 Create TranscodingPipeline service


    - Implement FFmpeg command builder with hardware acceleration
    - Create streaming transcoding with ArrayPool buffers
    - Implement backpressure handling for slow clients
    - Add proper cleanup on cancellation
    - _Requirements: 3.3, 3.4, 3.5, 3.8, 14.1, 14.9_
  
  - [x] 5.4 Implement FFmpeg process pool


    - Create ObjectPool for FFmpeg processes
    - Implement process lifecycle management
    - Add process monitoring and health checks
    - _Requirements: 14.9_

- [ ] 6. Implement streaming strategies
  - [ ] 6.1 Create IStreamingStrategy interface and base classes
    - Define IStreamingStrategy interface
    - Create base strategy class with common functionality
    - Implement ClientCapabilities detection logic
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_
  
  - [ ] 6.2 Implement DirectPlayStrategy
    - Write CanHandle logic for codec compatibility
    - Implement zero-copy file streaming with FileStream
    - Add HTTP range request support for seeking
    - _Requirements: 3.1, 3.7_
  
  - [ ] 6.3 Implement DirectStreamStrategy (Remux)
    - Write CanHandle logic for container incompatibility
    - Implement FFmpeg remux (copy codecs, change container)
    - Stream remuxed output to client
    - _Requirements: 3.2_
  
  - [ ] 6.4 Implement TranscodeVideoStrategy
    - Write CanHandle logic for video codec incompatibility
    - Implement video transcoding with audio copy
    - Use hardware acceleration when available
    - _Requirements: 3.3, 3.5_
  
  - [ ] 6.5 Implement FullTranscodeStrategy
    - Write CanHandle logic (always returns true as fallback)
    - Implement full video and audio transcoding
    - Support HLS/DASH segmented streaming
    - _Requirements: 3.4, 3.5, 14.7_
  
  - [ ] 6.6 Create StreamingStrategySelector
    - Implement strategy selection based on client capabilities
    - Add priority ordering (DirectPlay > DirectStream > Transcode)
    - Consider user preferences in selection
    - _Requirements: 3.6_

- [ ] 7. Implement transcoding session management
  - [ ] 7.1 Create TranscodingSessionManager
    - Implement session creation with unique IDs
    - Track active sessions in memory and database
    - Add session cleanup on client disconnect
    - Implement orphaned session detection on startup
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  
  - [ ] 7.2 Implement session monitoring and cleanup
    - Create background job for session health checks
    - Detect abandoned sessions (no activity for 30 seconds)
    - Terminate FFmpeg processes for dead sessions
    - Clean up temporary transcoding files
    - _Requirements: 8.2, 8.3, 8.4, 8.6_
  
  - [ ] 7.3 Add progress reporting for transcoding
    - Parse FFmpeg output for progress information
    - Broadcast progress via SignalR
    - _Requirements: 8.7, 12.4_

- [ ] 8. Implement caching layer
  - [ ] 8.1 Create ICacheService interface and implementations
    - Define ICacheService interface with Get/Set/Remove/RemoveByTag
    - Implement MemoryCacheService for L1 cache
    - Implement RedisCacheService for L2 cache
    - _Requirements: 5.5, 14.4_
  
  - [ ] 8.2 Implement HybridCacheService
    - Create two-tier caching (Memory + Redis)
    - Implement cache-aside pattern
    - Add tag-based cache invalidation
    - _Requirements: 5.5, 14.4_
  
  - [ ] 8.3 Add caching to query handlers
    - Cache library items with 10-minute expiration
    - Cache content details with 1-hour expiration
    - Cache metadata with tag-based invalidation
    - _Requirements: 5.5_

- [ ] 9. Implement WebApi layer - Controllers
  - [ ] 9.1 Create LibraryController
    - Implement GET /api/library/items endpoint with pagination
    - Implement GET /api/library/items/{id} endpoint
    - Implement POST /api/library/scan endpoint
    - Implement DELETE /api/library/items/{id} endpoint
    - Add output caching policies
    - _Requirements: 2.1, 4.1, 4.5_
  
  - [ ] 9.2 Create StreamingController
    - Implement POST /api/stream/{id}/start endpoint
    - Implement GET /api/stream/{sessionId}/stream endpoint with range support
    - Implement POST /api/stream/{sessionId}/progress endpoint
    - Implement DELETE /api/stream/{sessionId}/stop endpoint
    - Add rate limiting for streaming endpoints
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.7_
  
  - [ ] 9.3 Create ProfilesController
    - Implement GET /api/profiles endpoint
    - Implement POST /api/profiles endpoint
    - Implement PUT /api/profiles/{id} endpoint
    - Implement GET /api/profiles/{id}/history endpoint
    - Implement GET /api/profiles/{id}/watchlist endpoint
    - _Requirements: 1.3, 1.4_
  
  - [ ] 9.4 Create AppUpdateController for Android updates
    - Implement GET /api/app-updates/android/latest endpoint
    - Implement GET /api/app-updates/android/download/{version} endpoint
    - Implement POST /api/app-updates/android/upload endpoint (admin only)
    - Add APK file validation and checksum calculation
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.7, 13.8, 13.10_
  
  - [ ] 9.5 Create SettingsController
    - Implement GET /api/settings endpoint
    - Implement PUT /api/settings endpoint
    - Add configuration management
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ] 10. Implement SignalR hubs for real-time communication
  - [ ] 10.1 Create NotificationHub
    - Implement hub with group subscription methods
    - Add authentication to hub connections
    - _Requirements: 12.1, 12.6_
  
  - [ ] 10.2 Integrate SignalR with services
    - Broadcast library scan progress from LibraryScanService
    - Broadcast new content notifications
    - Broadcast transcoding progress from TranscodingPipeline
    - _Requirements: 12.2, 12.3, 12.4_
  
  - [ ] 10.3 Configure SignalR with Redis backplane
    - Set up Redis backplane for multi-server support
    - Configure connection lifetime and reconnection
    - _Requirements: 12.5, 12.7_

- [ ] 11. Implement middleware and cross-cutting concerns
  - [ ] 11.1 Create ExceptionHandlingMiddleware
    - Handle NotFoundException with 404 response
    - Handle ValidationException with 400 response
    - Handle TranscodingException with 500 response
    - Log all exceptions with structured logging
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_
  
  - [ ] 11.2 Implement authentication and authorization
    - Set up JWT authentication
    - Configure authorization policies
    - Add profile-based authorization
    - _Requirements: 2.6_
  
  - [ ] 11.3 Add rate limiting middleware
    - Configure global rate limiter
    - Add streaming-specific rate limits
    - Implement per-user rate limiting
    - _Requirements: 13.7_
  
  - [ ] 11.4 Configure CORS for client applications
    - Allow requests from web clients
    - Configure allowed methods and headers
    - _Requirements: 2.1_

- [ ] 12. Implement migration tool
  - [ ] 12.1 Create LegacyDatabaseReader
    - Read Content table from old SQLite database using Dapper
    - Read Profile table
    - Read WatchHistory table
    - Read Settings table
    - Read SeriesEpisode table
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [ ] 12.2 Create DataTransformer
    - Transform Content entities to new schema
    - Transform Profile entities
    - Transform WatchHistory entities
    - Transform Episode entities
    - Handle data type conversions and null values
    - _Requirements: 1.2_
  
  - [ ] 12.3 Create MigrationOrchestrator
    - Validate legacy database accessibility
    - Execute migration in transaction
    - Verify data integrity after migration
    - Generate detailed migration report
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_
  
  - [ ] 12.4 Implement configuration migration
    - Read .env file from legacy backend
    - Transform configuration to appsettings.json format
    - Migrate media paths
    - Migrate API keys
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  
  - [ ] 12.5 Create migration CLI tool
    - Build console application for migration
    - Add dry-run mode for validation
    - Add progress reporting
    - Add rollback capability
    - _Requirements: 1.6, 10.1, 10.2, 10.3_

- [ ] 13. Implement API compatibility layer
  - [ ] 13.1 Create legacy endpoint mappings
    - Map old endpoints to new endpoints
    - Transform request/response formats
    - Add version detection
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [ ] 13.2 Add response format compatibility
    - Wrap responses in legacy format when needed
    - Add success/message fields
    - _Requirements: 2.1, 2.4_
  
  - [ ] 13.3 Implement legacy token validation
    - Support old authentication tokens during transition
    - Add token migration endpoint
    - _Requirements: 2.6_

- [ ] 14. Add performance optimizations
  - [ ] 14.1 Implement ArrayPool for buffer management
    - Use ArrayPool in streaming operations
    - Use ArrayPool in transcoding pipeline
    - _Requirements: 5.6, 14.1, 14.2_
  
  - [ ] 14.2 Add Span and Memory usage
    - Replace byte[] with ReadOnlyMemory in streaming
    - Use Span for buffer operations
    - _Requirements: 5.7, 14.2_
  
  - [ ] 14.3 Configure output caching
    - Add output cache policies for library endpoints
    - Add output cache policies for metadata endpoints
    - Implement tag-based cache invalidation
    - _Requirements: 14.12_
  
  - [ ] 14.4 Optimize database queries
    - Add proper indexes to all entities
    - Use compiled queries for hot paths
    - Implement query result caching
    - _Requirements: 5.1, 5.4, 14.3, 14.10_
  
  - [ ] 14.5 Configure HTTP client pooling
    - Set up HttpClientFactory for TMDB client
    - Configure connection pooling parameters
    - Set appropriate timeouts
    - _Requirements: 5.10, 14.10_
  
  - [ ] 14.6 Implement response compression
    - Enable Brotli compression
    - Enable Gzip compression as fallback
    - _Requirements: 5.11_
  
  - [ ] 14.7 Add HTTP/2 and HTTP/3 support
    - Configure Kestrel for HTTP/2
    - Enable HTTP/3 support
    - _Requirements: 5.12_

- [ ] 15. Implement monitoring and observability
  - [ ] 15.1 Set up OpenTelemetry
    - Configure tracing for ASP.NET Core
    - Configure tracing for HTTP clients
    - Configure tracing for EF Core
    - Add custom tracing sources
    - _Requirements: 9.1, 9.2_
  
  - [ ] 15.2 Create custom metrics
    - Add stream start counter
    - Add stream duration histogram
    - Add active streams gauge
    - Add transcoding queue depth gauge
    - Add cache hit ratio metric
    - _Requirements: 5.5_
  
  - [ ] 15.3 Implement health checks
    - Add database health check
    - Add Redis health check
    - Add FFmpeg health check
    - Add disk space health check
    - _Requirements: 11.4_
  
  - [ ] 15.4 Configure structured logging
    - Set up Serilog with structured logging
    - Configure log sinks (file, console)
    - Implement log rotation
    - Add sensitive data redaction
    - _Requirements: 9.1, 9.2, 9.4, 9.5, 9.6, 9.7_

- [ ] 16. Create deployment configurations
  - [ ] 16.1 Set up single executable publishing
    - Configure PublishSingleFile
    - Configure PublishTrimmed
    - Test executable on Windows
    - _Requirements: 11.1_
  
  - [ ] 16.2 Create Docker configuration
    - Write Dockerfile with FFmpeg installation
    - Create docker-compose.yml with Redis
    - Test Docker deployment
    - _Requirements: 11.2_
  
  - [ ] 16.3 Create configuration templates
    - Create appsettings.json template
    - Create appsettings.Production.json
    - Document all configuration options
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  
  - [ ] 16.4 Write deployment documentation
    - Document migration process
    - Document rollback procedure
    - Document health check endpoints
    - _Requirements: 11.3, 11.4, 11.5, 11.6_

- [ ] 17. Write comprehensive tests
  - [ ] 17.1 Write unit tests for streaming strategies
    - Test DirectPlayStrategy.CanHandle logic
    - Test DirectStreamStrategy.CanHandle logic
    - Test TranscodeVideoStrategy.CanHandle logic
    - Test FullTranscodeStrategy.CanHandle logic
    - Test StreamingStrategySelector
    - _Requirements: 10.2_
  
  - [ ] 17.2 Write unit tests for CQRS handlers
    - Test GetLibraryItemsQueryHandler
    - Test StartStreamCommandHandler
    - Test CreateProfileCommandHandler
    - _Requirements: 10.2_
  
  - [ ] 17.3 Write integration tests for API endpoints
    - Test LibraryController endpoints
    - Test StreamingController endpoints
    - Test ProfilesController endpoints
    - Test AppUpdateController endpoints
    - _Requirements: 10.3_
  
  - [ ] 17.4 Write performance tests
    - Test concurrent streaming (10+ clients)
    - Test stream startup time (<500ms)
    - Test API response time (<100ms p95)
    - Test memory usage under load
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 10.4_
  
  - [ ] 17.5 Write migration validation tests
    - Test data integrity after migration
    - Compare record counts
    - Validate transformed data
    - _Requirements: 10.1, 10.7_

- [ ] 18. Perform migration and validation
  - [ ] 18.1 Execute migration in test environment
    - Run migration tool with dry-run
    - Review migration report
    - Execute actual migration
    - Verify all data migrated correctly
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_
  
  - [ ] 18.2 Validate API compatibility
    - Test all legacy endpoints
    - Compare responses with old backend
    - Verify streaming functionality
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_
  
  - [ ] 18.3 Perform performance benchmarking
    - Compare stream startup times
    - Compare API response times
    - Compare memory usage
    - Compare CPU usage
    - Verify performance meets or exceeds Jellyfin
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.8, 5.9_
  
  - [ ] 18.4 Execute parallel testing
    - Run both backends simultaneously
    - Compare outputs and behavior
    - Monitor for errors
    - _Requirements: 11.3, 11.4, 11.5_
  
  - [ ] 18.5 Prepare for production cutover
    - Document cutover procedure
    - Test rollback procedure
    - Create monitoring dashboards
    - _Requirements: 11.3, 11.4, 11.5, 11.6, 11.7_
