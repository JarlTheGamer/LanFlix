# Requirements Document

## Introduction

This document outlines the requirements for migrating the Lanflix media streaming server from the existing Node.js/TypeScript backend to a new high-performance C# backend built with ASP.NET Core 9.0. The migration aims to achieve superior performance, better scalability, and improved maintainability while preserving all existing functionality and ensuring seamless transition for existing users.

## Glossary

- **Legacy Backend**: The existing Node.js/TypeScript backend located in `server/backend-old`
- **New Backend**: The C# ASP.NET Core 9.0 backend to be implemented in `server/backend`
- **Migration System**: The tooling and processes that facilitate data and configuration transfer from Legacy Backend to New Backend
- **Content Library**: The collection of media files (movies, TV series) managed by the system
- **Stream Session**: An active media streaming connection between a client and the server
- **Transcoding Pipeline**: The FFmpeg-based system that converts media formats in real-time
- **Client Application**: The frontend application that consumes the backend API
- **Profile**: A user account within the system with personalized settings and watch history
- **Watch History**: The record of media consumption progress for each profile
- **TMDB**: The Movie Database, an external API for fetching media metadata
- **Direct Play**: Streaming mode where media is served without any transcoding
- **Direct Stream**: Streaming mode where container format is changed but codecs are preserved
- **Hardware Acceleration**: GPU-based video encoding/decoding (NVENC, QuickSync, AMF, VAAPI)
- **APK**: Android Package file format for distributing Android applications
- **OTA Update**: Over-The-Air update mechanism for delivering app updates to Android devices
- **Jellyfin**: Open-source media server software that serves as the performance benchmark

## Requirements

### Requirement 1: Data Migration

**User Story:** As a system administrator, I want to migrate all existing data from the Legacy Backend to the New Backend, so that users can continue using the system without losing their content library, watch history, or preferences.

#### Acceptance Criteria

1. WHEN the Migration System executes, THE Migration System SHALL read all content metadata from the Legacy Backend SQLite database
2. WHEN the Migration System processes content records, THE Migration System SHALL transform each record to match the New Backend schema
3. WHEN the Migration System encounters profile data, THE Migration System SHALL migrate all profile information including names, preferences, and settings
4. WHEN the Migration System processes watch history, THE Migration System SHALL preserve all playback positions and completion status for each profile
5. WHERE media file paths exist in the Legacy Backend, THE Migration System SHALL validate file accessibility and update paths if necessary
6. WHEN the Migration System completes successfully, THE Migration System SHALL generate a detailed migration report showing all migrated entities
7. IF the Migration System encounters data inconsistencies, THEN THE Migration System SHALL log warnings and continue processing remaining records

### Requirement 2: API Compatibility Layer

**User Story:** As a frontend developer, I want the New Backend to support the existing API endpoints, so that the Client Application can continue functioning without immediate changes.

#### Acceptance Criteria

1. WHEN the Client Application sends requests to existing API endpoints, THE New Backend SHALL respond with data in the expected format
2. WHERE endpoint paths differ between Legacy Backend and New Backend, THE New Backend SHALL provide route mappings for backward compatibility
3. WHEN the Client Application requests library items, THE New Backend SHALL return content data matching the Legacy Backend response structure
4. WHEN the Client Application initiates streaming, THE New Backend SHALL accept the same request parameters as the Legacy Backend
5. WHILE the compatibility layer is active, THE New Backend SHALL log all compatibility-routed requests for monitoring
6. WHEN authentication tokens from the Legacy Backend are presented, THE New Backend SHALL validate and accept them during a transition period

### Requirement 3: Streaming Functionality Parity

**User Story:** As a user, I want to stream media with the same or better quality and performance as the Legacy Backend, so that my viewing experience is not degraded.

#### Acceptance Criteria

1. THE New Backend SHALL support Direct Play streaming for compatible media formats
2. THE New Backend SHALL support Direct Stream (remux) streaming when container conversion is needed
3. THE New Backend SHALL support video-only transcoding when video codec is incompatible
4. THE New Backend SHALL support full transcoding when both video and audio codecs are incompatible
5. WHEN hardware acceleration is available, THE New Backend SHALL utilize it for transcoding operations
6. WHEN a client requests media streaming, THE New Backend SHALL select the optimal streaming strategy based on client capabilities
7. WHILE streaming is active, THE New Backend SHALL support HTTP range requests for seeking functionality
8. WHEN transcoding is required, THE New Backend SHALL achieve stream startup time of less than 500 milliseconds

### Requirement 4: Library Management

**User Story:** As a content manager, I want to scan, organize, and manage my media library, so that all content is properly cataloged and accessible.

#### Acceptance Criteria

