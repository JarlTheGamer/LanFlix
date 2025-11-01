@echo off
REM ============================================
REM Simple Android APK Builder
REM Downloads Gradle if needed and builds APK
REM ============================================

echo.
echo ========================================
echo   Building Lanflix Android APK
echo ========================================
echo.

REM Check Java
where java >nul 2>nul
if errorlevel 1 (
    echo ERROR: Java is not installed
    echo.
    echo Please install Java JDK 17 or higher:
    echo   https://adoptium.net/
    echo.
    pause
    exit /b 1
)

echo Java found: 
java -version
echo.

REM Check if Gradle wrapper exists
if not exist "gradle\wrapper\gradle-wrapper.jar" (
    echo Gradle wrapper not found. Downloading...
    echo.
    
    REM Download gradle wrapper jar
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/gradle/gradle/v8.2.0/gradle/wrapper/gradle-wrapper.jar' -OutFile 'gradle\wrapper\gradle-wrapper.jar'}"
    
    if not exist "gradle\wrapper\gradle-wrapper.jar" (
        echo ERROR: Failed to download Gradle wrapper
        echo.
        echo Please download manually:
        echo 1. Go to: https://gradle.org/releases/
        echo 2. Download Gradle 8.2
        echo 3. Extract gradle-wrapper.jar to: gradle\wrapper\
        pause
        exit /b 1
    )
    
    echo ✓ Gradle wrapper downloaded
    echo.
)

REM Build APK
echo Building APK...
echo.
call gradlew.bat assembleDebug

if errorlevel 1 (
    echo.
    echo ========================================
    echo   Build Failed!
    echo ========================================
    echo.
    echo Troubleshooting:
    echo 1. Check Java version (need 17+)
    echo 2. Check internet connection (Gradle downloads dependencies)
    echo 3. Try: gradlew clean assembleDebug
    echo.
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
