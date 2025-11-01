# Lanflix Server - Deployment Files Overview

This directory contains all the files needed to deploy the Lanflix Server in various environments.

## 📁 File Structure

```
server/backend/
├── Dockerfile                      # Docker image definition
├── docker-compose.yml              # Production Docker Compose
├── docker-compose.dev.yml          # Development Docker Compose
├── .dockerignore                   # Docker build exclusions
├── .env.example                    # Environment variables template
├── publish-win-x64.ps1            # Windows build script
├── publish-linux-x64.sh           # Linux build script
├── DEPLOYMENT.md                   # Comprehensive deployment guide
├── QUICK-START-DEPLOYMENT.md      # Quick start guide
├── CONFIGURATION.md                # Configuration reference
└── WebApi/
    ├── appsettings.json           # Base configuration
    ├── appsettings.Production.json # Production configuration
    └── Properties/
        └── PublishProfiles/       # Publish profiles
            ├── win-x64.pubxml
            ├── linux-x64.pubxml
            └── osx-x64.pubxml
```

## 🚀 Quick Start

Choose your deployment method:

### Docker (Recommended)
```bash
cp .env.example .env
# Edit .env with your settings
docker-compose up -d
```

### Windows
```powershell
.\publish-win-x64.ps1
cd WebApi\bin\Release\net9.0\publish\win-x64
.\Lanflix.WebApi.exe
```

### Linux
```bash
chmod +x publish-linux-x64.sh
./publish-linux-x64.sh
# Follow Linux deployment guide
```

## 📚 Documentation

### For Quick Setup
Start here: **[QUICK-START-DEPLOYMENT.md](QUICK-START-DEPLOYMENT.md)**
- Docker quick start
- Windows quick start
- Linux quick start
- Basic troubleshooting

### For Detailed Information
See: **[DEPLOYMENT.md](DEPLOYMENT.md)**
- Prerequisites and system requirements
- Detailed deployment steps for all platforms
- Migration process from legacy backend
- Rollback procedures
- Health checks and monitoring
- Performance tuning
- Scaling strategies
- Comprehensive troubleshooting

### For Configuration
See: **[CONFIGURATION.md](CONFIGURATION.md)**
- All configuration options explained
- Environment variable overrides
- Security best practices
- Configuration validation

## 🔧 Build Scripts

### Windows: `publish-win-x64.ps1`
Builds a self-contained single executable for Windows x64.

**Usage:**
```powershell
.\publish-win-x64.ps1
```

**Output:**
- Location: `WebApi\bin\Release\net9.0\publish\win-x64\Lanflix.WebApi.exe`
- Size: ~25MB
- Includes: .NET runtime, all dependencies

### Linux: `publish-linux-x64.sh`
Builds a self-contained single executable for Linux x64.

**Usage:**
```bash
chmod +x publish-linux-x64.sh
./publish-linux-x64.sh
```

**Output:**
- Location: `WebApi/bin/Release/net9.0/publish/linux-x64/Lanflix.WebApi`
- Size: ~25MB
- Includes: .NET runtime, all dependencies

## 🐳 Docker Files

### `Dockerfile`
Multi-stage Docker build that:
1. Builds the application
2. Creates optimized runtime image
3. Installs FFmpeg and dependencies
4. Configures non-root user
5. Sets up health checks

**Build:**
```bash
docker build -t lanflix-server .
```

### `docker-compose.yml`
Production-ready Docker Compose configuration with:
- Lanflix Server container
- Redis container for caching
- Volume mounts for data persistence
- Network configuration
- Health checks

**Usage:**
```bash
docker-compose up -d
```

### `docker-compose.dev.yml`
Development Docker Compose configuration with:
- Hot reload support
- Exposed ports for debugging
- Development volumes
- Optional PostgreSQL

**Usage:**
```bash
docker-compose -f docker-compose.dev.yml up
```

### `.dockerignore`
Excludes unnecessary files from Docker build context:
- Build artifacts (bin/, obj/)
- IDE files (.vs/, .vscode/)
- Documentation (*.md)
- Database files (*.db)
- Logs

## 🔐 Environment Configuration

### `.env.example`
Template for environment variables. Copy to `.env` and customize:

```bash
cp .env.example .env
nano .env
```

**Key variables:**
- `MEDIA_PATH_MOVIES` - Path to movie files
- `MEDIA_PATH_SERIES` - Path to TV series files
- `TMDB_API_KEY` - The Movie Database API key
- `JWT_KEY` - Secret key for JWT tokens (min 32 chars)
- `REDIS_CONNECTION_STRING` - Redis connection string

## ⚙️ Configuration Files

### `appsettings.json`
Base configuration with development defaults:
- Media paths
- Transcoding settings
- Streaming configuration
- Cache settings
- External API configuration
- Logging configuration

### `appsettings.Production.json`
Production overrides:
- Production-optimized paths
- Redis enabled by default
- Production CORS origins
- Production logging levels

**Note:** Environment variables override both files.

## 📋 Publish Profiles

Located in `WebApi/Properties/PublishProfiles/`:

### `win-x64.pubxml`
Visual Studio publish profile for Windows x64:
- Self-contained deployment
- Single file output
- Trimmed for size optimization

