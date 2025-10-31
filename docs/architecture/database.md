# Database Schema

Complete database schema documentation for Lanflix.

## Database Technology

- **Engine**: SQLite 3.x
- **ORM**: Sequelize
- **Location**: `backend/data/lanflix.db`
- **Migrations**: Sequelize CLI

## Schema Overview

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Profiles   │────<│ WatchHistory │>────│    Content      │
└─────────────┘     └──────────────┘     └─────────────────┘
       │                                           │
       │                                           │
       │            ┌──────────────┐              │
       └───────────<│  Watchlist   │>─────────────┘
                    └──────────────┘
                                                   │
                    ┌──────────────┐              │
                    │DownloadQueue │>─────────────┘
                    └──────────────┘
                                                   │
                    ┌──────────────┐              │
                    │SeriesEpisode │>─────────────┘
                    └──────────────┘

┌──────────────────┐
│    Settings      │
└──────────────────┘

┌──────────────────┐
│  DeviceTokens    │
└──────────────────┘

┌──────────────────────┐
│ AutoDeleteSchedule   │
└──────────────────────┘
```

## Tables

### Profiles

User profiles for personalized experience.

```sql
CREATE TABLE profiles (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name VARCHAR(255) NOT NULL,
  avatar_color_primary VARCHAR(7) NOT NULL,
  avatar_color_secondary VARCHAR(7) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Fields:**
- `id` - Unique profile identifier
- `name` - Profile display name
- `avatar_color_primary` - Primary avatar color (hex)
- `avatar_color_secondary` - Secondary avatar color (hex)
- `created_at` - Profile creation timestamp
- `updated_at` - Last update timestamp

**Relationships:**
- Has many `WatchHistory`
- Has many `Watchlist`

### Content

Movies and TV series metadata.

```sql
CREATE TABLE content (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  tmdb_id INTEGER NOT NULL UNIQUE,
  type VARCHAR(20) NOT NULL,
  title VARCHAR(255) NOT NULL,
  original_title VARCHAR(255),
  overview TEXT,
  release_date DATE,
  poster_path VARCHAR(255),
  backdrop_path VARCHAR(255),
  vote_average DECIMAL(3,1),
  vote_count INTEGER,
  genres TEXT,
  runtime INTEGER,
  status VARCHAR(50),
  file_path VARCHAR(500),
  added_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_content_tmdb_id ON content(tmdb_id);
CREATE INDEX idx_content_type ON content(type);
```

**Fields:**
- `id` - Unique content identifier
- `tmdb_id` - TMDB database ID
- `type` - Content type: 'movie' or 'tv'
- `title` - Display title
- `original_title` - Original language title
- `overview` - Plot summary
- `release_date` - Release/air date
- `poster_path` - Poster image path
- `backdrop_path` - Backdrop image path
- `vote_average` - TMDB rating (0-10)
- `vote_count` - Number of votes
- `genres` - JSON array of genres
- `runtime` - Duration in minutes
- `status` - Status: 'available', 'downloading', etc.
- `file_path` - Local file path
- `added_at` - When added to library
- `updated_at` - Last metadata update

**Relationships:**
- Has many `SeriesEpisode` (if type='tv')
- Has many `WatchHistory`
- Has many `Watchlist`
- Has many `DownloadQueue`

### SeriesEpisode

TV series episode data.

```sql
CREATE TABLE series_episodes (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content_id INTEGER NOT NULL,
  season_number INTEGER NOT NULL,
  episode_number INTEGER NOT NULL,
  title VARCHAR(255) NOT NULL,
  overview TEXT,
  air_date DATE,
  still_path VARCHAR(255),
  runtime INTEGER,
  file_path VARCHAR(500),
  added_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE
);

CREATE INDEX idx_episodes_content ON series_episodes(content_id);
CREATE INDEX idx_episodes_season ON series_episodes(season_number);
```

**Fields:**
- `id` - Unique episode identifier
- `content_id` - Parent series ID
- `season_number` - Season number
- `episode_number` - Episode number
- `title` - Episode title
- `overview` - Episode summary
- `air_date` - Original air date
- `still_path` - Episode thumbnail
- `runtime` - Episode duration
- `file_path` - Local file path
- `added_at` - When added
- `updated_at` - Last update

**Relationships:**
- Belongs to `Content`
- Has many `WatchHistory`

### WatchHistory

Playback progress tracking.

```sql
CREATE TABLE watch_history (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  content_id INTEGER,
  episode_id INTEGER,
  progress_seconds INTEGER NOT NULL DEFAULT 0,
  duration_seconds INTEGER NOT NULL,
  completed BOOLEAN NOT NULL DEFAULT 0,
  last_watched DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE,
  FOREIGN KEY (episode_id) REFERENCES series_episodes(id) ON DELETE CASCADE
);

CREATE INDEX idx_watch_history_profile ON watch_history(profile_id);
CREATE INDEX idx_watch_history_content ON watch_history(content_id);
CREATE INDEX idx_watch_history_episode ON watch_history(episode_id);
CREATE INDEX idx_watch_history_last_watched ON watch_history(last_watched);
```

**Fields:**
- `id` - Unique history entry
- `profile_id` - Watching profile
- `content_id` - Content being watched (for movies)
- `episode_id` - Episode being watched (for TV)
- `progress_seconds` - Current playback position
- `duration_seconds` - Total duration
- `completed` - Whether fully watched
- `last_watched` - Last playback timestamp

**Relationships:**
- Belongs to `Profile`
- Belongs to `Content` (optional)
- Belongs to `SeriesEpisode` (optional)

### Watchlist

User's saved content list.

```sql
CREATE TABLE watchlist (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  content_id INTEGER NOT NULL,
  added_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE,
  UNIQUE(profile_id, content_id)
);

CREATE INDEX idx_watchlist_profile ON watchlist(profile_id);
CREATE INDEX idx_watchlist_content ON watchlist(content_id);
```

**Fields:**
- `id` - Unique watchlist entry
- `profile_id` - Profile who added
- `content_id` - Content added
- `added_at` - When added

**Relationships:**
- Belongs to `Profile`
- Belongs to `Content`

### DownloadQueue

Content download queue management.

```sql
CREATE TABLE download_queue (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content_id INTEGER NOT NULL,
  profile_id INTEGER NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  progress INTEGER NOT NULL DEFAULT 0,
  error_message TEXT,
  added_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  started_at DATETIME,
  completed_at DATETIME,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE
);

CREATE INDEX idx_download_queue_status ON download_queue(status);
CREATE INDEX idx_download_queue_profile ON download_queue(profile_id);
```

**Fields:**
- `id` - Unique queue entry
- `content_id` - Content to download
- `profile_id` - Requesting profile
- `status` - Status: 'pending', 'downloading', 'completed', 'failed'
- `progress` - Download progress (0-100)
- `error_message` - Error details if failed
- `added_at` - When queued
- `started_at` - When download started
- `completed_at` - When completed

**Relationships:**
- Belongs to `Content`
- Belongs to `Profile`

### Settings

Application configuration key-value store.

```sql
CREATE TABLE settings (
  key VARCHAR(255) PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Fields:**
- `key` - Setting identifier
- `value` - Setting value (JSON string)
- `updated_at` - Last update timestamp

**Common Settings:**
- `transcoding` - Transcoding configuration
- `library` - Library scan settings
- `streaming` - Streaming preferences
- `downloads` - Download settings
- `notifications` - Notification preferences

### DeviceTokens

Push notification device tokens.

```sql
CREATE TABLE device_tokens (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  profile_id INTEGER NOT NULL,
  token VARCHAR(255) NOT NULL UNIQUE,
  device_type VARCHAR(50) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_used DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE
);

CREATE INDEX idx_device_tokens_profile ON device_tokens(profile_id);
```

**Fields:**
- `id` - Unique token entry
- `profile_id` - Associated profile
- `token` - Device push token
- `device_type` - Device type: 'ios', 'android', 'web'
- `created_at` - When registered
- `last_used` - Last notification sent

### AutoDeleteSchedule

Scheduled content deletion.

```sql
CREATE TABLE auto_delete_schedule (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content_id INTEGER NOT NULL,
  delete_after_days INTEGER NOT NULL,
  scheduled_date DATETIME NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (content_id) REFERENCES content(id) ON DELETE CASCADE
);

CREATE INDEX idx_auto_delete_content ON auto_delete_schedule(content_id);
CREATE INDEX idx_auto_delete_date ON auto_delete_schedule(scheduled_date);
```

**Fields:**
- `id` - Unique schedule entry
- `content_id` - Content to delete
- `delete_after_days` - Days until deletion
- `scheduled_date` - When to delete
- `created_at` - When scheduled

## Relationships

### One-to-Many

**Profile → WatchHistory**
```javascript
Profile.hasMany(WatchHistory, { foreignKey: 'profile_id' });
WatchHistory.belongsTo(Profile, { foreignKey: 'profile_id' });
```

**Profile → Watchlist**
```javascript
Profile.hasMany(Watchlist, { foreignKey: 'profile_id' });
Watchlist.belongsTo(Profile, { foreignKey: 'profile_id' });
```

**Content → SeriesEpisode**
```javascript
Content.hasMany(SeriesEpisode, { foreignKey: 'content_id' });
SeriesEpisode.belongsTo(Content, { foreignKey: 'content_id' });
```

**Content → WatchHistory**
```javascript
Content.hasMany(WatchHistory, { foreignKey: 'content_id' });
WatchHistory.belongsTo(Content, { foreignKey: 'content_id' });
```

**Content → Watchlist**
```javascript
Content.hasMany(Watchlist, { foreignKey: 'content_id' });
Watchlist.belongsTo(Content, { foreignKey: 'content_id' });
```

**Content → DownloadQueue**
```javascript
Content.hasMany(DownloadQueue, { foreignKey: 'content_id' });
DownloadQueue.belongsTo(Content, { foreignKey: 'content_id' });
```

## Queries

### Common Queries

**Get profile's watch history**
```sql
SELECT c.*, wh.progress_seconds, wh.last_watched
FROM watch_history wh
JOIN content c ON wh.content_id = c.id
WHERE wh.profile_id = ?
ORDER BY wh.last_watched DESC
LIMIT 20;
```

**Get profile's watchlist**
```sql
SELECT c.*
FROM watchlist w
JOIN content c ON w.content_id = c.id
WHERE w.profile_id = ?
ORDER BY w.added_at DESC;
```

**Get series with episodes**
```sql
SELECT c.*, 
  (SELECT COUNT(*) FROM series_episodes WHERE content_id = c.id) as episode_count
FROM content c
WHERE c.type = 'tv'
ORDER BY c.title;
```

**Get continue watching**
```sql
SELECT c.*, wh.progress_seconds, wh.duration_seconds
FROM watch_history wh
JOIN content c ON wh.content_id = c.id
WHERE wh.profile_id = ?
  AND wh.completed = 0
  AND wh.progress_seconds > 0
ORDER BY wh.last_watched DESC
LIMIT 10;
```

**Get download queue**
```sql
SELECT dq.*, c.title, c.poster_path
FROM download_queue dq
JOIN content c ON dq.content_id = c.id
WHERE dq.profile_id = ?
  AND dq.status IN ('pending', 'downloading')
ORDER BY dq.added_at;
```

## Migrations

### Running Migrations

```bash
# Run all pending migrations
npm run migrate

# Undo last migration
npm run migrate:undo

# Undo all migrations
npm run migrate:undo:all
```

### Creating Migrations

```bash
npx sequelize-cli migration:generate --name migration-name
```

### Migration Template

```javascript
'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    // Migration code
    await queryInterface.createTable('table_name', {
      // columns
    });
  },

  down: async (queryInterface, Sequelize) => {
    // Rollback code
    await queryInterface.dropTable('table_name');
  }
};
```

## Seeders

### Running Seeders

```bash
# Run all seeders
npm run seed

