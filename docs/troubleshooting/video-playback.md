# Video Playback Troubleshooting

Solutions for common video playback issues.

## 🔇 No Audio in Videos

### Symptoms
- Video plays normally
- Progress bar and controls work
- No sound output
- Volume controls don't help

### Diagnosis Steps

#### 1. Check Video File Has Audio
```bash
# Install ffprobe (part of FFmpeg)
# Windows: choco install ffmpeg
# Mac: brew install ffmpeg
# Linux: apt install ffmpeg

# Check for audio streams
ffprobe -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 your-video.mp4
```

**Expected Output**: `aac`, `mp3`, `ac3`, etc.  
**If Empty**: Video file has no audio track

#### 2. Check Audio Codec Compatibility

```bash
# Get detailed audio info
ffprobe -v error -select_streams a:0 -show_entries stream=codec_name,codec_long_name,channels,sample_rate -of json your-video.mp4
```

**Browser Compatibility**:
- ✅ **AAC** - Supported by all browsers
- ✅ **MP3** - Supported by all browsers
- ⚠️ **AC3/EAC3** - Limited support
- ⚠️ **DTS** - Not supported in browsers
- ⚠️ **FLAC** - Limited support

#### 3. Check Browser Console
Open browser developer tools (F12) and check for errors:
- `NotSupportedError` - Codec not supported
- `NotAllowedError` - Autoplay policy blocked
- `AbortError` - Network issue

### Solutions

#### Solution 1: Re-encode Audio Track
If audio codec is incompatible:

```bash
# Convert to AAC (most compatible)
ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4

# Keep video, only re-encode audio
# -c:v copy = copy video without re-encoding (fast)
# -c:a aac = encode audio to AAC
# -b:a 192k = audio bitrate 192 kbps
```

#### Solution 2: Add Audio Track
If video has no audio:

```bash
# Add silent audio track
ffmpeg -i input.mp4 -f lavfi -i anullsrc=r=44100:cl=stereo -c:v copy -c:a aac -shortest output.mp4
```

#### Solution 3: Remux Container
Sometimes the container format causes issues:

```bash
# Remux MKV to MP4
ffmpeg -i input.mkv -c copy output.mp4

# This doesn't re-encode, just changes container
```

#### Solution 4: Browser-Specific Fixes

**Chrome/Edge**:
```javascript
// Ensure video is not muted
video.muted = false;
video.volume = 1.0;

// Try playing with user interaction
document.addEventListener('click', () => {
  video.play();
}, { once: true });
```

**Safari**:
```javascript
// Safari requires specific MIME types
video.type = 'video/mp4; codecs="avc1.42E01E, mp4a.40.2"';
```

**Firefox**:
```javascript
// Firefox may need explicit audio context
const audioContext = new AudioContext();
audioContext.resume();
```

---

## 🎬 Video Won't Play

### Symptoms
- Black screen
- Loading spinner forever
- Error message
- Controls don't respond

### Diagnosis Steps

#### 1. Check File Exists
```bash
# Backend logs should show file path
# Verify file exists at that location
ls -la /path/to/media/file.mp4
```

#### 2. Check File Permissions
```bash
# Ensure backend can read the file
chmod 644 /path/to/media/file.mp4

# Check directory permissions
chmod 755 /path/to/media/
```

#### 3. Check Network Tab
Open browser DevTools → Network tab:
- Look for `/api/stream/:id` request
- Check response status code
- Check response headers

**Common Status Codes**:
- `200 OK` - Full file served
- `206 Partial Content` - Range request (normal for seeking)
- `404 Not Found` - File doesn't exist
- `403 Forbidden` - Permission denied
- `500 Internal Server Error` - Backend error

### Solutions

#### Solution 1: Fix File Path
Update database with correct file path:

```sql
-- Check current path
SELECT id, title, file_path FROM content WHERE id = 123;

-- Update path
UPDATE content SET file_path = '/correct/path/to/file.mp4' WHERE id = 123;
```

#### Solution 2: Fix Permissions
```bash
# Make files readable
find /path/to/media -type f -exec chmod 644 {} \;

# Make directories executable
find /path/to/media -type d -exec chmod 755 {} \;
```

#### Solution 3: Check Backend Logs
```bash
# View recent logs
tail -f backend/logs/combined.log

# Look for errors related to streaming
grep "stream" backend/logs/error.log
```

---

## ⏸️ Video Stutters or Buffers

### Symptoms
- Playback pauses frequently
- Loading spinner appears during playback
- Choppy video
- Audio/video out of sync

### Diagnosis Steps

#### 1. Check Network Speed
```bash
# Test download speed from backend
curl -o /dev/null http://localhost:3000/api/stream/123

# Monitor network usage
# Windows: Resource Monitor → Network
# Mac: Activity Monitor → Network
# Linux: iftop or nethogs
```

#### 2. Check File Bitrate
```bash
# Get video bitrate
ffprobe -v error -select_streams v:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 video.mp4

# Get audio bitrate
ffprobe -v error -select_streams a:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 video.mp4
```

**Typical Bitrates**:
- 720p: 2-5 Mbps
- 1080p: 5-10 Mbps
- 4K: 20-50 Mbps

