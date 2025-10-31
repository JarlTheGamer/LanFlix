# Quick Reference Guide

Fast access to common tasks and information.

## 🚀 Quick Start

```bash
# Backend
cd backend
npm install
cp .env.example .env
# Edit .env with your settings
npm run migrate
npm run dev

# Frontend (new terminal)
cd frontend
npm install
npm run dev
```

Access at: `http://localhost:5173`

## 🔧 Common Commands

### Backend
```bash
npm run dev          # Start development server
npm run build        # Build TypeScript
npm start            # Start production server
npm run migrate      # Run database migrations
npm run seed         # Seed database
```

### Frontend
```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run preview      # Preview production build
```

## 📁 Project Structure

```
lanflix/
├── backend/         # Node.js + TypeScript backend
│   ├── src/
│   │   ├── routes/      # API endpoints
│   │   ├── services/    # Business logic
│   │   ├── models/      # Database models
│   │   ├── clients/     # External APIs
│   │   └── utils/       # Utilities
│   └── data/            # SQLite database
│
├── frontend/        # Vanilla JS frontend
│   └── src/
│       ├── pages/       # HTML pages
│       ├── modules/     # JS modules
│       └── styles/      # CSS files
│
└── docs/            # Documentation wiki
    ├── getting-started/
    ├── architecture/
    ├── api/
    ├── features/
    ├── tasks/
    └── troubleshooting/
```

## 🌐 API Endpoints

### Content
```
GET  /api/content/discover          # Trending content
GET  /api/content/search?q=query    # Search
GET  /api/content/:id                # Content details
POST /api/content/:id/queue          # Queue download
```

### Library
```
GET  /api/library/movies             # List movies
GET  /api/library/series             # List TV series
GET  /api/library/recent             # Recently added
POST /api/jobs/scan-library          # Scan library
```

### Streaming
```
GET  /api/stream/:id                 # Stream video
POST /api/stream/:id/progress        # Update progress
GET  /api/stream/:id/subtitles       # Get subtitles
```

### Profiles
```
GET  /api/profiles                   # List profiles
POST /api/profiles                   # Create profile
GET  /api/profiles/:id/watchlist     # Get watchlist
```

## ⌨️ Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Space` / `K` | Play/Pause |
| `←` | Rewind 10s |
| `→` | Forward 10s |
| `↑` | Volume up |
| `↓` | Volume down |
| `M` | Mute |
| `F` | Fullscreen |
| `C` | Cycle subtitles |
| `Esc` | Exit fullscreen |

## 🔍 Troubleshooting

### No Audio
```bash
# Check video has audio
ffprobe -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 video.mp4

# Re-encode with AAC audio
ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4
```

### Video Won't Play
1. Check file exists
2. Verify file permissions
3. Check backend logs: `backend/logs/error.log`
4. Test API: `curl http://localhost:3000/api/stream/123`

### Port Already in Use
```bash
# Change backend port in .env
PORT=3001

# Frontend will auto-select next available port
```

## 📝 Environment Variables

### Required
```env
TMDB_API_KEY=your_key_here          # Get from themoviedb.org
MEDIA_ROOT_PATH=/path/to/media      # Your media folder
```

### Optional
```env
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_key
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_key
PROWLARR_URL=http://localhost:9696
PROWLARR_API_KEY=your_key
REDIS_URL=redis://localhost:6379
```

## 🗄️ Database

### Location
```
backend/data/lanflix.db
```

### Reset Database
```bash
cd backend
rm data/lanflix.db
npm run migrate
npm run seed
```

### Backup Database
```bash
cp backend/data/lanflix.db backend/data/lanflix.db.backup
```

## 📊 Logs

### Location
```
backend/logs/
├── combined.log    # All logs
├── error.log       # Errors only
└── exceptions.log  # Uncaught exceptions
```

### View Logs
```bash
# Tail all logs
tail -f backend/logs/combined.log

# View errors
tail -f backend/logs/error.log

# Search logs
grep "error" backend/logs/combined.log
```

## 🔐 API Keys

