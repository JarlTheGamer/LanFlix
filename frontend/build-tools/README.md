# Build Tools

This directory contains platform-specific build configurations and tools for deploying Lanflix to different platforms.

## Structure

```
build-tools/
├── android/          # Android/Android TV build tools
├── ios/              # iOS/tvOS build tools (future)
├── electron/         # Desktop (Windows/Mac/Linux) build tools (future)
└── web/              # Web deployment tools (future)
```

## Available Platforms

### Android
Build and deploy to Android phones, tablets, and Android TV.

See [android/ANDROID-QUICKSTART.md](./android/ANDROID-QUICKSTART.md) for quick setup.

### Coming Soon
- **iOS/tvOS** - iPhone, iPad, Apple TV
- **Electron** - Windows, macOS, Linux desktop apps
- **Web** - Static web deployment

## Quick Commands

From the `frontend/` directory:

```bash
# Android
npm run android:init      # First-time setup
npm run android:sync      # Build and sync
npm run android:run       # Build, sync, and run

# Future platforms
npm run ios:run           # iOS (coming soon)
npm run electron:build    # Desktop (coming soon)
```

## Adding New Platforms

When adding a new platform:

1. Create a new folder in `build-tools/`
2. Add platform-specific configuration files
3. Add build scripts to `package.json`
4. Document setup in platform's README
