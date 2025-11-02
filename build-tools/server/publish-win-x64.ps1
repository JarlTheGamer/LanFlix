# Lanflix Server - Windows x64 Single Executable Build Script
# This script builds a self-contained single executable for Windows

Write-Host "Building Lanflix Server for Windows x64..." -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "..\..\build-tools\server\build\win-x64") {
    Remove-Item -Path "..\..\build-tools\server\build\win-x64" -Recurse -Force
}

# Build and publish
Write-Host "Publishing single executable..." -ForegroundColor Yellow
dotnet publish WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    /p:TrimMode=partial `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:IncludeAllContentForSelfExtract=true `
    -o "..\..\build-tools\server\build\win-x64"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    Write-Host "Executable location: build-tools\server\build\win-x64\Lanflix.WebApi.exe" -ForegroundColor Green
    
    # Display file size
    $exePath = "..\..\build-tools\server\build\win-x64\Lanflix.WebApi.exe"
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host ("Executable size: {0:N2} MB" -f $fileSize) -ForegroundColor Cyan
    }
    
    Write-Host "`nTo run the server:" -ForegroundColor Yellow
    Write-Host "  cd build-tools\server\build\win-x64" -ForegroundColor White
    Write-Host "  .\Lanflix.WebApi.exe" -ForegroundColor White
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}
