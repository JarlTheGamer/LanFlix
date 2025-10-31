# Installation Guide

Complete installation instructions for Lanflix streaming media server.

## Prerequisites

### Required Software
- **Node.js** 18.x or higher
- **npm** or **yarn** package manager
- **FFmpeg** 4.4 or higher (for transcoding)
- **SQLite** 3.x (included with Node.js)

### Optional Software
- **Redis** 6.x or higher (for caching)
- **Docker** & **Docker Compose** (for containerized deployment)

### External Services (Optional)
- **Sonarr** - TV series management
- **Radarr** - Movie management
- **Prowlarr** - Indexer management
- **TMDB Account** - Metadata provider (free API key)

## Installation Methods

### Method 1: Standard Installation (Recommended)

#### 1. Clone Repository
```bash
git clone https://github.com/yourusername/lanflix.git
cd lanflix
```

#### 2. Install Backend Dependencies
```bash
cd backend
npm install
```

#### 3. Install Frontend Dependencies
```bash
cd ../frontend
npm install
```

#### 4. Configure Environment
```bash
cd ../backend
cp .env.example .env
```

Edit `.env` with your settings:
```env
PORT=3000
NODE_ENV=development
DATABASE_PATH=./data/lanflix.db
MEDIA_ROOT_PATH=/path/to/your/media
TMDB_API_KEY=your_tmdb_api_key_here
```

#### 5. Initialize Database
```bash
npm run migrate
npm run seed
```

#### 6. Verify FFmpeg Installation
```bash
ffmpeg -version
```

If FFmpeg is not installed:
- **Windows**: Download from [ffmpeg.org](https://ffmpeg.org/download.html)
- **macOS**: `brew install ffmpeg`
- **Linux**: `sudo apt install ffmpeg` or `sudo yum install ffmpeg`

#### 7. Start Services

Terminal 1 - Backend:
```bash
cd backend
npm run dev
```

Terminal 2 - Frontend:
```bash
cd frontend
npm run dev
```

#### 8. Access Application
Open browser to: `http://localhost:5173`

### Method 2: Docker Installation

#### 1. Clone Repository
```bash
git clone https://github.com/yourusername/lanflix.git
cd lanflix
```

#### 2. Configure Environment
```bash
cp backend/.env.example backend/.env
```

Edit `backend/.env` with your settings.

#### 3. Build and Start Containers
```bash
docker-compose up -d
```

#### 4. Access Application
Open browser to: `http://localhost:3000`

### Method 3: Production Build

#### 1. Build Frontend
```bash
cd frontend
npm run build
```

#### 2. Build Backend
```bash
cd ../backend
npm run build
```

#### 3. Start Production Server
```bash
NODE_ENV=production npm start
```

## Post-Installation Setup

### 1. Create First Profile
Navigate to Profiles page and create your user profile.

### 2. Configure Media Library
Go to Settings → Library and set your media paths.

### 3. Configure External Services (Optional)

#### Sonarr Setup
```env
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_api_key
```

#### Radarr Setup
```env
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_api_key
```

#### TMDB Setup
1. Create account at [themoviedb.org](https://www.themoviedb.org/)
2. Get API key from Settings → API
3. Add to `.env`:
```env
TMDB_API_KEY=your_api_key
```

### 4. Configure Transcoding
Go to Settings → Transcoding and configure:
- Video codec (H.264, H.265, VP9)
- Audio codec (AAC, MP3, Opus)
- Quality presets
- Hardware acceleration (if available)

### 5. Set Up Webhooks (Optional)
Configure Sonarr/Radarr webhooks to notify Lanflix of new content:
```
Webhook URL: http://your-lanflix-server:3000/api/webhook/sonarr
```

See [Webhook Configuration](../setup/webhook-configuration.md) for details.

## Verification

### Check Backend Status
```bash
curl http://localhost:3000/api/health
```

Expected response:
```json
{
  "status": "ok",
  "timestamp": "2025-10-31T12:00:00.000Z"
}
```

### Check Database
```bash
cd backend
node check-db.js
```

### Check FFmpeg
```bash
cd backend
node test-ffmpeg.js
```

## Directory Structure

After installation, your directory structure should look like:
```
lanflix/
├── backend/
│   ├── data/
│   │   ├── lanflix.db          # SQLite database
│   │   ├── posters/            # Cached poster images
│   │   └── backdrops/          # Cached backdrop images
│   ├── logs/                   # Application logs
│   ├── node_modules/
│   ├── src/
│   └── .env                    # Configuration
├── frontend/
│   ├── dist/                   # Production build
│   ├── node_modules/
│   └── src/
└── README.md
```

## Troubleshooting

### Port Already in Use
```bash
# Change port in backend/.env
PORT=3001
```

### FFmpeg Not Found
```bash
# Add FFmpeg to PATH or specify location
FFMPEG_PATH=/usr/local/bin/ffmpeg
```

### Database Migration Errors
```bash
# Reset database
cd backend
rm data/lanflix.db
npm run migrate
npm run seed
```

### Permission Errors
```bash
# Fix media directory permissions
chmod -R 755 /path/to/media
```

### Module Not Found Errors
```bash
# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
```

## Updating

### Update from Git
```bash
git pull origin main
cd backend && npm install
cd ../frontend && npm install
npm run migrate  # Run any new migrations
```

### Update Dependencies
```bash
# Backend
cd backend
npm update

# Frontend
cd frontend
npm update
```

## Uninstallation

### Remove Application
```bash
# Stop services
# Delete directory
rm -rf lanflix/
```

### Remove Docker Containers
```bash
docker-compose down -v
```

## Next Steps

- [Configuration Guide](./configuration.md) - Detailed configuration options
- [Quick Start](./quick-start.md) - Get started quickly
- [API Documentation](../api/overview.md) - API reference

**Last Updated**: October 31, 2025
