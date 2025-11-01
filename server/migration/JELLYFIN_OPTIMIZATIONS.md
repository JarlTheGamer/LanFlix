# Jellyfin-Inspired Optimizations

## Performance Techniques from Jellyfin

### 1. Zero-Copy Streaming
```csharp
// Use Memory<T> and Span<T> for zero-allocation streaming
public async Task StreamFileAsync(string path, Stream output, CancellationToken ct)
{
    using var fileStream = new FileStream(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 81920, // 80KB buffer
        useAsync: true);
    
    // Use PipeReader for efficient streaming
    var reader = PipeReader.Create(fileStream);
    
    try
    {
        while (true)
        {
            var result = await reader.ReadAsync(ct);
            var buffer = result.Buffer;
            
            if (buffer.IsEmpty && result.IsCompleted)
                break;
            
            // Write directly without copying
            foreach (var segment in buffer)
            {
                await output.WriteAsync(segment, ct);
            }
            
            reader.AdvanceTo(buffer.End);
        }
    }
    finally
    {
        await reader.CompleteAsync();
    }
}
```

### 2. Efficient Metadata Caching
```csharp
// Jellyfin uses a multi-tier caching strategy
public class MetadataCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IApplicationDbContext _db;
    
    public async Task<ContentMetadata> GetMetadataAsync(int tmdbId)
    {
        // L1: Memory cache (hot data)
        if (_memoryCache.TryGetValue($"metadata:{tmdbId}", out ContentMetadata cached))
            return cached;
        
        // L2: Redis cache (warm data)
        var json = await _distributedCache.GetStringAsync($"metadata:{tmdbId}");
        if (json != null)
        {
            var metadata = JsonSerializer.Deserialize<ContentMetadata>(json);
            _memoryCache.Set($"metadata:{tmdbId}", metadata, TimeSpan.FromHours(1));
            return metadata;
        }
        
        // L3: Database (cold data)
        var dbMetadata = await _db.Contents
            .Where(c => c.TmdbId == tmdbId)
            .Select(c => new ContentMetadata { /* ... */ })
            .FirstOrDefaultAsync();
            
        if (dbMetadata != null)
        {
            await _distributedCache.SetStringAsync(
                $"metadata:{tmdbId}",
                JsonSerializer.Serialize(dbMetadata),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });
            _memoryCache.Set($"metadata:{tmdbId}", dbMetadata, TimeSpan.FromHours(1));
        }
        
        return dbMetadata;
    }
}
```

### 3. Smart Transcoding Session Management
```csharp
// Jellyfin's approach to managing transcoding sessions
public class TranscodingSessionManager
{
    private readonly ConcurrentDictionary<string, TranscodingSession> _sessions = new();
    private readonly Timer _cleanupTimer;
    
    public TranscodingSessionManager()
    {
        // Cleanup idle sessions every 30 seconds
        _cleanupTimer = new Timer(CleanupIdleSessions, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    public async Task<TranscodingSession> CreateSessionAsync(StreamRequest request)
    {
        var session = new TranscodingSession
        {
            Id = Guid.NewGuid().ToString(),
            ContentId = request.ContentId,
            ProfileId = request.ProfileId,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            Process = null // Will be set when transcoding starts
        };
        
        _sessions.TryAdd(session.Id, session);
        return session;
    }
    
    public void UpdateActivity(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActivityAt = DateTime.UtcNow;
        }
    }
    
    private void CleanupIdleSessions(object state)
    {
        var idleThreshold = DateTime.UtcNow.AddMinutes(-5);
        
        foreach (var (sessionId, session) in _sessions)
        {
            if (session.LastActivityAt < idleThreshold)
            {
                // Kill the FFmpeg process
                session.Process?.Kill();
                session.Process?.Dispose();
                
                // Remove from dictionary
                _sessions.TryRemove(sessionId, out _);
                
                _logger.LogInformation(
                    "Cleaned up idle transcoding session {SessionId}", sessionId);
            }
        }
    }
}
```

