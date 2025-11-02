# Lanflix Full-Stack Build Script
# Builds frontend and backend into a single executable

Write-Host "🎬 Lanflix Full-Stack Build" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build Frontend
Write-Host "📦 Building frontend..." -ForegroundColor Yellow
Push-Location lanflix-server/app/WebApi/ClientApp
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "✅ Frontend built successfully!" -ForegroundColor Green
Write-Host ""

# Step 2: Build Backend
Write-Host "🔨 Building backend..." -ForegroundColor Yellow
dotnet build lanflix-server/app/Lanflix.Server.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Backend built successfully!" -ForegroundColor Green
Write-Host ""

# Step 3: Publish as single executable
Write-Host "📦 Publishing as single executable..." -ForegroundColor Yellow
$runtime = "win-x64"
$outputPath = "build-tools/server/build/$runtime"

dotnet publish lanflix-server/app/WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    /p:TrimMode=partial `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:IncludeAllContentForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    -o $outputPath `
    2>&1 | Select-Object -Last 50

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Output location: $outputPath/" -ForegroundColor Cyan
Write-Host "🚀 Run: $outputPath/Lanflix.WebApi.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 The executable includes:" -ForegroundColor Yellow
Write-Host "   - Backend API (.NET 9)" -ForegroundColor White
Write-Host "   - Frontend UI (embedded in wwwroot)" -ForegroundColor White
Write-Host "   - All dependencies" -ForegroundColor White
Write-Host ""