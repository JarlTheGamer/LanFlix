# Design Document

## Overview

This document details the design for migrating the Lanflix media streaming server from Node.js/TypeScript to C# ASP.NET Core 9.0. The design follows Clean Architecture principles with CQRS pattern, ensuring high performance, maintainability, and scalability comparable to Jellyfin.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Applications                      │
│              (Web, Android, iOS, Desktop)                    │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP/HTTPS, WebSocket
┌────────────────────▼────────────────────────────────────────┐
│                    WebApi Layer (ASP.NET Core)               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Controllers  │  │ SignalR Hubs │  │  Middleware  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────┬────────────────────────────────────────┘
                     │ MediatR Commands/Queries
┌────────────────────▼────────────────────────────────────────┐
│                   Application Layer                          │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Features (Vertical Slices)                          │   │
│  │  • Library  • Streaming  • Profiles  • Metadata     │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ EF Core  │  │  FFmpeg  │  │  Redis   │  │  TMDB    │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers


**1. Domain Layer** (No dependencies)
- Core business entities (Content, Profile, WatchHistory, StreamSession)
- Value objects (MediaInfo, VideoCodec, AudioCodec)
- Domain interfaces
- Business rules and validation

**2. Application Layer** (Depends on Domain)
- CQRS commands and queries using MediatR
- Feature-based organization (vertical slices)
- Application services and interfaces
- DTOs and mapping profiles
- Validation behaviors
- Caching strategies

**3. Infrastructure Layer** (Depends on Application)
- EF Core database context and repositories
- FFmpeg integration and transcoding pipeline
- External API clients (TMDB, Prowlarr, Radarr)
- Redis caching implementation
- File system operations
- Background jobs (Hangfire)

**4. WebApi Layer** (Depends on all)
- REST API controllers
- SignalR hubs for real-time communication
- Middleware (error handling, logging, authentication)
- API filters and attributes
- Dependency injection configuration

## Components and Interfaces

### 1. Migration Tool Component

**Purpose**: Migrate data from Legacy Backend SQLite database to New Backend

**Key Classes**:


```csharp
// Infrastructure/Migration/LegacyDatabaseReader.cs
public class LegacyDatabaseReader
{
    private readonly string _legacyDbPath;
    
    public async Task<LegacyData> ReadAllDataAsync()
    {
        // Read from old SQLite database using Dapper
        // Returns: Content, Profiles, WatchHistory, Settings, etc.
    }
}

// Infrastructure/Migration/DataTransformer.cs
public class DataTransformer
{
    public Content TransformContent(LegacyContent legacy);
    public Profile TransformProfile(LegacyProfile legacy);
    public WatchHistory TransformWatchHistory(LegacyWatchHistory legacy);
}

// Infrastructure/Migration/MigrationOrchestrator.cs
public class MigrationOrchestrator
{
    public async Task<MigrationResult> ExecuteMigrationAsync(
        MigrationOptions options,
        IProgress<MigrationProgress> progress,
        CancellationToken ct);
}
```

**Migration Process Flow**:
1. Validate Legacy Backend database accessibility
2. Read all data from Legacy Backend
3. Transform data to New Backend schema
4. Validate transformed data
5. Write to New Backend database in transaction
6. Verify data integrity
7. Generate migration report

### 2. Streaming Strategy Component

**Purpose**: Implement 4 streaming modes with automatic selection

**Strategy Pattern Implementation**:


```csharp
// Application/Features/Streaming/Strategies/IStreamingStrategy.cs
public interface IStreamingStrategy
{
    StreamingMode Mode { get; }
    int Priority { get; } // Lower = higher priority
    bool CanHandle(MediaInfo media, ClientCapabilities client);
    Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken ct);
}

// Strategy implementations with priority order:
// 1. DirectPlayStrategy (Priority: 1) - Zero transcoding
// 2. DirectStreamStrategy (Priority: 2) - Container remux only
// 3. TranscodeVideoStrategy (Priority: 3) - Video transcode, audio copy
// 4. FullTranscodeStrategy (Priority: 4) - Full transcode (fallback)

// Application/Features/Streaming/Services/StreamingStrategySelector.cs
public class StreamingStrategySelector
{
    private readonly IEnumerable<IStreamingStrategy> _strategies;
    
    public IStreamingStrategy SelectOptimalStrategy(
        MediaInfo media,
        ClientCapabilities client,
        UserPreferences preferences)
    {
        return _strategies
            .Where(s => s.CanHandle(media, client))
            .OrderBy(s => s.Priority)
            .FirstOrDefault() ?? _fallbackStrategy;
    }
}
```

