# Jellyfin Streaming Architecture - Deep Dive

## Overview

Jellyfin uses a sophisticated multi-stage streaming pipeline that intelligently decides how to deliver media based on client capabilities, network conditions, and media characteristics.

## The Jellyfin Streaming Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Client Requests Playback                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Step 1: Get Playback Info                                       │
│  GET /Items/{id}/PlaybackInfo                                    │
│  - Client sends capabilities (codecs, containers, resolution)    │
│  - Server analyzes media file (FFprobe)                          │
│  - Server determines optimal streaming method                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Step 2: Decision Tree                                           │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Can client play video codec? (h264, hevc, vp9, av1)    │   │
│  │ Can client play audio codec? (aac, mp3, opus, ac3)     │   │
│  │ Can client play container? (mp4, mkv, webm)            │   │
│  │ Does client support resolution? (4K, 1080p, 720p)      │   │
│  │ Is bitrate within client's bandwidth?                   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              ↓                                    │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐ │
│  │ Direct Play  │ Direct Stream│ Transcode    │ Remux        │ │
│  │ (Best)       │ (Good)       │ (Fallback)   │ (Container)  │ │
│  └──────────────┴──────────────┴──────────────┴──────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Step 3: Stream Delivery                                         │
│  GET /Videos/{id}/stream.{format}                               │
│  - Progressive download OR                                       │
│  - HLS segments OR                                               │
│  - DASH segments                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Streaming Methods

### 1. Direct Play (Zero Server Processing)

**When Used:**
- Client supports video codec (h264, hevc, etc.)
- Client supports audio codec (aac, mp3, etc.)
- Client supports container format (mp4, mkv)
- No transcoding needed

**How It Works:**
```
Client Request:
GET /Videos/12345/stream.original?static=true

Server Response:
- Opens file with FileStream
- Supports HTTP Range requests for seeking
- Streams raw bytes directly to client
- Zero CPU usage on server
```

**Jellyfin Implementation:**
```csharp
// Jellyfin.Api/Controllers/VideosController.cs
[HttpGet("{itemId}/stream.{container}")]
public async Task<ActionResult> GetVideoStream(
    [FromRoute] Guid itemId,
    [FromRoute] string container,
    [FromQuery] bool @static = false)
{
    if (@static)
    {
        // Direct Play - just serve the file
        var path = _libraryManager.GetItemById(itemId).Path;
        
        return PhysicalFile(
            path,
            MimeTypes.GetMimeType(path),
            enableRangeProcessing: true); // Critical for seeking!
    }
    
    // ... transcoding logic
}
```

**Key Features:**
- HTTP Range requests for seeking (206 Partial Content)
- ETag for caching
- Content-Length header for progress bar
- Accept-Ranges: bytes header

### 2. Direct Stream (Container Remux Only)

**When Used:**
- Client supports video/audio codecs
- Client does NOT support container format
- Example: MKV → MP4 (copy codecs, change container)

**How It Works:**
```
Client Request:
GET /Videos/12345/stream.mp4?videoCodec=copy&audioCodec=copy

FFmpeg Command:
ffmpeg -i input.mkv \
  -c:v copy \           # Copy video stream (no re-encoding)
  -c:a copy \           # Copy audio stream (no re-encoding)
  -f mp4 \              # Change container to MP4
  -movflags frag_keyframe+empty_moov \  # Enable streaming
  pipe:1                # Output to stdout

Server Response:
- Streams FFmpeg output directly to client
- Low CPU usage (no encoding)
- Fast startup (~100-200ms)
```

**Jellyfin Implementation:**
```csharp
// Jellyfin.Api/Controllers/VideosController.cs
private async Task<ActionResult> GetTranscodedStream(StreamState state)
{
    var commandLineArgs = new StringBuilder();
    
    // Input
    commandLineArgs.Append($"-i \"{state.MediaPath}\" ");
    
    // Copy codecs (no transcoding)
    if (state.VideoRequest.CopyVideoStream)
        commandLineArgs.Append("-c:v copy ");
    if (state.AudioRequest.CopyAudioStream)
        commandLineArgs.Append("-c:a copy ");
    
    // Container format
    commandLineArgs.Append($"-f {state.OutputContainer} ");
    
    // Streaming flags for MP4
    if (state.OutputContainer == "mp4")
        commandLineArgs.Append("-movflags frag_keyframe+empty_moov ");
    
    // Output to pipe
    commandLineArgs.Append("pipe:1");
    
    var process = _processFactory.Create(new ProcessOptions
    {
        FileName = _mediaEncoder.EncoderPath,
        Arguments = commandLineArgs.ToString(),
        RedirectStandardOutput = true,
        UseShellExecute = false
    });
    
    await process.StartAsync();
    
    return File(process.StandardOutput.BaseStream, "video/mp4");
}
```

