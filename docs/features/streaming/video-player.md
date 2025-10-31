# Video Player

Comprehensive guide to Lanflix's custom HTML5 video player.

## Overview

Lanflix features a custom-built video player with:
- ✅ Full playback controls
- ✅ Keyboard shortcuts
- ✅ Subtitle support
- ✅ Progress tracking
- ✅ Resume playback
- ✅ Fullscreen mode
- ✅ Remote control support (Android TV)

## Features

### Playback Controls

**Play/Pause**
- Click video to toggle
- Click play/pause button
- Press `Space` or `K`

**Seek**
- Click progress bar
- Press `←` (back 10s)
- Press `→` (forward 10s)
- Drag progress thumb

**Volume**
- Click volume button to mute/unmute
- Drag volume slider
- Press `↑` (increase)
- Press `↓` (decrease)
- Press `M` to mute

**Fullscreen**
- Click fullscreen button
- Press `F`
- Press `Esc` to exit

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Space` / `K` | Play/Pause |
| `←` | Rewind 10 seconds |
| `→` | Forward 10 seconds |
| `↑` | Volume up |
| `↓` | Volume down |
| `M` | Mute/Unmute |
| `F` | Toggle fullscreen |
| `C` | Cycle subtitles |
| `Esc` | Exit fullscreen |

### Subtitles

**Supported Formats**:
- SRT (SubRip)
- VTT (WebVTT)
- ASS (Advanced SubStation Alpha)
- SSA (SubStation Alpha)

**Features**:
- Multiple language support
- Automatic detection
- Cycle through subtitles with `C`
- Turn off subtitles

**File Naming**:
```
movie.mp4
movie.en.srt    # English
movie.es.srt    # Spanish
movie.fr.srt    # French
```

### Progress Tracking

**Automatic Saving**:
- Progress saved every 10 seconds
- Final progress saved on pause/stop
- Marked complete at 90% watched

**Resume Playback**:
- Automatically resumes from last position
- Shows "Continue Watching" option
- Skip to beginning if desired

### Video Quality

**Supported Formats**:
- MP4 (H.264/H.265)
- MKV (Matroska)
- WebM
- AVI
- MOV

**Recommended Settings**:
- **Codec**: H.264 (best compatibility)
- **Container**: MP4
- **Audio**: AAC
- **Resolution**: 1080p or lower
- **Bitrate**: 5-10 Mbps

### Adaptive Controls

**Desktop**:
- Mouse hover shows controls
- Auto-hide after 3 seconds
- Always visible when paused

**Mobile**:
- Tap to show/hide controls
- Touch-optimized buttons
- Swipe gestures (planned)

**TV Remote**:
- D-pad navigation
- OK button to play/pause
- Back button to exit

## Player Architecture

### Initialization

```javascript
import VideoPlayer from './modules/video-player.js';

const videoElement = document.getElementById('video');
const player = new VideoPlayer(videoElement, profileId);

await player.initialize(
  contentId,
  contentType,
  episodeId,
  startPosition
);
```

### Player States

```javascript
// Playing
player.isPlaying === true

// Paused
player.isPlaying === false

// Buffering
player.videoElement.readyState < 3

// Ended
player.videoElement.ended === true
```

### Events

```javascript
// Play event
videoElement.addEventListener('play', () => {
  console.log('Video started playing');
});

// Pause event
videoElement.addEventListener('pause', () => {
  console.log('Video paused');
});

// Time update
videoElement.addEventListener('timeupdate', () => {
  console.log('Current time:', videoElement.currentTime);
});

// Ended
videoElement.addEventListener('ended', () => {
  console.log('Video finished');
});
```

## Customization

### Styling

```css
/* Player container */
.player-container {
  background: #000;
  position: relative;
  width: 100%;
  aspect-ratio: 16/9;
}

