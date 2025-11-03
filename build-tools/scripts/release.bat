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

if not exist "build-tools\android" (
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

REM Step 2: Build web frontend
echo [2/8] Building web frontend...
cd lanflix-server\app\WebApi\ClientApp
call npm install
if errorlevel 1 (
    echo ERROR: npm install failed
    cd ..\..\..\..
    pause
    exit /b 1
)

call npm run build
if errorlevel 1 (
    echo ERROR: Frontend build failed
    cd ..\..\..\..
    pause
    exit /b 1
)
echo ✓ Web frontend built successfully
cd ..\..\..\..
echo.

REM Step 3: Build server
echo [3/8] Building server...
cd lanflix-server
call dotnet publish app\WebApi\Lanflix.WebApi.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true -o ..\releases\server-win-x64
if errorlevel 1 (
    echo ERROR: Server build failed
    cd ..
    pause
    exit /b 1
)
echo ✓ Server built successfully
cd ..
echo.

REM Step 4: Build Android APK
echo [4/8] Building Android APK...

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
        echo WARNING: Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: gradlew.bat not found
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
cd ..\..\..
echo.

REM Step 5: Copy release files
echo [5/8] Preparing release files...
if not exist "releases" mkdir releases

REM Find the APK (try release first, then debug)
set APK_SOURCE=build-tools\AndroidVersions\android-native\app\build\outputs\apk\release\app-release.apk
set APK_TYPE=release
if not exist "!APK_SOURCE!" (
    set APK_SOURCE=build-tools\AndroidVersions\android-native\app\build\outputs\apk\debug\app-debug.apk
    set APK_TYPE=debug
)

if not exist "!APK_SOURCE!" (
    echo ERROR: APK not found at: !APK_SOURCE!
    echo.
    echo Make sure the Android build completed successfully.
    pause
    exit /b 1
)

set APK_DEST=releases\lanflix-android-v!VERSION!.apk
copy "!APK_SOURCE!" "!APK_DEST!"
echo ✓ APK copied to: !APK_DEST! ^(!APK_TYPE! build^)

REM Copy server executable
set SERVER_SOURCE=releases\server-win-x64\Lanflix.WebApi.exe
if exist "!SERVER_SOURCE!" (
    set SERVER_DEST=releases\lanflix-server-v!VERSION!.exe
    copy "!SERVER_SOURCE!" "!SERVER_DEST!"
    echo ✓ Server copied to: !SERVER_DEST!
) else (
    echo WARNING: Server executable not found at: !SERVER_SOURCE!
)
echo.

REM Step 6: Git commit and tag
echo [6/8] Committing to Git...
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

REM Step 7: Create GitHub Release
echo [7/8] Creating GitHub Release...

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
    echo 4. Upload APK: !APK_DEST!
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
    echo ## 🎬 Lanflix v!VERSION! - Complete Media Streaming Solution > release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📦 What's Included >> release-notes.tmp
    echo - **🖥️ Server**: Complete media server with web interface >> release-notes.tmp
    echo - **📱 Android App**: Native app with Netflix-style UI >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### ✨ New Features >> release-notes.tmp
    echo - Pixel-perfect Netflix-style Android UI >> release-notes.tmp
    echo - Auto-server discovery on port 5037 >> release-notes.tmp
    echo - Native ExoPlayer video playback >> release-notes.tmp
    echo - Hardware-accelerated rendering >> release-notes.tmp
    echo - Android TV support with remote control >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 🚀 Performance >> release-notes.tmp
    echo - 60fps native Android UI vs WebView >> release-notes.tmp
    echo - Instant app startup >> release-notes.tmp
    echo - Optimized memory usage >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ## 📋 Installation >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 🖥️ Server Setup >> release-notes.tmp
    echo 1. Download `lanflix-server-v!VERSION!.exe` >> release-notes.tmp
    echo 2. Run the executable >> release-notes.tmp
    echo 3. Server starts on port 5037 >> release-notes.tmp
    echo 4. Access web interface at http://localhost:5037 >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### 📱 Android Setup >> release-notes.tmp
    echo 1. Download `lanflix-android-v!VERSION!.apk` >> release-notes.tmp
    echo 2. Install on your Android device >> release-notes.tmp
    echo 3. App will auto-discover your server >> release-notes.tmp
    echo 4. Enjoy Netflix-style native performance! >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Requirements:** >> release-notes.tmp
    echo - Windows 10/11 ^(for server^) >> release-notes.tmp
    echo - Android 7.0+ ^(for mobile app^) >> release-notes.tmp
    echo - Same network for auto-discovery >> release-notes.tmp

    echo Creating GitHub release...
    
    REM Prepare release assets
    set RELEASE_ASSETS="!APK_DEST!"
    if exist "!SERVER_DEST!" (
        set RELEASE_ASSETS=!RELEASE_ASSETS! "!SERVER_DEST!"
    )
    
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

REM Step 8: Final summary
echo [8/8] Release summary...
echo.
echo ========================================
echo   Release Complete!
echo ========================================
echo.
echo Version: v!VERSION!
echo.
echo 📱 Android APK: !APK_DEST! ^(!APK_TYPE! build^)
if exist "!SERVER_DEST!" (
    echo 🖥️  Server: !SERVER_DEST!
)
echo 🌐 Release: https://github.com/JarlTheGamer/Applications./releases/tag/v!VERSION!
echo.
echo 🎉 Both web and Android versions built successfully!
echo.
echo 📋 What's included:
echo   ✅ Web frontend ^(built into server^)
echo   ✅ Server executable ^(Windows x64^)
echo   ✅ Android APK ^(connects to port 5037^)
echo.
echo 🚀 Users can now:
echo   1. Run the server executable
echo   2. Install the Android APK
echo   3. Android app will auto-discover the server on port 5037
echo.
pause
