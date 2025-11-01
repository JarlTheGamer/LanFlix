# Lanflix C# Implementation Roadmap

## Phase 1: Foundation (Week 1)

### 1.1 Project Structure Setup
- [x] Create solution with Clean Architecture layers
- [ ] Set up Domain project (no dependencies)
- [ ] Set up Application project (depends on Domain)
- [ ] Set up Infrastructure project (depends on Application)
- [ ] Set up WebApi project (depends on all)
- [ ] Configure project references and dependencies

### 1.2 Core Domain Entities
```csharp
// Domain/Entities/Content.cs
public class Content : BaseEntity
{
    public int TmdbId { get; set; }
    public ContentType Type { get; set; }
    public string Title { get; set; }
    public string FilePath { get; set; }
    public MediaInfo MediaInfo { get; set; }
    // ... other properties
}

// Domain/ValueObjects/MediaInfo.cs
public record MediaInfo
{
    public VideoStream Video { get; init; }
    public List<AudioStream> Audio { get; init; }
    public List<SubtitleStream> Subtitles { get; init; }
    public TimeSpan Duration { get; init; }
    public long FileSize { get; init; }
}
```

### 1.3 Database Context
```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Content> Contents { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<WatchHistory> WatchHistory { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Indexes for performance
        modelBuilder.Entity<Content>()
            .HasIndex(c => c.TmdbId)
            .IsUnique();
            
        modelBuilder.Entity<Content>()
            .HasIndex(c => c.Type);
    }
}
```

## Phase 2: FFmpeg Integration (Week 1-2)

### 2.1 Media Analysis Service
```csharp
// Infrastructure/Services/FFmpeg/MediaAnalyzer.cs
public class MediaAnalyzer : IMediaAnalyzer
{
    public async Task<MediaInfo> AnalyzeAsync(string filePath)
    {
        // Use FFprobe to extract media information
        var mediaInfo = await FFmpeg.GetMediaInfo(filePath);
        
        return new MediaInfo
        {
            Video = ExtractVideoStream(mediaInfo),
            Audio = ExtractAudioStreams(mediaInfo),
            Subtitles = ExtractSubtitleStreams(mediaInfo),
            Duration = mediaInfo.Duration,
            FileSize = new FileInfo(filePath).Length
        };
    }
    
    private VideoStream ExtractVideoStream(IMediaInfo mediaInfo)
    {
        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        return new VideoStream
        {
            Codec = videoStream.Codec,
            Width = videoStream.Width,
            Height = videoStream.Height,
            Bitrate = videoStream.Bitrate,
            FrameRate = videoStream.Framerate,
            PixelFormat = videoStream.PixelFormat,
            ColorSpace = videoStream.ColorSpace,
            HDR = DetectHDR(videoStream)
        };
    }
}
```

### 2.2 Hardware Acceleration Detection
```csharp
// Infrastructure/Services/FFmpeg/HardwareAccelerationDetector.cs
public class HardwareAccelerationDetector
{
    public async Task<HardwareAcceleration> DetectAvailableAccelerationAsync()
    {
        var available = new List<HwAccelType>();
        
        // Test NVIDIA NVENC
        if (await TestHwAccel("h264_nvenc"))
            available.Add(HwAccelType.Nvenc);
            
        // Test Intel QuickSync
        if (await TestHwAccel("h264_qsv"))
            available.Add(HwAccelType.QuickSync);
            
        // Test AMD AMF
        if (await TestHwAccel("h264_amf"))
            available.Add(HwAccelType.Amf);
            
        // Test VAAPI (Linux)
        if (await TestHwAccel("h264_vaapi"))
            available.Add(HwAccelType.Vaapi);
            
        return new HardwareAcceleration
        {
            Available = available,
            Preferred = DeterminePreferred(available)
        };
    }
}
```

