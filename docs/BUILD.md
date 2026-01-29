# Lanflix Build Guide

This guide describes how to build the Lanflix media server from source.

## Architecture Overview

Lanflix is built using a modern decoupled architecture:
- **Backend**: C# 12 / .NET 9 Web API
- **Frontend**: Vanilla JS (ESNext) with Vite, built to static files
- **Database**: SQLite (managed with Entity Framework Core)
- **Containerization**: Multi-stage Docker build with FFmpeg support

## Prerequisites

- **.NET SDK 9.0+**
- **Node.js 18+** & **npm**
- **FFmpeg** (Required for server runtime)

## Building the Server

We provide a robust PowerShell script to handle the entire build process.

```powershell
# From the project root
.\lanflix-server\build.ps1 -Clean
```

This script will:
1. Verify all prerequisites.
2. Build the **Frontend** (`lanflix-server/app/WebApi/ClientApp`) into static assets.
3. Publish the **Backend** (`lanflix-server/app/WebApi`).
4. Package everything into a self-contained `publish/` directory.

## Build Artifacts

After a successful build, the `publish/` directory will contain:
- `Lanflix.WebApi.exe` - The main server executable.
- `wwwroot/` - The compiled web interface.
- `config/` - Default configuration files.
- All required .NET runtime dependencies.

## Manual Build Steps

If you cannot use the PowerShell script, you can build manually:

### 1. Build Frontend
```bash
cd lanflix-server/app/WebApi/ClientApp
npm install
npm run build
```

### 2. Publish Backend
```bash
cd lanflix-server/app/WebApi
dotnet publish -c Release -o ../../publish
```

## Docker Build

You can also build using Docker, which packages FFmpeg and all dependencies automatically.

```bash
cd lanflix-server/app
docker compose build
docker compose up -d
```

---
*Note: Legacy Node.js build tools located in `build-tools/` are deprecated and should not be used.*
