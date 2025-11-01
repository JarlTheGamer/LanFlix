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

REM Change to project root
cd /d "%~dp0..\.."

REM Check if we're in the right directory
if not exist "server\backend\package.json" (
    echo ERROR: Cannot find server\backend\package.json
    echo Current directory: %CD%
    pause
    exit /b 1
)

REM Step 1: Build the server
echo [1/4] Building server...
call npm run build:server
if errorlevel 1 (
    echo ERROR: Server build failed
    pause
    exit /b 1
)
echo ✓ Server built
echo.

REM Step 2: Package server as executable
echo [2/4] Packaging server as executable...
cd server\backend

REM Check if pkg is installed
where pkg >nul 2>nul
if errorlevel 1 (
    echo Installing pkg...
    call npm install -g pkg
    if errorlevel 1 (
        echo ERROR: Failed to install pkg
        cd ..\..
        pause
        exit /b 1
    )
)

REM Build executable
echo Building lanflix-server.exe...
call pkg . --targets node18-win-x64 --output ..\..\build-tools\server\build\lanflix-server.exe
if errorlevel 1 (
    echo ERROR: Failed to build executable
    cd ..\..
    pause
    exit /b 1
)

cd ..\..
echo ✓ Executable created
echo.

REM Step 2.5: Create distribution folder
echo [2.5/4] Creating distribution package...
set DIST_DIR=build-tools\server\build\lanflix-server
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM Copy executable
echo Copying executable...
copy /Y "build-tools\server\build\lanflix-server.exe" "%DIST_DIR%\"

REM Copy public files (frontend assets)
if exist "server\backend\public" (
    echo Copying public files...
    xcopy /E /I /Y "server\backend\public" "%DIST_DIR%\public\"
)

REM Copy config template
if exist "server\backend\.env.example" (
    copy /Y "server\backend\.env.example" "%DIST_DIR%\.env"
)

REM Copy runtime scripts
echo Copying runtime scripts...
if exist "build-tools\server\runtime\start-server.bat" copy /Y "build-tools\server\runtime\start-server.bat" "%DIST_DIR%\"
if exist "build-tools\server\runtime\install-service.bat" copy /Y "build-tools\server\runtime\install-service.bat" "%DIST_DIR%\"
if exist "build-tools\server\runtime\README.txt" copy /Y "build-tools\server\runtime\README.txt" "%DIST_DIR%\"

echo ✓ Distribution package created
echo.

REM Step 3: Create portable ZIP
echo [3/4] Creating portable ZIP...
set ZIP_FILE=build-tools\server\build\lanflix-server-portable.zip
if exist "%ZIP_FILE%" del "%ZIP_FILE%"

REM Use PowerShell to create ZIP
powershell -Command "Compress-Archive -Path '%DIST_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"

if errorlevel 1 (
    echo WARNING: Failed to create ZIP file
) else (
    echo ✓ Portable ZIP created: %ZIP_FILE%
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
    echo For now, use the portable ZIP: %ZIP_FILE%
) else (
    echo Building installer with NSIS...
    set INSTALLER_FILE=build-tools\server\build\lanflix-installer.exe
    makensis /DVERSION=1.0.0 build-tools\server\installer.nsi
    if errorlevel 1 (
        echo WARNING: Installer build failed
    ) else (
        echo ✓ Installer created: !INSTALLER_FILE!
    )
)
echo.

echo ========================================
echo   Build Complete!
echo ========================================
echo.
echo Output files:
echo   - Standalone EXE: build-tools\server\build\lanflix-server.exe
echo   - Portable ZIP: %ZIP_FILE%
if exist "build-tools\server\build\lanflix-installer.exe" (
    echo   - Installer: build-tools\server\build\lanflix-installer.exe
)
echo.
echo Distribution folder: %DIST_DIR%
echo.
echo The lanflix-server.exe is a standalone executable that includes
echo Node.js runtime and all dependencies. No Node.js installation required!
echo.
pause
