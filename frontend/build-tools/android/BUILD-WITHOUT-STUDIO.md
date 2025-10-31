# Building Android APK Without Android Studio

This guide shows you how to build the Lanflix Android APK using only command-line tools (gradlew), without needing to install Android Studio.

## Prerequisites

### Required Software

1. **Node.js** (v18+)
   - Download: https://nodejs.org/

2. **Java Development Kit (JDK)** 17 or higher
   - Download: https://adoptium.net/
   - Or use: `winget install EclipseAdoptium.Temurin.17.JDK`

3. **Android SDK Command Line Tools**
   - Download: https://developer.android.com/studio#command-tools
   - Or follow the setup below

### Quick Setup (Windows)

```cmd
# 1. Install Node.js (if not already installed)
winget install OpenJS.NodeJS.LTS

# 2. Install Java JDK 17
winget install EclipseAdoptium.Temurin.17.JDK

# 3. Verify installations
node --version
java --version
```

## Android SDK Setup (Without Android Studio)

### Option 1: Automatic Setup (Recommended)

Run the initialization script which will download the SDK for you:

```cmd
cd frontend
npm run android:init
```

This will:
- Download Android SDK command-line tools
- Install required SDK packages
- Set up the Android project
- Configure gradlew

### Option 2: Manual SDK Setup

1. **Download Android Command Line Tools:**
   - Go to: https://developer.android.com/studio#command-tools
   - Download "Command line tools only" for Windows
   - Extract to: `C:\Android\cmdline-tools\latest`

2. **Set Environment Variables:**
   ```cmd
   setx ANDROID_HOME "C:\Android"
   setx PATH "%PATH%;%ANDROID_HOME%\cmdline-tools\latest\bin;%ANDROID_HOME%\platform-tools"
   ```

3. **Install SDK Packages:**
   ```cmd
   sdkmanager "platform-tools" "platforms;android-33" "build-tools;33.0.0"
   sdkmanager --licenses
   ```

4. **Initialize Capacitor:**
   ```cmd
   cd frontend
   npm install
   npx cap add android
   ```

## Building the APK

### Quick Build (Release APK)

```cmd
cd frontend/build-tools/android
build-apk.bat
```

This will:
1. Build web assets
2. Sync to Capacitor
3. Build release APK with gradlew
4. Copy APK to `frontend/releases/` folder

**Output:** `frontend/releases/lanflix-android-v1.0.0.apk`

### Debug Build (Faster, for Testing)

```cmd
cd frontend/build-tools/android
build-apk-debug.bat
```

**Output:** `frontend/releases/lanflix-android-debug.apk`

### Manual Build Steps

If you prefer to run each step manually:

```cmd
# 1. Build web assets
cd frontend
npm run build

# 2. Sync to Capacitor
npx cap sync android

# 3. Build APK with gradlew
cd build-tools/android/android
gradlew.bat assembleRelease

# 4. Find your APK
# Location: app/build/outputs/apk/release/app-release.apk
```

## Signing the APK (For Production)

### Generate a Keystore (First Time Only)

```cmd
keytool -genkey -v -keystore lanflix-release.keystore -alias lanflix -keyalg RSA -keysize 2048 -validity 10000
```

Answer the prompts and remember your passwords!

### Configure Signing

1. Create `frontend/build-tools/android/android/keystore.properties`:
   ```properties
   storeFile=../../../lanflix-release.keystore
   storePassword=YOUR_STORE_PASSWORD
   keyAlias=lanflix
   keyPassword=YOUR_KEY_PASSWORD
   ```

2. Edit `frontend/build-tools/android/android/app/build.gradle`:
   ```gradle
   android {
       ...
       signingConfigs {
           release {
               def keystorePropertiesFile = rootProject.file("keystore.properties")
               def keystoreProperties = new Properties()
               keystoreProperties.load(new FileInputStream(keystorePropertiesFile))

               storeFile file(keystoreProperties['storeFile'])
               storePassword keystoreProperties['storePassword']
               keyAlias keystoreProperties['keyAlias']
               keyPassword keystoreProperties['keyPassword']
           }
       }
       buildTypes {
           release {
               signingConfig signingConfigs.release
               ...
           }
       }
   }
   ```

3. Build signed APK:
   ```cmd
   cd frontend/build-tools/android/android
   gradlew.bat assembleRelease
   ```

## Publishing to GitHub Releases

### 1. Build the Release APK

```cmd
cd frontend/build-tools/android
build-apk.bat
```

### 2. Test the APK