# Run specific seeder
npx sequelize-cli db:seed --seed seeder-name.js
```

### Initial Settings Seeder

```javascript
module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.bulkInsert('settings', [
      {
        key: 'transcoding',
        value: JSON.stringify({
          enabled: true,
          videoCodec: 'libx264',
          audioCodec: 'aac'
        }),
        updated_at: new Date()
      }
    ]);
  }
};
```

## Optimization

### Indexes

All foreign keys have indexes for faster joins:
- `idx_content_tmdb_id`
- `idx_content_type`
- `idx_episodes_content`
- `idx_watch_history_profile`
- `idx_watchlist_profile`
- `idx_download_queue_status`

### Query Optimization

```sql
-- Enable WAL mode for better concurrency
PRAGMA journal_mode=WAL;

-- Increase cache size
PRAGMA cache_size=-64000;  -- 64MB

-- Analyze tables for query planner
ANALYZE;
```

### Maintenance

```sql
-- Vacuum database (reclaim space)
VACUUM;

-- Rebuild indexes
REINDEX;

-- Update statistics
ANALYZE;
```

## Backup & Restore

### Backup

```bash
# Copy database file
cp backend/data/lanflix.db backend/data/lanflix.db.backup

# Or use SQLite backup
sqlite3 backend/data/lanflix.db ".backup backend/data/lanflix.db.backup"
```

### Restore

```bash
# Restore from backup
cp backend/data/lanflix.db.backup backend/data/lanflix.db
```

## Next Steps

- [Backend Architecture](./backend.md)
- [API Documentation](../api/overview.md)
- [Configuration Guide](../getting-started/configuration.md)

**Last Updated**: October 31, 2025
