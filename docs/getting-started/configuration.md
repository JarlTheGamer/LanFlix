# Configuration Guide

Complete configuration reference for Lanflix.

## Environment Variables

### Server Configuration

```env
# Server port (default: 3000)
PORT=3000

# Environment mode: development, production, test
NODE_ENV=development

# Logging level: error, warn, info, debug
LOG_LEVEL=info
```

### Database Configuration

```env
# SQLite database file path
DATABASE_PATH=./data/lanflix.db
```

### Media Storage

```env
# Root directory for media files
MEDIA_ROOT_PATH=/path/to/media

# Cache directories for images
POSTER_CACHE_PATH=./data/posters
BACKDROP_CACHE_PATH=./data/backdrops
```

### External Services

#### Sonarr (TV Series)
```env
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_sonarr_api_key
```

#### Radarr (Movies)
```env
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_radarr_api_key
```

#### Prowlarr (Indexers)
```env
PROWLARR_URL=http://localhost:9696
PROWLARR_API_KEY=your_prowlarr_api_key
```

#### TMDB (Metadata)
```env
TMDB_API_KEY=your_tmdb_api_key
```

### Optional Services

#### Redis (Caching)
```env
REDIS_URL=redis://localhost:6379
```

#### FFmpeg (Transcoding)
```env
# Optional: Specify FFmpeg binary path
FFMPEG_PATH=/usr/local/bin/ffmpeg
FFPROBE_PATH=/usr/local/bin/ffprobe
```

## Application Settings

Settings are stored in the database and can be configured via the Settings page or API.

### Transcoding Settings

```json
{
  "transcoding": {
    "enabled": true,
    "videoCodec": "libx264",
    "audioCodec": "aac",
    "preset": "medium",
    "crf": 23,
    "maxBitrate": "8000k",
    "audioBitrate": "192k",
    "hardwareAcceleration": false,
    "hwAccelType": null
  }
}
```

#### Video Codecs
- `libx264` - H.264 (best compatibility)
- `libx265` - H.265/HEVC (better compression)
- `libvpx-vp9` - VP9 (web optimized)

#### Audio Codecs
- `aac` - AAC (recommended)
- `libmp3lame` - MP3
- `libopus` - Opus

#### Presets
- `ultrafast` - Fastest encoding, larger files
- `fast` - Fast encoding
- `medium` - Balanced (recommended)
- `slow` - Better quality, slower
- `veryslow` - Best quality, very slow

#### CRF (Constant Rate Factor)
- `18-22` - High quality
- `23` - Default, good quality
- `24-28` - Lower quality, smaller files

#### Hardware Acceleration
- `none` - Software encoding
- `nvenc` - NVIDIA GPU
- `qsv` - Intel Quick Sync
- `vaapi` - Linux VA-API
- `videotoolbox` - macOS

### Library Settings

```json
{
  "library": {
    "scanInterval": 3600,
    "autoScan": true,
    "deleteOrphaned": false,
    "metadataLanguage": "en-US"
  }
}
```

### Streaming Settings

```json
{
  "streaming": {
    "chunkSize": 1048576,
    "bufferSize": 5242880,
    "maxConcurrentStreams": 3,
    "directPlayEnabled": true,
    "transcodingMode": "progressive"
  }
}
```

#### Transcoding Modes
- `progressive` - Stream while transcoding (recommended)
- `offline` - Pre-transcode entire file
- `adaptive` - Switch based on network conditions

### Download Settings

```json
{
  "downloads": {
    "maxConcurrent": 2,
    "autoStart": true,
    "deleteAfterDays": 7,
    "notifyOnComplete": true
  }
}
```

### Notification Settings

```json
{
  "notifications": {
    "enabled": true,
    "types": ["download_complete", "new_content", "errors"],
    "pushEnabled": false
  }
}
```

## Media Organization

### Recommended Directory Structure

```
/media/
├── movies/
│   ├── Movie Name (Year)/
│   │   └── Movie Name (Year).mkv
│   └── Another Movie (Year)/
│       └── Another Movie (Year).mp4
└── tv/
    ├── Series Name/
    │   ├── Season 01/
    │   │   ├── Series Name - S01E01.mkv
    │   │   └── Series Name - S01E02.mkv
    │   └── Season 02/
    │       └── Series Name - S02E01.mkv
    └── Another Series/
        └── Season 01/
            └── Another Series - S01E01.mp4
```