**Critical Flags:**
- `-movflags frag_keyframe+empty_moov`: Enables streaming before file is complete
- `-fflags +genpts`: Generate presentation timestamps
- `-avoid_negative_ts make_zero`: Fix timestamp issues

### 3. Transcode (Re-encode Video/Audio)

**When Used:**
- Client doesn't support video codec (need h264 instead of hevc)
- Client doesn't support audio codec (need aac instead of dts)
- Client needs lower bitrate/resolution
- Client needs different profile (high → main)

**How It Works:**

#### A. Progressive Streaming (Single File)
```
Client Request:
GET /Videos/12345/stream.mp4?videoCodec=h264&audioBitrate=128000

FFmpeg Command (GPU):
ffmpeg -hwaccel cuda \                    # Hardware acceleration
  -hwaccel_output_format cuda \           # Keep on GPU
  -i input.mkv \
  -c:v h264_nvenc \                       # NVIDIA encoder
  -preset p4 \                            # Balanced preset
  -b:v 8M \                               # 8 Mbps bitrate
  -maxrate 8M \
  -bufsize 16M \
  -c:a aac \                              # Audio to AAC
  -b:a 128k \
  -f mp4 \
  -movflags frag_keyframe+empty_moov \
  pipe:1

Server Response:
- Streams transcoded output in real-time
- Client plays as data arrives
- Seeking requires restarting transcode
```

#### B. HLS Streaming (Segmented)
```
Client Request:
GET /Videos/12345/master.m3u8

Server Generates Playlist:
#EXTM3U
#EXT-X-STREAM-INF:BANDWIDTH=8000000,RESOLUTION=1920x1080
stream-1080p.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=4000000,RESOLUTION=1280x720
stream-720p.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=854x480
stream-480p.m3u8

Client Requests Quality:
GET /Videos/12345/stream-1080p.m3u8

Server Returns Segment Playlist:
#EXTM3U
#EXT-X-TARGETDURATION:6
#EXT-X-MEDIA-SEQUENCE:0
#EXTINF:6.0,
segment-0.ts
#EXTINF:6.0,
segment-1.ts
#EXTINF:6.0,
segment-2.ts

FFmpeg Command (Segmented):
ffmpeg -hwaccel cuda \
  -i input.mkv \
  -c:v h264_nvenc \
  -preset p4 \
  -b:v 8M \
  -c:a aac \
  -b:a 128k \
  -f hls \                                # HLS format
  -hls_time 6 \                           # 6-second segments
  -hls_list_size 0 \                      # Keep all segments
  -hls_segment_filename segment-%d.ts \
  -start_number 0 \
  playlist.m3u8
```

**Jellyfin Implementation:**
```csharp
// MediaBrowser.Controller/MediaEncoding/EncodingHelper.cs
public string GetVideoEncoder(EncodingJobInfo state, EncodingOptions encodingOptions)
{
    var videoCodec = state.OutputVideoCodec;
    
    // Try hardware acceleration first
    if (encodingOptions.EnableHardwareEncoding)
    {
        // NVIDIA NVENC
        if (_mediaEncoder.SupportsEncoder("h264_nvenc"))
            return "h264_nvenc";
        
        // Intel QuickSync
        if (_mediaEncoder.SupportsEncoder("h264_qsv"))
            return "h264_qsv";
        
        // AMD AMF
        if (_mediaEncoder.SupportsEncoder("h264_amf"))
            return "h264_amf";
        
        // Apple VideoToolbox
        if (_mediaEncoder.SupportsEncoder("h264_videotoolbox"))
            return "h264_videotoolbox";
    }
    
    // Fallback to software encoding
    return "libx264";
}

public string GetVideoQualityParam(EncodingJobInfo state)
{
    var encoder = GetVideoEncoder(state);
    
    if (encoder.Contains("nvenc"))
    {
        // NVIDIA: Use preset + bitrate
        return $"-preset {state.Options.EncoderPreset} " +
               $"-b:v {state.OutputVideoBitrate} " +
               $"-maxrate {state.OutputVideoBitrate} " +
               $"-bufsize {state.OutputVideoBitrate * 2}";
    }
    else if (encoder == "libx264")
    {
        // Software: Use CRF for quality
        return $"-preset {state.Options.EncoderPreset} " +
               $"-crf 23";
    }
    
    return string.Empty;
}
```