**Client Capabilities Detection**:
```csharp
public record ClientCapabilities
{
    public string[] SupportedVideoCodecs { get; init; } // h264, hevc, vp9, av1
    public string[] SupportedAudioCodecs { get; init; } // aac, mp3, opus, ac3
    public string[] SupportedContainers { get; init; }  // mp4, mkv, webm
    public int MaxBitrate { get; init; }                // bps
    public VideoResolution MaxResolution { get; init; } // 1080p, 4K, etc.
    public bool SupportsHDR { get; init; }
}
```

### 3. FFmpeg Integration Component

**Purpose**: Handle media analysis and transcoding with hardware acceleration

**Key Classes**:


```csharp
// Infrastructure/Services/FFmpeg/MediaAnalyzer.cs
public class MediaAnalyzer : IMediaAnalyzer
{
    public async Task<MediaInfo> AnalyzeAsync(string filePath)
    {
        // Use FFprobe to extract:
        // - Video streams (codec, resolution, bitrate, HDR)
        // - Audio streams (codec, channels, language)
        // - Subtitle streams (format, language)
        // - Container format and duration
    }
}

// Infrastructure/Services/FFmpeg/HardwareAccelerationDetector.cs
public class HardwareAccelerationDetector
{
    public async Task<HwAccelCapabilities> DetectAsync()
    {
        // Test availability of:
        // - NVIDIA NVENC (h264_nvenc, hevc_nvenc)
        // - Intel QuickSync (h264_qsv, hevc_qsv)
        // - AMD AMF (h264_amf, hevc_amf)
        // - VAAPI (Linux) (h264_vaapi)
        // Returns preferred acceleration method
    }
}

// Infrastructure/Services/FFmpeg/TranscodingPipeline.cs
public class TranscodingPipeline
{
    private readonly ObjectPool<Process> _processPool;
    private readonly HwAccelCapabilities _hwAccel;
    
    public async IAsyncEnumerable<ReadOnlyMemory<byte>> StreamAsync(
        TranscodeRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Build FFmpeg command with hardware acceleration
        // Stream output in 80KB chunks using ArrayPool
        // Handle backpressure from slow clients
        // Cleanup on cancellation
    }
}
```

**FFmpeg Command Building**:
```csharp
// Example for NVENC hardware acceleration
ffmpeg -hwaccel cuda -hwaccel_output_format cuda 
       -i input.mkv 
       -c:v h264_nvenc -preset p4 -b:v 8M 
       -c:a copy 
       -f mpegts pipe:1
```

### 4. Caching Component

**Purpose**: Multi-tier caching for optimal performance

**Caching Strategy**:


```csharp
// Application/Common/Interfaces/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);
}

// Infrastructure/Services/Caching/HybridCacheService.cs
public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _l1Cache;      // Hot data, fast access
    private readonly IDistributedCache _l2Cache; // Redis, shared across instances
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        // Try L1 (memory) first
        if (_l1Cache.TryGetValue(key, out T? value))
            return value;
            
        // Try L2 (Redis) second
        var bytes = await _l2Cache.GetAsync(key, ct);
        if (bytes != null)
        {
            value = Deserialize<T>(bytes);
            _l1Cache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
        
        return default;
    }
}
```

**Cache Key Strategy**:
- Library items: `library:{type}:{page}`
- Content details: `content:{id}`
- Metadata: `metadata:{tmdbId}`
- Stream sessions: `session:{sessionId}`
- User preferences: `profile:{id}:prefs`

### 5. Android App Update Component

**Purpose**: Provide OTA updates for Android app

**API Endpoints**:


