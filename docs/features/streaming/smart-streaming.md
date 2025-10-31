# Smart Streaming (Jellyfin-Style)

Lanflix uses intelligent streaming that prioritizes direct play and only transcodes when necessary.

## How It Works

### 1. Direct Play (Preferred)
When your video file has browser-compatible codecs, it streams directly without any processing:
- **Zero CPU usage**
- **Instant playback**
- **Full quality**
- **Seeking works perfectly**

### 2. Smart Transcoding (When Needed)
If codecs are incompatible, only the problematic streams are transcoded:
- **Audio-only transcode**: Video copied, audio converted to AAC (fast!)
- **Video-only transcode**: Audio copied, video converted to H.264
- **Full transcode**: Both streams converted (rare)

## Browser-Compatible Codecs

### Audio Codecs ✅
- AAC (most common)
- MP3
- Opus
- Vorbis

### Video Codecs ✅
- H.264 / AVC (most common)
- VP8
- VP9
- AV1

## Incompatible Codecs (Auto-Transcoded)

### Audio Codecs ⚠️
- DTS / DTS-HD
- AC3 / E-AC3 (Dolby Digital)
- TrueHD
- FLAC
- PCM

### Video Codecs ⚠️
- HEVC / H.265 (browser-dependent)
- MPEG-2
- VC-1
- MPEG-4 Part 2

## API Endpoints

### Stream Video
```
GET /api/stream/:id
GET /api/stream/:id?episodeId=123
```

**Query Parameters**:
- `episodeId` - Episode ID for TV shows
- `transcode=true` - Force transcoding (for testing)

**Response Headers**:
- `X-Direct-Play: true` - File is being direct played
- `X-Transcode-Reason` - Why transcoding was needed

### Check Media Info
```
GET /api/stream/:id/info
GET /api/stream/:id/info?episodeId=123
```

**Response**:
```json
{
  "contentId": 1,
  "episodeId": null,
  "filePath": "/media/movies/example.mkv",
  "mediaInfo": {
    "hasVideo": true,
    "hasAudio": true,
    "videoCodec": "h264",
    "audioCodec": "dts",
    "duration": 7200,
    "bitrate": 8000000,
    "width": 1920,
    "height": 1080,
    "container": "matroska,webm"
  },
  "transcoding": {
    "needsTranscode": true,
    "transcodeAudio": true,
    "transcodeVideo": false,
    "reason": "Incompatible: audio(dts)"
  }
}
```

## Testing

### Test FFmpeg Probe
```bash
cd backend
npm run build
node test-ffmpeg.js /path/to/video.mp4
```

### Test Streaming
```bash
# Check media info
curl http://localhost:3000/api/stream/1/info | jq

# Stream video (auto-detects)
curl -I http://localhost:3000/api/stream/1

# Force transcode
curl -I http://localhost:3000/api/stream/1?transcode=true
```

## Performance Comparison

| Scenario | CPU Usage | Startup Time | Quality |
|----------|-----------|--------------|---------|
| Direct Play | 0% | Instant | Original |
| Audio Transcode | 5-10% | <1 second | Original video, AAC audio |
| Full Transcode | 20-50% | 2-5 seconds | Re-encoded |

## Optimizing Your Library

For best performance, pre-encode your library with compatible codecs:

### Quick Audio Fix (Fast)
```bash
# Only re-encode audio, copy video
ffmpeg -i input.mkv -c:v copy -c:a aac -b:a 192k output.mp4
```

### Full Re-encode (Best Compatibility)
```bash
# Re-encode everything with web-optimized settings
ffmpeg -i input.mkv \
  -c:v libx264 -preset medium -crf 23 \
  -c:a aac -b:a 192k \
  -movflags +faststart \
  output.mp4
```

### Batch Convert
```bash
# Convert all MKV files in a directory
for file in *.mkv; do
  ffmpeg -i "$file" -c:v copy -c:a aac -b:a 192k "${file%.mkv}.mp4"
done
```

## Troubleshooting

### No Audio
1. Check media info: `curl http://localhost:3000/api/stream/1/info`
2. Look for `hasAudio: false` - file has no audio track
3. Look for `transcodeAudio: true` - audio is being transcoded
4. Check browser console for errors

### Stuttering/Buffering
1. Check if transcoding is happening (high CPU usage)
2. Consider pre-encoding files for direct play
3. Check network bandwidth
4. Reduce video quality/bitrate

### Seeking Issues
- Direct play: Seeking works perfectly
- Transcoding: Seeking may restart transcode (slight delay)
- Solution: Pre-encode files for direct play

### FFmpeg Not Found
```bash
# Install FFmpeg
# Windows (Chocolatey)
choco install ffmpeg

# Mac (Homebrew)
brew install ffmpeg

# Linux (Ubuntu/Debian)
sudo apt install ffmpeg

# Verify installation
ffmpeg -version
```

## Configuration

### Environment Variables
```env
# FFmpeg binary path (optional, auto-detected)
FFMPEG_PATH=/usr/bin/ffmpeg
FFPROBE_PATH=/usr/bin/ffprobe

# Transcode settings
TRANSCODE_AUDIO_CODEC=aac
TRANSCODE_AUDIO_BITRATE=192k
TRANSCODE_VIDEO_CODEC=libx264
TRANSCODE_PRESET=ultrafast
```

## Future Enhancements

- [ ] Multiple quality levels (720p, 1080p, 4K)
- [ ] Adaptive bitrate streaming (HLS/DASH)
- [ ] Hardware acceleration (NVENC, QSV, VAAPI)
- [ ] Transcode caching
- [ ] Subtitle burning
- [ ] Audio track selection
- [ ] HDR tone mapping

## See Also

- [Video Player Documentation](./video-player.md)
- [Troubleshooting Guide](../troubleshooting/video-playback.md)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
