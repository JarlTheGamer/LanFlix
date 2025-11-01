# FFmpeg Integration Services

This directory contains the FFmpeg integration services for the Lanflix backend, providing media analysis, hardware acceleration detection, and transcoding capabilities.

## Components

### 1. MediaAnalyzer
**File:** `MediaAnalyzer.cs`

Analyzes media files using FFprobe to extract comprehensive stream information.

**Features:**
- Extracts video stream information (codec, resolution, bitrate, frame rate)
- Detects audio streams with language information
- Identifies subtitle streams
- Detects HDR content (HDR10, Dolby Vision, HLG)
- Normalizes codec names for consistency
- Parses FFprobe JSON output

**Usage:**
```csharp
var mediaInfo = await _mediaAnalyzer.AnalyzeAsync("/path/to/video.mkv");
Console.WriteLine($"Video: {mediaInfo.Video.Codec} {mediaInfo.Video.Width}x{mediaInfo.Video.Height}");
Console.WriteLine($"Audio tracks: {mediaInfo.Audio.Count}");
Console.WriteLine($"HDR: {mediaInfo.Video.IsHDR} ({mediaInfo.Video.HdrFormat})");
```

### 2. HardwareAccelerationDetector
**File:** `HardwareAccelerationDetector.cs`

Detects available hardware acceleration methods on the system.

**Supported Methods:**
- NVIDIA NVENC (h264_nvenc, hevc_nvenc)
- Intel QuickSync (h264_qsv, hevc_qsv)
- AMD AMF (h264_amf, hevc_amf)
- VAAPI - Linux (h264_vaapi, hevc_vaapi)
- VideoToolbox - macOS (h264_videotoolbox, hevc_videotoolbox)

**Priority Order:** NVENC > QuickSync > AMF > VAAPI > VideoToolbox

**Usage:**
```csharp
var capabilities = await _hwAccelDetector.DetectAsync();
if (capabilities.IsAvailable)
{
    Console.WriteLine($"Hardware acceleration: {capabilities.PreferredMethod}");
}
```

### 3. TranscodingPipeline
**File:** `TranscodingPipeline.cs`

Streams transcoded media using FFmpeg with hardware acceleration support.

**Features:**
- Builds FFmpeg commands based on streaming mode
- Supports hardware acceleration for all major platforms
- Uses ArrayPool for efficient memory management
- Streams data in 80KB chunks
- Handles process cleanup on cancellation
- Logs FFmpeg output for debugging

**Streaming Modes:**
- **DirectPlay:** No transcoding (copy streams)
- **DirectStream:** Container remux only
- **TranscodeVideo:** Video transcoding, audio copy
- **FullTranscode:** Both video and audio transcoding

**Usage:**
```csharp
var request = new TranscodeRequest
{
    InputPath = "/path/to/video.mkv",
    Mode = StreamingMode.TranscodeVideo,
    SourceMedia = mediaInfo,
    TargetVideoCodec = "h264",
    TargetVideoBitrate = 8_000_000,
    HwAccelMethod = HwAccelMethod.Nvenc
};

await foreach (var chunk in _transcodingPipeline.StreamAsync(request, cancellationToken))
{
    await outputStream.WriteAsync(chunk, cancellationToken);
}
```

### 4. FFmpegProcessPool
**File:** `FFmpegProcessPool.cs`

Manages a pool of FFmpeg processes to limit concurrent transcoding operations.

**Features:**
- Limits concurrent transcoding processes (default: 5)
- Tracks active processes with timestamps
- Detects and cleans up stale processes
- Provides health check functionality
- Thread-safe process management

**Usage:**
```csharp
using var slot = await _processPool.AcquireSlotAsync(sessionId);
// Start FFmpeg process
slot.AttachProcess(process);
slot.UpdateActivity(); // Update activity timestamp
// Process automatically released when slot is disposed
```

### 5. FFmpegProcessMonitor
**File:** `FFmpegProcessMonitor.cs`

Background service that monitors FFmpeg processes and cleans up stale ones.

**Features:**
- Runs health checks every 30 seconds
- Terminates processes with no activity for 60 seconds
- Logs process pool status
- Automatic cleanup on application shutdown

## Configuration

Add to `appsettings.json`:

```json
{
  "Lanflix": {
    "Transcoding": {
      "MaxConcurrentTranscodes": 5,
      "EnableHardwareAcceleration": true,
      "PreferredHwAccel": "auto",
      "DefaultBitrate": 8000000,
      "TempPath": "D:/Temp/Transcoding"
    }
  }
}
```

## Dependencies

All services are registered in `DependencyInjection.cs`:

```csharp
// FFmpeg Services
services.AddSingleton<IMediaAnalyzer, MediaAnalyzer>();
services.AddSingleton<IHardwareAccelerationDetector, HardwareAccelerationDetector>();
services.AddSingleton<ITranscodingPipeline, TranscodingPipeline>();

// FFmpeg Process Pool
services.AddSingleton<FFmpegProcessPool>();
services.AddHostedService<FFmpegProcessMonitor>();
```

## Requirements

- FFmpeg and FFprobe must be installed and available in PATH
- For hardware acceleration:
  - NVIDIA: CUDA drivers and NVENC support
  - Intel: QuickSync drivers
  - AMD: AMF drivers
  - Linux: VAAPI drivers

## Performance Optimizations

1. **ArrayPool Usage:** Buffers are rented from ArrayPool to reduce GC pressure
2. **Streaming:** Data is streamed in chunks rather than loading entire files
3. **Hardware Acceleration:** Automatically detects and uses GPU encoding
4. **Process Pooling:** Limits concurrent transcodes to prevent resource exhaustion
5. **Caching:** Hardware capabilities are cached after first detection

## Error Handling

All services include comprehensive error handling:
- FFmpeg process failures are logged with exit codes
- Stale processes are automatically cleaned up
- Cancellation is properly handled with process termination
- File not found errors are caught and reported

## Testing

To test FFmpeg integration:

1. Ensure FFmpeg is installed: `ffmpeg -version`
2. Test media analysis: Analyze a sample video file
3. Test hardware detection: Check detected capabilities
4. Test transcoding: Start a transcoding session
5. Monitor process pool: Check active processes and health

## Future Enhancements

- Adaptive bitrate streaming (HLS/DASH)
- HDR tone mapping for SDR displays
- Advanced subtitle handling (burn-in)
- Multi-pass encoding for better quality
- Thumbnail generation
- Progress reporting with percentage
