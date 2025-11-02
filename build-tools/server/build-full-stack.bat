@echo off
setlocal enabledelayedexpansion

echo.
echo ================================
echo    Lanflix Full-Stack Build
echo ================================
echo.

REM Step 1: Build Frontend
echo [1/3] Building frontend...
pushd lanflix-server\app\WebApi\ClientApp
call npm run build
if errorlevel 1 (
    echo.
    echo ERROR: Frontend build failed!
    popd
    exit /b 1
)
popd
echo SUCCESS: Frontend built successfully!
echo.

REM Step 2: Build Backend
echo [2/3] Building backend...
dotnet build lanflix-server\app\Lanflix.Server.sln -c Release
if errorlevel 1 (
    echo.
    echo ERROR: Backend build failed!
    exit /b 1
)
echo SUCCESS: Backend built successfully!
echo.

REM Step 3: Publish as single executable
echo [3/3] Publishing as single executable...
set RUNTIME=win-x64
set OUTPUT_PATH=build-tools\server\build\%RUNTIME%

dotnet publish lanflix-server\app\WebApi\Lanflix.WebApi.csproj -c Release -r %RUNTIME% --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true -o "%OUTPUT_PATH%"

if errorlevel 1 (
    echo.
    echo ERROR: Publish failed!
    exit /b 1
)

echo.
echo ================================
echo    BUILD COMPLETE!
echo ================================
echo.
echo Output location: %OUTPUT_PATH%\
echo Run: %OUTPUT_PATH%\Lanflix.WebApi.exe
echo.
echo The executable includes:
echo    - Backend API (.NET 9)
echo    - Frontend UI (embedded in wwwroot)
echo    - All dependencies
echo.

endlocal