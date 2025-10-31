# Lanflix - Streaming Media Server

A complete streaming media application with a Node.js backend and cross-platform frontend. Integrates with Sonarr, Radarr, and Prowlarr for automated content discovery and acquisition.

## Project Structure

```
lanflix/
├── backend/                 # Node.js backend server
│   ├── src/
│   │   ├── clients/        # External API clients (Sonarr, Radarr, Prowlarr, TMDB)
│   │   ├── config/         # Configuration and environment setup
│   │   ├── middleware/     # Express middleware
│   │   ├── migrations/     # Database migrations
│   │   ├── models/         # Sequelize database models
│   │   ├── routes/         # API route handlers
│   │   ├── seeders/        # Database seeders
│   │   ├── services/       # Business logic layer
│   │   ├── utils/          # Utility functions
│   │   └── app.ts          # Main application entry point
│   ├── data/               # SQLite database and cached images
│   ├── logs/               # Application logs
│   ├── .env.example        # Environment variables template
│   ├── package.json
│   └── tsconfig.json
│
├── frontend/               # Frontend UI
│   ├── src/
│   │   ├── modules/        # Modular JavaScript components
│   │   ├── pages/          # HTML pages
│   │   ├── styles/         # CSS stylesheets
│   │   └── assets/         # Images and static assets
│   ├── package.json
│   └── vite.config.js
│
└── .kiro/                  # Kiro specs and configuration
    └── specs/
        └── streaming-media-server/
            ├── requirements.md
            ├── design.md
            └── tasks.md
```

## Features

- **Content Discovery**: Browse trending movies and TV shows
- **Automated Downloads**: Queue content for download via Sonarr/Radarr
- **Media Streaming**: Stream your personal media library
- **Multi-Profile Support**: Individual profiles with personalized watch history
- **Cross-Platform**: Supports Android TV, Android phones, and PC
- **Auto-Delete**: Automatically manage storage with keep-watching notifications
- **Rich Metadata**: TMDB integration for posters, backdrops, and descriptions

## Backend Setup

### Prerequisites

- Node.js 18+
- SQLite
- Redis (optional, for caching)
- Sonarr, Radarr, and Prowlarr instances

### Installation

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Copy the environment template:
   ```bash
   copy .env.example .env
   ```

4. Configure your `.env` file with:
   - External service URLs and API keys (Sonarr, Radarr, Prowlarr, TMDB)
   - Media storage paths
   - Database path

5. Run database migrations:
   ```bash
   npm run migrate
   ```

6. Start the development server:
   ```bash
   npm run dev
   ```

The backend will be available at `http://localhost:6129`

## Frontend Setup

### Installation

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

The frontend will be available at `http://localhost:5173`

### Building for Production

```bash
npm run build
```

## API Documentation

### Content Endpoints

- `GET /api/content/discover` - Get trending content
- `GET /api/content/search?q={query}` - Search for content
- `GET /api/content/:id` - Get content details
- `POST /api/content/:id/queue` - Queue content for download

### Library Endpoints

- `GET /api/library/movies` - Get all movies
- `GET /api/library/series` - Get all TV series
- `GET /api/library/recent` - Get recently added content
- `GET /api/library/:id` - Get library item details

### Profile Endpoints

- `GET /api/profiles` - Get all profiles
- `POST /api/profiles` - Create new profile
- `GET /api/profiles/:id/watchlist` - Get user's watchlist

### Streaming Endpoints

- `GET /api/stream/:id` - Stream media file
- `POST /api/stream/:id/progress` - Update watch progress

## Configuration

### External Services

Configure the following services in your `.env` file:

**Sonarr**: TV series management
- URL: `http://localhost:8989`
- API Key: Get from Sonarr Settings → General

**Radarr**: Movie management
- URL: `http://localhost:7878`
- API Key: Get from Radarr Settings → General

**Prowlarr**: Indexer management
- URL: `http://localhost:9696`
- API Key: Get from Prowlarr Settings → General

**TMDB**: Metadata provider
- API Key: Get from https://www.themoviedb.org/settings/api

## Development

### Backend Development

```bash
cd backend
npm run dev
```

TypeScript files will be automatically compiled and the server will restart on changes.

### Frontend Development

```bash
cd frontend
npm run dev
```

Vite will provide hot module replacement for instant updates.

## Deployment

### Backend Deployment

1. Build the TypeScript code:
   ```bash
   cd backend
   npm run build
   ```

2. Start the production server:
   ```bash
   npm start
   ```

### Frontend Deployment

#### Electron (PC)
```bash
cd frontend
npm run build:electron
```

#### Capacitor (Android/Android TV)
```bash
cd frontend
npm run build:android
```

## License

MIT

## Contributing

See the `.kiro/specs/streaming-media-server/` directory for detailed requirements, design, and implementation tasks.
