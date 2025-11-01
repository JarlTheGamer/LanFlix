# Lanflix Full-Stack Build Script
# Builds frontend and backend into a single executable

Write-Host "🎬 Lanflix Full-Stack Build" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build Frontend
Write-Host "📦 Building frontend..." -ForegroundColor Yellow
Push-Location app/WebApi/ClientApp
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
dotnet build app/Lanflix.Server.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Backend built successfully!" -ForegroundColor Green
Write-Host ""

# Step 3: Publish as single executable
Write-Host "📦 Publishing as single executable..." -ForegroundColor Yellow
$runtime = "win-x64"  # Change to linux-x64 or osx-x64 as needed
dotnet publish app/WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    /p:TrimMode=partial `
    -o "./publish/$runtime"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Output location: ./publish/$runtime/" -ForegroundColor Cyan
Write-Host "🚀 Run: ./publish/$runtime/Lanflix.WebApi.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 The executable includes:" -ForegroundColor Yellow
Write-Host "   - Backend API (.NET 9)" -ForegroundColor White
Write-Host "   - Frontend UI (embedded in wwwroot)" -ForegroundColor White
Write-Host "   - All dependencies" -ForegroundColor White
Write-Host ""
