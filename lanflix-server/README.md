# Lanflix Server

🎬 High-performance media streaming server built with ASP.NET Core 9.0

## Quick Start

### Development

```powershell
# Run the server with hot reload (from lanflix-server folder)
dotnet watch run --project app/WebApi/Lanflix.WebApi.csproj
```

The server will start on:
- **HTTP**: http://localhost:5037
- **HTTPS**: https://localhost:7217

### Build

```powershell
# Build the solution
dotnet build app/Lanflix.Server.sln
```

### Test

```powershell
# Run all tests
dotnet test app/Lanflix.Server.sln

# Run specific test project
dotnet test app/Tests/Application.Tests
dotnet test app/Tests/WebApi.Tests
```


## Frontend Development

The frontend is integrated into the backend and served from `wwwroot/`.

### Build Frontend

```powershell
cd app/WebApi/ClientApp
npm install
npm run build
```

The build output goes to `app/WebApi/wwwroot/` and is automatically served by the backend.

### Frontend Dev Mode

```powershell
cd app/WebApi/ClientApp
npm run dev
```

This starts Vite dev server on http://localhost:5173 with proxy to backend.

## Production Deployment

### Single Executable

Build everything into one executable:

```powershell
# Use the build script
.\build-full-stack.ps1

# Or manually:
# 1. Build frontend
cd app/WebApi/ClientApp
npm run build
cd ../../..

# 2. Publish as single executable (Windows)
dotnet publish app/WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    -o ./publish/win-x64

# For Linux
dotnet publish app/WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r linux-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    -o ./publish/linux-x64
```

### Docker

```powershell
# Build Docker image
docker build -t lanflix-server .

# Run container
docker run -d `
    -p 5037:5037 `
    -v D:/Media:/media `
    -v lanflix-data:/app/data `
    --name lanflix `
    lanflix-server
```

## Configuration

Settings are stored in the database and can be changed from the admin dashboard.

Default configuration is in `app/WebApi/appsettings.json`:

```json
{
  "Lanflix": {
    "MediaPaths": {
      "Movies": "D:/Media/Movies",
      "Series": "D:/Media/Series"
    },
    "Transcoding": {
      "MaxConcurrentTranscodes": 2,
      "EnableHardwareAcceleration": true
    }
  }
}
```

## Project Structure

```
lanflix-server/
├── app/
│   ├── Domain/                 # Domain entities and interfaces
│   ├── Application/            # Business logic (CQRS with MediatR)
│   ├── Infrastructure/         # External services (EF Core, FFmpeg, etc.)
│   ├── WebApi/                 # API controllers and startup
│   │   ├── ClientApp/         # Frontend source (Vite)
│   │   └── wwwroot/           # Built frontend (served by backend)
│   └── Tests/                 # Unit and integration tests
└── build-full-stack.ps1       # Build script
```

## Key Features

- ✅ **Full-stack single executable** - Backend + Frontend in one file
- ✅ **Clean Architecture** - CQRS with MediatR
- ✅ **Automatic database setup** - No migrations needed
- ✅ **Hardware-accelerated transcoding** - NVENC, QuickSync, AMF, VAAPI
- ✅ **Real-time updates** - SignalR for progress notifications
- ✅ **Database-backed settings** - Persist configuration changes
- ✅ **Multiple streaming modes** - Direct Play, Direct Stream, Transcode
- ✅ **OpenTelemetry** - Built-in observability

## Database

The database is automatically created on first run using SQLite.

Location: `app/WebApi/lanflix.db`

To reset the database, simply delete the file and restart the server.

## Troubleshooting

### Port already in use

Change the port in `app/WebApi/Properties/launchSettings.json`

### FFmpeg not found

Install FFmpeg and ensure it's in your PATH:
- Windows: `choco install ffmpeg`
- Linux: `apt install ffmpeg`
- macOS: `brew install ffmpeg`

### Frontend not loading

Rebuild the frontend:
```powershell
cd app/WebApi/ClientApp
npm run build
```

## API Documentation

When running in development mode, OpenAPI is available at:
- http://localhost:5037/openapi/v1.json

## Health Checks

- **Full health check**: http://localhost:5037/health
- **Ready check**: http://localhost:5037/health/ready
- **Live check**: http://localhost:5037/health/live

## License

MIT
