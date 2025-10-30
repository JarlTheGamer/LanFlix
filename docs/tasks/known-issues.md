# Known Issues

Current bugs, limitations, and workarounds.

## 🔴 Critical Issues

### No Audio in Video Streams
**Severity**: Critical  
**Reported**: October 30, 2025  
**Status**: Under Investigation

**Description**: Video files stream correctly but have no audio when played through the web player.

**Symptoms**:
- Video plays normally
- Progress bar works
- Controls function properly
- No audio output

**Investigation**:
- Streaming backend serves files correctly
- Content-Type headers are properly set
- Range requests work for seeking
- Issue may be with source video files

**Possible Causes**:
1. Source video files missing audio tracks
2. Audio codec not supported by browser
3. Browser autoplay policy blocking audio
4. Incorrect MIME type for certain formats

**Workarounds**:
1. Check video files with ffprobe to verify audio tracks exist:
   ```bash
   ffprobe -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 video.mp4
   ```

2. Re-encode videos with compatible audio codec:
   ```bash
   ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4
   ```

3. Try different video formats (MP4, MKV, WebM)

**Fix in Progress**:
- Updated streaming route to detect proper Content-Type
- Added cache control headers
- Improved video player initialization
- Next: Add FFmpeg probe to detect audio tracks

---

## 🟡 High Priority Issues

### Episode Loading Rate Limit
**Severity**: High  
**Reported**: October 2025  
**Status**: Mitigated

**Description**: Loading all episodes for a series with many seasons triggers Sonarr rate limits.

**Symptoms**:
- Series with 10+ seasons fail to load all episodes
- API returns 429 Too Many Requests
- Some episodes missing from UI

**Workaround**: Progressive loading implemented - loads 3 seasons at a time with delays.

**Permanent Fix**: Implement pagination or lazy loading for episodes.

---

### Offline Mode Limitations
**Severity**: Medium  
**Reported**: October 2025  
**Status**: Known Limitation

**Description**: Some features don't work when external services are offline.

**Affected Features**:
- Content discovery (requires TMDB)
- Download queuing (requires Sonarr/Radarr)
- New metadata fetching

**Working Features**:
- Library browsing (uses cached data)
- Video streaming
- Watch history
- Profile management

**Workaround**: Ensure external services are running for full functionality.

---

## 🟢 Medium Priority Issues

### Large Library Performance
**Severity**: Medium  
**Reported**: October 2025  
**Status**: Optimization Needed

**Description**: Libraries with 1000+ items can be slow to load and render.

**Symptoms**:
- Slow initial page load
- Laggy scrolling
- High memory usage

**Workarounds**:
1. Implement pagination (20-50 items per page)
2. Use virtual scrolling for large lists
3. Add lazy loading for images
4. Implement infinite scroll

**Planned Fix**: Virtual scrolling and pagination in next release.

---

### Subtitle Sync Issues
**Severity**: Medium  
**Reported**: October 2025  
**Status**: Under Investigation

**Description**: Subtitles occasionally go out of sync during playback.

**Symptoms**:
- Subtitles appear too early or too late
- Sync gets worse over time
- Seeking makes it worse

**Possible Causes**:
- Frame rate mismatch
- Subtitle file timing issues
- Video player time drift

**Workaround**: Reload the video or try different subtitle file.

---

### Mobile Keyboard Navigation
**Severity**: Low  
**Reported**: October 2025  
**Status**: Known Limitation

**Description**: Keyboard shortcuts don't work on mobile devices.

**Affected Shortcuts**:
- Space (play/pause)
- Arrow keys (seek)
- F (fullscreen)
- M (mute)

**Reason**: Mobile browsers don't expose keyboard events the same way.

**Workaround**: Use on-screen controls on mobile devices.

---

## 🔵 Low Priority Issues

### Cache Invalidation Timing
**Severity**: Low  
**Reported**: October 2025  
**Status**: Minor Issue

**Description**: Cached data sometimes persists longer than expected.

**Symptoms**:
- Old metadata shown after update
- Deleted content still appears briefly
- Profile changes not immediately reflected

**Workaround**: Clear cache manually or wait for TTL expiration.

**Fix**: Implement cache invalidation events.

---

### Download Progress Not Real-Time
**Severity**: Low  
**Reported**: October 2025  
**Status**: Enhancement Needed

**Description**: Download progress updates every 30 seconds instead of real-time.

**Reason**: Polling-based updates to avoid overwhelming the API.

**Planned Fix**: Implement WebSocket for real-time updates.

---

### Theme Switching Flicker
**Severity**: Low  
**Reported**: October 2025  
**Status**: Cosmetic Issue

**Description**: Brief flicker when switching between light/dark themes.

**Workaround**: Theme preference is saved and applied on next load.

---

## 🐛 Browser-Specific Issues

### Safari Video Playback
**Severity**: Medium  
**Browser**: Safari 15+  
**Status**: Under Investigation

**Description**: Some video formats don't play in Safari.

**Affected Formats**:
- MKV files
- HEVC/H.265 codec
- Some WebM files

**Workaround**: Use MP4 with H.264 codec for Safari compatibility.

---

### Firefox Fullscreen Exit
**Severity**: Low  
**Browser**: Firefox  
**Status**: Known Limitation

**Description**: Exiting fullscreen with ESC key doesn't always work.

**Workaround**: Use the fullscreen button or F key.

---

### Chrome Autoplay Policy
**Severity**: Low  
**Browser**: Chrome  
**Status**: Browser Policy

**Description**: Videos don't autoplay on page load.

**Reason**: Chrome's autoplay policy requires user interaction.

**Workaround**: User must click play button to start video.

---

## 📱 Platform-Specific Issues

### Android TV Remote Navigation
**Severity**: Medium  
**Platform**: Android TV  
**Status**: Optimization Needed

**Description**: D-pad navigation sometimes skips elements.

**Workaround**: Use touch input or adjust focus manually.

**Planned Fix**: Improve focus management for TV remotes.

---

### iOS Safari Video Controls
**Severity**: Low  
**Platform**: iOS Safari  
**Status**: Known Limitation

**Description**: Custom video controls sometimes conflict with native iOS controls.

**Workaround**: Use native iOS controls on mobile Safari.

---

## 🔧 Configuration Issues

### Redis Connection Timeout
**Severity**: Medium  
**Reported**: October 2025  
**Status**: Configuration Issue

**Description**: Redis connection times out after period of inactivity.

**Solution**: Configure Redis keepalive in `.env`:
```env
REDIS_URL=redis://localhost:6379?keepAlive=30000
```

---

### Database Lock Errors
**Severity**: Low  
**Reported**: October 2025  
**Status**: SQLite Limitation

**Description**: Occasional "database is locked" errors under heavy load.

**Reason**: SQLite doesn't handle concurrent writes well.

**Solutions**:
1. Enable WAL mode (already implemented)
2. Reduce concurrent operations
3. Consider PostgreSQL for production

---

## 📝 Reporting New Issues

When reporting issues, please include:

1. **Environment**:
   - OS and version
   - Browser and version
   - Node.js version
   - Lanflix version

2. **Steps to Reproduce**:
   - Detailed steps to trigger the issue
   - Expected behavior
   - Actual behavior

3. **Logs**:
   - Backend logs from `backend/logs/`
   - Browser console errors
   - Network tab screenshots

4. **Additional Context**:
   - Video file format and codec
   - External service versions
   - Configuration details

**Submit issues on GitHub**: [github.com/yourusername/lanflix/issues](https://github.com/yourusername/lanflix/issues)

---

**Last Updated**: October 30, 2025
