# Lanflix C# Backend Architecture

## Overview
High-performance, modular media streaming server built with ASP.NET Core 9.0, designed to compete with Jellyfin in terms of performance, features, and extensibility.

## Core Principles

### 1. Performance First
- **Zero-copy streaming** where possible
- **Async/await** throughout for non-blocking I/O
- **Memory pooling** for buffer management
- **Efficient transcoding** with hardware acceleration support
- **Response caching** at multiple layers
- **Database query optimization** with proper indexing

### 2. Modular Architecture
- **Clean Architecture** (Domain, Application, Infrastructure, Presentation)
- **CQRS pattern** for complex operations
- **Repository pattern** for data access
- **Strategy pattern** for streaming modes
- **Factory pattern** for transcoding pipelines
- **Plugin architecture** for extensibility

### 3. Scalability
- **Horizontal scaling** support
- **Distributed caching** with Redis
- **Background job processing** with Hangfire
- **Event-driven architecture** with SignalR
- **Stateless API design**

## Project Structure

```
Lanflix.Server/
├── Domain/                          # Core business logic (no dependencies)
│   ├── Entities/                    # Domain entities
│   │   ├── Content.cs
│   │   ├── Episode.cs
│   │   ├── Profile.cs
│   │   ├── WatchHistory.cs
│   │   └── StreamSession.cs
│   ├── Enums/
│   │   ├── ContentType.cs
│   │   ├── StreamingMode.cs
│   │   └── TranscodeReason.cs
│   ├── ValueObjects/               # Immutable value objects
│   │   ├── MediaInfo.cs
│   │   ├── VideoCodec.cs
│   │   └── AudioCodec.cs
│   └── Interfaces/                 # Domain service interfaces
│       └── IMediaAnalyzer.cs
│
├── Application/                     # Application business logic
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IApplicationDbContext.cs
│   │   │   ├── ICacheService.cs
│   │   │   └── IDateTime.cs
│   │   ├── Behaviors/              # MediatR pipeline behaviors
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── PerformanceBehavior.cs
│   │   └── Exceptions/
│   │       ├── NotFoundException.cs
│   │       └── ValidationException.cs
│   ├── Features/                   # Feature-based organization (Vertical Slices)
│   │   ├── Library/
│   │   │   ├── Commands/
│   │   │   │   ├── ScanLibrary/
│   │   │   │   ├── AddContent/
│   │   │   │   └── RemoveContent/
│   │   │   └── Queries/
│   │   │       ├── GetLibraryItems/
│   │   │       ├── GetContentDetails/
│   │   │       └── SearchContent/
│   │   ├── Streaming/
│   │   │   ├── Commands/
│   │   │   │   ├── StartStream/
│   │   │   │   ├── UpdateProgress/
│   │   │   │   └── StopStream/
│   │   │   └── Queries/
│   │   │       ├── GetStreamInfo/
│   │   │       └── GetPlaybackInfo/
│   │   ├── Transcoding/
│   │   │   ├── Services/
│   │   │   │   ├── ITranscodingService.cs
│   │   │   │   ├── TranscodingOrchestrator.cs
│   │   │   │   └── TranscodingSessionManager.cs
│   │   │   └── Strategies/
│   │   │       ├── IStreamingStrategy.cs
│   │   │       ├── DirectPlayStrategy.cs
│   │   │       ├── DirectStreamStrategy.cs
│   │   │       ├── TranscodeStrategy.cs
│   │   │       └── RemuxStrategy.cs
│   │   ├── Metadata/
│   │   │   ├── Commands/
│   │   │   │   └── RefreshMetadata/
│   │   │   └── Services/
│   │   │       ├── ITmdbClient.cs
│   │   │       └── IMetadataProvider.cs
│   │   └── Profiles/
│   │       ├── Commands/
│   │       └── Queries/
│   └── DependencyInjection.cs
│
├── Infrastructure/                  # External concerns
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/         # EF Core configurations
│   │   │   ├── ContentConfiguration.cs
│   │   │   └── ProfileConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── ContentRepository.cs
│   │   │   └── ProfileRepository.cs
│   │   └── Migrations/
│   ├── Services/
│   │   ├── FFmpeg/
│   │   │   ├── FFmpegService.cs
│   │   │   ├── FFmpegCommandBuilder.cs
│   │   │   ├── HardwareAccelerationDetector.cs
│   │   │   └── TranscodingPipeline.cs
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs
│   │   │   └── MemoryCacheService.cs
│   │   ├── ExternalApis/
│   │   │   ├── TmdbClient.cs
│   │   │   ├── ProwlarrClient.cs
│   │   │   └── RadarrClient.cs
│   │   └── FileSystem/
│   │       ├── MediaScanner.cs
│   │       └── FileSystemWatcher.cs
│   ├── BackgroundJobs/
│   │   ├── LibraryScanJob.cs
│   │   ├── MetadataRefreshJob.cs
│   │   └── TranscodingCleanupJob.cs
│   └── DependencyInjection.cs
│
├── WebApi/                         # Presentation layer
│   ├── Controllers/
│   │   ├── LibraryController.cs
│   │   ├── StreamingController.cs
│   │   ├── ProfilesController.cs
│   │   └── SettingsController.cs
│   ├── Hubs/                       # SignalR hubs
│   │   ├── NotificationHub.cs
│   │   └── TranscodingProgressHub.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── PerformanceLoggingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Filters/
│   │   ├── ApiExceptionFilterAttribute.cs
│   │   └── ValidateModelStateAttribute.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── Tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    ├── Infrastructure.Tests/
    └── WebApi.Tests/
```