### Supported File Formats

#### Video Containers
- `.mkv` - Matroska (recommended)
- `.mp4` - MPEG-4
- `.avi` - Audio Video Interleave
- `.mov` - QuickTime
- `.webm` - WebM

#### Video Codecs
- H.264/AVC
- H.265/HEVC
- VP9
- AV1

#### Audio Codecs
- AAC
- MP3
- AC3
- DTS
- FLAC
- Opus

#### Subtitle Formats
- SRT
- ASS/SSA
- VTT
- SUB/IDX

## Security Configuration

### Rate Limiting

```typescript
// Default rate limits
{
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100 // requests per window
}
```

### CORS Configuration

```typescript
// Allowed origins
{
  origin: ['http://localhost:5173', 'http://localhost:3000'],
  credentials: true
}
```

### API Authentication

Currently, Lanflix uses profile-based access. Future versions will include:
- JWT authentication
- API key management
- Role-based access control

## Performance Tuning

### Database Optimization

```sql
-- Enable WAL mode for better concurrency
PRAGMA journal_mode=WAL;

-- Increase cache size
PRAGMA cache_size=-64000;  -- 64MB
```

### Redis Caching

```env
# Enable Redis for better performance
REDIS_URL=redis://localhost:6379

# Cache TTL settings (in seconds)
CACHE_TTL_SHORT=300      # 5 minutes
CACHE_TTL_MEDIUM=3600    # 1 hour
CACHE_TTL_LONG=86400     # 24 hours
```

### FFmpeg Optimization

```json
{
  "transcoding": {
    "threads": 0,  // 0 = auto-detect CPU cores
    "preset": "medium",
    "tune": "film",  // film, animation, grain
    "profile": "high"
  }
}
```

## Logging Configuration

### Log Levels

```env
# error - Only errors
# warn - Errors and warnings
# info - General information (default)
# debug - Detailed debugging
LOG_LEVEL=info
```

### Log Files

Logs are stored in `backend/logs/`:
- `combined.log` - All logs
- `error.log` - Error logs only
- `access.log` - HTTP access logs

### Log Rotation

Logs automatically rotate:
- Daily rotation
- Keep last 14 days
- Compress old logs

## Advanced Configuration

### Custom FFmpeg Arguments

```json
{
  "transcoding": {
    "customArgs": [
      "-movflags", "+faststart",
      "-pix_fmt", "yuv420p"
    ]
  }
}
```

### Webhook Configuration

See [Webhook Configuration](../setup/webhook-configuration.md) for details.

### Scheduled Jobs

```typescript
// Configured via cron expressions
{
  "jobs": {
    "libraryScan": "0 */6 * * *",      // Every 6 hours
    "metadataRefresh": "0 2 * * *",    // Daily at 2 AM
    "cleanupTemp": "0 3 * * *",        // Daily at 3 AM
    "autoDelete": "0 4 * * *"          // Daily at 4 AM
  }
}
```

## Configuration Files

### Backend Configuration

Primary config: `backend/.env`

### Frontend Configuration

Build config: `frontend/vite.config.js`

```javascript
export default {
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:3000'
    }
  }
}
```

### Database Configuration

Sequelize config: `backend/src/config/database.js`

## Environment-Specific Configs

### Development
```env
NODE_ENV=development
LOG_LEVEL=debug
```

### Production
```env
NODE_ENV=production
LOG_LEVEL=warn
```

### Testing
```env
NODE_ENV=test
DATABASE_PATH=./data/test.db
LOG_LEVEL=error
```

## Validation

### Check Configuration
```bash
cd backend
node -e "require('dotenv').config(); console.log(process.env)"
```

### Test External Services
```bash
# Test Sonarr connection
curl -H "X-Api-Key: YOUR_KEY" http://localhost:8989/api/v3/system/status

# Test Radarr connection
curl -H "X-Api-Key: YOUR_KEY" http://localhost:7878/api/v3/system/status

# Test TMDB
curl "https://api.themoviedb.org/3/configuration?api_key=YOUR_KEY"
```

## Next Steps

- [Installation Guide](./installation.md) - Install Lanflix
- [Quick Start](./quick-start.md) - Get started quickly
- [Troubleshooting](../troubleshooting/common-issues.md) - Common issues

**Last Updated**: October 31, 2025
