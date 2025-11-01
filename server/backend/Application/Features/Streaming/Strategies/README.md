# Streaming Strategies

This directory contains the implementation of streaming strategies for the Lanflix media server. The strategies follow the Strategy pattern to provide different streaming modes based on client capabilities and media characteristics.

## Overview

The streaming strategy system automatically selects the optimal way to deliver media content to clients:

1. **DirectPlay** - Serves media as-is without any processing (highest priority)
2. **DirectStream** - Remuxes container format while preserving codecs
3. **TranscodeVideo** - Transcodes video while copying audio
4. **FullTranscode** - Transcodes both video and audio (fallback)

## Architecture

### Core Components

- **IStreamingStrategy** - Interface defining the contract for all strategies
- **BaseStreamingStrategy** - Abstract base class with common functionality
- **StreamingStrategySelector** - Service that selects the optimal strategy

### Strategy Implementations

#### 1. DirectPlayStrategy (Priority: 1)

**Purpose**: Serve media files directly without any transcoding for maximum performance.

**When Used**:
- Video codec is supported by client
- At least one audio codec is supported
- Container format is supported
- Resolution is within client limits
- Bitrate is within client limits
- HDR support matches (if content is HDR)

**Features**:
- Zero-copy file streaming with FileStream
- HTTP range request support for seeking
- 80KB buffer size for optimal streaming
- LimitedStream wrapper for range requests

**Example**:
```
Media: MP4 container, H.264 video, AAC audio, 1080p
Client: Supports H.264, AAC, MP4, 1080p
Result: DirectPlay ✓
```

#### 2. DirectStreamStrategy (Priority: 2)

**Purpose**: Remux container format while preserving video and audio codecs.

**When Used**:
- Video codec is supported
- Audio codec is supported
- Container format is NOT supported (needs remux)
- Resolution and bitrate are within limits

**Features**:
- FFmpeg remux with codec copy
- Automatic target container selection (MP4 > MPEG-TS > WebM)
- Streaming output via RemuxStream wrapper
- No transcoding overhead

**Example**:
```
Media: MKV container, H.264 video, AAC audio
Client: Supports H.264, AAC, but not MKV
Result: DirectStream (MKV → MP4) ✓
```

#### 3. TranscodeVideoStrategy (Priority: 3)

**Purpose**: Transcode video while copying audio codec.

**When Used**:
- Video codec is NOT supported (needs transcoding)
- At least one audio codec IS supported (can copy)

**Features**:
- Hardware acceleration support (NVENC, QuickSync, AMF, VAAPI, VideoToolbox)
- Automatic codec selection (H.265 > H.264)
- Resolution scaling based on client capabilities
- Bitrate adaptation
- Audio codec copy for efficiency

**Example**:
```
Media: HEVC video, AAC audio, 4K
Client: Supports H.264 only, AAC, max 1080p
Result: TranscodeVideo (HEVC → H.264, 4K → 1080p, AAC copy) ✓
```

#### 4. FullTranscodeStrategy (Priority: 4)

**Purpose**: Transcode both video and audio. Fallback strategy that always works.

**When Used**:
- Video codec is not supported
- Audio codec is not supported
- Always returns true (fallback)

**Features**:
- Full video and audio transcoding
- Hardware acceleration support
- Adaptive bitrate and resolution
- HLS/DASH segmented streaming support
- Comprehensive codec selection

**Example**:
```
Media: VP9 video, Opus audio, 4K
Client: Supports H.264 only, AAC only, max 1080p
Result: FullTranscode (VP9 → H.264, Opus → AAC, 4K → 1080p) ✓
```

## StreamingStrategySelector

The selector service chooses the optimal strategy based on:

1. **User Preferences** - Can force transcoding
2. **Client Capabilities** - Supported codecs, containers, resolution, bitrate
3. **Media Characteristics** - Codecs, container, resolution, bitrate
4. **Strategy Priority** - Lower number = higher priority

