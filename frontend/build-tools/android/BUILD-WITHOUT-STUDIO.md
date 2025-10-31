# Building Android APK Without Android Studio

You can build the APK using command-line tools only, but you still need some Android SDK components.

## What You Actually Need

### Minimum Requirements:
1. **Java Development Kit (JDK)** 11 or 17
2. **Android Command Line Tools** (not the full Android Studio)
3. **Gradle** (included in the project)

## Setup Without Android Studio

### 1. Install JDK

**Windows (using Chocolatey):**
```cmd
choco install openjdk17
```

**Or download from:** https://adoptium.net/

Verify:
```cmd
java -version
```

### 2. Install Android Command Line Tools

1. Download from: https://developer.android.com/studio#command-tools
2. Extract to: `C:\Android\cmdline-tools\latest\`
3. Add to PATH:
   ```cmd
   setx ANDROID_HOME "C:\Android"
   setx PATH "%PATH%;%ANDROID_HOME%\cmdline-tools\latest\bin"
   ```

### 3. Install Required SDK Components

```cmd
sdkmanager "platform-tools" "platforms;android-33" "build-tools;33.0.0"
```

Accept licenses:
```cmd
sdkmanager --licenses
```

## Building the APK

### 1. Build Web Assets
```bash
cd frontend
npm run build
```

### 2. Sync to Android
```bash
npx cap sync android
```

### 3. Build APK with Gradle

**Debug APK (for testing):**
```cmd
cd build-tools\android\android
gradlew assembleDebug
```

**Release APK (for distribution):**
```cmd
gradlew assembleRelease
```

The APK will be in:
- Debug: `build-tools/android/android/app/build/outputs/apk/debug/app-debug.apk`
- Release: `build-tools/android/android/app/build/outputs/apk/release/app-release-unsigned.apk`

### 4. Install on Device

```cmd
adb install app-debug.apk
```

## Signing Release APK

For production, you need to sign the APK:

### 1. Generate Keystore (first time only)
```cmd
keytool -genkey -v -keystore lanflix.keystore -alias lanflix -keyalg RSA -keysize 2048 -validity 10000
```

### 2. Sign the APK
```cmd
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore lanflix.keystore app-release-unsigned.apk lanflix
```

### 3. Align the APK
```cmd
zipalign -v 4 app-release-unsigned.apk lanflix-release.apk
```

## Quick Build Script

Create `build-apk.bat` in `frontend/`:

```batch
@echo off
echo Building Lanflix APK...
call npm run build
call npx cap sync android
cd build-tools\android\android
call gradlew assembleDebug
echo.
echo APK built: build-tools\android\android\app\build\outputs\apk\debug\app-debug.apk
pause
```

## Alternative: Use Android Studio (Easier)

If you find command-line building too complex, Android Studio provides:
- Visual build tools
- Easy device management
- Built-in emulator
- Debugging tools
- One-click APK generation

Download: https://developer.android.com/studio

## Disk Space Requirements

- **Command Line Tools Only:** ~2-3 GB
- **Android Studio:** ~8-10 GB

## Troubleshooting

### "ANDROID_HOME not set"
```cmd
setx ANDROID_HOME "C:\Android"
```

### "sdkmanager not found"
Make sure cmdline-tools are in: `%ANDROID_HOME%\cmdline-tools\latest\bin\`

### "Gradle build failed"
Try:
```cmd
cd build-tools\android\android
gradlew clean
gradlew assembleDebug
```

### "License not accepted"
```cmd
sdkmanager --licenses
```
Press 'y' for all prompts.

## Recommendation

**For beginners:** Use Android Studio - it's much easier and handles everything automatically.

**For CI/CD or advanced users:** Command-line tools are perfect for automated builds.
