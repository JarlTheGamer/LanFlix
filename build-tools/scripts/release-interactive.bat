@echo off
REM ============================================
REM Lanflix Interactive Release Script
REM Builds APK and publishes to GitHub with custom release notes
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Lanflix Interactive Release
echo ========================================
echo.

REM Check prerequisites
if not exist "frontend\package.json" (
    echo ERROR: Please run this script from the project root directory
    pause
    exit /b 1
)

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: Git is not installed
    pause
    exit /b 1
)

where gh >nul 2>nul
if errorlevel 1 (
    echo ERROR: GitHub CLI is not installed
    echo Install with: winget install GitHub.cli
    pause
    exit /b 1
)

REM Get version
echo Enter version number or bump type:
echo   - Specific: 1.0.1
echo   - patch, minor, or major
echo.
set /p VERSION_INPUT="Version: "

if "%VERSION_INPUT%"=="" (
    echo ERROR: Version is required
    pause
    exit /b 1
)

REM Bump version
echo.
echo Bumping version...
node scripts\bump-version.js %VERSION_INPUT%
if errorlevel 1 (
    echo ERROR: Failed to bump version
    pause
    exit /b 1
)

REM Get actual version
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"\"version\"" frontend\package.json') do set VERSION=%%a
set VERSION=%VERSION:"=%

echo.
echo ========================================
echo   Release Notes for v%VERSION%
echo ========================================
echo.
echo Enter release notes (press Ctrl+Z then Enter when done):
echo.

REM Create release notes file
set NOTES_FILE=release-notes-%VERSION%.tmp
if exist "%NOTES_FILE%" del "%NOTES_FILE%"

:input_loop
set /p LINE="> "
if "%LINE%"=="" goto input_done
echo %LINE% >> "%NOTES_FILE%"
goto input_loop

:input_done

REM If no notes provided, use default
if not exist "%NOTES_FILE%" (
    echo ## What's New in v%VERSION% > "%NOTES_FILE%"
    echo. >> "%NOTES_FILE%"
    echo - Bug fixes and improvements >> "%NOTES_FILE%"
)

echo.
echo Building APK...
cd frontend
call npm run build
if errorlevel 1 (
    echo ERROR: Build failed
    cd ..
    pause
    exit /b 1
)

call npx cap sync android
cd build-tools\android\android
call gradlew.bat assembleRelease
if errorlevel 1 (
    echo ERROR: APK build failed
    cd ..\..\..\..
    pause
    exit /b 1
)
cd ..\..\..\..

REM Copy APK
if not exist "releases" mkdir releases
set APK_SOURCE=frontend\build-tools\android\android\app\build\outputs\apk\release\app-release.apk
set APK_DEST=releases\lanflix-android-v%VERSION%.apk
copy "%APK_SOURCE%" "%APK_DEST%"

echo.
echo Committing and tagging...
git add .
git commit -m "Release v%VERSION%"
git tag v%VERSION%
git push origin main
git push origin v%VERSION%

echo.
echo Creating GitHub release...
gh auth status >nul 2>nul
if errorlevel 1 (
    gh auth login
)

gh release create v%VERSION% "%APK_DEST%" ^
    --title "Lanflix v%VERSION%" ^
    --notes-file "%NOTES_FILE%" ^
    --repo JarlTheGamer/Applications.

if errorlevel 1 (
    echo ERROR: Failed to create release
    pause
    exit /b 1
)

del "%NOTES_FILE%"

echo.
echo ========================================
echo   Release Complete! 🎉
echo ========================================
echo.
echo Version: v%VERSION%
echo Release: https://github.com/JarlTheGamer/Applications./releases/tag/v%VERSION%
echo.
pause
