@echo off
REM Quick build script for Lanflix Android APK

echo.
echo ========================================
echo   Building Lanflix Android APK
echo ========================================
echo.

REM Check if gradlew exists
if not exist "gradlew.bat" (
    echo ERROR: gradlew.bat not found
    echo Please run this from build-tools/android directory
    pause
    exit /b 1
)

REM Set JAVA_HOME if needed
if not defined JAVA_HOME (
    echo Setting JAVA_HOME...
    set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-17.0.9.10-hotspot"
    if not exist "%JAVA_HOME%" (
        set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot"
    )
)

echo Using Java: %JAVA_HOME%
echo.

REM Clean build
echo Cleaning previous build...
call gradlew clean
echo.

REM Build debug APK
echo Building debug APK...
call gradlew assembleDebug

if errorlevel 1 (
    echo.
    echo ========================================
    echo   Build Failed!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build Successful!
echo ========================================
echo.
echo APK location:
echo   app\build\outputs\apk\debug\app-debug.apk
echo.
echo To install on connected device:
echo   gradlew installDebug
echo.
pause