```csharp
// WebApi/Controllers/AppUpdateController.cs
[ApiController]
[Route("api/app-updates")]
public class AppUpdateController : ControllerBase
{
    // GET /api/app-updates/android/latest
    [HttpGet("android/latest")]
    public async Task<ActionResult<AppUpdateInfo>> GetLatestAndroidVersion(
        [FromQuery] string currentVersion,
        [FromQuery] string architecture) // arm64-v8a, armeabi-v7a, x86_64
    {
        return new AppUpdateInfo
        {
            Version = "2.0.0",
            VersionCode = 20,
            ReleaseDate = DateTime.UtcNow,
            DownloadUrl = "/api/app-updates/android/download/2.0.0",
            FileSize = 45_000_000, // bytes
            Sha256Checksum = "abc123...",
            ReleaseNotes = "Bug fixes and performance improvements",
            IsForceUpdate = false,
            MinimumSupportedVersion = "1.5.0"
        };
    }
    
    // GET /api/app-updates/android/download/{version}
    [HttpGet("android/download/{version}")]
    public async Task<IActionResult> DownloadApk(string version)
    {
        var apkPath = Path.Combine(_apkStoragePath, $"lanflix-{version}.apk");
        return PhysicalFile(apkPath, "application/vnd.android.package-archive");
    }
    
    // POST /api/app-updates/android/upload (Admin only)
    [HttpPost("android/upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadApk(IFormFile apkFile, [FromForm] AppReleaseInfo info)
    {
        // Validate APK signature
        // Calculate SHA-256 checksum
        // Store APK file
        // Update version database
    }
}
```

### 6. SignalR Real-time Communication

**Purpose**: Push notifications and progress updates to clients

**Hub Implementation**:


```csharp
// WebApi/Hubs/NotificationHub.cs
public class NotificationHub : Hub
{
    public async Task SubscribeToLibraryUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "library-updates");
    }
    
    public async Task SubscribeToTranscodingProgress(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }
}

// Usage from services:
public class LibraryScanService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public async Task ScanLibraryAsync(IProgress<int> progress)
    {
        // ... scanning logic ...
        
        await _hubContext.Clients.Group("library-updates")
            .SendAsync("LibraryScanProgress", new { Percentage = 45 });
    }
}
```

## Data Models

### Domain Entities

**Content Entity**:
```csharp
public class Content : BaseEntity
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public ContentType Type { get; set; } // Movie, Series
    public string Title { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string FilePath { get; set; }
    public MediaInfo MediaInfo { get; set; } // JSON column
    public DateTime? ReleaseDate { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public double? Rating { get; set; }
    public string[]? Genres { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<Episode> Episodes { get; set; }
    public ICollection<WatchHistory> WatchHistories { get; set; }
}
```

**MediaInfo Value Object**:


```csharp
public record MediaInfo
{
    public VideoStream Video { get; init; }
    public List<AudioStream> Audio { get; init; }
    public List<SubtitleStream> Subtitles { get; init; }
    public TimeSpan Duration { get; init; }
    public long FileSize { get; init; }
    public string Container { get; init; }
}

public record VideoStream
{
    public string Codec { get; init; }      // h264, hevc, vp9, av1
    public int Width { get; init; }
    public int Height { get; init; }
    public long Bitrate { get; init; }
    public double FrameRate { get; init; }
    public string PixelFormat { get; init; }
    public string? ColorSpace { get; init; }
    public bool IsHDR { get; init; }
}

public record AudioStream
{
    public int Index { get; init; }
    public string Codec { get; init; }      // aac, mp3, ac3, eac3, opus
    public int Channels { get; init; }
    public int SampleRate { get; init; }
    public long Bitrate { get; init; }
    public string? Language { get; init; }
    public string? Title { get; init; }
}
```

**Profile Entity**:
```csharp
public class Profile : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? AvatarPath { get; set; }
    public bool IsKidsProfile { get; set; }
    public UserPreferences Preferences { get; set; } // JSON column
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<WatchHistory> WatchHistories { get; set; }
    public ICollection<Watchlist> Watchlists { get; set; }
}
```

**WatchHistory Entity**:


```csharp
public class WatchHistory : BaseEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public int? EpisodeId { get; set; }
    public long PositionTicks { get; set; }  // 1 tick = 100 nanoseconds
    public bool IsCompleted { get; set; }
    public DateTime LastWatchedAt { get; set; }
    
    // Navigation properties
    public Profile Profile { get; set; }
    public Content Content { get; set; }
    public Episode? Episode { get; set; }
}
```

**StreamSession Entity**:
```csharp
public class StreamSession : BaseEntity
{
    public string Id { get; set; } // GUID
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public StreamingMode Mode { get; set; }
    public string? TranscodingProcessId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public Profile Profile { get; set; }
    public Content Content { get; set; }
}
```

### Database Schema Design

