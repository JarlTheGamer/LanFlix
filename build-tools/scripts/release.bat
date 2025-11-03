@echo off
REM ============================================
REM Lanflix Native App Release Script
REM Builds Native WebView APK and publishes to GitHub
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Lanflix Native App Release
echo ========================================
echo.

if not exist "build-tools\AndroidVersions\native-app" (
    echo ERROR: Native app build tools not found
    pause
    exit /b 1
)

REM Check if git is installed
where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: Git is not installed or not in PATH
    pause
    exit /b 1
)

REM Check if GitHub CLI is installed
where gh >nul 2>nul
if errorlevel 1 (
    echo WARNING: GitHub CLI ^(gh^) is not installed
    echo.
    echo You have two options:
    echo   1. Install GitHub CLI: winget install GitHub.cli
    echo   2. Continue and upload APK manually to GitHub
    echo.
    set /p CONTINUE="Continue without GitHub CLI? (y/n): "
    if /i "!CONTINUE!" neq "y" (
        echo.
        echo Install GitHub CLI with: winget install GitHub.cli
        echo Then run this script again.
        pause
        exit /b 1
    )
    set MANUAL_UPLOAD=1
) else (
    set MANUAL_UPLOAD=0
)

REM Get version input
echo.
echo Enter version number or bump type:
echo   - Specific version: 1.0.1
echo   - patch: 1.0.0 -^> 1.0.1
echo   - minor: 1.0.0 -^> 1.1.0
echo   - major: 1.0.0 -^> 2.0.0
echo.
set /p VERSION_INPUT="Version: "

if "!VERSION_INPUT!"=="" (
    echo ERROR: Version is required
    pause
    exit /b 1
)

REM Step 1: Bump version
echo.
echo [1/7] Bumping version to !VERSION_INPUT!...
node build-tools\scripts\bump-version.js !VERSION_INPUT!
if errorlevel 1 (
    echo ERROR: Failed to bump version
    pause
    exit /b 1
)

REM Get the actual version from package.json
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"\"version\"" package.json') do set VERSION=%%a
set VERSION=%VERSION:"=%
echo ✓ Version bumped to %VERSION%
echo.

REM Step 2: Build Native WebView APK
echo [2/5] Building Native WebView APK...

REM Set JAVA_HOME to Java 21 for Gradle compatibility
if exist "C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot" (
    set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot"
) else if exist "C:\Program Files\Java\jdk-21" (
    set "JAVA_HOME=C:\Program Files\Java\jdk-21"
) else (
    echo WARNING: Java 21 not found at expected location
    echo Using system Java - build may fail if Java version is too new
)
set "PATH=%JAVA_HOME%\bin;%PATH%"
echo Using Java: %JAVA_HOME%

cd build-tools\AndroidVersions\native-app
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
    if errorlevel 1 (
        echo WARNING: Native Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: Native app gradlew.bat not found
    cd ..\..\..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Native app Gradle build failed
    cd ..\..\..
    pause
    exit /b 1
)
echo ✓ Native WebView APK built successfully
cd ..\..\..
echo.

REM Step 3: Copy APK files
echo [3/5] Preparing release files...
if not exist "releases" mkdir releases

REM Find Native WebView APK (try release first, then debug)
set NATIVE_APK_SOURCE=build-tools\AndroidVersions\native-app\app\build\outputs\apk\release\app-release.apk
set NATIVE_APK_TYPE=release
if not exist "!NATIVE_APK_SOURCE!" (
    set NATIVE_APK_SOURCE=build-tools\AndroidVersions\native-app\app\build\outputs\apk\debug\app-debug.apk
    set NATIVE_APK_TYPE=debug
)

if not exist "!NATIVE_APK_SOURCE!" (
    echo ERROR: Native WebView APK not found at: !NATIVE_APK_SOURCE!
    echo.
    echo Make sure the Native WebView Android build completed successfully.
    pause
    exit /b 1
)

set NATIVE_APK_DEST=releases\lanflix-native-webview-v!VERSION!.apk
copy "!NATIVE_APK_SOURCE!" "!NATIVE_APK_DEST!"
echo ✓ Native WebView APK copied to: !NATIVE_APK_DEST! ^(!NATIVE_APK_TYPE! build^)
echo.

REM Step 4: Git commit and tag
echo [4/5] Committing to Git...
git add .
git commit -m "Release v!VERSION!"
if errorlevel 1 (
    echo WARNING: Git commit failed ^(maybe no changes?^)
)

echo Creating tag v!VERSION!...

REM Check if tag already exists
git rev-parse v!VERSION! >nul 2>nul
if not errorlevel 1 (
    echo WARNING: Tag v!VERSION! already exists
    echo.
    set /p OVERWRITE="Delete and recreate tag? (y/n): "
    if /i "!OVERWRITE!" neq "y" (
        echo Skipping tag creation
        goto skip_tag
    )
    
    echo Deleting existing tag locally...
    git tag -d v!VERSION!
    
    echo Deleting existing tag on remote...
    git push origin :refs/tags/v!VERSION! 2>nul
)

git tag v!VERSION!
if errorlevel 1 (
    echo ERROR: Failed to create git tag
    pause
    exit /b 1
)

:skip_tag
echo Pushing to GitHub...
git push origin main
if errorlevel 1 (
    echo WARNING: Failed to push to main branch
)