Install on your device and test thoroughly:

```cmd
adb install frontend/releases/lanflix-android-v1.0.0.apk
```

### 3. Create GitHub Release

```cmd
# Tag the version
git tag v1.0.0
git push origin v1.0.0
```

Then on GitHub:
1. Go to: https://github.com/JarlTheGamer/Applications./releases
2. Click "Create a new release"
3. Select tag: `v1.0.0`
4. Title: "Lanflix v1.0.0"
5. Add release notes
6. Upload: `frontend/releases/lanflix-android-v1.0.0.apk`
7. Click "Publish release"

### 4. Users Can Now Update In-App!

Once published, users will see the update notification in the app and can download it directly.

## Troubleshooting

### "gradlew.bat not found"

**Solution:** Run the initialization first:
```cmd
cd frontend
npm run android:init
```

### "ANDROID_HOME not set"

**Solution:** Set the environment variable:
```cmd
setx ANDROID_HOME "C:\Android"
```
Then restart your terminal.

### "SDK location not found"

**Solution:** Create `frontend/build-tools/android/android/local.properties`:
```properties
sdk.dir=C:\\Android
```

### "Build failed: Could not find tools.jar"

**Solution:** Make sure you have JDK (not just JRE) installed:
```cmd
java -version
# Should show "openjdk" or similar, not just "java"
```

### "Execution failed for task ':app:mergeReleaseResources'"

**Solution:** Clean and rebuild:
```cmd
cd frontend/build-tools/android/android
gradlew.bat clean
gradlew.bat assembleRelease
```

### APK installs but crashes immediately

**Solution:** Check you built the web assets first:
```cmd
cd frontend
npm run build
npx cap sync android
```

## Build Variants

### Debug Build
- Faster to build
- Larger file size
- Includes debugging symbols
- Not optimized
- Use for testing

```cmd
gradlew.bat assembleDebug
```

### Release Build
- Slower to build
- Smaller file size
- Optimized and minified
- Should be signed
- Use for production

```cmd
gradlew.bat assembleRelease
```

## Automated Release Script

Want to automate the entire process? Create `release.bat`:

```batch
@echo off
echo Building and releasing Lanflix...

REM Get version
set /p VERSION="Enter version (e.g., 1.0.1): "

REM Bump version
cd ..\..\..
node scripts/bump-version.js %VERSION%

REM Build APK
cd frontend/build-tools/android
call build-apk.bat

REM Create git tag
cd ..\..\..
git add .
git commit -m "Release v%VERSION%"
git tag v%VERSION%
git push origin main
git push origin v%VERSION%

echo.
echo Release v%VERSION% is ready!
echo Now upload the APK to GitHub releases.
pause
```

## CI/CD Integration

For automated builds on every commit, see `docs/CI-CD-SETUP.md` (coming soon).

## Performance Tips

1. **Use Gradle Daemon** (enabled by default)
   - Speeds up subsequent builds

2. **Parallel Builds**
   - Add to `gradle.properties`:
     ```properties
     org.gradle.parallel=true
     org.gradle.caching=true
     ```

3. **Incremental Builds**
   - Only rebuild changed files
   - Don't run `clean` unless necessary

4. **Build Only What You Need**
   - Debug builds are faster
   - Use `assembleDebug` for testing

## File Locations

```
frontend/
├── releases/                          # Built APKs go here
│   ├── lanflix-android-v1.0.0.apk    # Release APK
│   └── lanflix-android-debug.apk     # Debug APK
├── build-tools/android/
│   ├── build-apk.bat                 # Release build script
│   ├── build-apk-debug.bat           # Debug build script
│   └── android/                      # Capacitor Android project
│       ├── gradlew.bat               # Gradle wrapper (Windows)
│       ├── gradlew                   # Gradle wrapper (Linux/Mac)
│       └── app/build/outputs/apk/    # Gradle output folder
└── dist/                             # Built web assets
```

## Next Steps

- [Configure signing for production](https://developer.android.com/studio/publish/app-signing)
- [Optimize APK size](https://developer.android.com/topic/performance/reduce-apk-size)
- [Set up CI/CD for automated builds](https://github.com/features/actions)
- [Publish to Google Play Store](https://play.google.com/console)

## Support

Having issues? Check:
1. Java version: `java -version` (should be 17+)
2. Node version: `node --version` (should be 18+)
3. Gradle version: `gradlew.bat --version`
4. Android SDK: Check `ANDROID_HOME` is set

For more help, open an issue on GitHub.
