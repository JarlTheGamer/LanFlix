@echo off
setlocal enabledelayedexpansion

echo.
echo ================================
echo    Lanflix Full-Stack Build
echo ================================
echo.

REM Step 1: Build Frontend
echo [1/4] Building frontend...
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
echo [2/4] Building backend...
dotnet build lanflix-server\app\Lanflix.Server.sln -c Release
if errorlevel 1 (
    echo.
    echo ERROR: Backend build failed!
    exit /b 1
)
echo SUCCESS: Backend built successfully!
echo.

REM Step 3: Publish as single executable
echo [3/4] Publishing backend...
set RUNTIME=win-x64
set OUTPUT_PATH=build-tools\server\build\%RUNTIME%

dotnet publish lanflix-server\app\WebApi\Lanflix.WebApi.csproj -c Release -r %RUNTIME% --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:TrimMode=partial /p:IncludeNativeLibrariesForSelfExtract=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true -o "%OUTPUT_PATH%"

if errorlevel 1 (
    echo.
    echo ERROR: Publish failed!
    exit /b 1
)

REM Step 4: Create self-extracting installer using IExpress
echo [4/4] Creating self-extracting installer with IExpress...

set SED_FILE=build-tools\server\lanflix-installer.sed
set FINAL_EXE=build-tools\server\Lanflix-Installer.exe

REM Create IExpress directive file
echo [Version] > "%SED_FILE%"
echo Class=IEXPRESS >> "%SED_FILE%"
echo SEDVersion=3 >> "%SED_FILE%"
echo [Options] >> "%SED_FILE%"
echo PackagePurpose=InstallApp >> "%SED_FILE%"
echo ShowInstallProgramWindow=0 >> "%SED_FILE%"
echo HideExtractAnimation=1 >> "%SED_FILE%"
echo UseLongFileName=1 >> "%SED_FILE%"
echo InsideCompressed=0 >> "%SED_FILE%"
echo CAB_FixedSize=0 >> "%SED_FILE%"
echo CAB_ResvCodeSigning=0 >> "%SED_FILE%"
echo RebootMode=N >> "%SED_FILE%"
echo InstallPrompt=Do you want to install Lanflix Server? >> "%SED_FILE%"
echo DisplayLicense= >> "%SED_FILE%"
echo FinishMessage=Lanflix Server installed successfully! >> "%SED_FILE%"
echo TargetName=%CD%\%FINAL_EXE% >> "%SED_FILE%"
echo FriendlyName=Lanflix Server >> "%SED_FILE%"
echo AppLaunched=cmd /c start "" "Lanflix.WebApi.exe" >> "%SED_FILE%"
echo PostInstallCmd=^<None^> >> "%SED_FILE%"
echo AdminQuietInstCmd= >> "%SED_FILE%"
echo UserQuietInstCmd= >> "%SED_FILE%"
echo SourceFiles=SourceFiles >> "%SED_FILE%"
echo [SourceFiles] >> "%SED_FILE%"
echo SourceFiles0=%CD%\%OUTPUT_PATH% >> "%SED_FILE%"
echo [SourceFiles0] >> "%SED_FILE%"

REM Add all files from output directory
set FILE_COUNT=0
for /r "%OUTPUT_PATH%" %%F in (*) do (
    set /a FILE_COUNT+=1
    echo %%~nxF= >> "%SED_FILE%"
)

echo.
echo Found %FILE_COUNT% files to package
echo.

REM Run IExpress
iexpress /N /Q "%SED_FILE%"

if errorlevel 1 (
    echo ERROR: IExpress failed!
    exit /b 1
)

REM Cleanup
del "%SED_FILE%"

echo.
echo ================================
echo    BUILD COMPLETE!
echo ================================
echo.
echo Self-extracting installer: %FINAL_EXE%
echo.
echo When users run this exe:
echo   1. They choose install location
echo   2. Files are extracted
echo   3. Lanflix.WebApi.exe launches automatically
echo.

endlocal