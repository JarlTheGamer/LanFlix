# Quick Start Guide

Get Lanflix up and running in under 5 minutes.

## Prerequisites

- Node.js 18 or higher
- npm or yarn
- Sonarr, Radarr, Prowlarr (optional but recommended)
- TMDB API key (free from themoviedb.org)

## Installation Steps

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/lanflix.git
cd lanflix
```

### 2. Setup Backend

```bash
cd backend
npm install
cp .env.example .env
```

Edit `.env` file with your configuration:

```env
# Server Configuration
PORT=3000
NODE_ENV=development

# Database
DATABASE_PATH=./data/lanflix.db

# Media Paths
MEDIA_ROOT_PATH=/path/to/your/media
POSTER_CACHE_PATH=./data/posters
BACKDROP_CACHE_PATH=./data/backdrops

# TMDB API (Required)
TMDB_API_KEY=your_tmdb_api_key_here

# Sonarr (Optional)
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_sonarr_api_key

# Radarr (Optional)
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_radarr_api_key

# Prowlarr (Optional)
PROWLARR_URL=http://localhost:9696
PROWLARR_API_KEY=your_prowlarr_api_key

# Redis (Optional)
REDIS_URL=redis://localhost:6379
```

### 3. Initialize Database

```bash
npm run migrate
npm run seed
```

### 4. Start Backend

```bash
npm run dev
```

Backend will be available at `http://localhost:3000`

### 5. Setup Frontend

Open a new terminal:

```bash
cd frontend
npm install
npm run dev
```

Frontend will be available at `http://localhost:5173`

### 6. Access Lanflix

Open your browser and navigate to:
```
http://localhost:5173
```

## First Time Setup

### 1. Select a Profile

The default profile "Main Profile" is automatically created. Click on it to start.

### 2. Browse Content

- Click **Discover** to see trending content
- Use the search icon to find specific titles
- Click on any content to see details

### 3. Queue Downloads

1. Click on a movie or TV show
2. Click the **Download** button
3. Content will be queued in Sonarr/Radarr
4. Once downloaded, it appears in your library

### 4. Watch Content

1. Go to **Movies** or **Series** in the menu
2. Click on any available content
3. Click **Play** to start streaming
4. Your progress is automatically saved

## Quick Configuration

### Get TMDB API Key

1. Go to https://www.themoviedb.org/
2. Create a free account
3. Go to Settings → API
4. Request an API key (choose "Developer")
5. Copy the API key to your `.env` file

### Connect Sonarr

1. Open Sonarr web interface
2. Go to Settings → General
3. Copy the API Key
4. Add to `.env` as `SONARR_API_KEY`
5. Restart backend

### Connect Radarr

1. Open Radarr web interface
2. Go to Settings → General
3. Copy the API Key
4. Add to `.env` as `RADARR_API_KEY`
5. Restart backend

## Verify Installation

### Check Backend Health

```bash
curl http://localhost:3000/health
```

Should return:
```json
{
  "status": "ok",
  "timestamp": "2025-10-30T..."
}
```

### Check API Status

```bash
curl http://localhost:3000/api/settings/api-status
```

Should show status of all external services.

### Test Streaming

1. Add a video file to your `MEDIA_ROOT_PATH`
2. Run library scan: `curl -X POST http://localhost:3000/api/jobs/scan-library`
3. Check library: `curl http://localhost:3000/api/library/movies`
4. Try streaming in the web interface

## Common Issues

### Port Already in Use

If port 3000 or 5173 is already in use:

```env
# In backend/.env
PORT=3001

# In frontend, Vite will auto-select next available port
```

### Database Migration Fails

```bash
cd backend
rm -rf data/lanflix.db
npm run migrate
npm run seed
```

### External Services Not Connecting

1. Verify services are running
2. Check URLs in `.env` are correct
3. Verify API keys are valid
4. Check firewall settings

### No Audio in Videos

Make sure your video files have audio tracks. Check with:

```bash
ffprobe your-video-file.mp4
```

If audio is missing, you may need to remux or re-encode the file.

## Next Steps

- [Full Installation Guide](./installation.md) - Detailed setup
- [Configuration Guide](./configuration.md) - Advanced configuration
- [Architecture Overview](../architecture/system-overview.md) - Understand the system
- [API Documentation](../api/overview.md) - API reference

## Getting Help

- Check [Common Issues](../troubleshooting/common-issues.md)
- Review [Known Issues](../tasks/known-issues.md)
- Open an issue on GitHub
