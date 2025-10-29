# Admin Settings Implementation

## Overview
Implemented a complete admin dashboard for managing API keys and external service configurations, with dynamic reloading of settings without server restart.

## Changes Made

### 1. Frontend - Admin Dashboard
**Files Created:**
- `frontend/src/pages/admin.html` - Admin dashboard UI
- `frontend/src/styles/admin.css` - Admin dashboard styling
- `frontend/src/scripts/admin-main.js` - Admin dashboard logic

**Features:**
- Storage path configuration (movies/series folders)
- TMDB API key management with clear instructions
- Sonarr/Radarr/Prowlarr integration settings
- Test connection buttons for each service
- Metadata settings (language, auto-fetch, image downloads)
- Password visibility toggles
- Save/Cancel actions with status feedback

### 2. Backend - Settings Management

#### Settings Route (`backend/src/routes/settings.routes.ts`)
**Added:**
- Admin setting keys to `validKeys` array:
  - `moviesPath`, `seriesPath`
  - `tmdbApiKey`
  - `sonarrUrl`, `sonarrApiKey`
  - `radarrUrl`, `radarrApiKey`
  - `prowlarrUrl`, `prowlarrApiKey`
  - `autoMetadata`, `downloadImages`, `metadataLanguage`

- **POST `/api/settings/test-connection`** endpoint
  - Tests connection to individual services (sonarr, radarr, prowlarr, tmdb)
  - Returns connection status and error messages

- **Fixed SQLite database lock issue**
  - Changed from parallel `Promise.all()` to sequential `for...of` loop
  - Prevents concurrent write conflicts

- **Dynamic API key reloading**
  - Updates TMDB client API key immediately when saved
  - No server restart required for TMDB changes

#### TMDB Client (`backend/src/clients/tmdb.client.ts`)
**Added:**
- `updateApiKey(newApiKey: string)` method
  - Dynamically updates API key without restart
  - Updates axios client defaults

#### Client Initialization (`backend/src/clients/index.ts`)
**Added:**
- `loadApiKeysFromDatabase()` function
  - Loads API keys from database on server startup
  - Falls back to .env values if not in database
  - Called during server initialization

#### App Startup (`backend/src/app.ts`)
**Modified:**
- Added call to `loadApiKeysFromDatabase()` after database initialization
- Ensures database settings take precedence over .env

### 3. Frontend - Settings Page
**Modified:** `frontend/src/pages/settings.html`
- Added "Admin Dashboard" button to settings sidebar
- Styled with red gradient to stand out
- Links to admin.html

### 4. API Client (`frontend/src/modules/api-client.js`)
**Added:**
- `testServiceConnection(service)` method
  - Calls backend test-connection endpoint
  - Used by admin dashboard test buttons

## How It Works

### Saving Settings
1. User enters API keys in admin dashboard
2. Frontend sends settings to `PUT /api/settings`
3. Backend saves to database sequentially (avoids locks)
4. Backend immediately updates TMDB client with new key
5. User sees success message

### Loading Settings on Startup
1. Server starts and initializes database
2. `loadApiKeysFromDatabase()` is called
3. Reads all settings from database
4. Updates TMDB client if key exists
5. Server continues startup

### Testing Connections
1. User clicks "Test Connection" button
2. Frontend calls `POST /api/settings/test-connection`
3. Backend attempts connection to specified service
4. Returns success/failure status
5. User sees result immediately

## TMDB API Key Clarification
The admin page now clearly specifies:
- Use "API Key (v3 auth)" from TMDB
- NOT the "API Read Access Token"
- Includes example format
- Direct link to TMDB API settings page

## Benefits
1. **No Restart Required** - TMDB API key updates immediately
2. **Database-First** - Settings in database override .env
3. **User-Friendly** - Clear UI for managing all settings
4. **Test Before Save** - Can test connections before committing
5. **No Lock Issues** - Sequential saves prevent SQLite conflicts
6. **Persistent** - Settings survive server restarts

## Future Improvements
- Add dynamic reload for Sonarr/Radarr/Prowlarr (currently requires restart)
- Add validation for API key format
- Add bulk test all services button
- Add settings export/import functionality
