@echo off
REM ============================================
REM Lanflix Android APK Builder
REM ============================================

setlocal

echo.
echo ========================================
echo   Building Lanflix Android APK
echo ========================================
echo.

REM Change to android directory
cd /d "%~dp0"

REM Check if gradlew exists
if not exist "gradlew.bat" (
    echo ERROR: gradlew.bat not found
    echo Please run this from build-tools/android directory
    pause
    exit /b 1
)

REM Set JAVA_HOME to Java 21
set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot"

if not exist "%JAVA_HOME%" (
    echo ERROR: Java 21 not found at: %JAVA_HOME%
    echo.
    echo Please install Java 21 from: https://adoptium.net/
    pause
    exit /b 1
)

echo Using Java: %JAVA_HOME%
java -version
echo.

REM Build debug APK
echo Building debug APK...
call gradlew.bat assembleDebug

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

endlocal
