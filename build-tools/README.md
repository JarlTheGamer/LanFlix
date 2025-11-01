# Lanflix Build Tools

This folder contains all the build and packaging tools for Lanflix.

## Structure

```
build-tools/
├── android/          # Native Android app
│   ├── app/         # App source code
│   ├── build.gradle # Build configuration
│   └── gradlew.bat  # Gradle wrapper
├── server/          # Server deployment scripts
│   ├── start-server.bat
│   └── install-service.bat
└── scripts/         # Build automation
    └── build-server.js
```

## What Each Folder Does

### `android/`
Native Kotlin Android app that connects to the Lanflix server.

**Build:**
```bash
cd android
gradlew assembleDebug
```

**Output:** `android/app/build/outputs/apk/debug/app-debug.apk`

### `server/`
Scripts for deploying and running the Lanflix server on Windows.

- `start-server.bat` - Start the server
- `install-service.bat` - Install as Windows service

### `scripts/`
Build automation scripts.

- `build-server.js` - Builds frontend and backend together

## Quick Commands

**Build everything:**
```bash
npm run build:all
```

**Build server only:**
```bash
npm run build:server
```

**Build Android only:**
```bash
npm run build:android
```

**Create release:**
```bash
npm run release
```

## Why This Structure?

This keeps all build-related files separate from the actual application code:
- `server/` = Application code (backend + frontend)
- `build-tools/` = Build and deployment tools
- `scripts/` = Release automation

Clean and organized! 🎯
