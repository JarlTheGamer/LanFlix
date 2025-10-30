# Jellyfin-Style Transcoding System

## Overview

Lanflix now implements a Jellyfin-style automatic transcoding detection system with four distinct playback modes. The system automatically detects what type of processing is needed for each media file and applies the minimal amount of transcoding necessary.

## Playback Modes

### 1. Direct Play
- **What it does**: Streams the file directly without any processing
- **When used**: When video codec, audio codec, AND container are all browser-compatible
- **Seeking**: Normal browser seeking works perfectly
- **Performance**: Fastest, no CPU/GPU usage
- **Example**: MP4 file with H.264 video and AAC audio

### 2. Remux
- **What it does**: Changes only the container format (e.g., MKV → MP4)
- **When used**: When codecs are compatible but container isn't
- **Seeking**: Reload stream at new position (fast, no re-encoding)
- **Performance**: Very fast, minimal CPU usage
- **Example**: MKV file with H.264 video and AAC audio → remux to MP4

### 3. Direct Stream
- **What it does**: Transcodes audio only, copies video stream
- **When used**: When video is compatible but audio isn't
- **Seeking**: Reload stream at new position
- **Performance**: Moderate CPU/GPU usage (audio encoding only)
- **Example**: MP4 with H.264 video but DTS/AC3 audio → transcode audio to AAC

### 4. Transcode
- **What it does**: Full video and audio transcoding
- **When used**: When video codec is incompatible
- **Seeking**: Reload stream at new position
- **Performance**: High CPU/GPU usage
- **Example**: HEVC/H.265 video → transcode to H.264

## Auto Mode (Recommended)

The "Auto" mode automatically detects which playback mode is needed:

```
Check file compatibility
  ↓
Audio compatible? Video compatible? Container compatible?
  ↓
YES + YES + YES → Direct Play
YES + YES + NO  → Remux
YES + NO  + ANY → Transcode
NO  + YES + ANY → Direct Stream
NO  + NO  + ANY → Transcode
```

## Codec Compatibility

### Browser-Compatible Video Codecs
- H.264 (AVC)
- VP8
- VP9
- AV1

### Browser-Compatible Audio Codecs
- AAC
- MP3
- Opus
- Vorbis

### Browser-Compatible Containers
- MP4 (and variants: MOV, M4V)
- WebM

### Common Incompatible Codecs
- **Video**: HEVC (H.265), MPEG-2, VC-1
- **Audio**: DTS, AC3 (Dolby Digital), TrueHD, FLAC
- **Container**: MKV (Matroska), AVI

## Seeking Behavior

### Direct Play
- Uses standard HTTP range requests
- Browser handles seeking natively
- Instant seeking to any position

### Remux / Direct Stream / Transcode
- Seeking triggers a stream reload with `?start=<seconds>` parameter
- FFmpeg starts encoding from the seek position
- Small delay (1-3 seconds) while new stream initializes
- Maintains playback state and resumes automatically

## User Settings

Users can control transcoding behavior in Settings → Playback:

1. **Playback Mode**
   - Auto (recommended) - Automatic detection
   - Direct Play - Force no transcoding
   - Direct Stream - Force audio-only transcoding
   - Transcode - Force full transcoding

2. **Audio Transcoding Toggle**
   - Enable/disable audio transcoding
   - When disabled, incompatible audio may not play

3. **Video Transcoding Toggle**
   - Enable/disable video transcoding
   - When disabled, incompatible video may not play

4. **Hardware Acceleration**
   - Uses NVIDIA NVENC for GPU encoding
   - 10-20x faster than CPU encoding
   - Requires NVIDIA GPU with NVENC support

5. **Encoding Preset**
   - P1-P7 (NVENC) or ultrafast-veryslow (CPU)
   - Balances speed vs quality
   - P4/medium recommended

## Implementation Details

### Backend (streaming.routes.ts)

```typescript
// 1. Load user preferences
const transcodingMode = 'auto'; // or user's choice

// 2. Check file compatibility
const compatCheck = await mediaConverterService.checkCompatibility(filePath);
// Returns: { playbackMode, transcodeAudio, transcodeVideo, needsRemux, ... }

// 3. Determine actual playback mode
let actualPlaybackMode = compatCheck.playbackMode;
if (transcodingMode !== 'auto') {
  actualPlaybackMode = transcodingMode; // User override
}

// 4. Apply appropriate streaming method
if (actualPlaybackMode === 'direct-play') {
  // Stream file directly with range support
} else if (actualPlaybackMode === 'remux') {
  // FFmpeg: copy codecs, change container
} else if (actualPlaybackMode === 'direct-stream') {
  // FFmpeg: transcode audio, copy video
} else if (actualPlaybackMode === 'transcode') {
  // FFmpeg: transcode both audio and video
}
```

### Frontend (video-player.js)

```javascript
// Detect playback mode from response headers
const playbackMode = response.headers.get('X-Playback-Mode');
this.isTranscoding = playbackMode !== 'direct-play';

// Handle seeking
seek(time) {
  if (this.isTranscoding) {
    // Reload stream with start parameter
    this.reloadStreamAtTime(time);
  } else {
    // Normal browser seeking
    this.videoElement.currentTime = time;
  }
}
```

## Response Headers

The backend sets these headers to inform the frontend:

- `X-Playback-Mode`: direct-play | remux | direct-stream | transcode
- `X-Transcode-Mode`: (legacy) audio-only | video+audio | remux
- `X-Direct-Play`: true | false

## Performance Comparison

| Mode | CPU Usage | GPU Usage | Latency | Seeking Speed |
|------|-----------|-----------|---------|---------------|
| Direct Play | 0% | 0% | None | Instant |
| Remux | 5-10% | 0% | Low | Fast (1-2s) |
| Direct Stream | 10-20% | 0-5% | Low | Fast (1-2s) |
| Transcode (CPU) | 80-100% | 0% | Medium | Slow (3-5s) |
| Transcode (GPU) | 10-20% | 30-50% | Low | Fast (1-2s) |

## Troubleshooting

### Seeking doesn't work
- Check if file is being transcoded (look for X-Playback-Mode header)
- Transcoded streams reload on seek - this is expected behavior
- Direct play should have instant seeking

### Audio but no video
- Video codec likely incompatible
- Enable video transcoding in settings
- Check if GPU acceleration is working

### No audio
- Audio codec likely incompatible (DTS, AC3)
- Enable audio transcoding in settings
- Check browser console for errors

### Stuttering during playback
- CPU/GPU may be overloaded
- Try lower encoding preset (P1-P3)
- Check if hardware acceleration is enabled
- Consider pre-transcoding files

## Future Enhancements

1. **Adaptive Bitrate Streaming**
   - HLS/DASH support
   - Multiple quality levels
   - Automatic quality switching

2. **Smart Caching**
   - Cache transcoded segments
   - Reduce re-encoding on rewind

3. **Subtitle Burning**
   - Burn-in subtitles during transcode
   - Support for PGS/VOBSUB

4. **Audio Track Selection**
   - Multiple audio tracks
   - Language selection
   - Surround sound passthrough

## Related Files

- `backend/src/services/media-converter.service.ts` - Transcoding logic
- `backend/src/routes/streaming.routes.ts` - Streaming endpoint
- `backend/src/utils/ffmpeg.ts` - FFmpeg utilities
- `frontend/src/modules/video-player.js` - Video player with seeking
- `frontend/src/pages/settings.html` - User settings UI