/* Video element */
video {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* Controls */
.player-controls {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(transparent, rgba(0,0,0,0.8));
  padding: 20px;
}

/* Progress bar */
.player-progress-bar {
  height: 4px;
  background: #e50914;
  transition: height 0.2s;
}

.player-progress-bar:hover {
  height: 6px;
}
```

### Custom Controls

```javascript
// Add custom button
const customButton = document.createElement('button');
customButton.className = 'player-btn';
customButton.innerHTML = '<svg>...</svg>';
customButton.addEventListener('click', () => {
  // Custom action
});

document.querySelector('.player-controls-right')
  .appendChild(customButton);
```

## Streaming Protocol

### HTTP Range Requests

The player uses HTTP range requests for efficient streaming:

```javascript
// Request specific byte range
fetch('/api/stream/123', {
  headers: {
    'Range': 'bytes=0-1000'
  }
});

// Response
// Status: 206 Partial Content
// Content-Range: bytes 0-1000/5000000
// Content-Length: 1001
```

**Benefits**:
- Efficient seeking
- Reduced bandwidth
- Faster start time
- Resume support

### Buffering Strategy

```javascript
// Check buffered ranges
const buffered = videoElement.buffered;
for (let i = 0; i < buffered.length; i++) {
  console.log(
    'Buffered:',
    buffered.start(i),
    'to',
    buffered.end(i)
  );
}

// Preload strategy
videoElement.preload = 'metadata'; // Only metadata
videoElement.preload = 'auto';     // Preload video
videoElement.preload = 'none';     // No preload
```

## Performance Optimization

### Hardware Acceleration

Enable in browser settings:
- **Chrome**: `chrome://settings/system`
- **Firefox**: `about:preferences#performance`
- **Edge**: `edge://settings/system`

### Reduce Buffering

```javascript
// Adjust buffer size
videoElement.addEventListener('progress', () => {
  const buffered = videoElement.buffered;
  if (buffered.length > 0) {
    const bufferedEnd = buffered.end(buffered.length - 1);
    const currentTime = videoElement.currentTime;
    const bufferedAhead = bufferedEnd - currentTime;
    
    // If buffered more than 30 seconds ahead, pause loading
    if (bufferedAhead > 30) {
      // Pause loading (implementation specific)
    }
  }
});
```

### Optimize Video Files

```bash
# Optimize for streaming (fast start)
ffmpeg -i input.mp4 -c copy -movflags +faststart output.mp4

# Reduce bitrate
ffmpeg -i input.mp4 -c:v libx264 -b:v 5M -c:a aac -b:a 192k output.mp4

# Create multiple quality versions
ffmpeg -i input.mp4 -c:v libx264 -b:v 2M -s 1280x720 output_720p.mp4
ffmpeg -i input.mp4 -c:v libx264 -b:v 5M -s 1920x1080 output_1080p.mp4
```

## Troubleshooting

### No Audio

See [Video Playback Troubleshooting](../troubleshooting/video-playback.md#-no-audio-in-videos)

**Quick Fixes**:
1. Check video file has audio track
2. Verify audio codec compatibility
3. Check browser console for errors
4. Try different browser

### Video Won't Play

**Common Causes**:
- File not found
- Unsupported format
- Network issue
- Browser compatibility

**Solutions**:
1. Check file exists
2. Verify file permissions
3. Check network tab in DevTools
4. Try different format

### Stuttering/Buffering

**Causes**:
- Slow network
- High bitrate
- CPU overload
- Disk I/O

**Solutions**:
1. Enable hardware acceleration
2. Reduce video quality
3. Close other applications
4. Check network speed

### Subtitles Not Working

**Causes**:
- Wrong format
- Encoding issues
- File not found
- Timing issues

**Solutions**:
1. Convert to VTT format
2. Check file encoding (UTF-8)
3. Verify file path
4. Adjust subtitle timing

## Advanced Features (Planned)

### Picture-in-Picture

```javascript
// Enter PiP mode
await videoElement.requestPictureInPicture();

// Exit PiP mode
await document.exitPictureInPicture();
```

### Playback Speed

```javascript
// Change playback speed
videoElement.playbackRate = 1.5; // 1.5x speed
videoElement.playbackRate = 0.5; // 0.5x speed
```

### Quality Selection

```javascript
// Switch quality
player.setQuality('1080p');
player.setQuality('720p');
player.setQuality('auto');
```

### Skip Intro/Outro

```javascript
// Skip intro
player.skipIntro(); // Skips to 1:30

// Skip outro
player.skipOutro(); // Skips to next episode
```

## API Reference

### VideoPlayer Class

```javascript
class VideoPlayer {
  constructor(videoElement, profileId)
  
  // Methods
  async initialize(contentId, contentType, episodeId, startPosition)
  play()
  pause()
  togglePlayPause()
  seek(time)
  setVolume(volume)
  toggleMute()
  toggleFullscreen()
  destroy()
  
  // Properties
  isPlaying: boolean
  currentTime: number
  duration: number
  volume: number
  isMuted: boolean
  isFullscreen: boolean
}
```

### Methods

**initialize(contentId, contentType, episodeId, startPosition)**
- Initializes the player with content
- Loads subtitles
- Sets up controls
- Starts playback

**play()**
- Starts video playback
- Returns Promise

**pause()**
- Pauses video playback

**seek(time)**
- Seeks to specific time in seconds
- Clamps to valid range

**setVolume(volume)**
- Sets volume (0.0 to 1.0)
- Clamps to valid range

**destroy()**
- Cleans up player
- Stops progress tracking
- Removes event listeners

## Best Practices

1. **Always handle errors**
   ```javascript
   player.play().catch(error => {
     console.error('Playback failed:', error);
   });
   ```

2. **Clean up on unmount**
   ```javascript
   window.addEventListener('beforeunload', () => {
     player.destroy();
   });
   ```

3. **Save progress regularly**
   ```javascript
   setInterval(() => {
     player.saveProgress();
   }, 10000);
   ```

4. **Handle network errors**
   ```javascript
   videoElement.addEventListener('error', (e) => {
     console.error('Video error:', e);
     // Show error message to user
   });
   ```

## Resources

- [MDN: HTMLMediaElement](https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement)
- [Video Playback Troubleshooting](../troubleshooting/video-playback.md)
- [Streaming API](../api/streaming.md)

**Last Updated**: October 30, 2025
