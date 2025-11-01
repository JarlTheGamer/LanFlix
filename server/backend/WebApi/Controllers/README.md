# WebApi Controllers Implementation

This document provides an overview of the implemented API controllers for the Lanflix backend.

## Implemented Controllers

### 1. LibraryController (`/api/library`)

Manages the media library with full CRUD operations.

**Endpoints:**
- `GET /api/library/items` - Get paginated library items with filtering and search
  - Query parameters: Type, PageNumber, PageSize, SearchTerm, Genre, SortBy, SortDescending
  - Output caching: 10 minutes
- `GET /api/library/items/{id}` - Get detailed content information
  - Output caching: 1 hour
- `POST /api/library/scan` - Trigger library scan
  - Body: ScanLibraryCommand (Path, FullScan)
- `DELETE /api/library/items/{id}` - Remove content from library

**Features:**
- Output caching policies for improved performance
- Pagination support
- Search and filtering capabilities
- Structured logging

### 2. StreamingController (`/api/stream`)

Handles media streaming with multiple strategies and range request support.

**Endpoints:**
- `POST /api/stream/{id}/start` - Start a new streaming session
  - Body: StartStreamCommand (ProfileId, ClientCapabilities, etc.)
- `GET /api/stream/{sessionId}/stream` - Stream media content
  - Supports HTTP range requests for seeking
  - Rate limited (max 3 concurrent streams per IP)
  - Returns 206 Partial Content for range requests
- `POST /api/stream/{sessionId}/progress` - Update playback progress
  - Body: UpdateProgressCommand (PositionTicks, IsCompleted)
- `DELETE /api/stream/{sessionId}/stop` - Stop streaming session

**Features:**
- Automatic streaming strategy selection (DirectPlay, DirectStream, Transcode)
- HTTP range request support for seeking
- Rate limiting for concurrent streams
- Session management with cleanup
- Client capability detection

### 3. ProfilesController (`/api/profiles`)

Manages user profiles and watch history.

**Endpoints:**
- `GET /api/profiles` - Get all profiles
  - Output caching: 10 minutes
- `POST /api/profiles` - Create new profile
  - Body: CreateProfileCommand (Name, AvatarPath, IsKidsProfile, Preferences)
- `PUT /api/profiles/{id}` - Update profile
  - Body: UpdateProfileCommand
- `GET /api/profiles/{id}/history` - Get watch history
  - Query parameter: limit (default: 50)
  - Output caching: 5 minutes
- `GET /api/profiles/{id}/watchlist` - Get watchlist
  - Output caching: 5 minutes

**Features:**
- Profile management with preferences
- Watch history tracking
- Watchlist support
- Output caching

### 4. AppUpdateController (`/api/app-updates`)

Manages Android app OTA updates.

**Endpoints:**
- `GET /api/app-updates/android/latest` - Check for updates
  - Query parameters: currentVersion, architecture (default: arm64-v8a)
  - Returns 204 No Content if no update available
- `GET /api/app-updates/android/download/{version}/{architecture}` - Download APK
  - Rate limited
  - Supports range requests for resume capability
- `POST /api/app-updates/android/upload` - Upload new APK (Admin only)
  - Form data: apkFile, version, versionCode, releaseNotes, etc.
  - Max file size: 200MB
  - Calculates SHA-256 checksum
- `GET /api/app-updates/android/history` - Get version history

**Features:**
- Version comparison logic
- APK file validation
- SHA-256 checksum calculation
- Architecture-specific builds support
- Force update capability
- Minimum version enforcement

### 5. SettingsController (`/api/settings`)

Manages server configuration.

**Endpoints:**
- `GET /api/settings` - Get current settings (Admin only)
- `PUT /api/settings` - Update settings (Admin only)
  - Body: ServerSettingsDto
- `POST /api/settings/validate` - Validate settings without saving (Admin only)

**Features:**
- Configuration management for:
  - Media paths
  - Transcoding settings
  - Streaming settings
  - Cache settings (Redis, Memory)
  - External APIs (TMDB)
- Settings validation
- Persists to appsettings.json

## Cross-Cutting Concerns

### Output Caching

Configured in `Program.cs` with multiple policies:
- `library` - 10 minutes, varies by query parameters
- `content-details` - 1 hour
- `profiles` - 10 minutes

### Rate Limiting

Configured in `Program.cs`:
- Global rate limiter: 100 requests per minute per IP
- Streaming rate limiter: Max 3 concurrent streams per IP

### Logging

All controllers use structured logging with:
- Request information
- Performance metrics
- Error details

### Error Handling

Controllers return appropriate HTTP status codes:
- 200 OK - Success
- 201 Created - Resource created
- 204 No Content - Success with no content
- 206 Partial Content - Range request success
- 400 Bad Request - Invalid input
- 404 Not Found - Resource not found
- 500 Internal Server Error - Server error

## Supporting Services

### AppUpdateService
- Manages APK storage and metadata
- Version comparison logic
- Checksum calculation
- File validation

### SettingsService
- Reads from IConfiguration
- Writes to appsettings.json
- Preserves existing configuration sections

## Configuration

All controllers are configured in `Program.cs`:
```csharp
builder.Services.AddControllers();
builder.Services.AddOutputCache(/* policies */);
builder.Services.AddRateLimiter(/* policies */);
```

Middleware pipeline:
```csharp
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthorization();
app.MapControllers();
```

## Future Enhancements

1. **Authentication & Authorization**
   - JWT token validation
   - Role-based access control
   - Profile-based authorization

2. **API Versioning**
   - URL-based versioning
   - Header-based versioning

3. **OpenAPI/Swagger**
   - Enhanced documentation
   - Request/response examples

4. **Health Checks**
   - Database connectivity
   - Redis connectivity
   - FFmpeg availability
   - Disk space monitoring

5. **Metrics & Telemetry**
   - OpenTelemetry integration
   - Custom metrics
   - Performance monitoring