### 4. Remux (Your Backend's 4th Mode)

**When Used:**
- Container incompatible but codecs are fine
- Similar to Direct Stream but Jellyfin calls it "Remux"

**Implementation:** Same as Direct Stream above

## Jellyfin's Session Management

### Session Creation
```csharp
// Jellyfin.Api/Controllers/SessionController.cs
[HttpPost("Playing")]
public async Task<ActionResult> ReportPlaybackStart(PlaybackStartInfo info)
{
    var session = new SessionInfo
    {
        Id = Guid.NewGuid(),
        UserId = User.GetUserId(),
        ItemId = info.ItemId,
        PlayMethod = info.PlayMethod, // DirectPlay, DirectStream, Transcode
        PlayState = new PlayerStateInfo
        {
            PositionTicks = info.PositionTicks,
            IsPaused = false,
            IsMuted = false
        },
        TranscodingInfo = info.PlayMethod == PlayMethod.Transcode 
            ? new TranscodingInfo
            {
                VideoCodec = info.VideoCodec,
                AudioCodec = info.AudioCodec,
                Container = info.Container,
                Bitrate = info.Bitrate,
                Framerate = info.Framerate,
                CompletionPercentage = 0
            }
            : null
    };
    
    _sessionManager.AddSession(session);
    
    return NoContent();
}
```

### Progress Reporting
```csharp
[HttpPost("Playing/Progress")]
public async Task<ActionResult> ReportPlaybackProgress(PlaybackProgressInfo info)
{
    var session = _sessionManager.GetSession(info.SessionId);
    
    if (session != null)
    {
        session.PlayState.PositionTicks = info.PositionTicks;
        session.PlayState.IsPaused = info.IsPaused;
        session.LastActivityDate = DateTime.UtcNow;
        
        // Update watch history
        await _userDataManager.SavePlaybackPosition(
            session.UserId,
            session.ItemId,
            info.PositionTicks);
        
        // Notify other clients
        await _sessionManager.SendPlaybackProgressNotification(session);
    }
    
    return NoContent();
}
```

### Session Cleanup
```csharp
// Background service that runs every 30 seconds
public class SessionCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var inactiveSessions = _sessionManager.Sessions
                .Where(s => s.LastActivityDate < DateTime.UtcNow.AddMinutes(-5))
                .ToList();
            
            foreach (var session in inactiveSessions)
            {
                // Kill FFmpeg process if transcoding
                if (session.TranscodingInfo != null)
                {
                    var process = _processManager.GetProcess(session.Id);
                    process?.Kill();
                }
                
                _sessionManager.RemoveSession(session.Id);
                _logger.LogInformation("Cleaned up inactive session {SessionId}", session.Id);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Jellyfin's Seeking Implementation

### Direct Play Seeking
```
Client sends HTTP Range request:
GET /Videos/12345/stream.original
Range: bytes=10485760-

Server responds:
HTTP/1.1 206 Partial Content
Content-Range: bytes 10485760-104857600/104857600
Content-Length: 94371840

[File bytes from position 10485760 onwards]
```

### Transcode Seeking
```
Client requests new position:
GET /Videos/12345/stream.mp4?startTimeTicks=6000000000

Server:
1. Kills existing FFmpeg process
2. Starts new FFmpeg with -ss parameter:
   ffmpeg -ss 600 -i input.mkv ...
3. Streams from new position
```

**Optimization:** Jellyfin uses `-ss` BEFORE `-i` for faster seeking:
```bash
# Fast (input seeking)
ffmpeg -ss 600 -i input.mkv ...

