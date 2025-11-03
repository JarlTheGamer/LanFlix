# Lanflix Android Build Script
# This script builds the native Android app with exact web UI replica

param(
    [string]$Action = "build",  # build, run, clean, install
    [string]$Variant = "debug", # debug, release
    [switch]$Help
)

if ($Help) {
    Write-Host "Lanflix Android Build Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\build-android.ps1 [-Action <action>] [-Variant <variant>]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Actions:" -ForegroundColor Cyan
    Write-Host "  build    - Build the Android APK"
    Write-Host "  run      - Build and run on connected device/emulator"
    Write-Host "  clean    - Clean build artifacts"
    Write-Host "  install  - Install APK on connected device"
    Write-Host ""
    Write-Host "Variants:" -ForegroundColor Cyan
    Write-Host "  debug    - Debug build (default)"
    Write-Host "  release  - Release build (optimized)"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Magenta
    Write-Host "  .\build-android.ps1 -Action build"
    Write-Host "  .\build-android.ps1 -Action run -Variant release"
    Write-Host "  .\build-android.ps1 -Action clean"
    exit 0
}

# Check if we're in the right directory
$androidDir = "android-native"
if (-not (Test-Path $androidDir)) {
    Write-Host "❌ Android project not found. Make sure you're in the build-tools/AndroidVersions directory." -ForegroundColor Red
    exit 1
}

# Check for Android SDK
$androidHome = $env:ANDROID_HOME
if (-not $androidHome -or -not (Test-Path $androidHome)) {
    Write-Host "❌ ANDROID_HOME not set or Android SDK not found." -ForegroundColor Red
    Write-Host "Please install Android Studio and set ANDROID_HOME environment variable." -ForegroundColor Yellow
    exit 1
}

# Check for Java
try {
    $javaVersion = java -version 2>&1 | Select-String "version"
    Write-Host "✅ Java found: $javaVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Java not found. Please install Java 8 or higher." -ForegroundColor Red
    exit 1
}

Set-Location $androidDir

Write-Host "🚀 Lanflix Android - Netflix-Style Native App" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

switch ($Action.ToLower()) {
    "clean" {
        Write-Host "🧹 Cleaning build artifacts..." -ForegroundColor Yellow
        .\gradlew clean
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Clean completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Clean failed!" -ForegroundColor Red
            exit 1
        }
    }
    
    "build" {
        Write-Host "🔨 Building Android app ($Variant)..." -ForegroundColor Yellow
        
        if ($Variant -eq "release") {
            .\gradlew assembleRelease
        } else {
            .\gradlew assembleDebug
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build completed successfully!" -ForegroundColor Green
            
            # Show APK location
            $apkPath = if ($Variant -eq "release") {
                "app\build\outputs\apk\release\app-release.apk"
            } else {
                "app\build\outputs\apk\debug\app-debug.apk"
            }
            
            if (Test-Path $apkPath) {
                $fullPath = Resolve-Path $apkPath
                Write-Host "📱 APK created: $fullPath" -ForegroundColor Cyan
                
                # Show APK size
                $size = (Get-Item $apkPath).Length / 1MB
                Write-Host "📊 APK size: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan
            }
        } else {
            Write-Host "❌ Build failed!" -ForegroundColor Red
            exit 1
        }
    }
    
    "install" {
        Write-Host "📲 Installing APK on device..." -ForegroundColor Yellow
        
        # Check for connected devices
        $devices = adb devices | Select-String "device$"
        if ($devices.Count -eq 0) {
            Write-Host "❌ No Android devices connected!" -ForegroundColor Red
            Write-Host "Please connect a device or start an emulator." -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "📱 Found $($devices.Count) connected device(s)" -ForegroundColor Green
        
        if ($Variant -eq "release") {
            .\gradlew installRelease
        } else {
            .\gradlew installDebug
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ App installed successfully!" -ForegroundColor Green
            Write-Host "🎬 Launch Lanflix on your device to see the Netflix-style UI!" -ForegroundColor Cyan
        } else {
            Write-Host "❌ Installation failed!" -ForegroundColor Red
            exit 1
        }
    }
    
    "run" {
        Write-Host "🏃 Building and running app ($Variant)..." -ForegroundColor Yellow
        
        # Check for connected devices
        $devices = adb devices | Select-String "device$"
        if ($devices.Count -eq 0) {
            Write-Host "❌ No Android devices connected!" -ForegroundColor Red
            Write-Host "Please connect a device or start an emulator." -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "📱 Found $($devices.Count) connected device(s)" -ForegroundColor Green
        
        if ($Variant -eq "release") {
            .\gradlew installRelease
        } else {
            .\gradlew installDebug
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ App installed and running!" -ForegroundColor Green
            Write-Host ""
            Write-Host "🎬 Lanflix Features:" -ForegroundColor Cyan
            Write-Host "  ✨ Exact Netflix-style UI replica" -ForegroundColor White
            Write-Host "  🚀 Native 60fps performance" -ForegroundColor White
            Write-Host "  📱 Server discovery and connection" -ForegroundColor White
            Write-Host "  👤 Profile management" -ForegroundColor White
            Write-Host "  🎥 ExoPlayer video playback" -ForegroundColor White
            Write-Host "  📺 Android TV support" -ForegroundColor White
            Write-Host ""
            
            # Try to launch the app
            Write-Host "🚀 Launching Lanflix..." -ForegroundColor Yellow
            adb shell am start -n com.lanflix.android/.MainActivity
        } else {
            Write-Host "❌ Run failed!" -ForegroundColor Red
            exit 1
        }
    }
    
    default {
        Write-Host "❌ Unknown action: $Action" -ForegroundColor Red
        Write-Host "Use -Help to see available actions." -ForegroundColor Yellow
        exit 1
    }
}

Set-Location ..

Write-Host ""
Write-Host "🎉 Done! Your Netflix-style Android app is ready!" -ForegroundColor Green