### 4. Hardware Acceleration Priority
```csharp
// Jellyfin's hardware acceleration detection and priority
public class HardwareAccelerationService
{
    private static readonly Dictionary<string, int> HwAccelPriority = new()
    {
        ["nvenc"] = 100,      // NVIDIA (best)
        ["qsv"] = 90,         // Intel QuickSync
        ["amf"] = 85,         // AMD
        ["videotoolbox"] = 80, // Apple
        ["vaapi"] = 70,       // Linux VAAPI
        ["none"] = 0          // Software fallback
    };
    
    public async Task<string> DetectBestHwAccelAsync()
    {
        var available = new List<(string name, int priority)>();
        
        // Test each hardware encoder
        foreach (var (name, priority) in HwAccelPriority.Where(x => x.Value > 0))
        {
            if (await TestEncoderAsync(GetEncoderName(name)))
            {
                available.Add((name, priority));
            }
        }
        
        // Return highest priority available
        return available
            .OrderByDescending(x => x.priority)
            .Select(x => x.name)
            .FirstOrDefault() ?? "none";
    }
    
    private async Task<bool> TestEncoderAsync(string encoder)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-f lavfi -i testsrc=duration=1:size=320x240:rate=1 " +
                               $"-c:v {encoder} -f null -",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private string GetEncoderName(string hwAccel) => hwAccel switch
    {
        "nvenc" => "h264_nvenc",
        "qsv" => "h264_qsv",
        "amf" => "h264_amf",
        "videotoolbox" => "h264_videotoolbox",
        "vaapi" => "h264_vaapi",
        _ => "libx264"
    };
}
```

### 5. Adaptive Bitrate Selection
```csharp
// Jellyfin's intelligent bitrate selection based on client and network
public class BitrateSelector
{
    public int SelectBitrate(ClientCapabilities client, NetworkConditions network)
    {
        // Start with client's max bitrate
        var maxBitrate = client.MaxBitrate ?? 20_000_000; // 20 Mbps default
        
        // Adjust based on resolution
        var resolutionBitrate = client.MaxResolution switch
        {
            (3840, 2160) => 25_000_000, // 4K
            (1920, 1080) => 10_000_000, // 1080p
            (1280, 720) => 5_000_000,   // 720p
            (854, 480) => 2_500_000,    // 480p
            _ => 1_500_000              // SD
        };
        
        // Adjust based on network conditions
        var networkBitrate = network.EstimatedBandwidth * 0.8; // 80% of bandwidth
        
        // Take the minimum of all constraints
        var selectedBitrate = Math.Min(
            Math.Min(maxBitrate, resolutionBitrate),
            (int)networkBitrate);
        
        // Ensure minimum quality
        return Math.Max(selectedBitrate, 500_000); // Min 500 Kbps
    }
}
```

### 6. Efficient Library Scanning
```csharp
// Jellyfin's incremental library scanning approach
public class LibraryScanner
{
    private readonly IApplicationDbContext _db;
    private readonly IMediaAnalyzer _analyzer;
    private readonly ILogger<LibraryScanner> _logger;
    
    public async Task ScanLibraryAsync(string libraryPath, IProgress<ScanProgress> progress)
    {
        // Get all files in library
        var files = Directory.EnumerateFiles(libraryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => IsVideoFile(f))
            .ToList();
        
        // Get existing content from database
        var existingContent = await _db.Contents
            .Where(c => c.FilePath.StartsWith(libraryPath))
            .ToDictionaryAsync(c => c.FilePath);
        
        var totalFiles = files.Count;
        var processedFiles = 0;
        
        // Process in batches for better performance
        await Parallel.ForEachAsync(
            files.Chunk(10),
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (batch, ct) =>
            {
                foreach (var file in batch)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        
                        // Skip if file hasn't changed
                        if (existingContent.TryGetValue(file, out var existing) &&
                            existing.FileModifiedAt == fileInfo.LastWriteTimeUtc)
                        {
                            processedFiles++;
                            continue;
                        }
                        
                        // Analyze media file
                        var mediaInfo = await _analyzer.AnalyzeAsync(file);
                        
                        // Extract metadata from filename/path
                        var metadata = ExtractMetadataFromPath(file);
                        
                        // Fetch from TMDB if possible
                        var tmdbData = await FetchTmdbDataAsync(metadata);
                        
                        // Create or update content
                        if (existing != null)
                        {
                            UpdateContent(existing, mediaInfo, tmdbData, fileInfo);
                        }
                        else
                        {
                            await CreateContentAsync(file, mediaInfo, tmdbData, fileInfo);
                        }
                        
                        processedFiles++;
                        progress?.Report(new ScanProgress
                        {
                            TotalFiles = totalFiles,
                            ProcessedFiles = processedFiles,
                            CurrentFile = file
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning file {File}", file);
                    }
                }
            });
        
        // Remove deleted files
        var deletedFiles = existingContent.Keys.Except(files).ToList();
        if (deletedFiles.Any())
        {
            _db.Contents.RemoveRange(
                existingContent.Where(x => deletedFiles.Contains(x.Key)).Select(x => x.Value));
            await _db.SaveChangesAsync();
        }
    }
}
```