### 2.3 Transcoding Pipeline
```csharp
// Infrastructure/Services/FFmpeg/TranscodingPipeline.cs
public class TranscodingPipeline
{
    private readonly ILogger<TranscodingPipeline> _logger;
    private readonly HardwareAcceleration _hwAccel;
    
    public async IAsyncEnumerable<ReadOnlyMemory<byte>> TranscodeAsync(
        TranscodeRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var conversion = BuildConversion(request);
        
        using var process = await conversion.Start(ct);
        var outputStream = process.StandardOutput.BaseStream;
        
        var buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB chunks
        try
        {
            int bytesRead;
            while ((bytesRead = await outputStream.ReadAsync(buffer, ct)) > 0)
            {
                yield return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    private IConversion BuildConversion(TranscodeRequest request)
    {
        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i \"{request.InputPath}\"");
            
        // Add hardware acceleration if available
        if (_hwAccel.Preferred != HwAccelType.None)
        {
            conversion.AddParameter(GetHwAccelParams(_hwAccel.Preferred));
        }
        
        // Video encoding
        if (request.TranscodeVideo)
        {
            conversion
                .SetVideoCodec(GetVideoCodec(request.TargetCodec, _hwAccel.Preferred))
                .SetVideoBitrate(request.VideoBitrate)
                .SetSize(request.Resolution);
        }
        else
        {
            conversion.AddParameter("-c:v copy");
        }
        
        // Audio encoding
        if (request.TranscodeAudio)
        {
            conversion
                .SetAudioCodec(request.AudioCodec)
                .SetAudioBitrate(request.AudioBitrate);
        }
        else
        {
            conversion.AddParameter("-c:a copy");
        }
        
        // Output format
        conversion
            .SetOutputFormat(request.OutputFormat)
            .SetOutput("pipe:1"); // Stream to stdout
            
        return conversion;
    }
}
```

## Phase 3: Streaming Strategies (Week 2)

### 3.1 Strategy Pattern Implementation
```csharp
// Application/Features/Streaming/Strategies/IStreamingStrategy.cs
public interface IStreamingStrategy
{
    StreamingMode Mode { get; }
    bool CanHandle(PlaybackInfo playbackInfo, ClientCapabilities client);
    Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken ct);
}

// Application/Features/Streaming/Strategies/DirectPlayStrategy.cs
public class DirectPlayStrategy : IStreamingStrategy
{
    public StreamingMode Mode => StreamingMode.DirectPlay;
    
    public bool CanHandle(PlaybackInfo playbackInfo, ClientCapabilities client)
    {
        // Check if client supports all codecs
        return client.SupportsVideoCodec(playbackInfo.VideoCodec) &&
               client.SupportsAudioCodec(playbackInfo.AudioCodec) &&
               client.SupportsContainer(playbackInfo.Container);
    }
    
    public async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken ct)
    {
        var fileStream = new FileStream(
            request.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
            
        return new StreamResult
        {
            Stream = fileStream,
            ContentType = GetContentType(request.FilePath),
            SupportsRangeRequests = true,
            TotalBytes = new FileInfo(request.FilePath).Length
        };
    }
}

// Application/Features/Streaming/Strategies/TranscodeStrategy.cs
public class FullTranscodeStrategy : IStreamingStrategy
{
    private readonly TranscodingPipeline _pipeline;
    private readonly ITranscodingSessionManager _sessionManager;
    
    public StreamingMode Mode => StreamingMode.Transcode;
    
    public bool CanHandle(PlaybackInfo playbackInfo, ClientCapabilities client)
    {
        // Always can handle as fallback
        return true;
    }
    
    public async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken ct)
    {
        var session = await _sessionManager.CreateSessionAsync(request);
        
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            TranscodeVideo = !request.Client.SupportsVideoCodec(request.VideoCodec),
            TranscodeAudio = !request.Client.SupportsAudioCodec(request.AudioCodec),
            VideoBitrate = DetermineBitrate(request.Client),
            Resolution = DetermineResolution(request.Client),
            OutputFormat = "mpegts" // For streaming
        };
        
        var stream = _pipeline.TranscodeAsync(transcodeRequest, ct);
        
        return new StreamResult
        {
            Stream = new TranscodingStream(stream),
            ContentType = "video/mp2t",
            SupportsRangeRequests = false,
            SessionId = session.Id
        };
    }
}
```