**EF Core Configuration**:
```csharp
// Infrastructure/Persistence/Configurations/ContentConfiguration.cs
public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.HasKey(c => c.Id);
        
        // Indexes for performance
        builder.HasIndex(c => c.TmdbId).IsUnique();
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.AddedAt);
        builder.HasIndex(c => new { c.Type, c.AddedAt });
        builder.HasIndex(c => c.Title); // For search
        
        // JSON column for MediaInfo
        builder.OwnsOne(c => c.MediaInfo, mi => mi.ToJson());
        
        // Soft delete query filter
        builder.HasQueryFilter(c => !c.IsDeleted);
        
        // Required fields
        builder.Property(c => c.Title).IsRequired().HasMaxLength(500);
        builder.Property(c => c.FilePath).IsRequired().HasMaxLength(1000);
    }
}
```

## Error Handling

### Exception Hierarchy



```csharp
// Application/Common/Exceptions/
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
}

public class NotFoundException : ApplicationException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found") { }
}

public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }
    
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }
}

public class TranscodingException : ApplicationException
{
    public string? FFmpegOutput { get; }
    
    public TranscodingException(string message, string? ffmpegOutput = null)
        : base(message)
    {
        FFmpegOutput = ffmpegOutput;
    }
}
```

### Global Exception Middleware

```csharp
// WebApi/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (TranscodingException ex)
        {
            _logger.LogError(ex, "Transcoding failed: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError);
        }
    }
}
```

## Testing Strategy

### Unit Tests

**Test Structure**:


```csharp
// Tests/Application.Tests/Features/Streaming/StreamingStrategyTests.cs
public class DirectPlayStrategyTests
{
    [Fact]
    public void CanHandle_WhenAllCodecsSupported_ReturnsTrue()
    {
        // Arrange
        var strategy = new DirectPlayStrategy();
        var media = CreateMediaInfo(videoCodec: "h264", audioCodec: "aac");
        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" });
        
        // Act
        var result = strategy.CanHandle(media, client);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("hevc", "aac", false)] // Video codec not supported
    [InlineData("h264", "opus", false)] // Audio codec not supported
    public void CanHandle_WhenCodecsNotSupported_ReturnsFalse(
        string videoCodec, string audioCodec, bool expected)
    {
        // Test implementation
    }
}
```

### Integration Tests

```csharp
// Tests/WebApi.Tests/Controllers/StreamingControllerTests.cs
public class StreamingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    
    [Fact]
    public async Task StartStream_WithValidRequest_ReturnsStreamSession()
    {
        // Arrange
        var request = new StartStreamRequest
        {
            ContentId = 1,
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities { /* ... */ }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/stream/start", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<StreamSessionDto>();
        session.Should().NotBeNull();
        session.StreamingMode.Should().BeOneOf(
            StreamingMode.DirectPlay,
            StreamingMode.DirectStream,
            StreamingMode.Transcode);
    }
}
```

### Performance Tests



```csharp
// Tests/Performance.Tests/StreamingPerformanceTests.cs
public class StreamingPerformanceTests
{
    [Fact]
    public async Task ConcurrentStreams_With10Clients_MaintainsPerformance()
    {
        // Arrange
        var clients = Enumerable.Range(1, 10)
            .Select(_ => CreateTestClient())
            .ToList();
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = clients.Select(c => c.StartStreamAsync());
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds
        
        // Verify all streams are active
        foreach (var client in clients)
        {
            var status = await client.GetStreamStatusAsync();
            status.IsActive.Should().BeTrue();
        }
    }
    
    [Fact]
    public async Task StreamStartup_CompletesWithin500ms()
    {
        // Test stream startup time
    }
}
```

## Performance Optimizations

### 1. Memory Management

**ArrayPool Usage**:
```csharp
public class OptimizedStreamReader
{
    public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAsync(
        Stream source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
            {
                yield return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
```

### 2. Database Query Optimization

**Compiled Queries**:


```csharp
// Infrastructure/Persistence/CompiledQueries.cs
public static class CompiledQueries
{
    private static readonly Func<ApplicationDbContext, int, Task<Content?>> 
        GetContentByIdQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) =>
                context.Contents
                    .Include(c => c.Episodes)
                    .FirstOrDefault(c => c.Id == id));
    
    public static Task<Content?> GetContentByIdAsync(
        this ApplicationDbContext context, int id)
    {
        return GetContentByIdQuery(context, id);
    }
}
```

### 3. Response Caching

