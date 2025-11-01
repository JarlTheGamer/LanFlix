@echo off
REM ============================================
REM Lanflix Server Installer Builder
REM Creates a standalone Windows installer
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Building Lanflix Server Installer
echo ========================================
echo.

REM Check if we're in the right directory
if not exist "..\..\server\backend\package.json" (
    echo ERROR: Please run this script from build-tools\server directory
    pause
    exit /b 1
)

REM Step 1: Build the server
echo [1/4] Building server...
cd ..\..\
call npm run build:server
if errorlevel 1 (
    echo ERROR: Server build failed
    pause
    exit /b 1
)
echo ✓ Server built
echo.

REM Step 2: Create distribution folder
echo [2/4] Creating distribution package...
set DIST_DIR=dist\lanflix-server
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM Copy backend files
echo Copying backend files...
xcopy /E /I /Y "server\backend\dist" "%DIST_DIR%\dist\"
xcopy /E /I /Y "server\backend\public" "%DIST_DIR%\public\"
xcopy /E /I /Y "server\backend\node_modules" "%DIST_DIR%\node_modules\"
copy /Y "server\backend\package.json" "%DIST_DIR%\"
copy /Y "server\backend\.env.example" "%DIST_DIR%\.env"

REM Copy runtime scripts
echo Copying runtime scripts...
copy /Y "build-tools\server\runtime\start-server.bat" "%DIST_DIR%\"
copy /Y "build-tools\server\runtime\install-service.bat" "%DIST_DIR%\"
copy /Y "build-tools\server\runtime\README.txt" "%DIST_DIR%\"

echo ✓ Distribution package created
echo.

REM Step 3: Create portable ZIP
echo [3/4] Creating portable ZIP...
if exist "dist\lanflix-server-portable.zip" del "dist\lanflix-server-portable.zip"

REM Use PowerShell to create ZIP
powershell -Command "Compress-Archive -Path '%DIST_DIR%\*' -DestinationPath 'dist\lanflix-server-portable.zip' -Force"

if errorlevel 1 (
    echo WARNING: Failed to create ZIP file
) else (
    echo ✓ Portable ZIP created: dist\lanflix-server-portable.zip
)
echo.

REM Step 4: Create installer (optional - requires NSIS)
echo [4/4] Creating installer...
where makensis >nul 2>nul
if errorlevel 1 (
    echo NSIS not found - skipping installer creation
    echo.
    echo To create an installer:
    echo   1. Install NSIS: https://nsis.sourceforge.io/
    echo   2. Run this script again
    echo.
    echo For now, use the portable ZIP: dist\lanflix-server-portable.zip
) else (
    echo Building installer with NSIS...
    makensis /DVERSION=1.0.0 build-tools\server\installer.nsi
    if errorlevel 1 (
        echo WARNING: Installer build failed
    ) else (
        echo ✓ Installer created: dist\lanflix-installer.exe
    )
)
echo.

echo ========================================
echo   Build Complete!
echo ========================================
echo.
echo Output files:
echo   - Portable ZIP: dist\lanflix-server-portable.zip
if exist "dist\lanflix-installer.exe" (
    echo   - Installer: dist\lanflix-installer.exe
)
echo.
echo Distribution folder: %DIST_DIR%
echo.
pause
