# Transcoding Modes

Lanflix supports three transcoding modes to handle media playback, giving you full control over how incompatible files are handled.

## Transcoding Modes

### 1. Direct Play (Default)
- **What it does**: Streams files as-is without any transcoding
- **Best for**: Compatible files (H.264 video + AAC audio)
- **Pros**: Fastest, no CPU/GPU usage, instant playback
- **Cons**: Incompatible files won't play

### 2. Streaming Transcoding (Jellyfin-Style)
- **What it does**: Real-time transcoding during playback
- **Best for**: Mixed libraries with various codecs
- **Pros**: Works with all files, no pre-processing needed
- **Cons**: Uses CPU/GPU during playback, slight startup delay

### 3. Offline Transcoding
- **What it does**: Pre-transcodes files in the background
- **Best for**: Large libraries where you want smooth playback
- **Pros**: Smooth playback, no real-time CPU usage
- **Cons**: Requires storage space, processing time upfront
- **Note**: Background transcoding job not yet implemented

## Stream-Specific Transcoding

You can enable/disable transcoding for audio and video independently:

### Audio Transcoding
- Converts incompatible audio codecs (DTS, TrueHD, etc.) to AAC
- Fast and lightweight
- Enable if you have audio compatibility issues

### Video Transcoding
- Converts incompatible video codecs (HEVC, VP9, etc.) to H.264
- More CPU/GPU intensive
- Enable if you have video compatibility issues

**Both can be enabled simultaneously** - the system will only transcode what's needed based on file compatibility.

## Hardware Acceleration

### NVIDIA NVENC (GPU)
- 10-20x faster than CPU encoding
- Recommended for streaming transcoding
- Requires NVIDIA GPU with NVENC support
- Uses NVDEC for decoding + NVENC for encoding (full GPU pipeline)

### CPU Fallback
- Automatically used if GPU fails
- Uses libx264 encoder
- Slower but works on all systems

## Encoding Presets

Balance between speed and quality:

- **P1**: Fastest (lowest quality) - for weak GPUs
- **P2-P3**: Fast - good for real-time streaming
- **P4**: Balanced (recommended) - best speed/quality ratio
- **P5-P6**: High quality - slower encoding
- **P7**: Best quality (slowest) - for offline transcoding

## How It Works

### Compatibility Check
When you play a file, the system checks:
1. Is the video codec compatible? (H.264, VP8, VP9)
2. Is the audio codec compatible? (AAC, MP3, Opus, Vorbis)

### Transcoding Decision
Based on your settings:

**Direct Play Mode:**
- Compatible file → Direct play
- Incompatible file → Won't play (error)

**Streaming Mode:**
- Compatible file → Direct play
- Incompatible audio → Transcode audio only, copy video
- Incompatible video → Transcode video only, copy audio
- Both incompatible → Transcode both

**Offline Mode:**
- All files → Use pre-transcoded version
- If not pre-transcoded → Direct play (fallback)

## Settings Location

Per-profile settings stored in:
- Backend: `Settings` table with key `streamingPreferences_{profileId}`
- Frontend: Settings → Playback → Transcoding

## API Usage

The streaming endpoint automatically applies your profile's transcoding settings:

```bash
# Normal streaming (uses profile settings)
GET /api/stream/:id?profileId=1

# Force transcoding (override settings)
GET /api/stream/:id?transcode=true

# Seek to specific time (works with all modes)
GET /api/stream/:id?start=120
```

## Recommendations

### For Most Users
- **Mode**: Streaming
- **Audio**: Enabled
- **Video**: Enabled
- **Hardware**: Enabled (if you have NVIDIA GPU)
- **Preset**: P4

### For Fast Networks / Compatible Files
- **Mode**: Direct Play
- **Audio**: Disabled
- **Video**: Disabled

### For Weak Systems
- **Mode**: Direct Play or Offline
- **Audio**: Enabled (lightweight)
- **Video**: Disabled (CPU intensive)

### For Maximum Compatibility
- **Mode**: Streaming
- **Audio**: Enabled
- **Video**: Enabled
- **Hardware**: Enabled
- **Preset**: P3 or P4

## Troubleshooting

### File Won't Play
1. Check transcoding mode (try Streaming mode)
2. Enable both audio and video transcoding
3. Check browser console for errors

### Stuttering During Playback
1. Lower encoding preset (P3 or P2)
2. Enable hardware acceleration
3. Try Direct Play mode if file is compatible

### GPU Errors
1. System falls back to CPU automatically
2. Check NVIDIA drivers are up to date
3. Verify GPU supports NVENC

### Seeking Issues
- Streaming mode: Uses time-based seeking (reloads stream)
- Direct play: Uses byte-range seeking (instant)
- Both work seamlessly

## Future Enhancements

- [ ] Offline transcoding background job
- [ ] Automatic quality adjustment based on network
- [ ] Multi-bitrate streaming (HLS/DASH)
- [ ] Intel QuickSync and AMD VCE support
- [ ] Subtitle burning during transcoding