**Output Cache Configuration**:
```csharp
// WebApi/Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .Tag("api"));
    
    options.AddPolicy("library", builder => builder
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("library")
        .SetVaryByQuery("type", "page", "search"));
    
    options.AddPolicy("metadata", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .Tag("metadata"));
});

// Usage in controllers
[HttpGet]
[OutputCache(PolicyName = "library")]
public async Task<ActionResult<PaginatedList<ContentDto>>> GetLibraryItems(
    [FromQuery] GetLibraryItemsQuery query)
{
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### 4. Connection Pooling

**HTTP Client Factory**:
```csharp
// Infrastructure/DependencyInjection.cs
services.AddHttpClient<ITmdbClient, TmdbClient>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = 10
});
```

## Deployment Architecture

### Single Executable Deployment



```bash
# Publish as single file with trimming
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:PublishTrimmed=true \
  /p:TrimMode=partial

# Output: lanflix-server.exe (~80MB including runtime)
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Lanflix.Server.sln", "./"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["WebApi/WebApi.csproj", "WebApi/"]
RUN dotnet restore

COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lanflix.Server.dll"]
```

### Configuration Management

**appsettings.json Structure**:
```json
{
  "Lanflix": {
    "MediaPaths": {
      "Movies": "D:/Media/Movies",
      "Series": "D:/Media/Series",
      "PosterCache": "D:/Media/Cache/Posters",
      "BackdropCache": "D:/Media/Cache/Backdrops"
    },
    "Transcoding": {
      "EnableHardwareAcceleration": true,
      "PreferredHwAccel": "auto",
      "MaxConcurrentTranscodes": 2,
      "TempPath": "D:/Temp/Transcoding",
      "DefaultBitrate": 8000000,
      "HlsSegmentDuration": 6
    },
    "Streaming": {
      "EnableDirectPlay": true,
      "EnableDirectStream": true,
      "ChunkSize": 81920
    },
    "Cache": {
      "Redis": {
        "Enabled": true,
        "ConnectionString": "localhost:6379",
        "InstanceName": "lanflix:"
      },
      "Memory": {
        "SizeLimit": 512
      }
    },
    "ExternalApis": {
      "Tmdb": {
        "ApiKey": "",
        "BaseUrl": "https://api.themoviedb.org/3/"
      }
    },
    "AppUpdates": {
      "ApkStoragePath": "D:/AppUpdates/Android",
      "EnableAutoUpdate": true
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lanflix.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## Migration Execution Plan

### Phase 1: Pre-Migration

1. Backup Legacy Backend database
2. Validate Legacy Backend data integrity
3. Document current configuration
4. Test New Backend in isolated environment

### Phase 2: Data Migration



1. Run migration tool with dry-run mode
2. Review migration report
3. Execute actual migration
4. Verify data integrity
5. Compare record counts between databases

### Phase 3: Parallel Testing

1. Run both backends simultaneously on different ports
2. Compare API responses
3. Test streaming functionality
4. Validate performance metrics
5. Monitor error rates

### Phase 4: Cutover

1. Stop Legacy Backend
2. Update client configurations to point to New Backend
3. Monitor New Backend health
4. Keep Legacy Backend available for rollback (24-48 hours)

### Phase 5: Post-Migration

1. Monitor performance metrics
2. Collect user feedback
3. Address any issues
4. Decommission Legacy Backend after stability confirmed

## API Compatibility Mapping

### Endpoint Mapping

| Legacy Endpoint | New Endpoint | Notes |
|----------------|--------------|-------|
| GET /api/content | GET /api/library/items | Response format preserved |
| GET /api/content/:id | GET /api/library/items/:id | Response format preserved |
| POST /api/stream/start | POST /api/stream/:id/start | Parameter structure updated |
| GET /api/stream/:id | GET /api/stream/:id/stream | Streaming endpoint |
| GET /api/profiles | GET /api/profiles | Response format preserved |
| POST /api/profiles | POST /api/profiles | Request format preserved |
| GET /api/watchhistory/:profileId | GET /api/profiles/:id/history | Endpoint restructured |

### Response Format Compatibility

**Legacy Format**:
```json
{
  "success": true,
  "data": { /* content */ },
  "message": "Success"
}
```

**New Format (with compatibility layer)**:
```json
{
  "success": true,
  "data": { /* content */ },
  "message": "Success",
  "version": "2.0.0"
}
```

## Monitoring and Observability

### OpenTelemetry Integration



```csharp
// WebApi/Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("Lanflix.Streaming")
        .AddSource("Lanflix.Transcoding"))
    .WithMetrics(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Lanflix.Streaming")
        .AddMeter("Lanflix.Transcoding"));
```

### Custom Metrics

```csharp
// Infrastructure/Telemetry/StreamingMetrics.cs
public class StreamingMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _streamStartCounter;
    private readonly Histogram<double> _streamDuration;
    private readonly ObservableGauge<int> _activeStreams;
    
    public StreamingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Lanflix.Streaming");
        
        _streamStartCounter = _meter.CreateCounter<long>(
            "streams.started",
            description: "Number of streams started");
        
        _streamDuration = _meter.CreateHistogram<double>(
            "stream.duration",
            unit: "s",
            description: "Stream duration in seconds");
        
        _activeStreams = _meter.CreateObservableGauge<int>(
            "streams.active",
            () => GetActiveStreamCount(),
            description: "Number of currently active streams");
    }
}
```

### Health Checks

```csharp
// WebApi/Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis(configuration["Lanflix:Cache:Redis:ConnectionString"])
    .AddCheck<FFmpegHealthCheck>("ffmpeg")
    .AddCheck<DiskSpaceHealthCheck>("disk-space");

// Map health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## Security Considerations

### Authentication & Authorization



```csharp
// WebApi/Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
});
```

### Rate Limiting

```csharp
// WebApi/Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.AddPolicy("streaming", context =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueLimit = 0
            }));
});
```

### Input Validation

```csharp
// Application/Features/Library/Commands/AddContent/AddContentCommandValidator.cs
public class AddContentCommandValidator : AbstractValidator<AddContentCommand>
{
    public AddContentCommandValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty()
            .Must(BeValidFilePath)
            .WithMessage("Invalid file path");
        