#### 3. Check CPU Usage
High CPU usage may indicate:
- Software decoding (no hardware acceleration)
- Inefficient codec
- Background processes

### Solutions

#### Solution 1: Enable Hardware Acceleration
**Chrome**: `chrome://settings/system` → Enable hardware acceleration  
**Firefox**: `about:preferences` → Performance → Use hardware acceleration  
**Edge**: `edge://settings/system` → Use hardware acceleration

#### Solution 2: Reduce Video Quality
```bash
# Create lower bitrate version
ffmpeg -i input.mp4 -c:v libx264 -b:v 3M -c:a aac -b:a 128k output.mp4

# -b:v 3M = 3 Mbps video bitrate
# -b:a 128k = 128 kbps audio bitrate
```

#### Solution 3: Use Different Format
```bash
# Convert to WebM (often more efficient)
ffmpeg -i input.mp4 -c:v libvpx-vp9 -b:v 2M -c:a libopus -b:a 128k output.webm
```

#### Solution 4: Optimize Network
- Use wired connection instead of WiFi
- Close other applications using bandwidth
- Check router QoS settings
- Consider local network issues

---

## 🔄 Seeking Issues

### Symptoms
- Can't seek to specific time
- Seeking causes video to freeze
- Progress bar doesn't respond
- Video restarts instead of seeking

### Diagnosis

#### Check Range Request Support
```bash
# Test range request
curl -H "Range: bytes=0-1000" http://localhost:3000/api/stream/123

# Should return 206 Partial Content
```

### Solutions

#### Solution 1: Ensure Proper Encoding
```bash
# Re-encode with proper keyframes
ffmpeg -i input.mp4 -c:v libx264 -g 30 -keyint_min 30 -c:a copy output.mp4

# -g 30 = keyframe every 30 frames (1 second at 30fps)
# This allows seeking to any second
```

#### Solution 2: Move Moov Atom
For MP4 files, the moov atom should be at the beginning:

```bash
# Move moov atom to beginning (fast start)
ffmpeg -i input.mp4 -c copy -movflags +faststart output.mp4
```

---

## 📺 Fullscreen Issues

### Symptoms
- Fullscreen button doesn't work
- Video exits fullscreen immediately
- Controls disappear in fullscreen
- Black bars in fullscreen

### Solutions

#### Solution 1: Browser Permissions
Some browsers require user interaction:
```javascript
// Must be triggered by user action
button.addEventListener('click', () => {
  video.requestFullscreen();
});
```

#### Solution 2: Fix Aspect Ratio
```css
/* Maintain aspect ratio in fullscreen */
video:fullscreen {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
```

#### Solution 3: Browser-Specific Fixes
```javascript
// Cross-browser fullscreen
function enterFullscreen(element) {
  if (element.requestFullscreen) {
    element.requestFullscreen();
  } else if (element.webkitRequestFullscreen) {
    element.webkitRequestFullscreen();
  } else if (element.mozRequestFullScreen) {
    element.mozRequestFullScreen();
  } else if (element.msRequestFullscreen) {
    element.msRequestFullscreen();
  }
}
```

---

## 🎯 Subtitle Issues

### Symptoms
- Subtitles don't appear
- Subtitles out of sync
- Wrong language
- Garbled text

### Solutions

#### Solution 1: Convert Subtitle Format
```bash
# Convert SRT to VTT (WebVTT)
ffmpeg -i subtitles.srt subtitles.vtt

# Extract subtitles from MKV
ffmpeg -i video.mkv -map 0:s:0 subtitles.srt
```

#### Solution 2: Fix Subtitle Encoding
```bash
# Convert to UTF-8
iconv -f ISO-8859-1 -t UTF-8 subtitles.srt > subtitles_utf8.srt
```

#### Solution 3: Adjust Subtitle Timing
```bash
# Delay subtitles by 2 seconds
ffmpeg -itsoffset 2 -i subtitles.srt -c copy subtitles_delayed.srt
```

---

## 🔍 Debugging Tools

### Browser DevTools
```javascript
// Check video properties
console.log('Duration:', video.duration);
console.log('Current Time:', video.currentTime);
console.log('Buffered:', video.buffered);
console.log('Ready State:', video.readyState);
console.log('Network State:', video.networkState);
```

### FFprobe Commands
```bash
# Full video information
ffprobe -v error -show_format -show_streams video.mp4

# Check for errors
ffmpeg -v error -i video.mp4 -f null -

# Analyze file
ffmpeg -i video.mp4 -vf "cropdetect" -f null -
```

### Network Analysis
```bash
# Test streaming endpoint
curl -v http://localhost:3000/api/stream/123

# Test with range request
curl -v -H "Range: bytes=0-1000" http://localhost:3000/api/stream/123
```

---

## 📞 Getting Help

If issues persist:

1. Check [Known Issues](../tasks/known-issues.md)
2. Review backend logs: `backend/logs/error.log`
3. Check browser console for errors
4. Test with different video file
5. Try different browser
6. Report issue on GitHub with:
   - Video file format and codec
   - Browser and version
   - Error messages
   - Network tab screenshots

**Last Updated**: October 30, 2025
