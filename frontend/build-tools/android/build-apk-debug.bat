@echo off
REM ============================================
REM Lanflix Android Debug APK Builder (Fast)
REM ============================================

echo.
echo ========================================
echo   Lanflix Debug APK Builder
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

REM Step 3: Build Debug APK with Gradle
echo [3/4] Building Debug APK with Gradle...
cd build-tools\android\android
if exist "gradlew.bat" (
    call gradlew.bat assembleDebug
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
echo ✓ Debug APK built successfully
echo.

REM Step 4: Copy APK to releases folder
echo [4/4] Copying APK...
cd ..\..\..

REM Create releases folder if it doesn't exist
if not exist "releases" mkdir releases

REM Copy debug APK
set APK_SOURCE=build-tools\android\android\app\build\outputs\apk\debug\app-debug.apk
set APK_DEST=releases\lanflix-android-debug.apk

if exist "%APK_SOURCE%" (
    copy "%APK_SOURCE%" "%APK_DEST%"
    echo ✓ Debug APK copied to: %APK_DEST%
) else (
    echo WARNING: APK not found at expected location
)

echo.
echo ========================================
echo   Debug Build Complete!
echo ========================================
echo.
echo APK Location: %APK_DEST%
echo.
echo This is a DEBUG build - not for production!
echo Use build-apk.bat for release builds.
echo.
pause
