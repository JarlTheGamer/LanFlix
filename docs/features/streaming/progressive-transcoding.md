# Progressive Transcoding (YouTube-Style)

## Overview
The video player now implements progressive transcoding similar to YouTube, where the video transcodes ahead of the current playback position rather than just a few seconds at a time.

## Features

### 1. Progressive Buffering
- **Continuous Transcoding**: When transcoding is active, FFmpeg continues to transcode ahead of the current playback position
- **Visual Indicator**: The progress bar shows buffered content in grey, with the current playback position in red
- **Smooth Playback**: Reduces buffering interruptions by transcoding ahead

### 2. Buffered Progress Visualization
The progress bar now has three visual states:
- **Dark Grey**: Unbuffered/untranscoded content
- **Light Grey**: Buffered/transcoded content ready to play
- **Red**: Current playback position

This gives users a clear indication of:
- How much content is ready to play
- How far ahead the transcoding has progressed
- Where they are in the video

### 3. Improved Controls Behavior
Controls now behave more intuitively:
- **Mouse Movement**: Controls appear when mouse moves over the player
- **Mouse Leave**: Controls only hide when the mouse leaves the player area (not when it stops moving)
- **Paused State**: Controls always stay visible when video is paused
- **Cursor**: Cursor is visible when controls are shown, hidden when controls are hidden

## Technical Implementation

### Backend Changes
- Added `progressCallback` parameter to transcode stream methods
- FFmpeg progress events are now tracked and can be exposed to clients
- Transcoding continues ahead of playback position automatically

### Frontend Changes
- Added `updateBufferedProgress()` method to track buffered ranges
- Progress bar now shows buffered content using the video element's `buffered` property
- Controls visibility logic updated to only hide on mouse leave
- Added `bufferedEnd` tracking for transcoded content

### CSS Updates
- Buffered progress bar (grey) positioned behind playback progress (red)
- Z-index layering ensures proper visual hierarchy
- Removed auto-hide timeout behavior
- Controls now only hide when mouse leaves player area

## User Experience

### Before
- Transcoding happened in small chunks
- No visual indication of buffered content
- Controls would hide after 3 seconds of no mouse movement
- Users had to move mouse to keep controls visible

### After
- Transcoding happens continuously ahead of playback
- Clear visual indication of buffered/transcoded content (grey bar)
- Controls stay visible as long as mouse is over player
- More intuitive and less distracting control behavior

## Browser Compatibility
- Uses standard HTML5 video `buffered` property
- Works with all modern browsers
- Gracefully handles browsers with limited buffering support
