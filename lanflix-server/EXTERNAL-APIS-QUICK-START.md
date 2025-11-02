# External APIs Quick Start Guide

## Overview
This guide will help you quickly set up and test the external API integrations in Lanflix.

## Prerequisites

1. **TMDB API Key** (Required)
   - Sign up at https://www.themoviedb.org/
   - Get your API key from https://www.themoviedb.org/settings/api

2. **Radarr** (Optional - for movie downloads)
   - Install from https://radarr.video/
   - Default URL: http://localhost:7878
   - Get API key from Settings → General → Security

3. **Sonarr** (Optional - for TV series downloads)
   - Install from https://sonarr.tv/
   - Default URL: http://localhost:8989
   - Get API key from Settings → General → Security

4. **Prowlarr** (Optional - for indexer management)
   - Install from https://prowlarr.com/
   - Default URL: http://localhost:9696
   - Get API key from Settings → General → Security

## Configuration

### Option 1: Using appsettings.json

Edit `lanflix-server/app/WebApi/appsettings.json`:

```json
{
  "Lanflix": {
    "ExternalApis": {
      "Tmdb": {
        "ApiKey": "your-tmdb-api-key-here",
        "BaseUrl": "https://api.themoviedb.org/3/"
      },
      "Radarr": {
        "Url": "http://localhost:7878",
        "ApiKey": "your-radarr-api-key-here"
      },
      "Sonarr": {
        "Url": "http://localhost:8989",
        "ApiKey": "your-sonarr-api-key-here"
      },
      "Prowlarr": {
        "Url": "http://localhost:9696",
        "ApiKey": "your-prowlarr-api-key-here"
      }
    }
  }
}
```

### Option 2: Using Environment Variables

Set these environment variables:

```bash
# Windows (PowerShell)
$env:Lanflix__ExternalApis__Tmdb__ApiKey="your-tmdb-api-key"
$env:Lanflix__ExternalApis__Radarr__Url="http://localhost:7878"
$env:Lanflix__ExternalApis__Radarr__ApiKey="your-radarr-api-key"
$env:Lanflix__ExternalApis__Sonarr__Url="http://localhost:8989"
$env:Lanflix__ExternalApis__Sonarr__ApiKey="your-sonarr-api-key"
$env:Lanflix__ExternalApis__Prowlarr__Url="http://localhost:9696"
$env:Lanflix__ExternalApis__Prowlarr__ApiKey="your-prowlarr-api-key"

# Linux/Mac
export Lanflix__ExternalApis__Tmdb__ApiKey="your-tmdb-api-key"
export Lanflix__ExternalApis__Radarr__Url="http://localhost:7878"
export Lanflix__ExternalApis__Radarr__ApiKey="your-radarr-api-key"
export Lanflix__ExternalApis__Sonarr__Url="http://localhost:8989"
export Lanflix__ExternalApis__Sonarr__ApiKey="your-sonarr-api-key"
export Lanflix__ExternalApis__Prowlarr__Url="http://localhost:9696"
export Lanflix__ExternalApis__Prowlarr__ApiKey="your-prowlarr-api-key"
```

## Testing the APIs

### 1. Test TMDB Connection

```bash
# Search for movies
curl "http://localhost:5000/api/content/discovery/search?q=inception&type=movie"

# Get movie details
curl "http://localhost:5000/api/content/550?type=movie"

# Search for TV series
curl "http://localhost:5000/api/content/discovery/search?q=breaking+bad&type=tv"

# Get TV series details
curl "http://localhost:5000/api/content/1396?type=tv"

# Get season episodes
curl "http://localhost:5000/api/content/1396/episodes?season=1"
```

### 2. Test Radarr Connection

```bash
# Test connection
curl -X POST "http://localhost:5000/api/content/test-connection" \
  -H "Content-Type: application/json" \
  -d '{"service": "radarr"}'

# Queue a movie download
curl -X POST "http://localhost:5000/api/content/550/queue" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "movie",
    "title": "Fight Club",
    "year": 1999,
    "profileId": 1
  }'
```

### 3. Test Sonarr Connection

