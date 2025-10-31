@echo off
echo ========================================
echo Building Lanflix for Android
echo ========================================
echo.

echo Step 1: Building web assets...
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
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

echo ========================================
echo Build complete!
echo ========================================
echo.
echo Next steps:
echo 1. Open Android Studio: npm run android:open
echo 2. Or run directly: npm run android:run
echo.
pause
