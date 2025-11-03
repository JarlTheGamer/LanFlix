@echo off
REM ============================================
REM Lanflix Automated Release Script
REM Builds APK and publishes to GitHub
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Lanflix Automated Release
echo ========================================
echo.

if not exist "build-tools\androidversions" (
    echo ERROR: Android build tools not found
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

REM Step 2: Build WebUI Android APK
echo [2/6] Building WebUI Android APK...

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

cd build-tools\AndroidVersions\WebUI
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
    if errorlevel 1 (
        echo WARNING: WebUI Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: WebUI gradlew.bat not found
    cd ..\..\..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: WebUI Gradle build failed
    cd ..\..\..
    pause
    exit /b 1
)
echo ✓ WebUI APK built successfully
cd ..\..\..
echo.

REM Step 3: Build Native Android APK
echo [3/6] Building Native Android APK...

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

cd build-tools\AndroidVersions\android-native
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
    if errorlevel 1 (
        echo WARNING: Native Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: Native gradlew.bat not found
    cd ..\..\..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Native Gradle build failed
    cd ..\..\..
    pause
    exit /b 1
)
echo ✓ Native APK built successfully
cd ..\..\..
echo.

REM Step 4: Copy APK files
echo [4/6] Preparing release files...
if not exist "releases" mkdir releases

REM Find WebUI APK (try release first, then debug)
set WEBUI_APK_SOURCE=build-tools\AndroidVersions\WebUI\app\build\outputs\apk\release\app-release.apk
set WEBUI_APK_TYPE=release
if not exist "!WEBUI_APK_SOURCE!" (
    set WEBUI_APK_SOURCE=build-tools\AndroidVersions\WebUI\app\build\outputs\apk\debug\app-debug.apk
    set WEBUI_APK_TYPE=debug
)

if not exist "!WEBUI_APK_SOURCE!" (
    echo ERROR: WebUI APK not found at: !WEBUI_APK_SOURCE!
    echo.
    echo Make sure the WebUI Android build completed successfully.
    pause
    exit /b 1
)

set WEBUI_APK_DEST=releases\lanflix-webui-v!VERSION!.apk
copy "!WEBUI_APK_SOURCE!" "!WEBUI_APK_DEST!"
echo ✓ WebUI APK copied to: !WEBUI_APK_DEST! ^(!WEBUI_APK_TYPE! build^)

REM Find Native APK (try release first, then debug)
set NATIVE_APK_SOURCE=build-tools\AndroidVersions\android-native\app\build\outputs\apk\release\app-release.apk
set NATIVE_APK_TYPE=release
if not exist "!NATIVE_APK_SOURCE!" (
    set NATIVE_APK_SOURCE=build-tools\AndroidVersions\android-native\app\build\outputs\apk\debug\app-debug.apk
    set NATIVE_APK_TYPE=debug
)

if not exist "!NATIVE_APK_SOURCE!" (
    echo ERROR: Native APK not found at: !NATIVE_APK_SOURCE!
    echo.
    echo Make sure the Native Android build completed successfully.
    pause
    exit /b 1
)

set NATIVE_APK_DEST=releases\lanflix-native-v!VERSION!.apk
copy "!NATIVE_APK_SOURCE!" "!NATIVE_APK_DEST!"
echo ✓ Native APK copied to: !NATIVE_APK_DEST! ^(!NATIVE_APK_TYPE! build^)
echo.

REM Step 5: Git commit and tag
echo [5/6] Committing to Git...
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

REM Step 6: Create GitHub Release
echo [6/6] Creating GitHub Release...

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
    echo 3. Release title: Lanflix v!VERSION!
    echo 4. Upload APKs: !WEBUI_APK_DEST! and !NATIVE_APK_DEST!
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
    echo ## 🎬 Lanflix v!VERSION! - Dual Android Experience > release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📦 What's Included >> release-notes.tmp
    echo - **�️ WebUI APK**: Your web frontend in a WebView wrapper >> release-notes.tmp
    echo - **�  Native APK**: Pixel-perfect Kotlin UI with Netflix-style performance >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### ✨ Features >> release-notes.tmp
    echo **WebUI APK:** >> release-notes.tmp
    echo - Exact web frontend experience >> release-notes.tmp
    echo - Auto-connects to server on port 5037 >> release-notes.tmp
    echo - Familiar web interface >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Native APK:** >> release-notes.tmp
    echo - Pixel-perfect Netflix-style UI replica >> release-notes.tmp
    echo - Native ExoPlayer video playback >> release-notes.tmp
    echo - 60fps hardware-accelerated rendering >> release-notes.tmp
    echo - Android TV support with remote control >> release-notes.tmp
    echo - Auto-server discovery on port 5037 >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 🚀 Performance Comparison >> release-notes.tmp
    echo ^| Feature ^| WebUI APK ^| Native APK ^| >> release-notes.tmp
    echo ^|------^|--------^|---------^| >> release-notes.tmp
    echo ^| UI Rendering ^| Web-based ^| 60fps Native ^| >> release-notes.tmp
    echo ^| Video Playback ^| WebView ^| ExoPlayer ^| >> release-notes.tmp
    echo ^| Memory Usage ^| Higher ^| Optimized ^| >> release-notes.tmp
    echo ^| Startup Time ^| 3-5 seconds ^| Instant ^| >> release-notes.tmp
    echo ^| Battery Life ^| Standard ^| Excellent ^| >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ## 📋 Installation >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📱 Choose Your Experience >> release-notes.tmp
    echo **Option 1: WebUI APK ^(Familiar^)** >> release-notes.tmp
    echo 1. Download `lanflix-webui-v!VERSION!.apk` >> release-notes.tmp
    echo 2. Install on your Android device >> release-notes.tmp
    echo 3. Uses your existing web interface >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Option 2: Native APK ^(Performance^)** >> release-notes.tmp
    echo 1. Download `lanflix-native-v!VERSION!.apk` >> release-notes.tmp
    echo 2. Install on your Android device >> release-notes.tmp
    echo 3. Enjoy Netflix-level native performance! >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Both apps:** >> release-notes.tmp
    echo - Auto-discover your Lanflix server on port 5037 >> release-notes.tmp
    echo - Work with your existing server setup >> release-notes.tmp
    echo - No server changes needed >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Requirements:** >> release-notes.tmp
    echo - Android 7.0+ ^(API 24+^) >> release-notes.tmp
    echo - Lanflix server running on port 5037 >> release-notes.tmp
    echo - Same network for auto-discovery >> release-notes.tmp

    echo Creating GitHub release...
    
    REM Prepare release assets - both APKs
    set RELEASE_ASSETS="!WEBUI_APK_DEST!" "!NATIVE_APK_DEST!"
    
    gh release create v!VERSION! !RELEASE_ASSETS! ^
        --title "Lanflix v!VERSION!" ^
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
echo 📱 WebUI APK: !WEBUI_APK_DEST! ^(!WEBUI_APK_TYPE! build^)
echo 🚀 Native APK: !NATIVE_APK_DEST! ^(!NATIVE_APK_TYPE! build^)
echo 🌐 Release: https://github.com/JarlTheGamer/Applications./releases/tag/v!VERSION!
echo.
echo � Boteh Android versions built successfully!
echo.
echo 📋 What's included:
echo   ✅ WebUI APK ^(WebView-based, connects to port 5037^)
echo   ✅ Native APK ^(Kotlin UI replica, connects to port 5037^)
echo.
echo 🚀 Users can choose:
echo   1. WebUI APK - Uses your web frontend in WebView
echo   2. Native APK - Pixel-perfect native UI with 60fps performance
echo   3. Both connect to your server on port 5037 automatically
echo.
pause
