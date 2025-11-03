# Lanflix Android App

WebView wrapper for the Lanflix server's web interface.

## Features

- Configure server connection
- Loads server's web UI in a WebView
- Auto-update checker
- Simple and lightweight

## Building

### Prerequisites

- Android Studio or Android SDK
- Java JDK 17+
- Gradle (included via wrapper)

### Build APK

```bash
# From project root
npm run build:android

# Or directly
cd build-tools/android
gradlew assembleDebug
```

The APK will be in `app/build/outputs/apk/debug/app-debug.apk`

## Configuration

On first launch, the app will ask for your server URL:
- Example: `http://192.168.1.100:8080`
- Make sure your device is on the same network as the server
- The server must be running and accessible

## Development

### Project Structure

```
build-tools/android/
├── app/
│   ├── src/main/
│   │   ├── java/com/lanflix/app/
│   │   │   ├── ui/           # Activities (Main, Settings)
│   │   │   └── utils/        # UpdateChecker, PreferenceManager
│   │   ├── res/              # Resources (layouts, drawables, etc.)
│   │   └── AndroidManifest.xml
│   └── build.gradle
├── build.gradle
├── settings.gradle
└── gradlew.bat
```

### Key Components

- **MainActivity**: WebView that loads server's web UI
- **SettingsActivity**: Server URL configuration
- **UpdateChecker**: Checks GitHub for new APK releases
- **PreferenceManager**: Stores server URL

## How It Works

The app is a simple WebView wrapper:

1. **First Launch**: Shows settings screen to configure server URL
2. **After Configuration**: Loads the server's web UI in a WebView
3. **All Features**: Provided by the server's frontend (search, browse, play, etc.)
4. **Updates**: Checks GitHub releases for new APK versions

## Troubleshooting

### Can't connect to server

1. Check server is running: `http://YOUR_IP:8080/health`
2. Ensure firewall allows port 8080
3. Use IP address, not localhost
4. Verify devices are on same network

### Build fails

1. Check Java version: `java -version` (need 17+)
2. Clean build: `gradlew clean`
3. Sync Gradle files in Android Studio

## Why WebView?

- **Simplicity**: One codebase for web and mobile
- **Consistency**: Same UI/UX across all platforms
- **Easy Updates**: Server updates automatically update the app UI
- **Less Maintenance**: No need to sync features between native and web
