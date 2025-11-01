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

REM Check if we're in the right directory
if not exist "server\backend\package.json" (
    echo ERROR: Please run this script from the project root directory
    pause
    exit /b 1
)

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
node scripts\bump-version.js !VERSION_INPUT!
if errorlevel 1 (
    echo ERROR: Failed to bump version
    pause
    exit /b 1
)

REM Get the actual version from package.json
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"\"version\"" server\frontend\package.json') do set VERSION=%%a
set VERSION=%VERSION:"=%
echo ✓ Version bumped to %VERSION%
echo.

REM Step 2: Build server (frontend + backend)
echo [2/7] Building server...
call npm run build:server
if errorlevel 1 (
    echo ERROR: Failed to build server
    pause
    exit /b 1
)
echo ✓ Server built
echo.

REM Step 3: Build Android APK
echo [3/7] Building Android APK...

REM Set JAVA_HOME to Java 21 for Gradle compatibility
set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot"
set "PATH=%JAVA_HOME%\bin;%PATH%"
echo Using Java 21 for Gradle build...

cd build-tools\android
if exist "gradlew.bat" (
    call gradlew.bat assembleDebug
) else (
    echo ERROR: gradlew.bat not found
    cd ..\..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Gradle build failed
    cd ..\..
    pause
    exit /b 1
)
echo ✓ APK built successfully
cd ..\..
echo.

REM Step 4: Copy APK to releases folder
echo [4/7] Preparing release files...
if not exist "releases" mkdir releases

REM Find the debug APK (auto-signed)
set APK_SOURCE=build-tools\android\app\build\outputs\apk\debug\app-debug.apk
if not exist "!APK_SOURCE!" (
    echo ERROR: APK not found at: !APK_SOURCE!
    echo.
    echo Make sure the Android build completed successfully.
    pause
    exit /b 1
)

set APK_DEST=releases\lanflix-android-v!VERSION!.apk
copy "!APK_SOURCE!" "!APK_DEST!"
echo ✓ APK copied to: !APK_DEST!
echo.

REM Step 5: Git commit and tag
echo [5/7] Committing to Git...
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
echo [6/7] Creating GitHub Release...

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
    echo ## What's New in v!VERSION! > release-notes.tmp
    echo. >> release-notes.tmp
    echo ### New Features >> release-notes.tmp
    echo - Automated release system >> release-notes.tmp
    echo - In-app update notifications >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### Bug Fixes >> release-notes.tmp
    echo - Various bug fixes and improvements >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ### Performance >> release-notes.tmp
    echo - Improved app performance >> release-notes.tmp
    echo. >> release-notes.tmp
    echo ## Installation >> release-notes.tmp
    echo Download the APK and install on your Android device. >> release-notes.tmp
    echo. >> release-notes.tmp
    echo **Requirements:** >> release-notes.tmp
    echo - Android 7.0 or higher >> release-notes.tmp
    echo - Backend server running on your network >> release-notes.tmp

    echo Creating GitHub release...
    gh release create v!VERSION! "!APK_DEST!" ^
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

echo.
echo ========================================
echo   Release Complete!
echo ========================================
echo.
echo Version: v!VERSION!
echo APK: !APK_DEST!
echo Release: https://github.com/JarlTheGamer/Applications./releases/tag/v!VERSION!
echo.
echo Users can now update via the in-app updater!
echo.
pause
