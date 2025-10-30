# Current Tasks

Active development tasks and work in progress.

## 🔴 Critical Priority

### Fix Video Audio Issues
**Status**: In Progress  
**Assigned**: -  
**Due**: ASAP

**Problem**: Video streams have no audio when played through the API.

**Investigation**:
- Streaming route serves files correctly with range support
- Content-Type headers now properly detect file format (mp4, mkv, webm, etc.)
- Issue may be with source video files lacking audio tracks

**Solution**:
1. ✅ Updated streaming route to detect proper Content-Type based on file extension
2. ✅ Added Cache-Control headers to prevent caching issues
3. ⏳ Need to verify source video files have audio tracks using ffprobe
4. ⏳ Consider adding FFmpeg transcoding for incompatible formats

**Files Modified**:
- `backend/src/routes/streaming.routes.ts`
- `frontend/src/modules/video-player.js`

**Next Steps**:
- Test with known good video files
- Add FFmpeg probe to detect audio tracks
- Implement transcoding if needed

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
**Status**: Not Started  
**Priority**: High  
**Estimated**: 3 hours

**Description**: Implement search functionality in the frontend.

**Requirements**:
- Search input in header
- Real-time search as user types (debounced)
- Search across movies and TV shows
- Display results in modal or dedicated page
- Keyboard shortcut (/) to focus search

**Files to Create/Modify**:
- `frontend/src/modules/search.js` - New search module
- `frontend/src/pages/index.html` - Add search input
- `frontend/src/styles/search.css` - Search styling

---

### Download Queue Management UI
**Status**: Not Started  
**Priority**: High  
**Estimated**: 5 hours

**Description**: Create a page to view and manage download queue.

**Requirements**:
- List all queued downloads
- Show download progress with progress bars
- Display ETA and download speed
- Allow cancellation of downloads
- Show completed downloads
- Real-time updates via WebSocket

**Files to Create**:
- `frontend/src/pages/downloads.html` - Downloads page
- `frontend/src/modules/download-manager.js` - Download management
- `frontend/src/styles/downloads.css` - Downloads styling
- `backend/src/routes/downloads.routes.ts` - Download API endpoints

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
