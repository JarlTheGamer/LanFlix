# Current Tasks

Active development tasks and work in progress.

## 🔴 Critical Priority

### Fix Video Audio Issues
**Status**: ✅ Completed  
**Assigned**: -  
**Completed**: October 30, 2025

**Problem**: Video streams have no audio when played through the API.

**Solution Implemented**: Jellyfin-style smart streaming
1. ✅ Direct play first (fastest, no transcoding)
2. ✅ FFprobe checks audio/video codec compatibility
3. ✅ Smart transcoding only when needed:
   - Audio incompatible → transcode audio only (copy video)
   - Video incompatible → transcode both
   - Both compatible → direct play
4. ✅ Added `/api/stream/:id/info` endpoint to debug media files

**How It Works**:
- Browser-compatible codecs: AAC, MP3, Opus, Vorbis (audio) / H.264, VP8, VP9, AV1 (video)
- Incompatible audio (DTS, AC3, TrueHD, etc.) → transcoded to AAC on-the-fly
- Video stream copied without re-encoding (fast!)
- Force transcode with `?transcode=true` query parameter

**Files Modified**:
- `backend/src/routes/streaming.routes.ts` - Smart streaming logic
- `backend/src/utils/ffmpeg.ts` - FFmpeg probe and transcode utilities

**Testing**:
```bash
# Check media info
curl http://localhost:3000/api/stream/1/info

# Direct play (if compatible)
curl http://localhost:3000/api/stream/1

# Force transcode
curl http://localhost:3000/api/stream/1?transcode=true
```

---

### Implement Continue Watching Row
**Status**: Not Started  
**Priority**: High  
**Estimated**: 4 hours

**Description**: Add a "Continue Watching" row to the homepage showing content with partial watch progress.

**Requirements**:
- Query watch history for incomplete content (>1 min watched, <90% complete)
- Display with progress bar overlay
- Sort by most recently watched
- Limit to 10 items
- Resume playback from saved position

**Implementation**:
```typescript
// backend/src/services/library.service.ts
async getContinueWatching(profileId: number, limit = 10) {
  const watchHistory = await WatchHistory.findAll({
    where: {
      profileId,
      completed: false,
      progressSeconds: { [Op.gt]: 60 }
    },
    include: [Content],
    order: [['lastWatchedAt', 'DESC']],
    limit
  });
  
  return watchHistory.map(wh => ({
    ...wh.content,
    watchProgress: {
      progressSeconds: wh.progressSeconds,
      durationSeconds: wh.durationSeconds,
      progressPercent: (wh.progressSeconds / wh.durationSeconds) * 100
    }
  }));
}
```

**Files to Create/Modify**:
- `backend/src/services/library.service.ts` - Add getContinueWatching method
- `backend/src/routes/library.routes.ts` - Add GET /api/library/continue-watching
- `frontend/src/modules/content-display.js` - Add continue watching row
- `frontend/src/styles/main.css` - Add progress bar styles

---

## 🟡 High Priority

### Add Search UI
**Status**: ✅ Completed  
**Assigned**: -  
**Completed**: October 30, 2025

**Description**: Implement search functionality in the frontend.

**Implementation**:
- ✅ Search button in header (magnifying glass icon)
- ✅ Search overlay modal with input
- ✅ Real-time search with 300ms debounce
- ✅ Search across movies and TV shows via API
- ✅ Grid display of results with posters
- ✅ Keyboard shortcut (/) to open search
- ✅ Escape key to close
- ✅ Click result to open content modal

**Files Created**:
- `frontend/src/modules/search.js` - Search module with debounced search
- `frontend/src/styles/search.css` - Search UI styling
- Updated `frontend/src/pages/index.html` - Added search button
- Updated `frontend/src/scripts/main.js` - Initialize search module

---

### Download Queue Management UI
**Status**: ✅ Completed  
**Priority**: High  
**Completed**: October 31, 2025

**Description**: Created a unified notifications and downloads center page.

**Implementation**:
- ✅ Created notifications/downloads page with 3 tabs (Notifications, Downloads, Jobs)
- ✅ Added notification bell button to header (left of settings)
- ✅ Notification badge shows unread count
- ✅ Displays keep-watching notifications from auto-delete system
- ✅ Shows background job status and allows manual triggering
- ✅ Placeholder for download queue (ready for future implementation)
- ✅ Auto-refresh every 5 seconds for active tabs
- ✅ Responsive design for mobile/tablet

**Files Created**:
- `frontend/src/pages/notifications.html` - Notifications & Downloads page
- `frontend/src/modules/notifications-manager.js` - Notifications management
- `frontend/src/modules/notification-badge.js` - Badge update module
- `frontend/src/scripts/notifications-main.js` - Page initialization
- `frontend/src/styles/notifications.css` - Page styling

**Files Modified**:
- `frontend/src/pages/index.html` - Added notifications button
- `frontend/src/pages/settings.html` - Added notifications button
- `frontend/src/pages/my-list.html` - Added notifications button
- `frontend/src/pages/admin.html` - Added notifications button
- `frontend/src/styles/main.css` - Added notification badge styles
- `frontend/src/scripts/main.js` - Initialize notification badge

---

