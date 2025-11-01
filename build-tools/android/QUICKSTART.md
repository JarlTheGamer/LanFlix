# Lanflix Android App - Quick Start

## What You Need

1. **Java JDK 17 or higher**
   - Download from: https://adoptium.net/
   - Or use: `winget install EclipseAdoptium.Temurin.17.JDK`

2. **Android SDK** (optional, Gradle will download if needed)
   - Or install Android Studio: https://developer.android.com/studio

## Build the APK

### Option 1: From Project Root (Recommended)

```bash
npm run build:android
```

The APK will be in: `build-tools/android/app/build/outputs/apk/debug/app-debug.apk`

### Option 2: Direct Build

```bash
cd build-tools/android
gradlew assembleDebug
```

### Option 3: Full Release Build

```bash
npm run release
```

This will:
1. Build the server
2. Build the Android APK
3. Create a GitHub release

## Install on Your Device

### Method 1: USB Cable

1. Enable Developer Options on your Android device
2. Enable USB Debugging
3. Connect device via USB
4. Run: `gradlew installDebug`

### Method 2: Manual Install

1. Copy the APK to your device
2. Open the APK file on your device
3. Allow installation from unknown sources if prompted
4. Install

## First Run Setup

1. Open the Lanflix app
2. Enter your server URL (e.g., `http://192.168.1.100:8080`)
3. Tap "Test Connection" to verify
4. Tap "Save Settings"
5. Start browsing!

## Troubleshooting

### Build Fails

**Java not found:**
```bash
# Check Java version
java -version

# Should show version 17 or higher
```

**Gradle fails:**
```bash
# Clean and rebuild
cd build-tools/android
gradlew clean
gradlew assembleDebug
```

### Can't Connect to Server

1. **Check server is running:**
   - Open browser: `http://YOUR_IP:8080/health`
   - Should show: `{"status":"ok",...}`

2. **Check firewall:**
   - Windows: Allow port 8080 in Windows Firewall
   - Router: Ensure devices are on same network

3. **Use IP address, not localhost:**
   - ✅ `http://192.168.1.100:8080`
   - ❌ `http://localhost:8080`

4. **Find your server IP:**
   ```bash
   # On server machine
   ipconfig
   # Look for IPv4 Address
   ```

### App Crashes

1. Check Android version (need 7.0+)
2. Clear app data: Settings > Apps > Lanflix > Clear Data
3. Reinstall the app

## Development

### Open in Android Studio

1. Open Android Studio
2. File > Open
3. Select `build-tools/android` folder
4. Wait for Gradle sync
5. Run on emulator or device

### Make Changes

1. Edit files in `app/src/main/java/com/lanflix/app/`
2. Sync Gradle
3. Build and run

## Project Structure

```
build-tools/android/
├── app/
│   ├── src/main/
│   │   ├── java/com/lanflix/app/
│   │   │   ├── api/          # API client
│   │   │   ├── models/       # Data models
│   │   │   ├── ui/           # Activities
│   │   │   └── utils/        # Utilities
│   │   ├── res/              # Resources
│   │   └── AndroidManifest.xml
│   └── build.gradle
├── build.gradle
├── settings.gradle
└── gradlew.bat
```

## Next Steps

- [ ] Set up your media library on the server
- [ ] Configure server settings
- [ ] Start streaming!

## Need Help?

- Check the main README.md
- Check BUILD.md for detailed build instructions
- Check server logs: `server/backend/logs/`
