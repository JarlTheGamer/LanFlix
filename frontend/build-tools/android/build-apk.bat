@echo off
REM ============================================
REM Lanflix Android APK Builder (No Android Studio Required)
REM ============================================

echo.
echo ========================================
echo   Lanflix Android APK Builder
echo ========================================
echo.

REM Check if we're in the right directory
if not exist "..\..\package.json" (
    echo ERROR: Please run this script from frontend/build-tools/android/
    pause
    exit /b 1
)

REM Step 1: Build web assets
echo [1/4] Building web assets...
cd ..\..
call npm run build
if errorlevel 1 (
    echo ERROR: Failed to build web assets
    pause
    exit /b 1
)
echo ✓ Web assets built successfully
echo.

REM Step 2: Sync to Capacitor
echo [2/4] Syncing to Capacitor...
call npx cap sync android
if errorlevel 1 (
    echo ERROR: Failed to sync to Capacitor
    pause
    exit /b 1
)
echo ✓ Synced to Capacitor successfully
echo.

REM Step 3: Build APK with Gradle
echo [3/4] Building APK with Gradle...
cd build-tools\android\android
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
) else (
    echo ERROR: gradlew.bat not found. Please run 'npm run android:init' first.
    cd ..\..\..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Gradle build failed
    cd ..\..\..
    pause
    exit /b 1
)
echo ✓ APK built successfully
echo.

REM Step 4: Copy APK to releases folder
echo [4/4] Copying APK to releases folder...
cd ..\..\..

REM Create releases folder if it doesn't exist
if not exist "releases" mkdir releases

REM Get version from package.json (simple approach)
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"\"version\"" package.json') do set VERSION=%%a
set VERSION=%VERSION:"=%

REM Copy and rename APK
set APK_SOURCE=build-tools\android\android\app\build\outputs\apk\release\app-release.apk
set APK_DEST=releases\lanflix-android-v%VERSION%.apk

if exist "%APK_SOURCE%" (
    copy "%APK_SOURCE%" "%APK_DEST%"
    echo ✓ APK copied to: %APK_DEST%
) else (
    echo WARNING: APK not found at expected location
    echo Looking for APK in build outputs...
    dir /s /b build-tools\android\android\app\build\outputs\apk\*.apk
)

echo.
echo ========================================
echo   Build Complete!
echo ========================================
echo.
echo APK Location: %APK_DEST%
echo Version: %VERSION%
echo.
echo Next steps:
echo   1. Test the APK on your device
echo   2. Upload to GitHub releases
echo   3. Tag the release as v%VERSION%
echo.
pause
