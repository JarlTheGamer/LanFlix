@echo off
REM ============================================
REM Build Both Lanflix Android APKs
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Building Both Lanflix Android APKs
echo ========================================
echo.

REM Set JAVA_HOME to Java 21 for Gradle compatibility
if exist "C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot" (
    set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.9.10-hotspot"
) else if exist "C:\Program Files\Java\jdk-21" (
    set "JAVA_HOME=C:\Program Files\Java\jdk-21"
) else (
    echo WARNING: Java 21 not found at expected location
    echo Using system Java - build may fail if Java version is too new
)
set "PATH=%JAVA_HOME%\bin;%PATH%"
echo Using Java: %JAVA_HOME%
echo.

REM Step 1: Build WebUI APK
echo [1/2] Building WebUI APK (WebView-based)...
cd WebUI
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
    if errorlevel 1 (
        echo WARNING: WebUI Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: WebUI gradlew.bat not found
    cd ..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: WebUI build failed
    cd ..
    pause
    exit /b 1
)
echo ✅ WebUI APK built successfully
cd ..
echo.

REM Step 2: Build Native APK
echo [2/2] Building Native APK (Kotlin UI replica)...
cd android-native
if exist "gradlew.bat" (
    call gradlew.bat assembleRelease
    if errorlevel 1 (
        echo WARNING: Native Release build failed, trying debug build...
        call gradlew.bat assembleDebug
    )
) else (
    echo ERROR: Native gradlew.bat not found
    cd ..
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Native build failed
    cd ..
    pause
    exit /b 1
)
echo ✅ Native APK built successfully
cd ..
echo.

REM Show results
echo ========================================
echo   Build Complete!
echo ========================================
echo.

REM Find WebUI APK
set WEBUI_APK=WebUI\app\build\outputs\apk\release\app-release.apk
set WEBUI_TYPE=release
if not exist "!WEBUI_APK!" (
    set WEBUI_APK=WebUI\app\build\outputs\apk\debug\app-debug.apk
    set WEBUI_TYPE=debug
)

REM Find Native APK
set NATIVE_APK=android-native\app\build\outputs\apk\release\app-release.apk
set NATIVE_TYPE=release
if not exist "!NATIVE_APK!" (
    set NATIVE_APK=android-native\app\build\outputs\apk\debug\app-debug.apk
    set NATIVE_TYPE=debug
)

echo 📱 WebUI APK: !WEBUI_APK! ^(!WEBUI_TYPE! build^)
echo 🚀 Native APK: !NATIVE_APK! ^(!NATIVE_TYPE! build^)
echo.
echo 🎯 Both APKs connect to port 5037 automatically
echo.
echo 📋 What's the difference?
echo   WebUI APK   - Uses your web frontend in WebView
echo   Native APK  - Pixel-perfect Kotlin UI with 60fps performance
echo.
echo 🚀 To install on device:
echo   adb install "!WEBUI_APK!"
echo   adb install "!NATIVE_APK!"
echo.

endlocal
pause