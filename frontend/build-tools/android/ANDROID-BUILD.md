# Building Lanflix for Android

This guide will help you build and deploy the Lanflix app on Android devices.

## Prerequisites

1. **Node.js** (v18 or higher)
2. **Android Studio** (latest version)
3. **Java Development Kit (JDK)** 11 or higher
4. **Android SDK** (installed via Android Studio)

## Initial Setup

### 1. Install Dependencies

```bash
cd frontend
npm install
```

### 2. Initialize Android Project

```bash
npm run android:init
```

This creates the `android/` directory with the native Android project.

### 3. Configure Android Studio

1. Open Android Studio
2. Install Android SDK Platform 33 (or latest)
3. Install Android SDK Build-Tools
4. Set up an Android Virtual Device (AVD) or connect a physical device

## Building the App

### Development Build

1. **Build the web assets and sync to Android:**
   ```bash
   npm run android:sync
   ```

2. **Open in Android Studio:**
   ```bash
   npm run android:open
   ```

3. **Run from Android Studio:**
   - Click the "Run" button (green play icon)
   - Select your device/emulator
   - Wait for the app to install and launch

### Quick Run (All-in-One)

```bash
npm run android:run
```

This command builds, syncs, and runs the app on your connected device.

## First-Time App Configuration

When you first launch the app on Android:

1. The app will open to the home screen
2. Navigate to **Settings** → **App Configuration**
3. Enter your backend server URL (e.g., `http://192.168.1.100:3000`)
4. Click **Test Connection** to verify
5. Click **Save Configuration**

**Important:** Make sure your Android device is on the same network as your backend server!

## Building for Production

### 1. Generate Signed APK

1. Open Android Studio
2. Go to **Build** → **Generate Signed Bundle / APK**
3. Select **APK**
4. Create or select a keystore
5. Fill in keystore details
6. Choose **release** build variant
7. Click **Finish**

The APK will be generated in `android/app/release/app-release.apk`

### 2. Install on Device

```bash
adb install android/app/release/app-release.apk
```

## Network Configuration

### Finding Your Backend Server IP

On your backend server machine:

**Windows:**
```cmd
ipconfig
```
Look for "IPv4 Address" under your active network adapter.

**Linux/Mac:**
```bash
ifconfig
# or
ip addr show
```

### Firewall Configuration

Make sure port 3000 (or your backend port) is open:

**Windows Firewall:**
```powershell
netsh advfirewall firewall add rule name="Lanflix Backend" dir=in action=allow protocol=TCP localport=3000
```

**Linux (ufw):**
```bash
sudo ufw allow 3000/tcp
```

## Troubleshooting

### App Won't Connect to Backend

1. Verify backend is running: `http://YOUR_IP:3000/api/settings`
2. Check firewall settings
3. Ensure devices are on same network
4. Try using IP address instead of hostname
5. Check Android app logs in Android Studio Logcat

### Build Errors

**Gradle sync failed:**
- Update Android Studio to latest version
- Update Android SDK tools
- Clear Gradle cache: `./gradlew clean` in `android/` directory

**Capacitor sync errors:**
```bash
npm run build
npx cap sync android
```

### App Crashes on Launch

1. Check Android Studio Logcat for errors
2. Verify all dependencies are installed
3. Try rebuilding: `npm run android:sync`

## Development Tips

### Live Reload

For faster development, you can run the web version with live reload:

```bash
npm run dev
```

Then access it from your Android device's browser at `http://YOUR_PC_IP:5173`

### Debugging

1. Enable USB debugging on your Android device
2. Connect via USB
3. Open Chrome and navigate to `chrome://inspect`
4. Click "inspect" under your device to open DevTools

### Updating the App

After making changes to the frontend:

```bash
npm run android:sync
```

Then rebuild in Android Studio or run `npm run android:run`

## App Permissions

The app requires these permissions (automatically configured):

- **INTERNET** - To connect to backend server
- **ACCESS_NETWORK_STATE** - To check network connectivity
- **WAKE_LOCK** - To keep screen on during video playback

## Platform-Specific Features

### Android TV Support

The app is compatible with Android TV. To optimize for TV:

1. Use D-pad navigation
2. Test with TV remote
3. Consider adding leanback launcher support

### Chromecast Support

Chromecast functionality is built into the web player and works on Android automatically.

## Next Steps

- Configure backend server URL in app settings
- Test video playback
- Set up profiles
- Start streaming!

## Support

For issues or questions, check the main project README or open an issue on GitHub.
