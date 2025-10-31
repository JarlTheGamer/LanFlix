# Download Management

Queue and manage content downloads through Sonarr and Radarr integration.

## Overview

Lanflix provides a unified download queue that integrates with Sonarr (TV) and Radarr (Movies) to automate content acquisition.

## Features

### Download Queue

Central queue for all download requests.

**Queue Information:**
- Content title and poster
- Download status
- Progress percentage
- Estimated time remaining
- Error messages (if failed)

**Queue States:**
- `pending` - Waiting to start
- `downloading` - Currently downloading
- `completed` - Download finished
- `failed` - Download error

### Download Options

#### Movies
- Download entire movie
- Quality profile selection
- Storage location

#### TV Series
- Download single episode
- Download full season
- Download entire series
- Episode selection

### Queue Management

**Actions:**
- View queue status
- Pause downloads
- Resume downloads
- Cancel downloads
- Retry failed downloads
- Remove from queue

### Notifications

Get notified when downloads complete:
- Browser notifications
- Push notifications (mobile)
- In-app notifications
- Email notifications (planned)

## How It Works

### Download Flow

```
User Request → Lanflix Queue → Sonarr/Radarr
                                    ↓
                              Indexer Search
                                    ↓
                              Download Client
                                    ↓
                              Media Directory
                                    ↓
                              Webhook Callback
                                    ↓
                              Library Scan
                                    ↓
                              Available to Watch
```

### Integration Points

1. **User initiates download** - From content discovery or detail modal
2. **Lanflix adds to queue** - Creates queue entry in database
3. **Sonarr/Radarr receives request** - Via API call
4. **External service searches** - Finds best release
5. **Download begins** - Through configured download client
6. **Progress updates** - Polled from Sonarr/Radarr
7. **Webhook notification** - When download completes
8. **Library scan** - Automatically triggered
9. **Content available** - Ready to stream

## API Usage

### Queue Movie Download

```javascript
const response = await fetch('/api/content/550/queue', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    type: 'movie',
    title: 'Fight Club',
    year: 1999
  })
});
```

### Queue Episode Download

```javascript
const response = await fetch('/api/content/1396/queue/episode', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    title: 'Breaking Bad',
    seasonNumber: 1,
    episodeNumber: 1,
    year: 2008
  })
});
```

### Queue Season Download

```javascript
const response = await fetch('/api/content/1396/queue/season', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    title: 'Breaking Bad',
    seasonNumber: 1,
    year: 2008
  })
});
```

### Get Queue Status

```javascript
const response = await fetch('/api/jobs/download-queue');
const queue = await response.json();
```

## Configuration

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

**Settings:**
- `maxConcurrent` - Maximum simultaneous downloads
- `autoStart` - Auto-start downloads when queued
- `deleteAfterDays` - Auto-delete after X days (0 = never)
- `notifyOnComplete` - Send notifications

### Sonarr Configuration

```env
SONARR_URL=http://localhost:8989
SONARR_API_KEY=your_api_key
```

**Required Sonarr Settings:**
- Root folder configured
- Quality profile set
- Download client connected
- Indexers configured

### Radarr Configuration

```env
RADARR_URL=http://localhost:7878
RADARR_API_KEY=your_api_key
```

**Required Radarr Settings:**
- Root folder configured
- Quality profile set
- Download client connected
- Indexers configured

## Queue UI

### Queue Page Layout

```
┌─────────────────────────────────────┐
│  Download Queue (3 items)           │
├─────────────────────────────────────┤
│  ┌──────┐                           │
│  │Poster│  Inception                │
│  │      │  Downloading... 45%       │
│  └──────┘  [Pause] [Cancel]         │
├─────────────────────────────────────┤
│  ┌──────┐                           │
│  │Poster│  Breaking Bad S01E01      │
│  │      │  Pending...               │
│  └──────┘  [Cancel]                 │
├─────────────────────────────────────┤
│  ┌──────┐                           │
│  │Poster│  The Matrix               │
│  │      │  Completed ✓              │
│  └──────┘  [Remove]                 │
└─────────────────────────────────────┘
```

### Progress Indicators

**Visual Elements:**
- Progress bar (0-100%)
- Status text
- Time remaining
- Download speed
- File size

**Status Colors:**
- Blue - Downloading
- Yellow - Pending
- Green - Completed
- Red - Failed

## Auto-Delete

Automatically remove content after specified days.

**How it works:**
1. Content downloaded and watched
2. Auto-delete schedule created
3. Daily job checks schedules
4. Content deleted after X days
5. Library updated

**Configuration:**
```javascript
// Set auto-delete for content
await fetch('/api/library/123/auto-delete', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    deleteAfterDays: 7
  })
});
```

## Webhook Integration

Sonarr and Radarr send webhooks when downloads complete.

### Webhook URL

```
http://your-lanflix-server:3000/api/webhook/sonarr
http://your-lanflix-server:3000/api/webhook/radarr
```

### Webhook Configuration

See [Webhook Configuration](../setup/webhook-configuration.md) for setup details.

### Webhook Events

**Sonarr Events:**
- `Download` - Episode downloaded
- `Rename` - Episode renamed
- `SeriesDelete` - Series deleted

**Radarr Events:**
- `Download` - Movie downloaded
- `Rename` - Movie renamed
- `MovieDelete` - Movie deleted

## Error Handling

### Common Errors

**No Indexers Available**
```json
{
  "error": "No indexers configured in Sonarr/Radarr",
  "code": "NO_INDEXERS"
}
```

**Quality Profile Not Found**
```json
{
  "error": "Quality profile not configured",
  "code": "NO_QUALITY_PROFILE"
}
```

**Download Failed**
```json
{
  "error": "Download failed: No suitable release found",
  "code": "DOWNLOAD_FAILED"
}
```

### Retry Logic

Failed downloads can be retried:
1. Click "Retry" button
2. System re-queues download
3. Sonarr/Radarr searches again
4. New download attempt

## Monitoring

### Queue Statistics

- Total queued items
- Active downloads
- Completed today
- Failed downloads
- Average download time

### Download History

View past downloads:
- Download date
- Content title
- File size
- Quality
- Time taken

## Performance

### Concurrent Downloads

Control simultaneous downloads to manage:
- Network bandwidth
- Disk I/O
- System resources

**Recommended:**
- Fast connection: 3-5 concurrent
- Medium connection: 2-3 concurrent
- Slow connection: 1-2 concurrent

### Queue Processing

Queue is processed:
- Every 5 minutes (status check)
- On webhook callback (immediate)
- On manual trigger

## Mobile Support

Download queue accessible on mobile:
- Responsive layout
- Touch-friendly controls
- Push notifications
- Background updates

## Troubleshooting

### Downloads Not Starting

**Check:**
1. Sonarr/Radarr running
2. API keys correct
3. Download client configured
4. Indexers working
5. Network connectivity

### Slow Downloads

**Check:**
1. Download client settings
2. Network bandwidth
3. Concurrent download limit
4. Indexer performance
5. Disk write speed

### Webhook Not Working

**Check:**
1. Webhook URL correct
2. Lanflix accessible from Sonarr/Radarr
3. Firewall rules
4. Webhook events enabled
5. Backend logs for errors

## Next Steps

- [Webhook Configuration](../setup/webhook-configuration.md) - Setup webhooks
- [Content Discovery](./content-discovery.md) - Find content
- [Library Management](../api/library.md) - Manage library

**Last Updated**: October 31, 2025
