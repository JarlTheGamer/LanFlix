@echo off
REM ============================================
REM Lanflix Server Startup Script
REM This file is included in the distribution
REM ============================================

title Lanflix Server
echo.
echo ========================================
echo   Lanflix Server
echo ========================================
echo.

REM Check if .env exists, if not copy from example
if not exist ".env" (
    if exist ".env.example" (
        echo Creating .env file from example...
        copy ".env.example" ".env"
    )
)

REM Display server info
echo Server starting...
echo.
echo Web UI will be available at:
echo   - Local: http://localhost:8080
echo   - Network: http://YOUR_IP:8080
echo.
echo To find your IP address, open another terminal and run: ipconfig
echo.
echo Press Ctrl+C to stop the server
echo.
echo ========================================
echo.

REM Start the server executable
lanflix-server.exe

REM If server exits, pause so user can see error
if errorlevel 1 (
    echo.
    echo ========================================
    echo   Server stopped with error
    echo ========================================
    echo.
)
pause
