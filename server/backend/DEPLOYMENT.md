# Lanflix Server Deployment Guide

This guide covers deploying the Lanflix Server in various environments including Windows, Linux, and Docker.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Deployment Methods](#deployment-methods)
3. [Windows Deployment](#windows-deployment)
4. [Linux Deployment](#linux-deployment)
5. [Docker Deployment](#docker-deployment)
6. [Migration Process](#migration-process)
7. [Rollback Procedure](#rollback-procedure)
8. [Health Checks](#health-checks)
9. [Monitoring](#monitoring)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **.NET 9.0 Runtime** (for standalone deployment) or Docker
- **FFmpeg** (version 4.4 or higher)
- **Redis** (optional but recommended for production)
- **PostgreSQL** (optional, SQLite is default)

### System Requirements

**Minimum:**
- CPU: 2 cores
- RAM: 2 GB
- Storage: 10 GB (plus media storage)

**Recommended:**
- CPU: 4+ cores (for transcoding)
- RAM: 4+ GB
- Storage: 20 GB SSD (plus media storage)
- GPU: NVIDIA/Intel/AMD for hardware transcoding

### Network Requirements

- Port 5000 (HTTP)
- Port 5001 (HTTPS)
- Port 6379 (Redis, if using)
- Port 5432 (PostgreSQL, if using)

---

## Deployment Methods

### 1. Single Executable (Recommended for Windows)
- Self-contained executable with all dependencies
- No .NET runtime installation required
- ~25MB file size

### 2. Framework-Dependent
- Requires .NET 9.0 runtime installed
- Smaller deployment size
- Shared runtime across applications

### 3. Docker (Recommended for Linux/Production)
- Containerized deployment
- Includes all dependencies
- Easy scaling and management

---

## Windows Deployment

### Method 1: Single Executable

#### Step 1: Build the Executable

```powershell
# Navigate to backend directory
cd server/backend

# Run the build script
.\publish-win-x64.ps1
```

Or manually:

```powershell
dotnet publish WebApi/Lanflix.WebApi.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true `
    -o publish/win-x64
```

#### Step 2: Install FFmpeg

Download FFmpeg from https://ffmpeg.org/download.html and add to PATH:

```powershell
# Add FFmpeg to PATH (PowerShell as Administrator)
$env:Path += ";C:\ffmpeg\bin"
[Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::Machine)
```

#### Step 3: Configure the Application

1. Copy `appsettings.json` to the publish directory
2. Edit configuration values:

```json
{
  "Lanflix": {
    "MediaPaths": {
      "Movies": "D:/Media/Movies",
      "Series": "D:/Media/Series"
    },
    "ExternalApis": {
      "Tmdb": {
        "ApiKey": "your_tmdb_api_key"
      }
    }
  },
  "Jwt": {
    "Key": "YourSecureRandomKey32CharactersMin"
  }
}
```

#### Step 4: Run the Server

```powershell
cd publish/win-x64
.\Lanflix.WebApi.exe
```

#### Step 5: Install as Windows Service (Optional)

Using NSSM (Non-Sucking Service Manager):

```powershell
# Download NSSM from https://nssm.cc/download

# Install service
nssm install Lanflix "C:\path\to\Lanflix.WebApi.exe"

# Configure service
nssm set Lanflix AppDirectory "C:\path\to\publish\win-x64"
nssm set Lanflix DisplayName "Lanflix Media Server"
nssm set Lanflix Description "Lanflix streaming media server"
nssm set Lanflix Start SERVICE_AUTO_START

# Start service
nssm start Lanflix
```

---

## Linux Deployment

### Method 1: Single Executable

#### Step 1: Build the Executable

```bash
# Navigate to backend directory
cd server/backend

# Run the build script
chmod +x publish-linux-x64.sh
./publish-linux-x64.sh
```

Or manually:

```bash
dotnet publish WebApi/Lanflix.WebApi.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    -o publish/linux-x64
```

#### Step 2: Install FFmpeg

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install ffmpeg

# CentOS/RHEL
sudo yum install epel-release
sudo yum install ffmpeg

# Arch Linux
sudo pacman -S ffmpeg
```

#### Step 3: Create Application User

```bash
# Create dedicated user
sudo useradd -r -s /bin/false lanflix

# Create directories
sudo mkdir -p /opt/lanflix
sudo mkdir -p /var/lib/lanflix/data
sudo mkdir -p /var/log/lanflix

# Set permissions
sudo chown -R lanflix:lanflix /opt/lanflix
sudo chown -R lanflix:lanflix /var/lib/lanflix
sudo chown -R lanflix:lanflix /var/log/lanflix
```

#### Step 4: Deploy Application

```bash
# Copy files
sudo cp -r publish/linux-x64/* /opt/lanflix/

# Make executable
sudo chmod +x /opt/lanflix/Lanflix.WebApi

# Copy configuration
sudo cp appsettings.Production.json /opt/lanflix/appsettings.json
```

#### Step 5: Configure Application

Edit `/opt/lanflix/appsettings.json`:

```json
{
  "Lanflix": {
    "MediaPaths": {
      "Movies": "/media/movies",
      "Series": "/media/series"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/lanflix/data/lanflix.db"
  }
}
```

#### Step 6: Create Systemd Service

Create `/etc/systemd/system/lanflix.service`:

```ini
[Unit]
Description=Lanflix Media Server
After=network.target

[Service]
Type=notify
User=lanflix
Group=lanflix
WorkingDirectory=/opt/lanflix
ExecStart=/opt/lanflix/Lanflix.WebApi
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=lanflix
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000

[Install]
WantedBy=multi-user.target
```

#### Step 7: Start Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service
sudo systemctl enable lanflix

# Start service
sudo systemctl start lanflix

# Check status
sudo systemctl status lanflix

# View logs
sudo journalctl -u lanflix -f
```

---

## Docker Deployment

### Method 1: Docker Compose (Recommended)

#### Step 1: Prepare Environment

Create `.env` file:

```bash
# Copy example
cp .env.example .env

# Edit values
nano .env
```

Example `.env`:

```bash
MEDIA_PATH_MOVIES=/path/to/movies
MEDIA_PATH_SERIES=/path/to/series
TMDB_API_KEY=your_api_key
JWT_KEY=your_secure_random_key_32_chars_min
```

#### Step 2: Deploy with Docker Compose

```bash
# Build and start services
docker-compose up -d

# View logs
docker-compose logs -f lanflix-server

# Check status
docker-compose ps
```

#### Step 3: Verify Deployment

```bash
# Check health
curl http://localhost:5000/health

# Check API
curl http://localhost:5000/api/health
```

### Method 2: Docker Run

```bash
# Build image
docker build -t lanflix-server .

# Run container
docker run -d \
  --name lanflix-server \
  -p 5000:5000 \
  -p 5001:5001 \
  -v /path/to/movies:/app/media/movies:ro \
  -v /path/to/series:/app/media/series:ro \
  -v lanflix-data:/app/data \
  -e Lanflix__ExternalApis__Tmdb__ApiKey=your_key \
  -e Jwt__Key=your_jwt_key \
  lanflix-server
```

### Docker with GPU Support (NVIDIA)

```bash
# Install NVIDIA Container Toolkit
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list
sudo apt-get update && sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker

# Run with GPU
docker run -d \
  --name lanflix-server \
  --gpus all \
  -p 5000:5000 \
  -v /path/to/media:/app/media:ro \
  lanflix-server
```

---

## Migration Process

### Pre-Migration Checklist

- [ ] Backup legacy database
- [ ] Document current configuration
- [ ] Test new backend in isolated environment
- [ ] Verify FFmpeg installation
- [ ] Prepare rollback plan

### Step 1: Backup Legacy System

```bash
# Backup database
cp /path/to/legacy/database.db /backup/database.db.backup

# Backup configuration
cp /path/to/legacy/.env /backup/.env.backup

# Backup media metadata
tar -czf /backup/media-metadata.tar.gz /path/to/legacy/metadata
```

### Step 2: Run Migration Tool

```bash
# Navigate to migration tool
cd server/backend/MigrationTool

# Run migration with dry-run
dotnet run -- \
  --legacy-db /path/to/legacy/database.db \
  --new-db /path/to/new/lanflix.db \
  --dry-run

# Review migration report
cat migration-report.json

# Execute actual migration
dotnet run -- \
  --legacy-db /path/to/legacy/database.db \
  --new-db /path/to/new/lanflix.db \
  --config /path/to/legacy/.env
```

### Step 3: Verify Migration

```bash
# Check record counts
sqlite3 /path/to/new/lanflix.db "SELECT COUNT(*) FROM Contents;"
sqlite3 /path/to/new/lanflix.db "SELECT COUNT(*) FROM Profiles;"
sqlite3 /path/to/new/lanflix.db "SELECT COUNT(*) FROM WatchHistories;"

# Compare with legacy
sqlite3 /path/to/legacy/database.db "SELECT COUNT(*) FROM Content;"
```

### Step 4: Parallel Testing

Run both backends simultaneously on different ports:

```bash
# Legacy backend (port 3000)
cd /path/to/legacy
npm start

# New backend (port 5000)
cd /path/to/new
./Lanflix.WebApi
```

Test critical functionality:
- Library browsing
- Content playback
- Profile management
- Watch history

### Step 5: Cutover

```bash
# Stop legacy backend
sudo systemctl stop lanflix-legacy

# Update client configuration to point to new backend
# Update reverse proxy/load balancer

# Start new backend
sudo systemctl start lanflix

# Monitor logs
sudo journalctl -u lanflix -f
```

### Step 6: Post-Migration Validation

- [ ] Verify all content is accessible
- [ ] Test streaming functionality
- [ ] Verify watch history preserved
- [ ] Test profile switching
- [ ] Verify metadata display
- [ ] Test search functionality

---

## Rollback Procedure

### When to Rollback

Rollback if you encounter:
- Critical functionality failures
- Data integrity issues
- Performance degradation
- Unrecoverable errors

### Rollback Steps

#### Step 1: Stop New Backend

```bash
# Systemd
sudo systemctl stop lanflix

# Docker
docker-compose down

# Windows Service
nssm stop Lanflix
```

#### Step 2: Restore Legacy Backend

```bash
# Start legacy backend
sudo systemctl start lanflix-legacy

# Or Docker
docker-compose -f docker-compose.legacy.yml up -d
```

#### Step 3: Update Client Configuration

```bash
# Update reverse proxy to point back to legacy backend
# Update client app configuration
```

#### Step 4: Verify Legacy System

```bash
# Check health
curl http://localhost:3000/health

# Test critical functionality
```

#### Step 5: Restore Database (if needed)

```bash
# Restore from backup
cp /backup/database.db.backup /path/to/legacy/database.db

# Restart legacy backend
sudo systemctl restart lanflix-legacy
```

### Rollback Time Estimate

- Simple rollback (no data changes): 5-10 minutes
- Full rollback with database restore: 15-30 minutes

---

## Health Checks

### Health Check Endpoints

#### Basic Health Check

```bash
GET /health
```

Response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0098765"
    },
    "ffmpeg": {
      "status": "Healthy",
      "duration": "00:00:00.0012345"
    },
    "disk-space": {
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    }
  }
}
```

#### Detailed Health Check

```bash
GET /health/ready
```

Checks:
- Database connectivity
- Redis connectivity (if enabled)
- FFmpeg availability
- Disk space availability
- Configuration validity

### Monitoring Health Checks

#### Using curl

```bash
# Simple check
curl -f http://localhost:5000/health || echo "Health check failed"

# With timeout
curl -f --max-time 10 http://localhost:5000/health
```

#### Using Docker Health Check

Already configured in Dockerfile:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1
```

#### Using Systemd

Add to service file:

```ini
[Service]
ExecStartPost=/bin/sleep 10
ExecStartPost=/usr/bin/curl -f http://localhost:5000/health
```

#### Using Kubernetes

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 5000
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 3
```

---

## Monitoring

### Metrics Endpoints

#### OpenTelemetry Metrics

The server exports metrics in OpenTelemetry format:

- Stream start counter
- Stream duration histogram
- Active streams gauge
- Transcoding queue depth
- Cache hit ratio
- API response times

#### Prometheus Integration

Add Prometheus exporter (if needed):

```bash
# Install OpenTelemetry Prometheus exporter
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

Access metrics:
```bash
GET /metrics
```

### Log Monitoring

#### Log Locations

- **Windows**: `logs/lanflix-*.log`
- **Linux**: `/var/log/lanflix/lanflix-*.log`
- **Docker**: `docker logs lanflix-server`

#### Log Levels

- `Information`: Normal operations
- `Warning`: Potential issues
- `Error`: Errors that don't stop the application
- `Fatal`: Critical errors that stop the application

#### Viewing Logs

```bash
# Linux (systemd)
sudo journalctl -u lanflix -f

# Docker
docker logs -f lanflix-server

# Windows (PowerShell)
Get-Content logs\lanflix-*.log -Wait -Tail 50
```

### Performance Monitoring

#### Key Metrics to Monitor

1. **Stream Performance**
   - Stream startup time (target: <500ms)
   - Active concurrent streams
   - Transcoding queue depth

2. **API Performance**
   - Response time p95 (target: <100ms)
   - Request rate
   - Error rate

3. **Resource Usage**
   - CPU usage (idle target: <5%)
   - Memory usage (idle target: <200MB)
   - Disk I/O
   - Network bandwidth

4. **Cache Performance**
   - Cache hit ratio (target: >70%)
   - Cache size
   - Cache eviction rate

---

## Troubleshooting

### Common Issues

#### 1. Server Won't Start

**Symptoms:**
- Application exits immediately
- "Port already in use" error

**Solutions:**
```bash
# Check if port is in use
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux

# Kill process using port
taskkill /PID <pid> /F        # Windows
kill -9 <pid>                 # Linux

# Use different port
export ASPNETCORE_URLS=http://+:5001
```

#### 2. FFmpeg Not Found

**Symptoms:**
- "FFmpeg not found" error
- Transcoding fails

**Solutions:**
```bash
# Verify FFmpeg installation
ffmpeg -version

# Add to PATH (Linux)
export PATH=$PATH:/usr/local/bin

# Add to PATH (Windows)
$env:Path += ";C:\ffmpeg\bin"

# Install FFmpeg
sudo apt install ffmpeg  # Ubuntu
brew install ffmpeg      # macOS
```

#### 3. Database Connection Failed

**Symptoms:**
- "Unable to open database" error
- Migration fails

**Solutions:**
```bash
# Check file permissions
ls -l /path/to/lanflix.db

# Fix permissions
sudo chown lanflix:lanflix /path/to/lanflix.db
sudo chmod 644 /path/to/lanflix.db

# Verify connection string
cat appsettings.json | grep ConnectionString
```

#### 4. Redis Connection Failed

**Symptoms:**
- "Redis connection failed" warning
- Cache not working

**Solutions:**
```bash
# Check Redis status
redis-cli ping

# Start Redis
sudo systemctl start redis  # Linux
redis-server               # Manual start

# Disable Redis (use memory cache only)
# In appsettings.json:
"Cache": {
  "Redis": {
    "Enabled": false
  }
}
```

#### 5. High CPU Usage

**Symptoms:**
- CPU usage >80%
- Slow response times

**Solutions:**
```bash
# Check active transcoding sessions
curl http://localhost:5000/api/stream/sessions

# Reduce concurrent transcodes
# In appsettings.json:
"Transcoding": {
  "MaxConcurrentTranscodes": 1
}

# Disable hardware acceleration (if causing issues)
"Transcoding": {
  "EnableHardwareAcceleration": false
}
```

#### 6. Out of Memory

**Symptoms:**
- Application crashes
- "OutOfMemoryException" errors

**Solutions:**
```bash
# Reduce memory cache size
# In appsettings.json:
"Cache": {
  "Memory": {
    "SizeLimit": 256
  }
}

# Increase system memory
# Or use swap space (Linux)
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
```

### Getting Help

#### Log Collection

Collect logs for support:

```bash
# Linux
tar -czf lanflix-logs.tar.gz /var/log/lanflix/

# Docker
docker logs lanflix-server > lanflix-logs.txt 2>&1

# Windows
Compress-Archive -Path logs\* -DestinationPath lanflix-logs.zip
```

#### System Information

```bash
# Linux
uname -a
cat /etc/os-release
free -h
df -h

# Docker
docker info
docker-compose version

# Windows
systeminfo
```

#### Support Channels

- GitHub Issues: https://github.com/your-repo/issues
- Documentation: https://docs.lanflix.com
- Community Forum: https://forum.lanflix.com

---

## Security Considerations

### 1. Use HTTPS in Production

```bash
# Generate self-signed certificate (development only)
dotnet dev-certs https --trust

# Use Let's Encrypt (production)
sudo certbot --nginx -d your-domain.com
```

### 2. Secure Configuration Files

```bash
# Set restrictive permissions
chmod 600 appsettings.json
chmod 600 .env

# Never commit secrets
echo "appsettings.Production.json" >> .gitignore
echo ".env" >> .gitignore
```

### 3. Use Strong JWT Keys

```bash
# Generate secure random key
openssl rand -base64 32
```

### 4. Run as Non-Root User

```bash
# Create dedicated user
sudo useradd -r -s /bin/false lanflix

# Run service as user
# In systemd service file:
User=lanflix
Group=lanflix
```

### 5. Configure Firewall

```bash
# Ubuntu/Debian
sudo ufw allow 5000/tcp
sudo ufw allow 5001/tcp
sudo ufw enable

# CentOS/RHEL
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --permanent --add-port=5001/tcp
sudo firewall-cmd --reload
```

---

## Performance Tuning

### 1. Database Optimization

```bash
# SQLite
# Enable WAL mode for better concurrency
sqlite3 lanflix.db "PRAGMA journal_mode=WAL;"

# PostgreSQL
# Tune configuration
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
```

### 2. Transcoding Optimization

```json
{
  "Transcoding": {
    "EnableHardwareAcceleration": true,
    "PreferredHwAccel": "nvenc",
    "MaxConcurrentTranscodes": 3,
    "DefaultBitrate": 6000000
  }
}
```

### 3. Caching Strategy

```json
{
  "Cache": {
    "Redis": {
      "Enabled": true
    },
    "Memory": {
      "SizeLimit": 512
    }
  }
}
```

### 4. Connection Pooling

Already configured in the application:
- HTTP client pooling
- Database connection pooling
- Redis connection pooling

---

## Backup and Recovery

### Backup Strategy

#### 1. Database Backup

```bash
# SQLite
cp /var/lib/lanflix/data/lanflix.db /backup/lanflix-$(date +%Y%m%d).db

# PostgreSQL
pg_dump -U lanflix lanflix > /backup/lanflix-$(date +%Y%m%d).sql
```

#### 2. Configuration Backup

```bash
tar -czf /backup/config-$(date +%Y%m%d).tar.gz \
  /opt/lanflix/appsettings.json \
  /opt/lanflix/.env
```

#### 3. Automated Backups

Create cron job:

```bash
# Edit crontab
crontab -e

# Add daily backup at 2 AM
0 2 * * * /opt/lanflix/scripts/backup.sh
```

### Recovery

```bash
# Restore database
cp /backup/lanflix-20240101.db /var/lib/lanflix/data/lanflix.db

# Restore configuration
tar -xzf /backup/config-20240101.tar.gz -C /opt/lanflix/

# Restart service
sudo systemctl restart lanflix
```

---

## Scaling

### Horizontal Scaling

#### Load Balancer Configuration

```nginx
upstream lanflix_backend {
    least_conn;
    server lanflix1:5000;
    server lanflix2:5000;
    server lanflix3:5000;
}

server {
    listen 80;
    server_name lanflix.example.com;

    location / {
        proxy_pass http://lanflix_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

#### Requirements for Scaling

- Redis for distributed caching
- Redis for SignalR backplane
- Shared storage for media files
- Shared database (PostgreSQL recommended)

### Vertical Scaling

- Increase CPU cores for more concurrent transcoding
- Increase RAM for larger caches
- Add GPU for hardware transcoding
- Use SSD for database and cache

---

## Conclusion

This deployment guide covers the most common deployment scenarios. For specific use cases or advanced configurations, refer to the [Configuration Guide](CONFIGURATION.md) or contact support.

**Next Steps:**
1. Choose your deployment method
2. Follow the appropriate section
3. Run the migration tool
4. Verify deployment
5. Monitor performance
6. Optimize as needed

Good luck with your deployment!
