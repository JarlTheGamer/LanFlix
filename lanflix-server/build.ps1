# Lanflix Server Build Script
# Usage: .\build.ps1 [options]

param (
    [switch]$SkipFrontend = $false,
    [switch]$SkipBackend = $false,
    [switch]$Clean = $false,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Setup Paths
$ScriptRoot = $PSScriptRoot
$FrontendPath = Join-Path $ScriptRoot "app\WebApi\ClientApp"
$BackendPath = Join-Path $ScriptRoot "app\WebApi"
$PublishPath = Join-Path $ScriptRoot "publish"

# Helper Functions
function Write-Step {
    param([string]$Message)
    Write-Host "`n>>> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

try {
    # 1. Pre-flight Checks
    Write-Step "Checking prerequisites..."
    
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "dotnet SDK is not installed or not in PATH."
    }
    
    if (-not (Get-Command "npm" -ErrorAction SilentlyContinue)) {
        throw "npm is not installed or not in PATH."
    }

    Write-Success "Prerequisites met."

    # 2. Cleaning
    if ($Clean) {
        Write-Step "Cleaning previous builds..."
        
        if (Test-Path $PublishPath) {
            Remove-Item $PublishPath -Recurse -Force
            Write-Host "Cleaned publish directory."
        }
        
        # Clean bin/obj folders
        Get-ChildItem -Path $ScriptRoot -Include bin,obj -Recurse | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "Clean completed."
    }

    # 3. Build Frontend
    if (-not $SkipFrontend) {
        Write-Step "Building Frontend..."
        
        if (-not (Test-Path $FrontendPath)) {
            throw "Frontend directory not found at $FrontendPath"
        }

        Push-Location $FrontendPath
        try {
            if (-not (Test-Path "node_modules")) {
                Write-Host "Installing dependencies..."
                npm install --no-audit --no-fund
            }
            
            Write-Host "Compiling assets..."
            npm run build
            
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend build failed."
            }
        }
        finally {
            Pop-Location
        }
        Write-Success "Frontend built successfully."
    }

    # 4. Build Backend
    if (-not $SkipBackend) {
        Write-Step "Building Backend..."
        
        if (-not (Test-Path $BackendPath)) {
            throw "Backend directory not found at $BackendPath"
        }

        # Ensure publish directory exists
        if (-not (Test-Path $PublishPath)) {
            New-Item -ItemType Directory -Path $PublishPath | Out-Null
        }

        # Run dotnet publish
        Write-Host "Publishing backend..."
        dotnet publish $BackendPath -c $Configuration -o $PublishPath /p:DebugType=None /p:DebugSymbols=false /nologo /v:m

        if ($LASTEXITCODE -ne 0) {
            throw "Backend build failed."
        }
        
        Write-Success "Backend published to: $PublishPath"
    }

    Write-Step "Build Summary"
    Write-Success "Build completed successfully!"
    Write-Host "Executable: $(Join-Path $PublishPath 'Lanflix.WebApi.exe')"

}
catch {
    Write-ErrorMsg "Build Failed: $($_.Exception.Message)"
    exit 1
}
