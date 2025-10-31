# Watch History & Progress Tracking

Automatic tracking of viewing progress and watch history.

## Overview

Lanflix automatically tracks what you watch, how much you've watched, and where you left off, allowing seamless resume across sessions.

## Features

### Automatic Progress Tracking

**What's Tracked:**
- Current playback position
- Total duration
- Watch percentage
- Last watched timestamp
- Completion status

**Tracking Frequency:**
- Updates every 10 seconds during playback
- Saved on pause
- Saved on player close
- Saved on page navigation

### Resume Playback

**Resume Points:**
- Automatically resume from last position
- Skip intro/credits detection (planned)
- "Start from beginning" option
- Resume threshold (95% = completed)

**Resume UI:**
```
┌─────────────────────────────────────┐
│  Continue watching from 45:23?      │
│  [Resume] [Start Over]              │
└─────────────────────────────────────┘
```

### Continue Watching

Personalized section showing in-progress content.

**Display:**
- Recently watched content
- Progress bar overlay
- Time remaining
- Last watched timestamp

**Sorting:**
- Most recently watched first
- Completed items auto-removed
- Maximum 20 items shown

### Watch History

Complete viewing history per profile.

**History Information:**
- Content title and poster
- Watch date and time
- Duration watched
- Completion status
- Episode details (for series)

**History Actions:**
- View full history
- Remove items
- Clear history
- Export history (planned)

## Progress Tracking

### Movies

**Tracking:**
- Single progress entry per movie
- Updates during playback
- Marked complete at 95%
- Resume from last position

**Example:**
```json
{
  "contentId": 123,
  "profileId": 1,
  "progressSeconds": 3600,
  "durationSeconds": 7200,
  "completed": false,
  "lastWatched": "2025-10-31T20:00:00.000Z"
}
```

### TV Series

**Tracking:**
- Progress per episode
- Episode completion tracking
- Season progress calculation
- Series progress calculation
- Next episode auto-play

**Episode Progress:**
```json
{
  "episodeId": 456,
  "profileId": 1,
  "progressSeconds": 1800,
  "durationSeconds": 2700,
  "completed": true,
  "lastWatched": "2025-10-31T21:00:00.000Z"
}
```

**Series Progress:**
- Episodes watched / Total episodes
- Current season
- Next unwatched episode
- Completion percentage

## Continue Watching Section

### Display Logic

**Shown When:**
- Progress > 0%
- Progress < 95%
- Watched within last 30 days

**Not Shown When:**
- Completed (>95%)
- Not watched in 30+ days
- Manually removed

### UI Layout

```
┌─────────────────────────────────────┐
│  Continue Watching                  │
│                                     │
│  ┌──────┐  ┌──────┐  ┌──────┐     │
│  │Poster│  │Poster│  │Poster│     │
│  │ 45%  │  │ 78%  │  │ 12%  │     │
│  └──────┘  └──────┘  └──────┘     │
│  Inception  The Matrix  Interstellar│
│  1h 15m left 30m left  2h 20m left │
└─────────────────────────────────────┘
```

### Progress Indicators

**Visual Elements:**
- Progress bar (colored overlay)
- Percentage text
- Time remaining
- Last watched timestamp

**Progress Colors:**
- 0-25%: Blue
- 25-50%: Green
- 50-75%: Yellow
- 75-95%: Orange
- 95-100%: Completed (removed)

## API Usage

### Update Progress

```javascript
const response = await fetch('/api/stream/123/progress', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    progressSeconds: 3600,
    durationSeconds: 7200
  })
});
```

### Get Watch History

```javascript
const response = await fetch('/api/profiles/1/history');
const { items } = await response.json();
```

### Get Continue Watching

```javascript
const response = await fetch('/api/profiles/1/continue-watching');
const { items } = await response.json();
```

### Mark as Watched

```javascript
const response = await fetch('/api/stream/123/complete', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1
  })
});
```

## Progress Calculation

### Percentage Calculation

```javascript
const percentage = (progressSeconds / durationSeconds) * 100;
```

### Time Remaining

