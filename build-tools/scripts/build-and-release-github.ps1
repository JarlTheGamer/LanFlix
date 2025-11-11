#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory=$true)]
    [string]$VersionName,
    
    [Parameter(Mandatory=$true)]
    [int]$VersionCode,
    
    [string]$ReleaseNotes = "",
    
    [switch]$Mandatory = $false,
    
    [switch]$Prerelease = $false,
    
    [string]$BuildType = "release",
    
    [switch]$NoBuild = $false
)

# Script to build Android APK and create GitHub release
$ErrorActionPreference = "Stop"

Write-Host "Building and Releasing Lanflix v$VersionName to GitHub" -ForegroundColor Green
Write-Host "Version: $VersionName ($VersionCode)" -ForegroundColor Yellow

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$AndroidDir = Join-Path $RootDir "build-tools\AndroidVersions\native-app"

try {
    if ($NoBuild) {
        # Skip build, just find existing APK
        Write-Host "`nSkipping build (--NoBuild flag set)..." -ForegroundColor Yellow
        Write-Host "Looking for existing APK..." -ForegroundColor Blue
        
        # Find the built APK (try signed first, then unsigned as fallback)
        $SignedApkPath = Join-Path $AndroidDir "app\build\outputs\apk\$BuildType\app-$BuildType.apk"
        $UnsignedApkPath = Join-Path $AndroidDir "app\build\outputs\apk\$BuildType\app-$BuildType-unsigned.apk"
        
        if (Test-Path $SignedApkPath) {
            $ApkPath = $SignedApkPath
            Write-Host "Found signed APK: $ApkPath" -ForegroundColor Green
        } elseif (Test-Path $UnsignedApkPath) {
            $ApkPath = $UnsignedApkPath
            Write-Host "Found unsigned APK: $ApkPath" -ForegroundColor Yellow
        } else {
            throw "No existing APK found at $SignedApkPath or $UnsignedApkPath. Build first without --NoBuild flag."
        }
        
        # Calculate file size and checksum
        $ApkFile = Get-Item $ApkPath
        $FileSize = $ApkFile.Length
        $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
        
        Write-Host "Calculating checksum..." -ForegroundColor Blue
        $Checksum = (Get-FileHash $ApkPath -Algorithm SHA256).Hash.ToLower()
        
        Write-Host "Using existing APK!" -ForegroundColor Green
        Write-Host "File: $ApkPath" -ForegroundColor Yellow
        Write-Host "Size: $FileSizeMB MB" -ForegroundColor Yellow
        Write-Host "Checksum: $Checksum" -ForegroundColor Yellow
    } else {
        # Step 1: Build the APK
        Write-Host "`nStep 1: Building APK..." -ForegroundColor Blue
        
        # Update version in build.gradle.kts
        Write-Host "Updating version information..." -ForegroundColor Blue
        
        $BuildGradlePath = Join-Path $AndroidDir "app\build.gradle.kts"
        $BuildGradleContent = Get-Content $BuildGradlePath -Raw
        
        # Update version code and name
        $BuildGradleContent = $BuildGradleContent -replace 'versionCode = \d+', "versionCode = $VersionCode"
        $BuildGradleContent = $BuildGradleContent -replace 'versionName = "[^"]*"', "versionName = `"$VersionName`""
        
        Set-Content -Path $BuildGradlePath -Value $BuildGradleContent -NoNewline
        
        Write-Host "Updated version to $VersionName ($VersionCode)" -ForegroundColor Green
        
        # Build the APK
        Write-Host "Building APK..." -ForegroundColor Blue
        
        Push-Location $AndroidDir
        
        # Clean and build
        & .\gradlew.bat clean
        if ($LASTEXITCODE -ne 0) {
            throw "Gradle clean failed"
        }
        
        & .\gradlew.bat "assemble$BuildType"
        if ($LASTEXITCODE -ne 0) {
            throw "Gradle build failed"
        }
        
        Pop-Location
        
        # Find the built APK (try signed first, then unsigned as fallback)
        $SignedApkPath = Join-Path $AndroidDir "app\build\outputs\apk\$BuildType\app-$BuildType.apk"
        $UnsignedApkPath = Join-Path $AndroidDir "app\build\outputs\apk\$BuildType\app-$BuildType-unsigned.apk"
        
        if (Test-Path $SignedApkPath) {
            $ApkPath = $SignedApkPath
            Write-Host "Using signed APK: $ApkPath" -ForegroundColor Green
        } elseif (Test-Path $UnsignedApkPath) {
            $ApkPath = $UnsignedApkPath
            Write-Host "WARNING: Using unsigned APK: $ApkPath" -ForegroundColor Yellow
            Write-Host "This APK may not install on devices. Consider setting up proper signing." -ForegroundColor Yellow
        } else {
            throw "No APK found at $SignedApkPath or $UnsignedApkPath"
        }
        
        if (!(Test-Path $ApkPath)) {
            throw "APK not found at $ApkPath"
        }
        
        # Calculate file size and checksum
        $ApkFile = Get-Item $ApkPath
        $FileSize = $ApkFile.Length
        $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
        
        Write-Host "Calculating checksum..." -ForegroundColor Blue
        $Checksum = (Get-FileHash $ApkPath -Algorithm SHA256).Hash.ToLower()
        
        Write-Host "APK built successfully!" -ForegroundColor Green
        Write-Host "File: $ApkPath" -ForegroundColor Yellow
        Write-Host "Size: $FileSizeMB MB" -ForegroundColor Yellow
        Write-Host "Checksum: $Checksum" -ForegroundColor Yellow
    }

    # Step 2: Create GitHub release using GitHub CLI
    Write-Host "`nStep 2: Creating GitHub Release..." -ForegroundColor Blue
    
    # Check if GitHub CLI is installed
    $ghExists = Get-Command "gh" -ErrorAction SilentlyContinue
    if (-not $ghExists) {
        Write-Host "ERROR: GitHub CLI (gh) is not installed" -ForegroundColor Red
        Write-Host "Install it with: winget install GitHub.cli" -ForegroundColor Yellow
        throw "GitHub CLI required for release"
    }
    
    # Check if authenticated (suppress output to avoid prompts)
    $null = & gh auth status 2>$null
    $isAuthenticated = $LASTEXITCODE -eq 0
    
    if (-not $isAuthenticated) {
        Write-Host "GitHub CLI not authenticated. Please run 'gh auth login' first." -ForegroundColor Red
        throw "GitHub authentication required"
    }
    
    Write-Host "GitHub CLI authenticated successfully" -ForegroundColor Green
    
    $ReleaseApkName = "lanflix-native-webview-v$VersionName.apk"
    $ReleaseApkPath = $ApkPath
    
    # Copy APK to final name
    $FinalApkPath = Join-Path (Split-Path $ApkPath) $ReleaseApkName
    Copy-Item $ApkPath $FinalApkPath -Force
    
    # Create release notes
    $ReleaseNotesContent = @"
## Lanflix Native WebView v$VersionName - OTA Update System

### What's New
$ReleaseNotes

### Features
- **Native WebView APK**: Optimized hybrid experience
- **OTA Updates**: Automatic update system integrated
- **Hardware Acceleration**: Smooth performance on all devices
- **Android TV Support**: Remote control navigation
- **Auto-orientation**: Works on phones and tablets

### Installation
1. Download ``lanflix-native-webview-v$VersionName.apk``
2. Install on your Android device
3. The app will automatically check for future updates

### Requirements
- Android 5.0+ (API 21+)
- Lanflix server running
- Network access to server

### OTA Update System
This version includes the new Over-The-Air update system that allows seamless app updates directly from the settings page.
"@
    
    # Create GitHub release with APK
    Write-Host "Creating GitHub release with APK..." -ForegroundColor Blue
    
    $releaseArgs = @(
        "release", "create", "v$VersionName",
        $FinalApkPath,
        "--title", "Lanflix Native WebView v$VersionName",
        "--notes", $ReleaseNotesContent,
        "--repo", "JarlTheGamer/Applications."
    )
    
    if ($Prerelease) {
        $releaseArgs += "--prerelease"
    }
    
    & gh @releaseArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create GitHub release"
    }
    
    Write-Host "GitHub Release Complete!" -ForegroundColor Green
    Write-Host "Release URL: https://github.com/JarlTheGamer/Applications./releases/tag/v$VersionName" -ForegroundColor Cyan
    
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Update AppUpdateController.cs with version $VersionName (versionCode: $VersionCode)" -ForegroundColor White
    Write-Host "2. Test the OTA update functionality" -ForegroundColor White
    Write-Host "3. Announce the update to users" -ForegroundColor White
    
} catch {
    Write-Error "Build and release failed: $_"
    exit 1
}

Write-Host "`nBuild and release completed successfully!" -ForegroundColor Green