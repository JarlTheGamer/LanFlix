# External APIs Migration Summary

## Overview
This document summarizes the migration of external API clients from the old Node.js/TypeScript backend to the new C# backend.

## Completed Migrations

### 1. TMDB Client ✅
**Status:** Already implemented and enhanced

**Location:**
- Interface: `Application/Common/Interfaces/ITmdbClient.cs`
- Implementation: `Infrastructure/Services/ExternalApis/TmdbClient.cs`
- Models: `Application/Common/Models/TmdbModels.cs`

**Features:**
- Movie search
- TV series search
- Movie details with genres, cast, ratings
- TV series details with seasons
- Season details with episodes
- HTTP client pooling with connection management
- Proper error handling and logging
- JSON serialization with snake_case support

**Configuration:**
```json
"Lanflix": {
  "ExternalApis": {
    "Tmdb": {
      "ApiKey": "your-api-key-here",
      "BaseUrl": "https://api.themoviedb.org/3/"
    }
  }
}
```

### 2. Radarr Client ✅
**Status:** Newly implemented

**Location:**
- Interface: `Application/Common/Interfaces/IRadarrClient.cs`
- Implementation: `Infrastructure/Services/ExternalApis/RadarrClient.cs`
- Models: `Application/Common/Models/RadarrModels.cs`

**Features:**
- Test connection
- Search movies by title
- Add movie to Radarr
- Get all movies
- Get movie by TMDB ID
- Get download queue
- Delete movie
- Get root folders
- Get quality profiles