```bash
# Test connection
curl -X POST "http://localhost:5000/api/content/test-connection" \
  -H "Content-Type: application/json" \
  -d '{"service": "sonarr"}'

# Queue a TV series download
curl -X POST "http://localhost:5000/api/content/1396/queue" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "series",
    "title": "Breaking Bad",
    "year": 2008,
    "profileId": 1
  }'
```

### 4. Test Prowlarr Connection

```bash
# Test connection
curl -X POST "http://localhost:5000/api/content/test-connection" \
  -H "Content-Type: application/json" \
  -d '{"service": "prowlarr"}'
```

## API Endpoints Reference

### Content Discovery

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/content/discovery/search` | Search TMDB for movies/TV series |
| GET | `/api/content/{id}` | Get detailed content information |
| GET | `/api/content/{id}/episodes` | Get episodes for a TV series |
| POST | `/api/content/{id}/queue` | Queue a download |
| POST | `/api/content/test-connection` | Test external service connection |

### Query Parameters

**Search Endpoint:**
- `q` (required): Search query
- `type` (optional): Content type - "movie", "tv", or "all" (default: "all")

**Content Details Endpoint:**
- `type` (required): Content type - "movie" or "tv"

**Episodes Endpoint:**
- `season` (optional): Season number (if omitted, returns all seasons)

## Troubleshooting

### TMDB API Issues

**Error: "TMDB API key not configured"**
- Solution: Add your TMDB API key to appsettings.json or environment variables

**Error: "401 Unauthorized"**
- Solution: Check that your TMDB API key is valid

### Radarr/Sonarr Issues

**Error: "Radarr is not configured"**
- Solution: Add Radarr URL and API key to configuration

**Error: "Radarr is not properly configured. Please set up root folders and quality profiles"**
- Solution: 
  1. Open Radarr at http://localhost:7878
  2. Go to Settings → Media Management → Root Folders
  3. Add at least one root folder
  4. Go to Settings → Profiles
  5. Ensure at least one quality profile exists

**Error: "Failed to connect to Radarr"**
- Solution: 
  1. Check that Radarr is running
  2. Verify the URL is correct
  3. Verify the API key is correct

### Connection Test Failures

If connection tests fail:
1. Verify the service is running
2. Check the URL is accessible (try opening in browser)
3. Verify the API key is correct
4. Check firewall settings
5. Review server logs for detailed error messages

## Radarr/Sonarr Setup Guide

### Radarr Setup

1. **Install Radarr**
   - Download from https://radarr.video/
   - Run the installer or Docker container

2. **Configure Root Folder**
   - Open http://localhost:7878
   - Go to Settings → Media Management
   - Click "Add Root Folder"
   - Select your movies directory
   - Save changes

3. **Configure Quality Profile**
   - Go to Settings → Profiles
   - Default profiles should already exist
   - Customize if needed

4. **Get API Key**
   - Go to Settings → General → Security
   - Copy the API Key
   - Add to Lanflix configuration

### Sonarr Setup

1. **Install Sonarr**
   - Download from https://sonarr.tv/
   - Run the installer or Docker container

2. **Configure Root Folder**
   - Open http://localhost:8989
   - Go to Settings → Media Management
   - Click "Add Root Folder"
   - Select your TV series directory
   - Save changes

3. **Configure Quality Profile**
   - Go to Settings → Profiles
   - Default profiles should already exist
   - Customize if needed

4. **Get API Key**
   - Go to Settings → General → Security
   - Copy the API Key
   - Add to Lanflix configuration

## Next Steps

1. **Configure Download Clients** in Radarr/Sonarr
   - Add qBittorrent, Transmission, or other download clients
   - Configure connection settings

2. **Configure Indexers** in Prowlarr
   - Add torrent/usenet indexers
   - Sync with Radarr/Sonarr

3. **Test Downloads**
   - Queue a movie or TV series
   - Monitor download progress in Radarr/Sonarr
   - Verify files appear in your media folders

4. **Integrate with Library**
   - Scan library to detect new content
   - Verify metadata is enriched from TMDB

## Support

For issues or questions:
- Check the logs in `lanflix-server/app/WebApi/logs/`
- Review the EXTERNAL-APIS-MIGRATION.md document
- Check Radarr/Sonarr logs for download issues