1. WHEN a library scan is triggered, THE New Backend SHALL recursively scan configured media directories
2. WHEN new media files are discovered, THE New Backend SHALL extract metadata using FFprobe
3. WHEN media metadata is incomplete, THE New Backend SHALL fetch additional information from TMDB API
4. THE New Backend SHALL detect and catalog video streams, audio streams, and subtitle tracks for each media file
5. WHEN media files are moved or deleted, THE New Backend SHALL update the database accordingly
6. WHEN duplicate content is detected, THE New Backend SHALL log warnings and allow manual resolution
7. WHILE scanning is in progress, THE New Backend SHALL broadcast progress updates via SignalR to connected clients

### Requirement 5: Performance Requirements (Jellyfin Parity)

**User Story:** As a system administrator, I want the New Backend to match or exceed Jellyfin's performance characteristics, so that the system provides a competitive media streaming experience.

#### Acceptance Criteria

1. THE New Backend SHALL handle at least 10 concurrent streaming sessions without performance degradation
2. THE New Backend SHALL maintain base memory usage below 200 megabytes when idle
3. THE New Backend SHALL maintain CPU usage below 5 percent when idle
4. WHEN serving API requests, THE New Backend SHALL respond within 100 milliseconds for the 95th percentile
5. WHEN caching is enabled, THE New Backend SHALL achieve a cache hit ratio above 70 percent for metadata requests
6. THE New Backend SHALL utilize async/await patterns throughout to prevent blocking operations
7. WHEN streaming media, THE New Backend SHALL employ zero-copy techniques where possible to minimize memory allocations
8. THE New Backend SHALL achieve transcoding throughput equal to or greater than Jellyfin for equivalent hardware
9. WHEN multiple clients request different quality levels, THE New Backend SHALL efficiently manage bandwidth allocation
10. THE New Backend SHALL implement connection pooling for all external API calls to minimize latency
11. WHEN serving static assets, THE New Backend SHALL utilize response compression with Brotli or Gzip
12. THE New Backend SHALL support HTTP/2 and HTTP/3 protocols for improved performance

### Requirement 6: Configuration Migration

**User Story:** As a system administrator, I want to migrate configuration settings from the Legacy Backend, so that the New Backend operates with the same preferences and paths.

#### Acceptance Criteria

1. WHEN the Migration System reads Legacy Backend configuration, THE Migration System SHALL extract media library paths
2. WHEN the Migration System processes transcoding settings, THE Migration System SHALL convert them to New Backend configuration format
3. WHEN the Migration System encounters Redis configuration, THE Migration System SHALL preserve connection strings and settings
4. THE Migration System SHALL migrate TMDB API keys and external service credentials
5. WHERE configuration values are incompatible, THE Migration System SHALL apply sensible defaults and log the changes
6. WHEN configuration migration completes, THE Migration System SHALL generate a configuration file for the New Backend

### Requirement 7: Database Schema Evolution

**User Story:** As a database administrator, I want the New Backend to use an optimized database schema, so that queries execute efficiently and data integrity is maintained.

#### Acceptance Criteria

1. THE New Backend SHALL implement proper database indexes for frequently queried fields
2. THE New Backend SHALL use Entity Framework Core for database operations
3. WHEN complex queries are required, THE New Backend SHALL utilize Dapper for performance optimization
4. THE New Backend SHALL store media metadata as JSON columns for flexible schema evolution
5. THE New Backend SHALL implement soft deletes using query filters
6. WHEN database migrations are applied, THE New Backend SHALL preserve all existing data
7. THE New Backend SHALL support both SQLite for single-user deployments and PostgreSQL for multi-user scenarios

### Requirement 8: Transcoding Session Management

**User Story:** As a system administrator, I want transcoding sessions to be properly managed and cleaned up, so that system resources are not wasted on abandoned streams.

#### Acceptance Criteria

1. WHEN a transcoding session starts, THE New Backend SHALL create a session record with unique identifier
2. WHEN a client disconnects, THE New Backend SHALL detect the disconnection within 30 seconds
3. WHEN a session is abandoned, THE New Backend SHALL terminate the associated FFmpeg process
4. WHEN temporary transcoding files are created, THE New Backend SHALL delete them after session completion
5. THE New Backend SHALL maintain a registry of active transcoding sessions
6. WHEN the server restarts, THE New Backend SHALL clean up any orphaned transcoding processes
7. WHILE transcoding is active, THE New Backend SHALL report progress updates to connected clients

### Requirement 9: Error Handling and Logging

**User Story:** As a system administrator, I want comprehensive error logging and handling, so that I can diagnose and resolve issues quickly.

#### Acceptance Criteria

1. WHEN errors occur, THE New Backend SHALL log detailed error information including stack traces
2. THE New Backend SHALL implement structured logging using a consistent format
3. WHEN critical errors occur, THE New Backend SHALL return appropriate HTTP status codes to clients
4. THE New Backend SHALL log all streaming session starts, stops, and errors
5. WHEN FFmpeg processes fail, THE New Backend SHALL capture and log stderr output
6. THE New Backend SHALL implement log rotation to prevent disk space exhaustion
7. WHERE sensitive information exists in logs, THE New Backend SHALL redact or mask it

