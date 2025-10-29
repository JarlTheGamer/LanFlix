# Background Jobs Scheduler

This document describes the background jobs scheduler implementation for the Lanflix streaming media server.

## Overview

The job scheduler manages automated background tasks that keep the system running smoothly. It uses `node-cron` for scheduling and runs various maintenance and monitoring tasks at specified intervals.

## Scheduled Jobs

### 1. Download Queue Polling
- **Schedule**: Every 60 seconds
- **Purpose**: Monitors download progress from Sonarr and Radarr
- **Actions**:
  - Polls Sonarr queue for TV series downloads
  - Polls Radarr queue for movie downloads
  - Updates download progress in database
  - Detects completed downloads and triggers library updates
- **Runs on startup**: Yes

### 2. Auto-Delete Check
- **Schedule**: Daily at 2:00 AM
- **Purpose**: Processes scheduled content deletions
- **Actions**:
  - Finds content scheduled for deletion (past scheduled date)
  - Excludes content marked as "keep" by users
  - Deletes media files and removes from library
  - Updates auto-delete schedule records
- **Runs on startup**: No

### 3. Metadata Refresh
- **Schedule**: Daily at 3:00 AM
- **Purpose**: Refreshes stale metadata from TMDB
- **Actions**:
  - Finds content with metadata older than 7 days
  - Fetches updated metadata from TMDB API
  - Updates database records
  - Saves metadata to media folders
  - Invalidates cache entries
- **Runs on startup**: No

### 4. Library Scan
- **Schedule**: Every 6 hours (0:00, 6:00, 12:00, 18:00)
- **Purpose**: Scans media folders for new content
- **Actions**:
  - Scans movies folder for new video files
  - Scans series folders for new episodes
  - Loads metadata from media folder JSON files
  - Adds new content to library database
  - Updates file paths for existing content
- **Runs on startup**: Yes

### 5. Cache Cleanup
- **Schedule**: Every hour
- **Purpose**: Cleans up expired cache entries and old data
- **Actions**:
  - Removes expired memory cache entries (automatic)
  - Cleans up device tokens not used in 90 days
  - Logs cache statistics
- **Runs on startup**: No

### 6. Keep-Watching Notifications
- **Schedule**: Daily at 10:00 AM
- **Purpose**: Sends notifications for content scheduled for deletion
- **Actions**:
  - Finds content scheduled for deletion in 7 days
  - Identifies profiles that have watched the content
  - Sends push notifications with keep/delete options
  - Marks notifications as sent in database
- **Runs on startup**: No

## Architecture

### Job Scheduler Class

The `JobScheduler` class manages all scheduled jobs:

```typescript
class JobScheduler {
  start(): void              // Start all scheduled jobs
  stop(): void               // Stop all scheduled jobs
  getStatus(): object        // Get scheduler status
  triggerJob(name): Promise  // Manually trigger a job
}
```

### Integration

The job scheduler is integrated into the application lifecycle:

1. **Startup**: Initialized in `app.ts` after database and cache initialization
2. **Shutdown**: Gracefully stopped on SIGTERM/SIGINT signals
3. **Monitoring**: Status available via `/api/jobs/status` endpoint

## API Endpoints

### Get Job Status
```
GET /api/jobs/status
```

Returns:
```json
{
  "isRunning": true,
  "jobs": [
    "download-queue-polling",
    "auto-delete-check",
    "metadata-refresh",
    "library-scan",
    "cache-cleanup",
    "keep-watching-notifications"
  ]
}
```

### Manually Trigger Job
```
POST /api/jobs/:jobName/trigger
```

Example:
```bash
curl -X POST http://localhost:3000/api/jobs/library-scan/trigger
```

Available job names:
- `download-queue-polling`
- `auto-delete-check`
- `metadata-refresh`
- `library-scan`
- `cache-cleanup`
- `keep-watching-notifications`

## Error Handling

All jobs include comprehensive error handling:

- **Job-level errors**: Logged but don't crash the scheduler
- **Individual item errors**: Logged and counted, processing continues
- **Critical errors**: Logged with full context for debugging

## Logging

Jobs log their activities at different levels:

- **INFO**: Job start/completion, significant actions
- **DEBUG**: Detailed progress, cache hits/misses
- **WARN**: Non-critical failures, missing data
- **ERROR**: Critical failures, exceptions

## Performance Considerations

### Download Queue Polling
- Runs frequently (every 60 seconds) but is lightweight
- Uses flag to prevent concurrent executions
- Only polls when active downloads exist

### Library Scan
- Can be resource-intensive for large libraries
- Runs every 6 hours to balance freshness and performance
- Processes folders in batches

### Metadata Refresh
- Rate-limited by TMDB API (40 requests per 10 seconds)
- Only refreshes stale content (>7 days old)
- Runs during low-traffic hours (3 AM)

### Cache Cleanup
- Memory cache cleanup is automatic and efficient
- Runs hourly to maintain optimal memory usage

## Configuration

Job schedules are defined in the scheduler code using cron expressions:

```typescript
// Every 60 seconds
'*/60 * * * * *'

// Daily at 2 AM
'0 2 * * *'

// Every 6 hours
'0 */6 * * *'

// Every hour
'0 * * * *'
```

## Monitoring

Monitor job execution through:

1. **Application logs**: All job activities are logged
2. **Job status endpoint**: Check if scheduler is running
3. **Manual triggers**: Test individual jobs on demand

## Troubleshooting

### Jobs Not Running

Check:
1. Scheduler started: `GET /api/jobs/status`
2. Application logs for errors
3. System time is correct (affects cron scheduling)

### Download Polling Not Working

Check:
1. Sonarr/Radarr connection settings
2. Active downloads exist in queue
3. External service API keys are valid

### Library Scan Missing Content

Check:
1. Media folder paths are correct
2. Metadata JSON files exist in media folders
3. File permissions allow reading
4. Video file extensions are supported

### Metadata Refresh Failing

Check:
1. TMDB API key is valid
2. Rate limits not exceeded
3. Network connectivity to TMDB
4. Content still exists in TMDB database

## Future Enhancements

Potential improvements:

1. **Configurable schedules**: Allow users to customize job schedules
2. **Job history**: Track job execution history and results
3. **Retry logic**: Automatic retry for failed jobs
4. **Job priorities**: Prioritize critical jobs during high load
5. **Distributed scheduling**: Support for multiple server instances