### Settings Page UI
**Status**: Not Started  
**Priority**: High  
**Estimated**: 4 hours

**Description**: Create settings page for user preferences.

**Requirements**:
- Video quality preferences
- Auto-delete configuration
- Subtitle language preferences
- Theme selection
- External service status display
- API key management

**Files to Create**:
- `frontend/src/pages/settings.html` - Settings page (exists but incomplete)
- `frontend/src/modules/settings.js` - Settings management
- `frontend/src/styles/settings.css` - Settings styling

---

## 🟢 Medium Priority

### WebSocket Support for Real-Time Updates
**Status**: Not Started  
**Priority**: Medium  
**Estimated**: 6 hours

**Description**: Add WebSocket support for real-time notifications.

**Use Cases**:
- Download progress updates
- Library scan notifications
- New content alerts
- Watch party synchronization

**Implementation**:
```typescript
// backend/src/app.ts
import { Server } from 'socket.io';

const io = new Server(server, {
  cors: { origin: '*' }
});

io.on('connection', (socket) => {
  socket.on('subscribe:downloads', (profileId) => {
    socket.join(`downloads:${profileId}`);
  });
});
```

**Files to Create/Modify**:
- `backend/src/websocket/index.ts` - WebSocket server setup
- `backend/src/services/download-manager.service.ts` - Emit progress events
- `frontend/src/modules/websocket-client.js` - WebSocket client

---

### Recommendations Engine
**Status**: Not Started  
**Priority**: Medium  
**Estimated**: 8 hours

**Description**: Build content recommendation system based on watch history.

**Algorithm**:
1. Extract top genres from watch history
2. Find similar content from TMDB
3. Filter out already watched content
4. Score based on ratings and popularity
5. Return top 20 recommendations

**Files to Create**:
- `backend/src/services/recommendations.service.ts` - Recommendation logic
- `backend/src/routes/recommendations.routes.ts` - API endpoints

---

### Parental Controls
**Status**: Not Started  
**Priority**: Medium  
**Estimated**: 6 hours

**Description**: Add parental control features.

**Requirements**:
- Kids profile flag
- Maximum content rating (G, PG, PG-13, R)
- PIN protection for adult profiles
- Content filtering based on rating

**Database Changes**:
```sql
ALTER TABLE profiles ADD COLUMN is_kids_profile BOOLEAN DEFAULT 0;
ALTER TABLE profiles ADD COLUMN max_rating VARCHAR(10) DEFAULT 'R';
ALTER TABLE profiles ADD COLUMN pin_protected BOOLEAN DEFAULT 0;
ALTER TABLE profiles ADD COLUMN pin_code VARCHAR(255);
```

---

## 🔵 Low Priority

### Smart Downloads
**Status**: Not Started  
**Priority**: Low  
**Estimated**: 10 hours

**Description**: Automatically download next episode based on viewing patterns.

**Features**:
- Auto-download next episode when 80% through current
- Download based on viewing schedule
- Predictive downloads for binge-watching
- Configurable per profile

---

### Transcoding Support
**Status**: Not Started  
**Priority**: Low  
**Estimated**: 15 hours

**Description**: Add FFmpeg transcoding for incompatible formats.

**Features**:
- On-the-fly transcoding
- Multiple quality levels
- Adaptive bitrate streaming (HLS/DASH)
- Hardware acceleration support

---

### Collections & Playlists
**Status**: Not Started  
**Priority**: Low  
**Estimated**: 8 hours

**Description**: Allow users to create custom collections.

**Features**:
- User-created collections
- Auto-collections (Marvel, Star Wars, etc.)
- Playlist support
- Share collections between profiles

---

## 📋 Backlog

### Watch Together / Sync Play
Synchronized playback for multiple users watching together.

### Advanced Analytics
Watch time statistics, viewing patterns, genre preferences.

### Social Features
Share watchlist, see what friends are watching, ratings and reviews.

### Mobile Apps
Native Android and iOS apps using Capacitor.

### Desktop App
Electron-based desktop application for Windows, Mac, Linux.

### Plugin System
Allow third-party plugins to extend functionality.

---

## ✅ Recently Completed

### Hardware-Accelerated Transcoding with Seeking
**Completed**: October 30, 2025
- Implemented Jellyfin-style hardware-accelerated transcoding
- NVDEC (GPU decode) + NVENC (GPU encode) pipeline
- Configurable presets (p1-p7) for speed/quality balance
- Smart seeking: reloads stream at target time for transcoded content
- Settings for hardware acceleration, preset, audio-only, video-only
- MPEG-TS streaming format for better seeking support
- Handles client disconnects gracefully

### Series Playback Fix
**Completed**: October 30, 2025
- Fixed "Media file is empty" error when playing series
- Series play button now finds first available episode
- Shows alert if no episodes are downloaded
- Episodes play correctly with episodeId parameter

### Video Player Audio Fix (Partial)
- Updated Content-Type detection for different video formats
- Added proper cache control headers
- Improved video element initialization
- **Status**: Needs verification with actual video files

---

## 📝 Notes

- Tasks are prioritized based on user impact and dependencies
- Estimated times are for a single developer
- Some tasks may require external service configuration
- Breaking changes should be documented in migration guides

**Last Updated**: October 30, 2025
