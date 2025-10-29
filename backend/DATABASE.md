# Database Setup Guide

## Overview

This project uses SQLite with Sequelize ORM for data persistence. The database includes models for profiles, content, episodes, watch history, watchlist, download queue, settings, auto-delete scheduling, and device tokens.

## Database Models

### Core Models

1. **Profile** - User profiles with personalized settings
2. **Content** - Movies and TV series metadata
3. **SeriesEpisode** - Individual episodes for TV series
4. **WatchHistory** - Tracks viewing progress for each profile
5. **Watchlist** - User's "My List" of content
6. **DownloadQueue** - Manages content download requests
7. **Settings** - Application-wide settings (key-value store)
8. **AutoDeleteSchedule** - Schedules automatic content deletion
9. **DeviceToken** - Push notification device tokens

## Setup Instructions

### 1. Install Dependencies

```bash
npm install
```

### 2. Run Migrations

Create all database tables with proper indexes:

```bash
npm run migrate
```

### 3. Seed Initial Data

Populate the database with initial settings and test profiles:

```bash
npm run seed
```

### 4. Verify Setup

Start the development server:

```bash
npm run dev
```

The database will be created at the path specified in `.env` (default: `./data/lanflix.db`).

## Migration Commands

- **Run all pending migrations**: `npm run migrate`
- **Undo last migration**: `npm run migrate:undo`
- **Seed database**: `npm run seed`

## Database Schema

### Profiles Table
- Stores user profile information
- Each profile has unique avatar colors
- Timestamps for creation and updates

### Content Table
- Stores movie and TV series metadata from TMDB
- Includes poster/backdrop paths, ratings, genres
- Links to local file paths for downloaded content
- Indexed on `tmdb_id` and `type` for fast queries

### Series_Episodes Table
- Stores individual episode information
- Foreign key to Content table
- Indexed on `content_id` and season/episode numbers

### Watch_History Table
- Tracks playback progress for each profile
- Stores progress in seconds and completion status
- Indexed on `profile_id` and `content_id` for fast lookups

### Watchlist Table
- User's personalized "My List"
- Unique constraint on (profile_id, content_id)
- Indexed for fast profile-based queries

### Download_Queue Table
- Manages content download requests
- Tracks status (queued, downloading, completed, failed)
- Links to external Sonarr/Radarr IDs
- Indexed on status for queue processing

### Settings Table
- Key-value store for application settings
- Includes language, theme, video quality preferences
- Auto-delete and notification settings

### Auto_Delete_Schedule Table
- Schedules content deletion after 30 days
- Tracks notification status and user responses
- Indexed on scheduled_delete_at for daily jobs

### Device_Tokens Table
- Stores push notification tokens
- Supports Android, Android TV, and Web platforms
- Unique constraint on device_token

## Model Associations

- Profile → WatchHistory (one-to-many)
- Profile → Watchlist (one-to-many)
- Profile → DownloadQueue (one-to-many)
- Profile → DeviceToken (one-to-many)
- Content → SeriesEpisode (one-to-many)
- Content → WatchHistory (one-to-many)
- Content → Watchlist (one-to-many)
- Content → DownloadQueue (one-to-many)
- Content → AutoDeleteSchedule (one-to-one)
- SeriesEpisode → WatchHistory (one-to-many)

## Development Notes

- Models are defined in TypeScript with proper type definitions
- Migrations use JavaScript for Sequelize CLI compatibility
- All foreign keys have CASCADE delete for data integrity
- Indexes are created for frequently queried columns
- Timestamps use snake_case in database, camelCase in models