### 3.2 Strategy Selector
```csharp
// Application/Features/Streaming/Services/StreamingStrategySelector.cs
public class StreamingStrategySelector
{
    private readonly IEnumerable<IStreamingStrategy> _strategies;
    
    public IStreamingStrategy SelectStrategy(
        PlaybackInfo playbackInfo,
        ClientCapabilities client,
        UserPreferences preferences)
    {
        // Priority order based on preferences
        var orderedStrategies = preferences.PreferDirectPlay
            ? _strategies.OrderBy(s => s.Mode)
            : _strategies.OrderByDescending(s => s.Mode);
            
        return orderedStrategies.FirstOrDefault(s => s.CanHandle(playbackInfo, client))
            ?? _strategies.First(s => s.Mode == StreamingMode.Transcode);
    }
}
```

## Phase 4: CQRS with MediatR (Week 2-3)

### 4.1 Query Example
```csharp
// Application/Features/Library/Queries/GetLibraryItems/GetLibraryItemsQuery.cs
public record GetLibraryItemsQuery : IRequest<PaginatedList<ContentDto>>
{
    public ContentType? Type { get; init; }
    public string? Genre { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

// Handler
public class GetLibraryItemsQueryHandler 
    : IRequestHandler<GetLibraryItemsQuery, PaginatedList<ContentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    
    public async Task<PaginatedList<ContentDto>> Handle(
        GetLibraryItemsQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"library:{request.Type}:{request.PageNumber}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var query = _context.Contents.AsQueryable();
            
            if (request.Type.HasValue)
                query = query.Where(c => c.Type == request.Type.Value);
                
            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(c => EF.Functions.Like(c.Title, $"%{request.SearchTerm}%"));
                
            return await query
                .OrderByDescending(c => c.AddedAt)
                .ProjectTo<ContentDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize, ct);
        }, TimeSpan.FromMinutes(5));
    }
}
```

### 4.2 Command Example
```csharp
// Application/Features/Streaming/Commands/StartStream/StartStreamCommand.cs
public record StartStreamCommand : IRequest<StreamSessionDto>
{
    public int ContentId { get; init; }
    public int ProfileId { get; init; }
    public ClientCapabilities Client { get; init; }
    public long? StartPositionTicks { get; init; }
}

// Handler
public class StartStreamCommandHandler 
    : IRequestHandler<StartStreamCommand, StreamSessionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly StreamingStrategySelector _strategySelector;
    private readonly ITranscodingSessionManager _sessionManager;
    
    public async Task<StreamSessionDto> Handle(
        StartStreamCommand request,
        CancellationToken ct)
    {
        var content = await _context.Contents
            .Include(c => c.MediaInfo)
            .FirstOrDefaultAsync(c => c.Id == request.ContentId, ct)
            ?? throw new NotFoundException(nameof(Content), request.ContentId);
            
        var playbackInfo = new PlaybackInfo
        {
            VideoCodec = content.MediaInfo.Video.Codec,
            AudioCodec = content.MediaInfo.Audio.First().Codec,
            Container = Path.GetExtension(content.FilePath)
        };
        
        var strategy = _strategySelector.SelectStrategy(
            playbackInfo,
            request.Client,
            await GetUserPreferences(request.ProfileId, ct));
            
        var session = await _sessionManager.CreateSessionAsync(new StreamSession
        {
            ContentId = content.Id,
            ProfileId = request.ProfileId,
            Strategy = strategy.Mode,
            StartedAt = DateTime.UtcNow
        }, ct);
        
        return new StreamSessionDto
        {
            SessionId = session.Id,
            StreamingMode = strategy.Mode,
            StreamUrl = GenerateStreamUrl(session.Id, strategy.Mode)
        };
    }
}
```

## Phase 5: API Controllers (Week 3)

