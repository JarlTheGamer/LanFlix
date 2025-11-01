# Lanflix Android App

Native Android app for the Lanflix streaming server.

## Overview

The Android app is a standalone native Kotlin application that connects to your Lanflix server. It provides a clean, Material Design interface for browsing and searching your media library.

## Location

The Android app is located in: `build-tools/android/`

This is separate from the server code (`server/backend` and `server/frontend`).

## Features

✅ **Search** - Search movies and TV shows from your server
✅ **Browse** - View trending and popular content
✅ **Server Connection** - Easy setup with server URL
✅ **Material Design** - Clean, modern UI
✅ **Network Streaming** - Connect to server on local network

## Quick Build

```bash
# From project root
npm run build:android
```

APK will be in: `build-tools/android/app/build/outputs/apk/debug/app-debug.apk`

## Installation

1. Build the APK (see above)
2. Copy APK to your Android device
3. Install (allow unknown sources if needed)
4. Open app and enter server URL
5. Start browsing!

## Server URL Setup

On first launch, enter your server URL:
- Format: `http://YOUR_IP:8080`
- Example: `http://192.168.1.100:8080`
- Use your server's local IP address
- Make sure server is running

## Architecture

```
build-tools/android/
├── app/
│   ├── src/main/java/com/lanflix/app/
│   │   ├── api/          # Retrofit API client
│   │   ├── models/       # Data models (Content, Search, etc.)
│   │   ├── ui/           # Activities (Main, Search, Settings)
│   │   └── utils/        # Preferences, helpers
│   ├── src/main/res/     # Layouts, drawables, strings
│   └── build.gradle      # App dependencies
├── build.gradle          # Project config
├── settings.gradle       # Module settings
└── gradlew.bat          # Gradle wrapper
```

## API Integration

The app uses Retrofit to connect to the Lanflix server API:

### Endpoints Used

- `GET /api/content/search?q={query}` - Search content
- `GET /api/content/discover` - Get trending/popular
- `GET /api/content/{id}?type={type}` - Get details
- `GET /health` - Server health check

### Models

- `Content` - Movie/series data
- `SearchResponse` - Search results
- `DiscoverResponse` - Trending/popular content
- `HealthResponse` - Server status

## Development

### Prerequisites

- Java JDK 17+
- Android SDK (or Android Studio)
- Gradle (included via wrapper)

### Open in Android Studio

1. Open Android Studio
2. File > Open
3. Select `build-tools/android`
4. Wait for Gradle sync
5. Run on device/emulator

### Build Commands

```bash
cd build-tools/android

# Clean build
gradlew clean

# Build debug APK
gradlew assembleDebug

# Install on connected device
gradlew installDebug

# Build release APK (requires signing)
gradlew assembleRelease
```

## Key Components

### Activities

- **MainActivity** - Home screen with content grid
- **SearchActivity** - Search interface with debounced input
- **SettingsActivity** - Server configuration
- **PlayerActivity** - Video player (future)

### API Client

- **ApiClient** - Singleton Retrofit client
- **LanflixApi** - API interface definitions
- Uses OkHttp for networking
- Includes logging interceptor for debugging

### Adapters

- **ContentAdapter** - RecyclerView adapter for content grid
- Uses DiffUtil for efficient updates
- Glide for image loading

## Troubleshooting

### Build Issues

**Java not found:**
```bash
java -version  # Should be 17+
```

**Gradle fails:**
```bash
gradlew clean
gradlew assembleDebug
```

### Connection Issues

1. Check server is running: `http://YOUR_IP:8080/health`
2. Verify firewall allows port 8080
3. Use IP address, not localhost
4. Ensure same network

### App Issues

1. Clear app data: Settings > Apps > Lanflix > Clear Data
2. Reinstall app
3. Check Android version (need 7.0+)

## Future Enhancements

- [ ] Video playback with ExoPlayer
- [ ] Download for offline viewing
- [ ] Watchlist and watch history
- [ ] Profile management
- [ ] Push notifications for new content
- [ ] Chromecast support
- [ ] Picture-in-picture mode

## Documentation

- [Quick Start Guide](build-tools/android/QUICKSTART.md)
- [Detailed README](build-tools/android/README.md)
- [Build Instructions](BUILD.md)

## Release Process

The Android app is built as part of the automated release:

```bash
npm run release
```

This will:
1. Bump version
2. Build server
3. Build Android APK
4. Create GitHub release
5. Upload APK

## License

MIT License - Same as main project