**Configuration:**
```json
"Lanflix": {
  "ExternalApis": {
    "Radarr": {
      "Url": "http://localhost:7878",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### 3. Sonarr Client ✅
**Status:** Newly implemented

**Location:**
- Interface: `Application/Common/Interfaces/ISonarrClient.cs`
- Implementation: `Infrastructure/Services/ExternalApis/SonarrClient.cs`
- Models: `Application/Common/Models/SonarrModels.cs`

**Features:**
- Test connection
- Search TV series by title
- Add series to Sonarr
- Get all series
- Get series by TVDB ID
- Get download queue
- Delete series
- Get root folders
- Get quality profiles
- Get episodes for a series
- Search for specific episode
- Search for entire season

**Configuration:**
```json
"Lanflix": {
  "ExternalApis": {
    "Sonarr": {
      "Url": "http://localhost:8989",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### 4. Prowlarr Client ✅
**Status:** Newly implemented

**Location:**
- Interface: `Application/Common/Interfaces/IProwlarrClient.cs`
- Implementation: `Infrastructure/Services/ExternalApis/ProwlarrClient.cs`
- Models: `Application/Common/Models/ProwlarrModels.cs`

**Features:**
- Test connection
- Search across all indexers
- Get all configured indexers
- Get health status
- Category filtering (movies, TV)

**Configuration:**
```json
"Lanflix": {
  "ExternalApis": {
    "Prowlarr": {
      "Url": "http://localhost:9696",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### 5. Content Controller ✅
**Status:** Newly implemented

**Location:** `WebApi/Controllers/ContentController.cs`

**Endpoints:**
- `GET /api/content/discovery/search` - Search TMDB for movies and TV series
- `GET /api/content/{id}` - Get detailed content information
- `GET /api/content/{id}/episodes` - Get episodes for a TV series
- `POST /api/content/{id}/queue` - Queue a download via Radarr/Sonarr
- `POST /api/content/test-connection` - Test connection to external services

## HTTP Client Configuration

All external API clients are configured with:
- Connection pooling (15-minute lifetime)
- Idle timeout (5 minutes)
- Max connections per server (5-10)
- Automatic decompression (GZip, Deflate)
- Connection timeout (10 seconds)
- HTTP/2 support
- Handler lifetime (30 minutes)

This ensures optimal performance and resource management.

## Comparison with Old Backend

### Old Backend (Node.js/TypeScript)
```
backend-old/src/clients/
├── tmdb.client.ts
├── radarr.client.ts
├── sonarr.client.ts
└── prowlarr.client.ts
```

### New Backend (C#)
```
app/Application/Common/Interfaces/
├── ITmdbClient.cs
├── IRadarrClient.cs
├── ISonarrClient.cs
└── IProwlarrClient.cs

app/Infrastructure/Services/ExternalApis/
├── TmdbClient.cs
├── RadarrClient.cs
├── SonarrClient.cs
└── ProwlarrClient.cs

app/Application/Common/Models/
├── TmdbModels.cs
├── RadarrModels.cs
├── SonarrModels.cs
└── ProwlarrModels.cs
```

## Key Improvements

1. **Type Safety**: Strong typing with C# models vs. TypeScript interfaces
2. **Dependency Injection**: Proper DI with HttpClientFactory
3. **Connection Pooling**: Built-in HTTP client pooling for better performance
4. **Error Handling**: Consistent error handling with structured logging
5. **Async/Await**: Native async support throughout
6. **Configuration**: Centralized configuration in appsettings.json
7. **Testing**: Easier to mock interfaces for unit testing

## Missing Features from Old Backend

The following features from the old backend need to be implemented:

### 1. Download Manager Service
**Old Location:** `backend-old/src/services/download-manager.service.ts`

**Features to implement:**
- Queue download tracking
- Progress polling from Radarr/Sonarr
- Download completion handling
- Auto-delete scheduling
- Episode-specific downloads
- Season-specific downloads

**Recommendation:** Create `DownloadManagerService` in `Infrastructure/Services/Downloads/`

### 2. Metadata Service
**Old Location:** `backend-old/src/services/metadata.service.ts`

**Features to implement:**
- Metadata caching
- Image downloading (posters, backdrops)
- Metadata refresh logic
- Season/episode metadata
- Metadata staleness checking

**Recommendation:** Create `MetadataService` in `Infrastructure/Services/Metadata/`

### 3. Library Service Enhancements
**Old Location:** `backend-old/src/services/library.service.ts`

**Features to implement:**
- Library scanning integration with Radarr/Sonarr
- Content matching with TMDB
- Automatic metadata enrichment

## Testing Checklist

- [ ] Test TMDB API connection
- [ ] Test Radarr API connection
- [ ] Test Sonarr API connection
- [ ] Test Prowlarr API connection
- [ ] Test movie search
- [ ] Test TV series search
- [ ] Test movie details retrieval
- [ ] Test TV series details retrieval
- [ ] Test season/episode details
- [ ] Test movie download queueing
- [ ] Test TV series download queueing
- [ ] Test root folder retrieval
- [ ] Test quality profile retrieval
- [ ] Test download queue monitoring

## Configuration Example

Complete configuration in `appsettings.json`:

```json
{
  "Lanflix": {
    "ExternalApis": {
      "Tmdb": {
        "ApiKey": "your-tmdb-api-key",
        "BaseUrl": "https://api.themoviedb.org/3/"
      },
      "Radarr": {
        "Url": "http://localhost:7878",
        "ApiKey": "your-radarr-api-key"
      },
      "Sonarr": {
        "Url": "http://localhost:8989",
        "ApiKey": "your-sonarr-api-key"
      },
      "Prowlarr": {
        "Url": "http://localhost:9696",
        "ApiKey": "your-prowlarr-api-key"
      }
    }
  }
}
```

## Next Steps

1. **Implement Download Manager Service**
   - Create service for tracking downloads
   - Implement progress polling
   - Add completion handlers

2. **Implement Metadata Service**
   - Create metadata caching layer
   - Implement image downloading
   - Add metadata refresh logic

3. **Enhance Library Service**
   - Integrate with Radarr/Sonarr for library scanning
   - Add automatic metadata enrichment
   - Implement content matching

4. **Add Background Jobs**
   - Download progress polling job
   - Metadata refresh job
   - Auto-delete job

5. **Add Admin UI**
   - External API configuration page
   - Connection testing UI
   - Download queue monitoring

## Notes

- All external API clients are optional and will only be registered if configured
- The system gracefully handles missing API configurations
- HTTP clients use connection pooling for optimal performance
- All operations are fully async with cancellation token support
- Comprehensive logging is implemented throughout