### 5.1 Streaming Controller
```csharp
// WebApi/Controllers/StreamingController.cs
[ApiController]
[Route("api/stream")]
public class StreamingController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost("{contentId}/start")]
    public async Task<ActionResult<StreamSessionDto>> StartStream(
        int contentId,
        [FromBody] StartStreamRequest request)
    {
        var command = new StartStreamCommand
        {
            ContentId = contentId,
            ProfileId = request.ProfileId,
            Client = request.ClientCapabilities
        };
        
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    [HttpGet("{sessionId}/stream")]
    public async Task Stream(string sessionId)
    {
        var query = new GetStreamQuery { SessionId = sessionId };
        var streamResult = await _mediator.Send(query);
        
        Response.ContentType = streamResult.ContentType;
        Response.Headers.Append("Accept-Ranges", "bytes");
        
        if (streamResult.SupportsRangeRequests && Request.Headers.Range.Any())
        {
            await HandleRangeRequest(streamResult);
        }
        else
        {
            await streamResult.Stream.CopyToAsync(Response.Body);
        }
    }
    
    private async Task HandleRangeRequest(StreamResult streamResult)
    {
        var range = Request.Headers.Range.First();
        var ranges = range.Ranges.First();
        
        var start = ranges.From ?? 0;
        var end = ranges.To ?? streamResult.TotalBytes - 1;
        
        Response.StatusCode = 206; // Partial Content
        Response.Headers.Append("Content-Range", 
            $"bytes {start}-{end}/{streamResult.TotalBytes}");
        Response.ContentLength = end - start + 1;
        
        streamResult.Stream.Seek(start, SeekOrigin.Begin);
        await streamResult.Stream.CopyToAsync(Response.Body);
    }
}
```

## Phase 6: Performance Optimization (Week 3-4)

### 6.1 Response Caching
```csharp
// WebApi/Extensions/CachingExtensions.cs
public static class CachingExtensions
{
    public static IServiceCollection AddResponseCaching(this IServiceCollection services)
    {
        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 1024 * 1024; // 1MB
            options.UseCaseSensitivePaths = false;
        });
        
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("api"));
                
            options.AddPolicy("library", builder => builder
                .Expire(TimeSpan.FromMinutes(10))
                .Tag("library"));
        });
        
        return services;
    }
}
```

### 6.2 Database Query Optimization
```csharp
// Infrastructure/Persistence/Configurations/ContentConfiguration.cs
public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.HasKey(c => c.Id);
        
        // Indexes for common queries
        builder.HasIndex(c => c.TmdbId).IsUnique();
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.AddedAt);
        builder.HasIndex(c => new { c.Type, c.AddedAt });
        
        // Value object mapping
        builder.OwnsOne(c => c.MediaInfo, mi =>
        {
            mi.ToJson(); // Store as JSON column
        });
        
        // Query filters
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
```

## Phase 7: Testing (Week 4)

### 7.1 Unit Tests
```csharp
// Tests/Application.Tests/Features/Streaming/StreamingStrategyTests.cs
public class DirectPlayStrategyTests
{
    [Fact]
    public void CanHandle_WhenClientSupportsAllCodecs_ReturnsTrue()
    {
        // Arrange
        var strategy = new DirectPlayStrategy();
        var playbackInfo = new PlaybackInfo
        {
            VideoCodec = "h264",
            AudioCodec = "aac",
            Container = ".mp4"
        };
        var client = new ClientCapabilities
        {
            SupportedVideoCodecs = new[] { "h264", "hevc" },
            SupportedAudioCodecs = new[] { "aac", "mp3" },
            SupportedContainers = new[] { ".mp4", ".mkv" }
        };
        
        // Act
        var result = strategy.CanHandle(playbackInfo, client);
        
        // Assert
        result.Should().BeTrue();
    }
}
```

### 7.2 Integration Tests
```csharp
// Tests/WebApi.Tests/Controllers/StreamingControllerTests.cs
public class StreamingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    [Fact]
    public async Task StartStream_WithValidRequest_ReturnsStreamSession()
    {
        // Arrange
        var request = new StartStreamRequest
        {
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities { /* ... */ }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/stream/1/start", request);
        
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

## Success Metrics

- [ ] **Performance**: Stream startup < 500ms
- [ ] **Throughput**: Handle 10+ concurrent streams
- [ ] **Memory**: < 200MB base memory usage
- [ ] **CPU**: < 5% idle CPU usage
- [ ] **Latency**: API response time < 100ms (p95)
- [ ] **Reliability**: 99.9% uptime
- [ ] **Test Coverage**: > 80% code coverage

## Timeline Summary

- **Week 1**: Foundation + FFmpeg Integration
- **Week 2**: Streaming Strategies + CQRS
- **Week 3**: API Controllers + Optimization
- **Week 4**: Testing + Documentation
- **Week 5**: Performance tuning + Deployment

Total: **4-5 weeks** to production-ready v1.0
