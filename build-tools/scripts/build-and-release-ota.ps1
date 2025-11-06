#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory=$true)]
    [string]$VersionName,
    
    [Parameter(Mandatory=$true)]
    [int]$VersionCode,
    
    [string]$ReleaseNotes = "",
    
    [switch]$Mandatory = $false,
    
    [string]$BuildType = "release"
)

# Script to build Android APK with OTA update support
$ErrorActionPreference = "Stop"

Write-Host "Building Lanflix Android App with OTA Support" -ForegroundColor Green
Write-Host "Version: $VersionName ($VersionCode)" -ForegroundColor Yellow

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$AndroidDir = Join-Path $RootDir "build-tools\AndroidVersions\native-app"
$ReleasesDir = Join-Path $RootDir "releases"

# Ensure releases directory exists
if (!(Test-Path $ReleasesDir)) {
    New-Item -ItemType Directory -Path $ReleasesDir -Force
}

try {
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
    
    # Find the built APK
    $ApkPath = Join-Path $AndroidDir "app\build\outputs\apk\$BuildType\app-$BuildType.apk"
    
    if (!(Test-Path $ApkPath)) {
        throw "APK not found at $ApkPath"
    }
    
    # Calculate file size and checksum
    $ApkFile = Get-Item $ApkPath
    $FileSize = $ApkFile.Length
    $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
    
    Write-Host "Calculating checksum..." -ForegroundColor Blue
    $Checksum = (Get-FileHash $ApkPath -Algorithm SHA256).Hash.ToLower()
    
    # Copy to releases directory with version name
    $ReleaseApkName = "lanflix-native-webview-v$VersionName.apk"
    $ReleaseApkPath = Join-Path $ReleasesDir $ReleaseApkName
    
    Copy-Item $ApkPath $ReleaseApkPath -Force
    
    Write-Host "APK built successfully!" -ForegroundColor Green
    Write-Host "File: $ReleaseApkPath" -ForegroundColor Yellow
    Write-Host "Size: $FileSizeMB MB" -ForegroundColor Yellow
    Write-Host "Checksum: $Checksum" -ForegroundColor Yellow
    
    # Generate update info JSON for server
    $UpdateInfo = @{
        versionName = $VersionName
        versionCode = $VersionCode
        downloadUrl = "http://192.168.178.13:5037/api/app/download/$ReleaseApkName"
        releaseNotes = $ReleaseNotes
        mandatory = $Mandatory.IsPresent
        fileSize = $FileSize
        checksum = $Checksum
        buildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    }
    
    $UpdateInfoJson = $UpdateInfo | ConvertTo-Json -Depth 3
    $UpdateInfoPath = Join-Path $ReleasesDir "update-info-v$VersionName.json"
    
    Set-Content -Path $UpdateInfoPath -Value $UpdateInfoJson
    
    Write-Host "`nUpdate Info JSON created:" -ForegroundColor Green
    Write-Host $UpdateInfoJson -ForegroundColor Cyan
    
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Update your server's AppUpdateController with the new version info" -ForegroundColor White
    Write-Host "2. Copy the APK to your server's releases directory" -ForegroundColor White
    Write-Host "3. Test the OTA update functionality" -ForegroundColor White
    
    # Optionally open the releases directory
    if ($IsWindows) {
        Start-Process "explorer.exe" -ArgumentList $ReleasesDir
    }
    
} catch {
    Write-Error "Build failed: $_"
    exit 1
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green