```javascript
const remaining = durationSeconds - progressSeconds;
const minutes = Math.floor(remaining / 60);
const hours = Math.floor(minutes / 60);
```

### Completion Detection

```javascript
const isCompleted = (progressSeconds / durationSeconds) >= 0.95;
```

## Episode Progression

### Auto-Play Next Episode

**Behavior:**
1. Current episode reaches 95%
2. Show "Next Episode" countdown (10 seconds)
3. Auto-play next episode
4. Update progress for new episode

**Countdown UI:**
```
┌─────────────────────────────────────┐
│  Next Episode in 10 seconds         │
│  S01E02: "The One Where..."         │
│  [Play Now] [Cancel]                │
└─────────────────────────────────────┘
```

### Season Completion

**When season completes:**
- Show season completion message
- Suggest next season
- Update series progress
- Add to completed list

### Series Completion

**When series completes:**
- Show completion celebration
- Suggest similar content
- Add to completed list
- Remove from continue watching

## Watch History Page

### History List

```
┌─────────────────────────────────────┐
│  Watch History                      │
│                                     │
│  Today                              │
│  ┌──────┐                           │
│  │Poster│  Inception                │
│  │      │  Watched 2 hours ago      │
│  └──────┘  Duration: 2h 28m         │
│                                     │
│  Yesterday                          │
│  ┌──────┐                           │
│  │Poster│  Breaking Bad S01E01      │
│  │      │  Watched yesterday        │
│  └──────┘  Duration: 58m            │
│                                     │
│  This Week                          │
│  ...                                │
└─────────────────────────────────────┘
```

### History Filters

**Filter Options:**
- All content
- Movies only
- TV series only
- Completed only
- In progress only

**Sort Options:**
- Most recent first
- Oldest first
- Title (A-Z)
- Duration

### History Actions

**Per Item:**
- Remove from history
- Re-watch
- View details
- Share (planned)

**Bulk Actions:**
- Clear all history
- Clear completed
- Export history
- Import history (planned)

## Data Storage

### Database Schema

```sql
CREATE TABLE watch_history (
  id INTEGER PRIMARY KEY,
  profile_id INTEGER NOT NULL,
  content_id INTEGER,
  episode_id INTEGER,
  progress_seconds INTEGER DEFAULT 0,
  duration_seconds INTEGER NOT NULL,
  completed BOOLEAN DEFAULT 0,
  last_watched DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id),
  FOREIGN KEY (content_id) REFERENCES content(id),
  FOREIGN KEY (episode_id) REFERENCES series_episodes(id)
);
```

### Indexes

```sql
CREATE INDEX idx_watch_history_profile ON watch_history(profile_id);
CREATE INDEX idx_watch_history_last_watched ON watch_history(last_watched);
```

## Privacy

### Profile Isolation

Watch history is completely isolated per profile:
- No cross-profile visibility
- Independent tracking
- Separate histories

### History Management

Users can:
- View their history
- Remove items
- Clear all history
- Disable tracking (planned)

### Data Retention

**Current:**
- History kept indefinitely
- Manual deletion only

**Planned:**
- Auto-delete after X days
- Configurable retention
- Archive old history

## Performance

### Efficient Updates

Progress updates optimized:
- Debounced writes (10 seconds)
- Batch updates
- Async processing
- No blocking

### Caching

History data cached:
- Recent history in memory
- Continue watching cached
- Invalidated on update

## Troubleshooting

### Progress Not Saving

**Check:**
1. Profile ID included in request
2. Network connectivity
3. Database write permissions
4. Browser console errors

### Resume Not Working

**Check:**
1. Progress > 0 seconds
2. Progress < 95%
3. Content still in library
4. Player initialization

### Continue Watching Empty

**Check:**
1. Content watched recently
2. Progress between 0-95%
3. Profile selected
4. Database queries

## Next Steps

- [Multi-Profile Support](./multi-profile.md) - Profile system
- [Video Player](./video-player.md) - Player features
- [Streaming API](../api/streaming.md) - API reference

**Last Updated**: October 31, 2025
