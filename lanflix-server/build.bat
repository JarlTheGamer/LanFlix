@echo off
setlocal

echo 🚀 Starting Build Process...

:: 1. Build Frontend
echo.
echo 📦 Building User Interface...
cd /d "%~dp0app\WebApi\ClientApp"

if not exist node_modules (
    echo Installing npm dependencies...
    call npm install
)

call npm run build
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Frontend build failed!
    exit /b %ERRORLEVEL%
)

:: 2. Build Backend
echo.
echo ⚙️  Building Single-File Executable...
cd /d "%~dp0app\WebApi"

dotnet publish -c Release -o "%~dp0publish" /p:DebugType=None /p:DebugSymbols=false
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Backend build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo ✅ Build Complete!
echo The executable is located in 'publish' folder.
echo Remember to keep your config and data folders when updating!

pause
