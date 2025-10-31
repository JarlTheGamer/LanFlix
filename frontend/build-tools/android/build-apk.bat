@echo off
echo ========================================
echo Building Lanflix Android APK
echo ========================================
echo.

cd ..\..

echo Step 1: Building web assets...
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: Web build failed!
    pause
    exit /b %errorlevel%
)
echo.

echo Step 2: Syncing to Android...
call npx cap sync android
if %errorlevel% neq 0 (
    echo ERROR: Capacitor sync failed!
    pause
    exit /b %errorlevel%
)
echo.

echo Step 3: Building APK with Gradle...
cd build-tools\android\android
call gradlew assembleDebug
if %errorlevel% neq 0 (
    echo ERROR: Gradle build failed!
    cd ..\..\..
    pause
    exit /b %errorlevel%
)
cd ..\..\..
echo.

echo ========================================
echo Build Complete!
echo ========================================
echo.
echo APK Location:
echo build-tools\android\android\app\build\outputs\apk\debug\app-debug.apk
echo.
echo To install on device:
echo adb install build-tools\android\android\app\build\outputs\apk\debug\app-debug.apk
echo.
pause
