# Core Backend Services Implementation

This document describes the core backend services implemented for the Lanflix streaming media server.

## Overview

Five core services have been implemented to handle content discovery, library management, downloads, metadata, and notifications:

1. **MetadataService** - Manages content metadata from TMDB
2. **ContentService** - Handles content discovery and search
3. **LibraryService** - Manages the media library
4. **DownloadManager** - Coordinates downloads via Sonarr/Radarr
5. **NotificationService** - Handles push notifications

## Services

### 1. MetadataService (`metadata.service.ts`)

Manages content metadata from TMDB with caching and local storage.

**Key Features:**
- Fetches movie and TV series metadata from TMDB API
- Downloads and caches poster and backdrop images
- Saves metadata to media folders as JSON files
- Loads metadata from media folder JSON files
- Implements staleness checking (7-day refresh cycle)
- Automatic metadata refresh for stale content

**Main Methods:**
- `fetchMovieMetadata(tmdbId)` - Fetch movie metadata from TMDB
- `fetchSeriesMetadata(tmdbId)` - Fetch TV series metadata from TMDB
- `downloadPosterImage(posterPath, contentId)` - Download poster image
- `downloadBackdropImage(backdropPath, contentId)` - Download backdrop image
- `saveMetadataToMediaFolder(contentId, mediaFolderPath)` - Save metadata to disk
- `loadMetadataFromMediaFolder(mediaFolderPath)` - Load metadata from disk
- `refreshMetadata(contentId)` - Refresh stale metadata
- `isMetadataStale(fetchedAt)` - Check if metadata needs refresh

**Caching:**
- Metadata cached for 7 days
- Images cached indefinitely
- Automatic cache invalidation on refresh

### 2. ContentService (`content.service.ts`)

Handles content discovery, search, and trending content with caching.

**Key Features:**
- Search content using TMDB
- Get trending and popular content
- Retrieve detailed content information
- Detect content type (movie vs series)
- Search availability via Prowlarr
- Mark content as in library or watchlist

**Main Methods:**
- `searchContent(query, type, profileId)` - Search for content
- `getTrendingContent(profileId)` - Get trending movies and series
- `getPopularContent(type, page, profileId)` - Get popular content
- `getContentDetails(tmdbId, type, profileId)` - Get detailed content info
- `detectContentType(tmdbId)` - Detect if content is movie or series
- `searchAvailability(title, type)` - Search for download sources

**Caching:**
- Trending content cached for 6 hours
- Popular content cached for 6 hours
- Search results not cached (real-time)

### 3. LibraryService (`library.service.ts`)

Manages the media library with scanning, filtering, and watch progress tracking.

**Key Features:**
- Get library items with filtering and sorting
- Scan media folders for new content
- Add and remove content from library
- Track watch progress per profile
- Get recently added content
- Support for both movies and TV series with episodes

**Main Methods:**
- `getLibraryItems(filters, profileId)` - Get filtered library items
- `getLibraryItem(id, profileId)` - Get specific library item
- `addToLibrary(tmdbId, type, filePath)` - Add content to library
- `removeFromLibrary(id, deleteFiles)` - Remove content from library
- `getRecentlyAdded(limit, profileId)` - Get recently added content
- `scanLibraryFolder()` - Scan media folders for new files

**Filtering Options:**
- Filter by type (movie/series)
- Filter by genre
- Search by title
- Sort by addedAt, title, releaseDate, voteAverage
- Pagination support

**Library Scanning:**
- Scans movies and series folders
- Detects video files (.mp4, .mkv, .avi, etc.)
- Loads metadata from folder JSON files
- Creates/updates database entries
- Handles episode files for series

### 4. DownloadManager (`download-manager.service.ts`)

Coordinates content downloads via Sonarr and Radarr with progress tracking.

**Key Features:**
- Queue downloads via Sonarr (series) or Radarr (movies)
- Track download progress and status
- Poll Sonarr/Radarr queues for updates
- Handle download completion
- Schedule auto-delete (30 days after completion)
- Cancel downloads