### Selection Algorithm

```csharp
1. Check user preferences for forced transcoding
2. Filter strategies that can handle the request
3. Sort by priority (ascending)
4. Select first strategy (highest priority)
```

### Usage Example

```csharp
var selector = new StreamingStrategySelector(strategies, logger);

var strategy = selector.SelectOptimalStrategy(
    media: mediaInfo,
    client: clientCapabilities,
    preferences: userPreferences
);

var result = await strategy.ExecuteAsync(streamRequest, cancellationToken);
```

## Client Capabilities

Clients must provide their capabilities:

```csharp
var capabilities = new ClientCapabilities
{
    SupportedVideoCodecs = new[] { "h264", "hevc" },
    SupportedAudioCodecs = new[] { "aac", "mp3" },
    SupportedContainers = new[] { "mp4", "webm" },
    MaxBitrate = 8_000_000, // 8 Mbps
    MaxResolution = VideoResolution.HD1080p,
    SupportsHDR = false
};
```

## Hardware Acceleration

All transcoding strategies support hardware acceleration:

- **NVIDIA NVENC** - h264_nvenc, hevc_nvenc
- **Intel QuickSync** - h264_qsv, hevc_qsv
- **AMD AMF** - h264_amf, hevc_amf
- **VAAPI (Linux)** - h264_vaapi, hevc_vaapi
- **VideoToolbox (macOS)** - h264_videotoolbox, hevc_videotoolbox

Hardware acceleration is automatically detected and used when available.

## Stream Result

All strategies return a `StreamResult`:

```csharp
public class StreamResult
{
    public Stream DataStream { get; init; }           // Media data stream
    public string ContentType { get; init; }          // MIME type
    public long? ContentLength { get; init; }         // Size (null if unknown)
    public StreamingMode Mode { get; init; }          // Strategy used
    public bool SupportsRangeRequests { get; init; }  // Seeking support
    public long? RangeStart { get; init; }            // Range start
    public long? RangeEnd { get; init; }              // Range end
    public string? TranscodingProcessId { get; init; } // FFmpeg PID
    public Action? CleanupAction { get; init; }       // Cleanup callback
}
```

## Performance Considerations

### DirectPlay
- **Startup Time**: < 50ms
- **CPU Usage**: Minimal (file I/O only)
- **Memory**: 80KB buffer
- **Best For**: Compatible media, local network

### DirectStream
- **Startup Time**: 200-500ms
- **CPU Usage**: Low (remux only)
- **Memory**: 80KB chunks
- **Best For**: Container incompatibility

### TranscodeVideo
- **Startup Time**: 300-800ms
- **CPU Usage**: Medium-High (video encoding)
- **Memory**: 80KB chunks + FFmpeg buffers
- **Best For**: Video codec incompatibility

### FullTranscode
- **Startup Time**: 400-1000ms
- **CPU Usage**: High (full encoding)
- **Memory**: 80KB chunks + FFmpeg buffers
- **Best For**: Full incompatibility, adaptive streaming

## Error Handling

All strategies handle errors gracefully:

- **File Not Found**: Throws `FileNotFoundException`
- **Transcoding Failure**: Throws `TranscodingException`
- **Client Disconnect**: Cancellation token triggers cleanup
- **Stream Disposal**: Automatic cleanup via `CleanupAction`

## Testing

To test strategy selection:

```csharp
var results = selector.TestStrategies(mediaInfo, clientCapabilities);

foreach (var (mode, canHandle) in results)
{
    Console.WriteLine($"{mode}: {(canHandle ? "✓" : "✗")}");
}
```

## Future Enhancements

- [ ] Adaptive bitrate streaming (HLS/DASH)
- [ ] Multi-quality transcoding profiles
- [ ] Tone mapping for HDR content
- [ ] Subtitle burning for incompatible formats
- [ ] Audio normalization
- [ ] Chapter marker support
