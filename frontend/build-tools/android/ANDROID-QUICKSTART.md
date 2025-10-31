# Android Quick Start Guide

Get your Lanflix app running on Android phones, tablets, Android TV, and Fire TV in 5 minutes!

## Prerequisites Check

Before starting, make sure you have:
- [ ] Node.js installed (v18+)
- [ ] Android Studio installed
- [ ] Android device, emulator, or TV ready

## Supported Devices

✅ **Android Phones & Tablets** - Full touch support
✅ **Android TV** - D-pad navigation with remote
✅ **Fire TV / Fire TV Stick** - Optimized for 10-foot UI
✅ **Google TV** - Full compatibility

## Step-by-Step Setup

### 1. Install Dependencies (First Time Only)

```bash
cd frontend
npm install
```

### 2. Initialize Android Project (First Time Only)

```bash
npm run android:init
```

This creates the Android project in the `android/` folder.

### 3. Build and Run

**Option A: Quick Run (Recommended)**
```bash
npm run android:run
```

**Option B: Manual Steps**
```bash
# Build web assets
npm run build

# Sync to Android
npx cap sync android

# Open in Android Studio
npm run android:open
```

Then click the green "Run" button in Android Studio.

### 4. Configure the App

When the app launches for the first time:

1. You'll be redirected to the configuration screen
2. Click **Auto-Discover Server** to automatically find your server, or
3. Enter your backend server URL manually:
   - Example: `http://192.168.1.100:6129`
   - Use your computer's local IP address
   - Make sure your Android device is on the same WiFi network
3. Click "Test Connection"
4. Click "Save Configuration"

**Finding Your Backend IP:**

On Windows (where your backend runs):
```cmd
ipconfig
```
Look for "IPv4 Address" (e.g., 192.168.1.100)

### 5. Start Using the App!

**On Mobile:**
- Touch to navigate
- Tap to select
- Swipe to browse

**On TV (Android TV / Fire TV):**
- Use D-pad (arrow keys) to navigate
- Press Enter/OK to select
- Press Back to go back
- Automatic TV mode detection

## TV-Specific Features

### 🎮 Remote Control Support
- **Arrow Keys**: Navigate through menus and content
- **Enter/OK**: Select items and play videos
- **Back**: Return to previous screen
- **Play/Pause**: Control video playback

### 📺 Optimized for TV
- Large, easy-to-read text
- Clear focus indicators
- 10-foot UI design
- Smooth D-pad navigation

### 🔥 Fire TV Installation

1. **Enable ADB on Fire TV:**
   - Settings → My Fire TV → Developer Options
   - Turn on "ADB Debugging"
   - Turn on "Apps from Unknown Sources"

2. **Connect via ADB:**
   ```bash
   adb connect YOUR_FIRETV_IP:5555
   ```

3. **Install APK:**
   ```bash
   adb install app-debug.apk
   ```

4. **Launch the app** from Fire TV home screen

## Common Issues

### "Cannot connect to backend"

1. Check your backend is running: `npm run dev` in the backend folder
2. Verify the IP address is correct
3. Make sure both devices are on the same WiFi network
4. Check Windows Firewall allows port 6129 (or your configured backend port)

### "Build failed"

Try:
```bash
npm run build
npx cap sync android
```

### "Android Studio won't open"

Make sure Android Studio is installed and in your PATH, or open it manually and import the `android/` folder.

### "TV navigation not working"

1. Verify you're on a TV device (Android TV/Fire TV)
2. Try using keyboard arrow keys to test
3. Check browser console for errors
4. Ensure app is up to date

### "Video not playing on TV"

1. Check transcoding is enabled in settings
2. Verify backend is accessible from TV
3. Test with a different video format
4. Check network speed

## Making Changes

After updating your frontend code:

```bash
npm run android:sync
```

Then rebuild in Android Studio (Ctrl+F9) or run again.

## Need Help?

See the full [ANDROID-BUILD.md](./ANDROID-BUILD.md) guide for detailed instructions and troubleshooting.
