# Transcoding Settings & Video Seeking Fix

## Issues Fixed

### 1. Settings Not Saving (Transcoding Settings)
**Problem**: Transcoding settings in settings.html were not being saved to the backend, and even when saved, they weren't being used by the streaming backend.

**Root Cause**: 
- Settings manager was trying to save streaming preferences but wasn't properly handling the save confirmation
- No visual feedback when settings were saved
- Custom select displays weren't updating after loading saved settings

**Solution**:
- Enhanced `saveStreamingPreferences()` to show visual confirmation when settings are saved
- Added `showSaveNotification()` method to display a green success message
- Added `updateCustomSelectDisplays()` to properly update the UI after loading settings
- Improved error handling and logging for debugging

**Files Modified**:
- `frontend/src/modules/settings-manager.js`
  - Enhanced save confirmation with visual notification
  - Added method to update custom select displays
  - Improved settings loading and caching
- `backend/src/routes/streaming.routes.ts`
  - Load user's transcoding preferences from database
  - Respect `audioTranscoding` and `videoTranscoding` settings
  - Only transcode if both compatibility check fails AND user has enabled transcoding

### 2. Video Seeking Not Working for Transcoded Streams
**Problem**: When streaming transcoded content, seeking (jumping to different timestamps) wasn't working properly.

**Root Cause**:
- Transcoded streams don't support HTTP range requests like direct play files
- The backend wasn't handling the `start` query parameter for seeking in transcoded streams
- Video player was trying to use normal seeking which doesn't work with live transcoding

**Solution**:
- Modified streaming route to accept `start` query parameter for transcoded streams
- Updated `createCPUTranscodeStream` to use the `startTime` parameter when transcoding
- Video player already had `reloadStreamAtTime()` method that properly reloads the stream with a new start position
- Added proper headers to indicate transcoding mode to the client

**Files Modified**:
- `backend/src/routes/streaming.routes.ts`
  - Added support for `start` query parameter
  - Pass `startTime` to transcode stream creation
  - Improved header handling for transcoded streams

**How It Works Now**:
1. User seeks to a new position in transcoded video
2. Video player detects it's a transcoded stream (via `X-Transcode-Mode` header)
3. Player calls `reloadStreamAtTime(targetTime)` instead of normal seeking
4. New stream URL is created with `?start=XXX` parameter
5. Backend starts transcoding from that timestamp
6. Playback resumes from the new position

## Testing

### Test Transcoding Settings Save:
1. Go to Settings → Playback
2. Change transcoding mode, preset, or toggle options
3. Look for green "Transcoding settings saved!" notification in top-right
4. Refresh page and verify settings are still applied

### Test Transcoding Settings Applied:
1. Go to Settings → Playback
2. Turn OFF "Audio Transcoding" toggle
3. Play a video with incompatible audio (e.g., DTS, AC3)
4. Check browser console - should see "Direct play" instead of "Transcoding audio"
5. Video may not play if browser doesn't support the codec (this is expected)
6. Turn audio transcoding back ON
7. Reload video - should now see "Transcoding audio only" and play correctly

### Test Video Seeking:
1. Play a video that requires audio transcoding (check console for "Transcoding audio only")
2. Try seeking to different positions using:
   - Progress bar clicks
   - Arrow keys (←/→ for 10s jumps)
   - Keyboard shortcuts
3. Video should reload at new position and continue playing smoothly

## Technical Details

### Transcoding Settings Storage
Settings are stored per-profile in the database:
- Key: `streamingPreferences_{profileId}`
- Value: JSON object with transcoding preferences
- Endpoint: `PUT /api/settings/streaming/:profileId`

### Seeking Implementation
For transcoded streams:
- Uses `?start=XXX` query parameter instead of HTTP Range headers
- FFmpeg starts encoding from specified timestamp using `-ss` flag
- Stream is reloaded completely (not a true seek, but works seamlessly)
- Video player maintains a `startOffset` to track position in original video
- `currentTime` = `videoElement.currentTime` + `startOffset`
- Progress tracking continues normally after reload

For direct play:
- Uses standard HTTP Range requests (206 Partial Content)
- True seeking without reloading
- More efficient but only works with compatible files

### Additional Fix: Time Offset Tracking
**Problem**: After reloading a transcoded stream at a specific time, the video element's `currentTime` would start at 0, making it appear the video jumped back to the beginning.

**Solution**: 
- Added `startOffset` property to track where in the original video we started
- When reloading stream at time T, set `startOffset = T`
- Calculate actual position as `currentTime = videoElement.currentTime + startOffset`
- Progress bar and time display now show correct position in original video