**Main Methods:**
- `queueDownload(options)` - Queue a download
- `getDownloadStatus(contentId)` - Get download status
- `cancelDownload(contentId)` - Cancel a download
- `pollDownloadProgress()` - Poll for download updates
- `handleDownloadComplete(contentId)` - Handle completion
- `scheduleAutoDelete(contentId, days)` - Schedule auto-delete
- `startPolling(intervalMs)` - Start automatic polling
- `stopPolling()` - Stop automatic polling
- `getActiveDownloads()` - Get all active downloads

**Download Flow:**
1. User queues download
2. Content added to Sonarr/Radarr
3. Download queue entry created
4. Polling monitors progress
5. On completion, library is updated
6. Auto-delete scheduled for 30 days

**Polling:**
- Default interval: 60 seconds
- Checks Sonarr and Radarr queues
- Updates progress percentages
- Detects completed downloads

### 5. NotificationService (`notification.service.ts`)

Manages push notifications for keep-watching prompts and other alerts.

**Key Features:**
- Register device tokens for push notifications
- Send Firebase Cloud Messaging (FCM) notifications
- Send Web Push API notifications
- Keep-watching notifications (7 days before deletion)
- Handle user responses (keep/delete)
- Notification history tracking

**Main Methods:**
- `registerDeviceToken(profileId, token, platform)` - Register device
- `unregisterDeviceToken(token)` - Unregister device
- `sendPushNotification(profileId, payload)` - Send notification
- `sendKeepWatchingPrompt(profileId, contentId, title)` - Send keep-watching prompt
- `handleKeepWatchingResponse(contentId, profileId, keep)` - Handle response
- `checkAndSendKeepWatchingNotifications()` - Check and send notifications
- `getNotificationHistory(profileId, limit)` - Get notification history
- `cleanupOldDeviceTokens()` - Remove unused tokens

**Notification Types:**
- Keep-watching prompts (7 days before auto-delete)
- Download completion alerts
- Custom notifications

**Platforms Supported:**
- Android (FCM)
- Android TV (FCM)
- Web (Web Push API)

## Integration

All services are exported from `services/index.ts` for easy importing:

```typescript
import {
  MetadataService,
  ContentService,
  LibraryService,
  DownloadManager,
  NotificationService
} from './services';

// Or use default instances
import services from './services';
services.metadataService.fetchMovieMetadata(123);
```

## Dependencies

Services depend on:
- **External Clients**: TMDBClient, SonarrClient, RadarrClient, ProwlarrClient
- **Models**: Content, SeriesEpisode, WatchHistory, Watchlist, DownloadQueue, AutoDeleteSchedule, DeviceToken
- **Utils**: CacheManager, Logger
- **Config**: Environment configuration

## Error Handling

All services implement comprehensive error handling:
- Try-catch blocks around all operations
- Detailed error logging with context
- Graceful degradation when external services fail
- Proper error propagation to API layer

## Caching Strategy

Services use the CacheManager for performance:
- **MetadataService**: 7-day TTL for metadata
- **ContentService**: 6-hour TTL for trending/popular content
- **LibraryService**: No caching (real-time data)
- **DownloadManager**: No caching (real-time status)
- **NotificationService**: No caching (real-time notifications)

## Background Jobs

Services support background job integration:
- **DownloadManager**: Polling every 60 seconds
- **MetadataService**: Daily refresh of stale metadata
- **LibraryService**: Periodic library scanning
- **NotificationService**: Daily check for keep-watching notifications

## Testing

All services are designed to be testable:
- Constructor dependency injection
- Mockable external dependencies
- Clear separation of concerns
- No global state

## Next Steps

To complete the backend implementation:
1. Implement REST API routes (task 6)
2. Set up background jobs and scheduled tasks (task 7)
3. Add error handling middleware
4. Implement authentication/authorization
5. Add comprehensive logging
6. Write unit and integration tests

## Requirements Fulfilled

This implementation fulfills the following requirements:
- **12.1, 12.2, 12.3, 12.5**: Metadata fetching and management
- **3.2, 3.3, 3.4, 3.6**: Content discovery and search
- **5.1, 5.2, 5.3, 5.4, 5.5, 5.6**: Library management
- **4.2, 4.3, 4.4, 4.5, 4.6, 4.7**: Download management
- **4.6**: Push notifications and keep-watching prompts
