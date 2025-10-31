# 📺 Building Lanflix for Android TV / Fire TV

## Quick Build (3 Steps)

### 1. Navigate to frontend folder
```bash
cd frontend
```

### 2. Run the build script
**Windows:**
```bash
build-tools\android\build-android.bat
```

**Linux/Mac:**
```bash
chmod +x build-tools/android/build-android.sh
./build-tools/android/build-android.sh
```

### 3. Open in Android Studio
```bash
npm run android:open
```

Then click the green **Run** button to build and install.

---

## What's New - TV Support

✅ **D-pad Navigation** - Use remote control arrow keys
✅ **Enter Key Selection** - Press Enter/OK to select items
✅ **TV Mode Auto-Detection** - Automatically enables for Fire TV, Android TV
✅ **Optimized UI** - Large text and clear focus indicators
✅ **Fixed Video Player** - Works on mobile and TV devices

---

## Testing on Different Devices

### Fire TV Stick
1. Enable ADB: Settings → My Fire TV → Developer Options → ADB Debugging
2. Connect: `adb connect YOUR_FIRETV_IP:5555`
3. Install: `adb install app-debug.apk`

### Android TV
1. Enable Developer Options (tap Build 7 times)
2. Enable USB Debugging
3. Connect and install via ADB

### Mobile Android
1. Enable USB Debugging
2. Connect via USB
3. Run from Android Studio

---

## APK Location

After building, find your APK at:
```
frontend/build-tools/android/android/app/build/outputs/apk/debug/app-debug.apk
```

---

## Navigation Controls

### On TV (Remote Control)
- **Arrow Keys** → Navigate
- **Enter/OK** → Select
- **Back** → Go back
- **Play/Pause** → Control video

### On Desktop (Testing)
- **Arrow Keys** → Navigate
- **Enter** → Select
- **Escape** → Go back
- **Space** → Play/Pause video

---

## Troubleshooting

### Video not playing?
1. Check transcoding settings in app
2. Ensure backend is running
3. Verify network connection

### Navigation not working?
1. Verify TV mode is detected (check console)
2. Test with keyboard first
3. Ensure latest build is installed

### Can't connect to backend?
1. Use your computer's local IP (not localhost)
2. Example: `http://192.168.1.100:3000`
3. Both devices must be on same WiFi

---

## Full Documentation

- **Quick Start**: `frontend/build-tools/android/ANDROID-QUICKSTART.md`
- **Detailed Build Guide**: `BUILD-GUIDE.md`
- **TV Features**: `docs/features/android-tv-support.md`

---

## Need Help?

1. Check the troubleshooting sections above
2. Review the full documentation
3. Verify all prerequisites are installed
4. Try a clean build: `npm run build && npx cap sync android`

**Happy Streaming! 🎬**
