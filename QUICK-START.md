# 🚀 Lanflix - Quick Start

## Build for Android TV / Fire TV

### 1️⃣ Navigate to frontend
```bash
cd frontend
```

### 2️⃣ Run build script
```bash
# Windows
build-tools\android\build-android.bat

# Linux/Mac
./build-tools/android/build-android.sh
```

### 3️⃣ Open in Android Studio
```bash
npm run android:open
```

### 4️⃣ Click Run ▶️

---

## Install on Fire TV

```bash
# Enable ADB on Fire TV first
adb connect YOUR_FIRETV_IP:5555
adb install app-debug.apk
```

---

## Test Navigation

**Desktop (keyboard):**
- Arrow keys = Navigate
- Enter = Select
- Escape = Back

**TV (remote):**
- D-pad = Navigate  
- OK/Enter = Select
- Back = Back

---

## APK Location
```
frontend/build-tools/android/android/app/build/outputs/apk/debug/app-debug.apk
```

---

## Need Help?

📖 **Full Guides:**
- `TV-BUILD-INSTRUCTIONS.md` - Build guide
- `ANDROID-TV-READY.md` - What's new
- `BUILD-GUIDE.md` - Complete documentation

🎮 **TV Features:**
- `docs/features/android-tv-support.md`

---

**That's it! Happy streaming! 🎬**