## Streaming Architecture (4 Modes)

### 1. Direct Play
**When**: Client supports all codecs, no transcoding needed
**Implementation**:
```csharp
public class DirectPlayStrategy : IStreamingStrategy
{
    public async Task<StreamResult> ExecuteAsync(StreamRequest request)
    {
        // Return file stream directly with range support
        // Zero-copy streaming using FileStream with FileOptions.Asynchronous
        // Support HTTP Range requests for seeking
    }
}
```

### 2. Direct Stream (Remux)
**When**: Container format incompatible, but codecs are supported
**Implementation**:
```csharp
public class DirectStreamStrategy : IStreamingStrategy
{
    public async Task<StreamResult> ExecuteAsync(StreamRequest request)
    {
        // FFmpeg remux: copy codecs, change container
        // ffmpeg -i input.mkv -c copy -f mp4 pipe:1
        // Stream output directly to client
    }
}
```

### 3. Transcode Video Only
**When**: Video codec incompatible, audio is fine
**Implementation**:
```csharp
public class TranscodeVideoStrategy : IStreamingStrategy
{
    public async Task<StreamResult> ExecuteAsync(StreamRequest request)
    {
        // FFmpeg: transcode video, copy audio
        // Use hardware acceleration when available
        // Adaptive bitrate based on client capabilities
    }
}
```

### 4. Full Transcode
**When**: Both video and audio need transcoding
**Implementation**:
```csharp
public class FullTranscodeStrategy : IStreamingStrategy
{
    public async Task<StreamResult> ExecuteAsync(StreamRequest request)
    {
        // FFmpeg: transcode both streams
        // HLS/DASH segmented streaming for adaptive bitrate
        // Session management for cleanup
    }
}
```

## Key Technical Decisions

### Database
- **SQLite** for single-user deployments (like Jellyfin)
- **PostgreSQL** support for multi-user/enterprise
- **EF Core** with proper indexing and query optimization
- **Dapper** for performance-critical queries

### Caching Strategy
```
┌─────────────────────────────────────────┐
│  L1: Memory Cache (Hot Data)            │
│  - Active stream sessions                │
│  - Recently accessed metadata            │
│  - User preferences                      │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  L2: Redis Cache (Distributed)          │
│  - Metadata cache                        │
│  - Transcoding session state             │
│  - API response cache                    │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  L3: Database (Persistent)               │
│  - Content metadata                      │
│  - User data                             │
│  - Watch history                         │
└─────────────────────────────────────────┘
```