### TMDB
1. Go to https://www.themoviedb.org/
2. Create account
3. Settings → API → Request API Key
4. Add to `.env`: `TMDB_API_KEY=...`

### Sonarr
1. Open Sonarr web interface
2. Settings → General
3. Copy API Key
4. Add to `.env`: `SONARR_API_KEY=...`

### Radarr
1. Open Radarr web interface
2. Settings → General
3. Copy API Key
4. Add to `.env`: `RADARR_API_KEY=...`

## 🎬 Video Formats

### Recommended
- **Container**: MP4
- **Video Codec**: H.264
- **Audio Codec**: AAC
- **Resolution**: 1080p
- **Bitrate**: 5-10 Mbps

### Convert Video
```bash
# Convert to recommended format
ffmpeg -i input.mkv -c:v libx264 -preset medium -crf 23 -c:a aac -b:a 192k output.mp4

# Fast start for streaming
ffmpeg -i input.mp4 -c copy -movflags +faststart output.mp4
```

## 📱 Ports

| Service | Port |
|---------|------|
| Backend | 3000 |
| Frontend | 5173 |
| Sonarr | 8989 |
| Radarr | 7878 |
| Prowlarr | 9696 |
| Redis | 6379 |

## 🔗 Quick Links

- [Full Documentation](./README.md)
- [Quick Start Guide](./getting-started/quick-start.md)
- [API Reference](./api/overview.md)
- [Video Player Guide](./features/video-player.md)
- [Progressive Transcoding](./features/progressive-transcoding.md) ⭐ NEW
- [Known Issues](./tasks/known-issues.md)
- [Troubleshooting](./troubleshooting/video-playback.md)
- [Version History](./versions/README.md)
- [Current Version (v0.3)](./versions/v0.3/RELEASE-NOTES.md)

## 🆘 Getting Help

1. Check [Known Issues](./tasks/known-issues.md)
2. Review [Troubleshooting](./troubleshooting/video-playback.md)
3. Search [GitHub Issues](https://github.com/yourusername/lanflix/issues)
4. Ask in [Discussions](https://github.com/yourusername/lanflix/discussions)

## 📞 Health Checks

```bash
# Backend health
curl http://localhost:3000/health

# API status
curl http://localhost:3000/api/settings/api-status

# Test streaming
curl -I http://localhost:3000/api/stream/123
```

## 🎯 Common Tasks

### Add New Content
1. Browse or search in UI
2. Click content card
3. Click "Download" button
4. Wait for download to complete
5. Content appears in library

### Create Profile
```bash
curl -X POST http://localhost:3000/api/profiles \
  -H "Content-Type: application/json" \
  -d '{"name": "John", "avatar": "👤"}'
```

### Scan Library
```bash
curl -X POST http://localhost:3000/api/jobs/scan-library
```

### Clear Cache
```bash
# Redis cache
redis-cli FLUSHALL

# Restart backend to clear memory cache
```

## 🐛 Debug Mode

### Backend
```env
# In .env
LOG_LEVEL=debug
NODE_ENV=development
```

### Frontend
```javascript
// In browser console
localStorage.setItem('debug', 'true');
```

## 📈 Performance

### Check Database Size
```bash
ls -lh backend/data/lanflix.db
```

### Check Cache Hit Rate
```bash
redis-cli INFO stats | grep hit_rate
```

### Monitor Memory
```bash
# Backend memory usage
ps aux | grep node

# Redis memory
redis-cli INFO memory
```

## ⭐ Recent Updates

### Progressive Transcoding (YouTube-Style)
- Video now transcodes continuously ahead of playback (not just 10 seconds)
- Buffered content shown as **grey bar** in progress indicator
- Current playback shown as **red bar**
- Controls only hide when mouse leaves player area (not after 3 seconds)
- More intuitive and less distracting control behavior

See [Progressive Transcoding Guide](./features/progressive-transcoding.md) for details.

---

**Last Updated**: October 31, 2025  
**For detailed information, see the [full documentation](./README.md)**
