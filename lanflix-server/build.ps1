# Builds the modular Lanflix.Host as the Windows production artifact.
# Usage: .\build.ps1 [-Configuration Release] [-Runtime win-x64] [-Clean]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidatePattern("^[a-z0-9-]+$")]
    [string]$Runtime = "win-x64",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
$hostProject = Join-Path $scriptRoot "app\Host\Lanflix.Host.csproj"
$publishDirectory = Join-Path $scriptRoot "publish"
$expectedExecutable = Join-Path $publishDirectory "Lanflix.Host.exe"

function Write-Step([string]$message) { Write-Host "`n>>> $message" -ForegroundColor Cyan }
function Write-Success([string]$message) { Write-Host "[OK] $message" -ForegroundColor Green }

try {
    Write-Step "Checking the .NET SDK and modular Host project"
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw ".NET SDK is not available in PATH." }
    if (-not (Test-Path -LiteralPath $hostProject -PathType Leaf)) { throw "Lanflix.Host project was not found: $hostProject" }

    $resolvedRoot = [System.IO.Path]::GetFullPath($scriptRoot)
    $resolvedPublish = [System.IO.Path]::GetFullPath($publishDirectory)
    if (-not $resolvedPublish.StartsWith($resolvedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Publish directory resolved outside the Lanflix server workspace."
    }

    if (Test-Path -LiteralPath $resolvedPublish) {
        Write-Step "Removing the previous publish artifact"
        Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolvedPublish -Force | Out-Null

    Write-Step "Running server tests"
    dotnet test (Join-Path $scriptRoot "app\Tests\Host.Tests\Lanflix.Host.Tests.csproj") -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw "Host tests failed." }

    Write-Step "Publishing Lanflix.Host for $Runtime"
    dotnet publish $hostProject -c $Configuration -r $Runtime --self-contained true -o $resolvedPublish `
        /p:PublishSingleFile=true /p:DebugType=None /p:DebugSymbols=false --nologo
    if ($LASTEXITCODE -ne 0) { throw "Lanflix.Host publish failed." }
    if (-not (Test-Path -LiteralPath $expectedExecutable -PathType Leaf)) {
        throw "Publish completed without the expected Lanflix.Host.exe artifact."
    }
    $publishedFiles = @(Get-ChildItem -LiteralPath $resolvedPublish -File -Recurse)
    if ($publishedFiles.Count -ne 1 -or $publishedFiles[0].FullName -ne $expectedExecutable) {
        throw "Single-file publish produced unexpected companion files: $($publishedFiles.Name -join ', ')"
    }

    $artifact = Get-Item -LiteralPath $expectedExecutable
    Write-Success "Build completed"
    Write-Host "Executable: $($artifact.FullName)"
    Write-Host "Size: $([Math]::Round($artifact.Length / 1MB, 2)) MB"
}
catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