        RuleFor(x => x.TmdbId)
            .GreaterThan(0)
            .WithMessage("TMDB ID must be positive");
    }
    
    private bool BeValidFilePath(string path)
    {
        return Path.IsPathFullyQualified(path) && 
               !path.Contains("..");  // Prevent directory traversal
    }
}
```

## Design Decisions and Rationale

### 1. Clean Architecture

**Decision**: Use Clean Architecture with CQRS pattern

**Rationale**:
- Clear separation of concerns
- Testability through dependency inversion
- Flexibility to change infrastructure without affecting business logic
- CQRS allows optimization of read and write operations separately

### 2. MediatR for CQRS

**Decision**: Use MediatR library for implementing CQRS

**Rationale**:
- Reduces coupling between controllers and business logic
- Pipeline behaviors for cross-cutting concerns (logging, validation, caching)
- Simplifies testing by isolating command/query handlers

### 3. Entity Framework Core + Dapper

**Decision**: Use EF Core for standard operations, Dapper for performance-critical queries

**Rationale**:
- EF Core provides excellent developer experience and migrations
- Dapper offers raw SQL performance when needed
- Best of both worlds approach

### 4. SignalR for Real-time Communication

**Decision**: Use SignalR instead of polling

**Rationale**:
- Efficient real-time updates without constant polling
- Automatic reconnection handling
- Scales well with backplane (Redis)

### 5. Strategy Pattern for Streaming

**Decision**: Implement streaming modes as strategies

**Rationale**:
- Easy to add new streaming modes
- Clear separation of concerns
- Testable in isolation
- Runtime selection based on capabilities

## Success Metrics

### Performance Targets

- Stream startup time: < 500ms (p95)
- API response time: < 100ms (p95)
- Concurrent streams: 10+ without degradation
- Memory usage (idle): < 200MB
- CPU usage (idle): < 5%
- Cache hit ratio: > 70%

### Migration Success Criteria

- 100% data migration accuracy
- Zero data loss
- API compatibility maintained
- Performance equal or better than Legacy Backend
- All existing features functional
- Successful rollback capability tested

## Future Enhancements

1. **Plugin System**: Allow third-party extensions
2. **Multi-server Clustering**: Horizontal scaling support
3. **Advanced Analytics**: User behavior tracking and recommendations
4. **Live TV Support**: DVR and live streaming capabilities
5. **Mobile Sync**: Offline download and sync for mobile devices
6. **Advanced Transcoding**: Tone mapping for HDR content
7. **Multi-language Support**: Internationalization of metadata
