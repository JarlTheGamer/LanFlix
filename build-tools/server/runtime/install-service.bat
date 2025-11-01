@echo off
REM ============================================
REM Lanflix Windows Service Installer
REM Installs Lanflix as a Windows service using NSSM
REM ============================================

echo.
echo ========================================
echo   Lanflix Service Installer
echo ========================================
echo.

REM Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script requires administrator privileges.
    echo.
    echo Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

REM Check if NSSM is available
if not exist "nssm.exe" (
    echo NSSM not found. Downloading...
    echo.
    echo Please download NSSM from: https://nssm.cc/download
    echo Extract nssm.exe to this folder and run this script again.
    echo.
    pause
    exit /b 1
)

REM Get current directory
set "INSTALL_DIR=%~dp0"
set "NODE_PATH=%INSTALL_DIR%node_modules"
set "APP_PATH=%INSTALL_DIR%dist\app.js"

echo Installing Lanflix as Windows service...
echo.
echo Installation directory: %INSTALL_DIR%
echo.

REM Install service
nssm install Lanflix "node" "%APP_PATH%"
nssm set Lanflix AppDirectory "%INSTALL_DIR%"
nssm set Lanflix DisplayName "Lanflix Media Server"
nssm set Lanflix Description "Self-hosted streaming media server"
nssm set Lanflix Start SERVICE_AUTO_START
nssm set Lanflix AppStdout "%INSTALL_DIR%logs\service-output.log"
nssm set Lanflix AppStderr "%INSTALL_DIR%logs\service-error.log"

if errorlevel 1 (
    echo.
    echo ERROR: Service installation failed
    pause
    exit /b 1
)

echo.
echo ✓ Service installed successfully!
echo.
echo To start the service:
echo   nssm start Lanflix
echo.
echo To stop the service:
echo   nssm stop Lanflix
echo.
echo To uninstall the service:
echo   nssm remove Lanflix confirm
echo.
echo Or use Windows Services (services.msc) to manage the service.
echo.
pause