# Slow (output seeking)
ffmpeg -i input.mkv -ss 600 ...
```

## Jellyfin's Bandwidth Detection

```csharp
// Jellyfin.Api/Controllers/VideosController.cs
private int GetOptimalBitrate(HttpContext context, MediaSourceInfo mediaSource)
{
    // Check if client specified max bitrate
    if (context.Request.Query.TryGetValue("maxStreamingBitrate", out var maxBitrate))
    {
        return int.Parse(maxBitrate);
    }
    
    // Estimate based on connection type
    var userAgent = context.Request.Headers["User-Agent"].ToString();
    
    if (userAgent.Contains("Mobile"))
        return 4_000_000; // 4 Mbps for mobile
    
    if (context.Connection.RemoteIpAddress.IsIPv4MappedToIPv6)
    {
        // Local network - use high bitrate
        return 20_000_000; // 20 Mbps
    }
    
    // Default to 8 Mbps
    return 8_000_000;
}
```

## Jellyfin's Codec Decision Matrix

```csharp
public class CodecDecisionMatrix
{
    public PlayMethod DeterminePlayMethod(
        MediaStream videoStream,
        MediaStream audioStream,
        DeviceProfile deviceProfile)
    {
        // Check video codec compatibility
        var videoCompatible = deviceProfile.DirectPlayProfiles
            .Any(p => p.VideoCodec?.Contains(videoStream.Codec) == true);
        
        // Check audio codec compatibility
        var audioCompatible = deviceProfile.DirectPlayProfiles
            .Any(p => p.AudioCodec?.Contains(audioStream.Codec) == true);
        
        // Check container compatibility
        var containerCompatible = deviceProfile.DirectPlayProfiles
            .Any(p => p.Container?.Contains(videoStream.Container) == true);
        
        if (videoCompatible && audioCompatible && containerCompatible)
        {
            return PlayMethod.DirectPlay;
        }
        
        if (videoCompatible && audioCompatible && !containerCompatible)
        {
            return PlayMethod.DirectStream; // Remux
        }
        
        return PlayMethod.Transcode;
    }
}
```

## Key Jellyfin Optimizations We Should Implement

### 1. Process Pooling
```csharp
// Reuse FFmpeg processes for better performance
public class FFmpegProcessPool
{
    private readonly ConcurrentBag<Process> _availableProcesses = new();
    
    public Process GetOrCreate()
    {
        if (_availableProcesses.TryTake(out var process))
        {
            return process;
        }
        
        return CreateNewProcess();
    }
    
    public void Return(Process process)
    {
        if (process.HasExited)
        {
            process.Dispose();
        }
        else
        {
            _availableProcesses.Add(process);
        }
    }
}
```

### 2. Segment Caching
```csharp
// Cache HLS segments for reuse
public class SegmentCache
{
    private readonly string _cachePath;
    
    public async Task<string> GetOrCreateSegment(
        string videoId,
        int segmentIndex,
        Func<Task<Stream>> generator)
    {
        var segmentPath = Path.Combine(_cachePath, videoId, $"segment-{segmentIndex}.ts");
        
        if (File.Exists(segmentPath))
        {
            return segmentPath;
        }
        
        using var stream = await generator();
        using var fileStream = File.Create(segmentPath);
        await stream.CopyToAsync(fileStream);
        
        return segmentPath;
    }
}
```

### 3. Throttling
```csharp
// Throttle transcoding to match playback speed
public class ThrottledStream : Stream
{
    private readonly Stream _innerStream;
    private readonly int _targetBitrate;
    private DateTime _startTime = DateTime.UtcNow;
    private long _bytesWritten = 0;
    
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, ct);
        _bytesWritten += bytesRead;
        
        // Calculate expected time based on bitrate
        var expectedSeconds = (_bytesWritten * 8.0) / _targetBitrate;
        var actualSeconds = (DateTime.UtcNow - _startTime).TotalSeconds;
        
        // If we're ahead, throttle
        if (actualSeconds < expectedSeconds)
        {
            var delayMs = (int)((expectedSeconds - actualSeconds) * 1000);
            await Task.Delay(delayMs, ct);
        }
        
        return bytesRead;
    }
}
```

## Summary: How to Implement Jellyfin-Style Streaming

1. **Playback Info Endpoint**: Analyze media and return optimal play method
2. **Strategy Pattern**: Implement DirectPlay, DirectStream, Transcode strategies
3. **Session Management**: Track active streams, cleanup idle sessions
4. **Range Request Support**: Enable seeking for direct play
5. **FFmpeg Integration**: Use hardware acceleration, proper flags
6. **HLS/DASH Support**: Segment transcoding for adaptive bitrate
7. **Progress Reporting**: Track watch position, notify clients
8. **Bandwidth Detection**: Adjust quality based on network
9. **Process Management**: Pool FFmpeg processes, cleanup on disconnect
10. **Caching**: Cache segments, metadata, and playback decisions

This is exactly what we'll implement in the new C# backend! 🚀