### 7. Image Caching Strategy
```csharp
// Jellyfin's image caching and resizing
public class ImageCacheService
{
    private readonly string _cachePath;
    private readonly HttpClient _httpClient;
    
    public async Task<string> GetCachedImageAsync(
        string imageUrl,
        int? width = null,
        int? height = null)
    {
        var cacheKey = GenerateCacheKey(imageUrl, width, height);
        var cachedPath = Path.Combine(_cachePath, cacheKey);
        
        // Return cached if exists
        if (File.Exists(cachedPath))
            return cachedPath;
        
        // Download original
        var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
        
        // Resize if dimensions specified
        if (width.HasValue || height.HasValue)
        {
            imageBytes = await ResizeImageAsync(imageBytes, width, height);
        }
        
        // Save to cache
        Directory.CreateDirectory(Path.GetDirectoryName(cachedPath));
        await File.WriteAllBytesAsync(cachedPath, imageBytes);
        
        return cachedPath;
    }
    
    private async Task<byte[]> ResizeImageAsync(
        byte[] originalBytes,
        int? width,
        int? height)
    {
        using var image = await Image.LoadAsync(originalBytes);
        
        var options = new ResizeOptions
        {
            Size = new Size(width ?? 0, height ?? 0),
            Mode = ResizeMode.Max // Maintain aspect ratio
        };
        
        image.Mutate(x => x.Resize(options));
        
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 90 });
        return ms.ToArray();
    }
}
```

### 8. Subtitle Extraction and Conversion
```csharp
// Jellyfin's subtitle handling
public class SubtitleService
{
    public async Task<List<SubtitleTrack>> ExtractSubtitlesAsync(string videoPath)
    {
        var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
        var subtitles = new List<SubtitleTrack>();
        
        foreach (var subtitleStream in mediaInfo.SubtitleStreams)
        {
            var track = new SubtitleTrack
            {
                Index = subtitleStream.Index,
                Language = subtitleStream.Language,
                Codec = subtitleStream.Codec,
                IsForced = subtitleStream.Disposition?.Forced ?? false,
                IsDefault = subtitleStream.Disposition?.Default ?? false
            };
            
            // Extract to WebVTT for web playback
            if (subtitleStream.Codec != "webvtt")
            {
                track.WebVttPath = await ConvertToWebVttAsync(
                    videoPath,
                    subtitleStream.Index);
            }
            
            subtitles.Add(track);
        }
        
        return subtitles;
    }
    
    private async Task<string> ConvertToWebVttAsync(string videoPath, int streamIndex)
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Path.GetFileNameWithoutExtension(videoPath)}_{streamIndex}.vtt");
        
        await FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoPath}\"")
            .AddParameter($"-map 0:{streamIndex}")
            .AddParameter("-c:s webvtt")
            .SetOutput(outputPath)
            .Start();
        
        return outputPath;
    }
}
```