### Requirement 10: Testing and Validation

**User Story:** As a quality assurance engineer, I want comprehensive tests to validate the migration, so that I can ensure the New Backend functions correctly.

#### Acceptance Criteria

1. THE Migration System SHALL include validation tests that compare Legacy Backend and New Backend data
2. THE New Backend SHALL include unit tests for all streaming strategies
3. THE New Backend SHALL include integration tests for API endpoints
4. WHEN performance tests are executed, THE New Backend SHALL meet or exceed Legacy Backend performance metrics
5. THE New Backend SHALL include tests for concurrent streaming scenarios
6. THE New Backend SHALL include tests for FFmpeg integration and hardware acceleration detection
7. WHEN migration validation runs, THE Migration System SHALL verify data integrity and completeness

### Requirement 11: Deployment and Rollback

**User Story:** As a system administrator, I want a safe deployment process with rollback capability, so that I can revert to the Legacy Backend if issues arise.

#### Acceptance Criteria

1. THE New Backend SHALL support deployment as a single self-contained executable
2. THE New Backend SHALL support Docker containerization for flexible deployment
3. WHEN the New Backend is deployed, THE Legacy Backend SHALL remain accessible for rollback purposes
4. THE deployment process SHALL include a health check endpoint for monitoring
5. WHERE database migrations are applied, THE deployment process SHALL create backups first
6. WHEN rollback is required, THE system SHALL provide clear instructions for reverting to Legacy Backend
7. THE New Backend SHALL include a startup validation routine that checks for required dependencies

### Requirement 12: Real-time Communication

**User Story:** As a user, I want to receive real-time notifications about library updates and transcoding progress, so that I stay informed about system activities.

#### Acceptance Criteria

1. THE New Backend SHALL implement SignalR hubs for real-time communication
2. WHEN library scanning progresses, THE New Backend SHALL broadcast percentage completion updates
3. WHEN new content is added to the library, THE New Backend SHALL notify connected clients
4. WHEN transcoding is in progress, THE New Backend SHALL send progress updates every 2 seconds
5. THE New Backend SHALL maintain WebSocket connections with automatic reconnection support
6. WHEN clients connect to SignalR hubs, THE New Backend SHALL authenticate them using the same mechanism as REST APIs
7. THE New Backend SHALL support multiple concurrent SignalR connections per profile

### Requirement 13: Android App Auto-Update System

**User Story:** As an Android app user, I want the app to automatically check for and install updates, so that I always have the latest features and bug fixes without manual intervention.

#### Acceptance Criteria

1. THE New Backend SHALL provide an API endpoint that returns the latest Android app version information
2. WHEN the Android app queries for updates, THE New Backend SHALL return version number, release notes, and APK download URL
3. THE New Backend SHALL host APK files in a publicly accessible location with proper MIME types
4. WHEN a new APK version is available, THE New Backend SHALL include file size and SHA-256 checksum in the response
5. THE New Backend SHALL support incremental update checks with conditional requests to minimize bandwidth
6. WHERE multiple APK variants exist, THE New Backend SHALL serve the appropriate variant based on device architecture
7. THE New Backend SHALL implement rate limiting on APK downloads to prevent abuse
8. WHEN APK files are uploaded, THE New Backend SHALL validate file integrity and signature
9. THE New Backend SHALL maintain a version history with rollback capability for previous APK versions
10. THE New Backend SHALL provide an admin API for uploading and managing APK releases

### Requirement 14: Advanced Optimization Features

**User Story:** As a system administrator, I want advanced optimization features comparable to Jellyfin, so that the system operates efficiently under various conditions.

#### Acceptance Criteria

1. THE New Backend SHALL implement intelligent buffer management using ArrayPool for memory reuse
2. THE New Backend SHALL utilize Span and Memory types for zero-allocation buffer operations
3. WHEN database queries are executed, THE New Backend SHALL use compiled queries and proper indexing strategies
4. THE New Backend SHALL implement a multi-tier caching strategy with memory cache and distributed Redis cache
5. WHEN transcoding multiple streams, THE New Backend SHALL implement a priority queue based on user activity
6. THE New Backend SHALL detect and utilize available hardware acceleration automatically without manual configuration
7. WHEN serving media files, THE New Backend SHALL implement adaptive bitrate streaming with HLS or DASH protocols
8. THE New Backend SHALL monitor system resources and throttle operations when thresholds are exceeded
9. WHEN FFmpeg processes are spawned, THE New Backend SHALL use process pooling to reduce startup overhead
10. THE New Backend SHALL implement database connection pooling with configurable pool sizes
11. THE New Backend SHALL use PipeReader and PipeWriter for efficient streaming I/O operations
12. WHEN serving API responses, THE New Backend SHALL implement output caching with tag-based invalidation
