# Android Quick Start Guide

Get your Lanflix app running on Android in 5 minutes!

## Prerequisites Check

Before starting, make sure you have:
- [ ] Node.js installed (v18+)
- [ ] Android Studio installed
- [ ] Android device or emulator ready

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
2. Enter your backend server URL:
   - Example: `http://192.168.1.100:3000`
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

- Browse content
- Create profiles
- Start streaming

## Common Issues

### "Cannot connect to backend"

1. Check your backend is running: `npm run dev` in the backend folder
2. Verify the IP address is correct
3. Make sure both devices are on the same WiFi network
4. Check Windows Firewall allows port 3000

### "Build failed"

Try:
```bash
npm run build
npx cap sync android
```

### "Android Studio won't open"

Make sure Android Studio is installed and in your PATH, or open it manually and import the `android/` folder.

## Making Changes

After updating your frontend code:

```bash
npm run android:sync
```

Then rebuild in Android Studio (Ctrl+F9) or run again.

## Need Help?

See the full [ANDROID-BUILD.md](./ANDROID-BUILD.md) guide for detailed instructions and troubleshooting.
