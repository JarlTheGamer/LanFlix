# Lanflix Native WebView App

A lightweight Android app that wraps your Lanflix web application in an optimized WebView for better performance on low-end devices.

## Features

- **Hardware Acceleration**: Enabled for smooth scrolling and animations
- **Optimized WebView**: Configured for best performance on low-end devices
- **Pull to Refresh**: Swipe down to refresh the web content
- **Native Navigation**: Back button support for web navigation
- **Splash Screen**: Professional loading screen while app initializes
- **Error Handling**: Graceful error messages for network issues
- **External Link Handling**: Opens external links in system browser

## Setup

1. **Update Server URL**: 
   - Open `MainActivity.kt`
   - Change the `serverUrl` variable to your Lanflix server address:
   ```kotlin
   private val serverUrl = "http://YOUR_SERVER_IP:5000"
   ```

2. **Build the App**:
   ```bash
   cd build-tools/AndroidVersions/native-app
   ./gradlew assembleDebug
   ```

3. **Install on Device**:
   ```bash
   ./gradlew installDebug
   ```

## Performance Optimizations

- **Hardware acceleration** enabled for smooth rendering
- **Optimized cache settings** for faster loading
- **Minimal memory footprint** suitable for low-end devices
- **Efficient WebView configuration** for media playback
- **Progressive loading** with visual feedback

## Customization

### Change App Icon
Replace the default launcher icons in:
- `app/src/main/res/mipmap-*/ic_launcher.png`
- `app/src/main/res/mipmap-*/ic_launcher_round.png`

### Modify Colors
Edit `app/src/main/res/values/colors.xml` to match your brand colors.

### Update App Name
Change the app name in `app/src/main/res/values/strings.xml`.

## Build Commands

- **Debug Build**: `./gradlew assembleDebug`
- **Release Build**: `./gradlew assembleRelease`
- **Install Debug**: `./gradlew installDebug`
- **Clean**: `./gradlew clean`

## Requirements

- Android SDK 21+ (Android 5.0+)
- Target SDK 34 (Android 14)
- Kotlin support
- Internet permission for web content