### `linux-x64.pubxml`
Visual Studio publish profile for Linux x64:
- Self-contained deployment
- Single file output
- Trimmed for size optimization

### `osx-x64.pubxml`
Visual Studio publish profile for macOS x64:
- Self-contained deployment
- Single file output
- Trimmed for size optimization

**Usage in Visual Studio:**
1. Right-click WebApi project
2. Select "Publish"
3. Choose profile
4. Click "Publish"

## 🏗️ Deployment Scenarios

### Scenario 1: Single Server (Docker)
**Best for:** Small to medium deployments, home servers

```bash
docker-compose up -d
```

**Features:**
- Easy setup and management
- Automatic restarts
- Built-in health checks
- Redis caching included

### Scenario 2: Single Server (Native)
**Best for:** Windows servers, specific OS requirements

```bash
# Windows
.\publish-win-x64.ps1
# Install as Windows Service

# Linux
./publish-linux-x64.sh
# Install as systemd service
```

**Features:**
- Direct OS integration
- Lower overhead
- Easier debugging

### Scenario 3: Multi-Server (Docker + Load Balancer)
**Best for:** High availability, high traffic

```bash
# Scale to 3 instances
docker-compose up -d --scale lanflix-server=3
```

**Requirements:**
- Load balancer (nginx, HAProxy)
- Redis for distributed caching
- Shared storage for media
- Shared database (PostgreSQL)

## 🔍 Health Checks

All deployment methods include health checks:

**Endpoint:** `http://localhost:5000/health`

**Checks:**
- Database connectivity
- Redis connectivity (if enabled)
- FFmpeg availability
- Disk space
- Configuration validity

**Docker Health Check:**
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1
```

**Systemd Health Check:**
Add to service file:
```ini
ExecStartPost=/usr/bin/curl -f http://localhost:5000/health
```

## 📊 Monitoring

### Logs

**Docker:**
```bash
docker logs -f lanflix-server
```

**Linux (systemd):**
```bash
sudo journalctl -u lanflix -f
```

**Windows:**
```powershell
Get-Content logs\lanflix-*.log -Wait -Tail 50
```

### Metrics

**Health endpoint:**
```bash
curl http://localhost:5000/health
```

**OpenTelemetry metrics:**
- Stream performance
- API response times
- Cache hit ratios
- Resource usage

## 🔄 Updates

### Docker
```bash
# Pull latest image
docker-compose pull

# Restart with new image
docker-compose up -d
```

### Native
```bash
# Build new version
./publish-linux-x64.sh

# Stop service
sudo systemctl stop lanflix

# Replace files
sudo cp -r publish/linux-x64/* /opt/lanflix/

# Start service
sudo systemctl start lanflix
```

## 🆘 Troubleshooting

### Quick Checks

1. **Is the server running?**
   ```bash
   curl http://localhost:5000/health
   ```

2. **Check logs:**
   ```bash
   docker logs lanflix-server          # Docker
   sudo journalctl -u lanflix -n 50    # Linux
   ```

3. **Verify FFmpeg:**
   ```bash
   ffmpeg -version
   ```

4. **Check ports:**
   ```bash
   netstat -ano | findstr :5000  # Windows
   lsof -i :5000                 # Linux
   ```

### Common Issues

| Issue | Solution |
|-------|----------|
| Port already in use | Change port in configuration or kill process |
| FFmpeg not found | Install FFmpeg and add to PATH |
| Database locked | Check file permissions, close other connections |
| Redis connection failed | Start Redis or disable in configuration |
| Out of memory | Reduce cache size or increase system memory |

See [DEPLOYMENT.md](DEPLOYMENT.md#troubleshooting) for detailed troubleshooting.

## 📞 Support

- **Documentation:** [DEPLOYMENT.md](DEPLOYMENT.md)
- **Configuration:** [CONFIGURATION.md](CONFIGURATION.md)
- **Quick Start:** [QUICK-START-DEPLOYMENT.md](QUICK-START-DEPLOYMENT.md)
- **Migration:** [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)
- **GitHub Issues:** Report bugs and request features

## ✅ Deployment Checklist

Before deploying to production:

- [ ] FFmpeg installed and working
- [ ] TMDB API key configured
- [ ] Strong JWT key generated (32+ characters)
- [ ] Media paths configured and accessible
- [ ] Database path configured
- [ ] Redis configured (recommended)
- [ ] HTTPS configured (recommended)
- [ ] Firewall rules configured
- [ ] Backup strategy in place
- [ ] Monitoring configured
- [ ] Health checks working
- [ ] Rollback plan documented

## 🎯 Next Steps

1. **Choose deployment method** (Docker recommended)
2. **Follow quick start guide** ([QUICK-START-DEPLOYMENT.md](QUICK-START-DEPLOYMENT.md))
3. **Configure application** ([CONFIGURATION.md](CONFIGURATION.md))
4. **Test deployment** (health checks, streaming)
5. **Set up monitoring** (logs, metrics)
6. **Plan for scaling** (if needed)

---

**Ready to deploy?** Start with [QUICK-START-DEPLOYMENT.md](QUICK-START-DEPLOYMENT.md)!
