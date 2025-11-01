# Lanflix Build Tools

All build, packaging, and release tools for Lanflix.

## Structure

```
build-tools/
├── android/              # Android app (WebView wrapper)
│   ├── app/             # App source code
│   ├── build.gradle     # Build configuration
│   └── gradlew.bat      # Gradle wrapper
│
├── server/              # Server installer builder
│   ├── runtime/         # Files included in distribution
│   │   ├── start-server.bat
│   │   ├── install-service.bat
│   │   └── README.txt
│   └── build-installer.bat  # Creates portable ZIP/installer
│
└── scripts/             # Build and release automation
    ├── build-server.js      # Builds frontend + backend
    ├── bump-version.js      # Version management
    └── release.bat          # Automated release to GitHub
```

## Quick Commands

From project root:

```bash
# Build server (frontend + backend)
npm run build:server

# Build Android APK
npm run build:android

# Build server installer/ZIP
npm run build:installer

# Build everything
npm run build:all

# Create GitHub release (builds everything + publishes)
npm run release
```

## What Each Tool Does

### 1. Android App (`android/`)

WebView wrapper that loads the server's web interface.

**Build:**
```bash
cd build-tools/android
gradlew assembleDebug
```

**Output:** `android/app/build/outputs/apk/debug/app-debug.apk`

**Features:**
- Configure server URL
- Load server's web UI
- Auto-update checker

### 2. Server Installer (`server/`)

Creates a portable distribution of the Lanflix server.

**Build:**
```bash
cd build-tools/server
build-installer.bat
```

**Output:**
- `dist/lanflix-server-portable.zip` - Portable ZIP
- `dist/lanflix-server/` - Distribution folder

**Includes:**
- Compiled backend
- Built frontend
- Runtime scripts (start-server.bat, etc.)
- README and configuration

### 3. Build Scripts (`scripts/`)

#### `build-server.js`
Builds the complete server:
1. Builds frontend (Vite)
2. Copies to backend/public
3. Compiles backend (TypeScript)

#### `bump-version.js`
Updates version numbers across all files:
- `server/backend/package.json`
- `server/frontend/package.json`
- `build-tools/android/app/build.gradle`

Usage:
```bash
node build-tools/scripts/bump-version.js 1.0.1
node build-tools/scripts/bump-version.js patch
node build-tools/scripts/bump-version.js minor
node build-tools/scripts/bump-version.js major
```

#### `release.bat`
Automated release process:
1. Bump version
2. Build server
3. Build Android APK
4. Commit and tag
5. Push to GitHub
6. Create GitHub release
7. Upload APK

Usage:
```bash
npm run release
```

## Typical Workflow

### Development
```bash
# Terminal 1: Backend dev server
npm run dev:backend

# Terminal 2: Frontend dev server
npm run dev:frontend
```

### Testing Build
```bash
# Build and test server
npm run build:server
cd server/backend
npm start

# Build and test Android
npm run build:android
# Install APK on device
```

### Release
```bash
# Create full release
npm run release

# Follow prompts:
# - Enter version (e.g., 1.0.1 or "patch")
# - Script builds everything
# - Creates GitHub release
# - Uploads APK
```

## File Organization

### What's Where

- **Application Code**: `server/backend/` and `server/frontend/`
- **Build Tools**: `build-tools/` (this folder)
- **Build Output**: `dist/` (created during build)
- **Releases**: `releases/` (APKs for GitHub)

### Clean Separation

```
lanflix/
├── server/           # Application code
│   ├── backend/     # Node.js API
│   └── frontend/    # Vite web UI
│
├── build-tools/     # Build and release tools
│   ├── android/    # Android app
│   ├── server/     # Server packager
│   └── scripts/    # Automation
│
├── dist/           # Build output (gitignored)
└── releases/       # Release APKs
```

## Requirements

### For Server Build
- Node.js 18+
- npm

### For Android Build
- Java JDK 17+
- Gradle (included via wrapper)

### For Release
- Git
- GitHub CLI (optional, for automated release)

## Troubleshooting

### Build Fails

**Server build:**
```bash
cd server/backend
rm -rf node_modules
npm install
npm run build
```

**Android build:**
```bash
cd build-tools/android
gradlew clean
gradlew assembleDebug
```

### Version Bump Fails

Check that these files exist:
- `server/backend/package.json`
- `server/frontend/package.json`
- `build-tools/android/app/build.gradle`

### Release Fails

1. Check Git is installed: `git --version`
2. Check you're on main branch: `git branch`
3. Check remote is set: `git remote -v`
4. For GitHub release, install CLI: `winget install GitHub.cli`

## Next Steps

After building:

1. **Test Server**: `cd server/backend && npm start`
2. **Test Android**: Install APK on device
3. **Create Installer**: `npm run build:installer`
4. **Release**: `npm run release`

## Documentation

- [Android App](android/README.md)
- [Server Installer](server/README.md)
- [Build Guide](../BUILD.md)