### FFmpeg Integration
```csharp
public class FFmpegService
{
    // Hardware acceleration detection
    private readonly HardwareAccelerationDetector _hwAccel;
    
    // Process pool for reusing FFmpeg instances
    private readonly ObjectPool<Process> _processPool;
    
    // Streaming with backpressure handling
    public async IAsyncEnumerable<byte[]> StreamTranscodeAsync(
        string inputPath,
        TranscodeOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Yield transcoded chunks as they're ready
        // Handle backpressure from slow clients
        // Cleanup on cancellation
    }
}
```

### Performance Optimizations

1. **Span<T> and Memory<T>** for zero-allocation buffer handling
2. **ArrayPool<T>** for buffer reuse
3. **ValueTask** for hot paths
4. **Channels** for producer-consumer patterns
5. **PipeReader/PipeWriter** for efficient I/O
6. **Response compression** with Brotli/Gzip
7. **HTTP/2** and HTTP/3 support
8. **Connection pooling** for external APIs

### Monitoring & Observability

```csharp
// OpenTelemetry integration
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation());

// Custom metrics
- Active stream count
- Transcoding queue depth
- Cache hit ratio
- API response times
- FFmpeg process count
```

## API Design

### RESTful Endpoints
```
GET    /api/library/items              # List all content
GET    /api/library/items/{id}         # Get content details
POST   /api/library/scan               # Trigger library scan
DELETE /api/library/items/{id}         # Remove content

GET    /api/stream/{id}/master.m3u8    # HLS master playlist
GET    /api/stream/{id}/direct         # Direct play/stream
POST   /api/stream/{id}/start          # Start streaming session
POST   /api/stream/{id}/progress       # Update watch progress
DELETE /api/stream/{id}/stop           # Stop streaming session

GET    /api/profiles                   # List profiles
POST   /api/profiles                   # Create profile
GET    /api/profiles/{id}/watchlist    # Get watchlist
```

### SignalR Hubs
```csharp
public class NotificationHub : Hub
{
    // Real-time notifications
    Task SendLibraryScanProgress(int percentage);
    Task SendTranscodingProgress(string sessionId, int percentage);
    Task SendNewContentAvailable(ContentDto content);
}
```

## Security

1. **API Key authentication** for external services
2. **Profile-based authorization** for multi-user
3. **Rate limiting** on all endpoints
4. **Input validation** with FluentValidation
5. **CORS** configuration for web clients
6. **HTTPS** enforcement
7. **Content Security Policy** headers

## Configuration

```json
{
  "Lanflix": {
    "MediaPaths": {
      "Movies": "D:/Media/Movies",
      "Series": "D:/Media/Series"
    },
    "Transcoding": {
      "EnableHardwareAcceleration": true,
      "PreferredHwAccel": "nvenc",
      "MaxConcurrentTranscodes": 2,
      "TempPath": "D:/Temp/Transcoding"
    },
    "Streaming": {
      "EnableDirectPlay": true,
      "EnableDirectStream": true,
      "DefaultBitrate": 8000000,
      "HlsSegmentDuration": 6
    },
    "Cache": {
      "Redis": {
        "Enabled": true,
        "ConnectionString": "localhost:6379"
      },
      "Memory": {
        "SizeLimit": 512
      }
    }
  }
}
```

## Deployment

### Single Binary
```bash
# Publish as single file
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

# Result: lanflix-server.exe (includes runtime)
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
# Install FFmpeg
RUN apt-get update && apt-get install -y ffmpeg
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lanflix.Server.dll"]
```

## Migration Strategy

1. **Database migration tool** to import from old backend
2. **API compatibility layer** for existing clients
3. **Gradual rollout** with feature flags
4. **Performance benchmarking** against old backend

## Next Steps

1. ✅ Set up project structure
2. ⬜ Implement Domain layer (entities, value objects)
3. ⬜ Implement Application layer (CQRS, MediatR)
4. ⬜ Implement Infrastructure (EF Core, FFmpeg service)
5. ⬜ Implement WebApi (controllers, middleware)
6. ⬜ Add comprehensive tests
7. ⬜ Performance testing and optimization
8. ⬜ Documentation and deployment guides