### 9. Playback Reporting
```csharp
// Jellyfin's playback progress tracking
public class PlaybackReporter
{
    private readonly IApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public async Task ReportProgressAsync(PlaybackProgress progress)
    {
        // Update watch history
        var history = await _db.WatchHistory
            .FirstOrDefaultAsync(h => 
                h.ContentId == progress.ContentId &&
                h.ProfileId == progress.ProfileId);
        
        if (history == null)
        {
            history = new WatchHistory
            {
                ContentId = progress.ContentId,
                ProfileId = progress.ProfileId
            };
            _db.WatchHistory.Add(history);
        }
        
        history.PositionTicks = progress.PositionTicks;
        history.LastPlayedAt = DateTime.UtcNow;
        
        // Mark as watched if > 90% complete
        var content = await _db.Contents.FindAsync(progress.ContentId);
        var percentComplete = (double)progress.PositionTicks / content.RuntimeTicks;
        history.IsWatched = percentComplete > 0.9;
        
        await _db.SaveChangesAsync();
        
        // Notify other clients
        await _hubContext.Clients
            .Group($"profile:{progress.ProfileId}")
            .SendAsync("PlaybackProgress", new
            {
                progress.ContentId,
                progress.PositionTicks,
                history.IsWatched
            });
    }
}
```

### 10. Trickplay Thumbnails (BIF Format)
```csharp
// Jellyfin's trickplay thumbnail generation
public class TrickplayService
{
    public async Task GenerateTrickplayAsync(string videoPath, int contentId)
    {
        var outputDir = Path.Combine(_trickplayPath, contentId.ToString());
        Directory.CreateDirectory(outputDir);
        
        // Generate thumbnails every 10 seconds
        await FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoPath}\"")
            .AddParameter("-vf fps=1/10,scale=320:-1") // 320px wide, maintain aspect
            .AddParameter($"-f image2 \"{outputDir}/thumb_%04d.jpg\"")
            .Start();
        
        // Create BIF file (Roku's Binary Index Format)
        await CreateBifFileAsync(outputDir, contentId);
    }
    
    private async Task CreateBifFileAsync(string thumbnailDir, int contentId)
    {
        var bifPath = Path.Combine(_trickplayPath, $"{contentId}.bif");
        var thumbnails = Directory.GetFiles(thumbnailDir, "*.jpg")
            .OrderBy(f => f)
            .ToList();
        
        using var bifStream = File.Create(bifPath);
        using var writer = new BinaryWriter(bifStream);
        
        // BIF header
        writer.Write(new byte[] { 0x89, 0x42, 0x49, 0x46, 0x0D, 0x0A, 0x1A, 0x0A });
        writer.Write(0); // Version
        writer.Write(thumbnails.Count);
        writer.Write(10000); // Timestamp multiplier (10 seconds)
        
        // Write thumbnail data
        var offset = 64 + (thumbnails.Count * 8); // Header + index
        foreach (var thumbnail in thumbnails)
        {
            writer.Write(offset);
            var bytes = await File.ReadAllBytesAsync(thumbnail);
            offset += bytes.Length;
        }
        
        // Write thumbnail images
        foreach (var thumbnail in thumbnails)
        {
            var bytes = await File.ReadAllBytesAsync(thumbnail);
            writer.Write(bytes);
        }
    }
}
```

## Key Takeaways

1. **Use System.IO.Pipelines** for efficient streaming
2. **Implement multi-tier caching** (Memory → Redis → Database)
3. **Manage transcoding sessions** with automatic cleanup
4. **Detect and prioritize hardware acceleration**
5. **Select bitrate adaptively** based on client and network
6. **Scan libraries incrementally** to avoid full rescans
7. **Cache and resize images** on-demand
8. **Extract and convert subtitles** to WebVTT
9. **Track playback progress** in real-time
10. **Generate trickplay thumbnails** for better seeking

These optimizations will make Lanflix competitive with Jellyfin in terms of performance and features!
