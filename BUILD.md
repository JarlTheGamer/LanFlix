# Lanflix Build Guide

## Architecture Overview

Lanflix uses a unified server architecture where:
- **Backend (Node.js/TypeScript)** serves both the API and web UI
- **Frontend (Vite)** builds to static files that the backend serves
- **Android App (Capacitor)** connects to the server

This is similar to how Jellyfin, Plex, and Emby work.

## Project Structure

```
lanflix/
├── server/            # Server components
│   ├── backend/      # Node.js/TypeScript API server
│   │   ├── src/      # Source code
│   │   ├── dist/     # Compiled JavaScript
│   │   └── public/   # Frontend build (generated)
│   └── frontend/     # Vite web UI
│       ├── src/      # Source code
│       └── dist/     # Build output
├── build-tools/      # Build and packaging tools
│   ├── android/      # Android app (native)
│   ├── server/       # Server installer scripts
│   └── scripts/      # Build automation
├── scripts/          # Release scripts
└── docs/            # Documentation
```

## Development Setup

### Prerequisites
- Node.js 18+ and npm
- For Android: Android Studio, Java JDK

### Backend Development
```bash
cd server/backend
npm install
npm run dev
```
Server runs on http://localhost:8080

### Frontend Development
```bash
cd server/frontend
npm install
npm run dev
```
Dev server runs on http://localhost:5173

## Building for Production

### Build Everything
```bash
npm run build:all
```

This will:
1. Build frontend to `server/frontend/dist`
2. Copy frontend build to `server/backend/public`
3. Compile backend TypeScript to `server/backend/dist`
4. Build Android APK

### Build Server Only
```bash
npm run build:server
```

### Build Android App Only
```bash
npm run build:android
```

### Build Server Installer
```bash
npm run build:installer
```

Creates a portable ZIP with the server ready to distribute.

## Running the Production Server

After building:

```bash
cd server/backend
npm start
```

The server will:
- Serve the web UI at `http://localhost:8080`
- Provide API at `http://localhost:8080/api`
- Be accessible from other devices on your network

## Android App Configuration

The Android app needs to know where your server is:

1. First run will show configuration screen
2. Enter your server IP (e.g., `http://192.168.1.100:8080`)
3. App will connect and work like the web UI

## Creating a Windows Installer

### Automated Build
```bash
npm run build:installer
```

This creates:
- `dist/lanflix-server-portable.zip` - Ready to distribute
- `dist/lanflix-server/` - Extracted distribution folder

The package includes:
- Compiled backend
- Built frontend
- `start-server.bat` - Start script
- `install-service.bat` - Windows service installer
- `README.txt` - User instructions
- `.env` - Configuration file

## Deployment Options

### Option 1: Manual Installation
1. Build the server
2. Copy backend folder to target machine
3. Run `npm install --production`
4. Run `start-server.bat`

### Option 2: Docker (Future)
```dockerfile
FROM node:18
WORKDIR /app
COPY backend/ .
RUN npm install --production
EXPOSE 8080
CMD ["node", "dist/app.js"]
```

### Option 3: Windows Service
Use NSSM or similar to run as a service

## Configuration

### Server Configuration
Edit `server/backend/.env`:
```env
PORT=8080
MEDIA_ROOT_PATH=D:/Movies
DATABASE_PATH=./data/lanflix.db
```

### Android App Configuration
First run configuration or edit in app settings:
- Server URL
- Connection timeout
- Cache settings

## Troubleshooting

### Frontend not showing
- Check `server/backend/public/` exists and has files
- Run `npm run build:server` again

### Android app can't connect
- Ensure server is running
- Check firewall allows port 8080
- Use IP address, not localhost
- Ensure devices are on same network

### Port already in use
- Change PORT in `server/backend/.env`
- Update Android app server URL

## Android App

The Android app is a native Kotlin app located in `build-tools/android/`.

### Features
- Search movies and TV shows
- Browse trending and popular content
- Connect to your Lanflix server
- Material Design UI

### Building
```bash
npm run build:android
```

Or directly:
```bash
cd build-tools/android
gradlew assembleDebug
```

The APK will be in `build-tools/android/app/build/outputs/apk/debug/app-debug.apk`

### Configuration
On first launch, enter your server URL (e.g., `http://192.168.1.100:8080`)

## Next Steps

1. **Automated Installer**: Create proper Windows installer with Electron Builder
2. **Service Installation**: Auto-install as Windows service
3. **Auto-discovery**: Let Android app find servers on network
4. **Updates**: Built-in update mechanism
5. **Docker**: Containerized deployment option