git push origin v!VERSION! 2>nul
if errorlevel 1 (
    echo WARNING: Failed to push tag ^(may already exist on remote^)
    echo Forcing push...
    git push -f origin v!VERSION!
    if errorlevel 1 (
        echo ERROR: Failed to force push tag
        pause
        exit /b 1
    )
)
echo ✓ Pushed to GitHub
echo.

REM Step 5: Create GitHub Release
echo [5/5] Creating GitHub Release...

if "!MANUAL_UPLOAD!"=="1" (
    echo.
    echo ========================================
    echo   Manual Upload Required
    echo ========================================
    echo.
    echo GitHub CLI is not installed.
    echo Please upload the APK manually:
    echo.
    echo 1. Go to: https://github.com/JarlTheGamer/Applications./releases/new
    echo 2. Choose tag: v!VERSION!
    echo 3. Release title: Lanflix Native WebView v!VERSION!
    echo 4. Upload APK: !NATIVE_APK_DEST!
    echo 5. Add release notes ^(see template below^)
    echo 6. Click "Publish release"
    echo.
    echo Release Notes Template:
    echo ------------------------
    echo ## What's New in v!VERSION!
    echo.
    echo ### New Features
    echo - Feature 1
    echo - Feature 2
    echo.
    echo ### Bug Fixes
    echo - Fix 1
    echo - Fix 2
    echo.
    echo ### Performance
    echo - Improvement 1
    echo.
    echo ## Installation
    echo Download the APK and install on your Android device.
    echo.
    start https://github.com/JarlTheGamer/Applications./releases/new?tag=v!VERSION!
) else (
    REM Check if gh is authenticated
    gh auth status >nul 2>nul
    if errorlevel 1 (
        echo Authenticating with GitHub...
        gh auth login
        if errorlevel 1 (
            echo ERROR: GitHub authentication failed
            pause
            exit /b 1
        )
    )

    REM Create release notes file
    echo ## 🎬 Lanflix Native WebView v!VERSION! - Optimized Hybrid Experience > release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📦 What's Included >> release-notes.tmp
    echo - **🌐 Native WebView APK**: Your web frontend in an optimized native wrapper >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### ✨ Features >> release-notes.tmp
    echo **Native WebView APK:** >> release-notes.tmp
    echo - Hardware-accelerated WebView for smooth performance >> release-notes.tmp
    echo - Optimized for low-end devices >> release-notes.tmp
    echo - Android TV support with remote control navigation >> release-notes.tmp
    echo - Auto-orientation support ^(portrait/landscape^) >> release-notes.tmp
    echo - Pull-to-refresh functionality >> release-notes.tmp
    echo - Native splash screen and loading indicators >> release-notes.tmp
    echo - Configurable server URL >> release-notes.tmp
    echo - External link handling >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 🚀 Performance Optimizations >> release-notes.tmp
    echo - Hardware acceleration enabled >> release-notes.tmp
    echo - Optimized cache settings >> release-notes.tmp
    echo - Minimal memory footprint >> release-notes.tmp
    echo - TV remote D-pad navigation >> release-notes.tmp
    echo - Focus management for TV interfaces >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ## 📋 Installation >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📱 Installation >> release-notes.tmp
    echo 1. Download `lanflix-native-webview-v!VERSION!.apk` >> release-notes.tmp
    echo 2. Install on your Android device >> release-notes.tmp
    echo 3. Update server URL in MainActivity.kt if needed >> release-notes.tmp
    echo 4. Enjoy optimized web experience with native performance! >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Features:** >> release-notes.tmp
    echo - Works with your existing Lanflix server >> release-notes.tmp
    echo - No server changes needed >> release-notes.tmp
    echo - Optimized for both phones and Android TV >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Requirements:** >> release-notes.tmp
    echo - Android 5.0+ ^(API 21+^) >> release-notes.tmp
    echo - Lanflix server running >> release-notes.tmp
    echo - Network access to server >> release-notes.tmp

    echo Creating GitHub release...
    
    REM Prepare release assets - native webview APK
    set RELEASE_ASSETS="!NATIVE_APK_DEST!"
    
    gh release create v!VERSION! !RELEASE_ASSETS! ^
        --title "Lanflix Native WebView v!VERSION!" ^
        --notes-file release-notes.tmp ^
        --repo JarlTheGamer/Applications.

    if errorlevel 1 (
        echo ERROR: Failed to create GitHub release
        echo.
        echo You can create it manually at:
        echo https://github.com/JarlTheGamer/Applications./releases/new?tag=v!VERSION!
        del release-notes.tmp
        pause
        exit /b 1
    )

    del release-notes.tmp
    echo ✓ GitHub release created successfully
)

REM Final summary
echo.
echo ========================================
echo   Release Complete!
echo ========================================
echo.
echo Version: v!VERSION!
echo.
echo 🌐 Native WebView APK: !NATIVE_APK_DEST! ^(!NATIVE_APK_TYPE! build^)
echo 🌐 Release: https://github.com/JarlTheGamer/Applications./releases/tag/v!VERSION!
echo.
echo ✅ Native WebView APK built successfully!
echo.
echo 📋 What's included:
echo   ✅ Optimized WebView wrapper for your web frontend
echo   ✅ Hardware acceleration for smooth performance
echo   ✅ Android TV support with remote control navigation
echo   ✅ Auto-orientation support for all screen sizes
echo.
echo 🚀 Features:
echo   1. Uses your existing web frontend in optimized WebView
echo   2. Hardware-accelerated rendering for low-end devices
echo   3. TV remote D-pad navigation support
echo   4. Configurable server URL in MainActivity.kt
echo.
pause
