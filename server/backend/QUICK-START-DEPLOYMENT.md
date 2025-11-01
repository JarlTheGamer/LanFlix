# Lanflix Server - Quick Start Deployment Guide

Get Lanflix Server up and running in minutes!

## Choose Your Platform

- [Windows Quick Start](#windows-quick-start)
- [Linux Quick Start](#linux-quick-start)
- [Docker Quick Start](#docker-quick-start-recommended)

---

## Docker Quick Start (Recommended)

### Prerequisites
- Docker and Docker Compose installed
- FFmpeg (included in Docker image)

### Steps

1. **Clone or download the repository**
   ```bash
   cd server/backend
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   nano .env
   ```

3. **Edit .env with your settings**
   ```bash
   MEDIA_PATH_MOVIES=/path/to/your/movies
   MEDIA_PATH_SERIES=/path/to/your/series
   TMDB_API_KEY=your_tmdb_api_key
   JWT_KEY=your_secure_random_key_minimum_32_characters
   ```

4. **Start the server**
   ```bash
   docker-compose up -d
   ```

5. **Verify it's running**
   ```bash
   curl http://localhost:5000/health
   ```

6. **Access the server**
   - API: http://localhost:5000
   - Health: http://localhost:5000/health

**That's it!** Your server is running.

### Useful Commands

```bash
# View logs
docker-compose logs -f lanflix-server

# Stop server
docker-compose down

# Restart server
docker-compose restart lanflix-server

# Update server
docker-compose pull
docker-compose up -d
```

---

## Windows Quick Start

### Prerequisites
- Windows 10/11 or Windows Server 2019+
- FFmpeg installed and in PATH

### Steps

1. **Install FFmpeg**
   - Download from https://ffmpeg.org/download.html
   - Extract to `C:\ffmpeg`
   - Add `C:\ffmpeg\bin` to system PATH

2. **Build the executable**
   ```powershell
   cd server\backend
   .\publish-win-x64.ps1
   ```

3. **Configure the server**
   - Navigate to `WebApi\bin\Release\net9.0\publish\win-x64`
   - Edit `appsettings.json`:
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

4. **Run the server**
   ```powershell
   .\Lanflix.WebApi.exe
   ```

5. **Verify it's running**
   ```powershell
   curl http://localhost:5000/health
   ```

**Done!** Server is running on http://localhost:5000

### Run as Windows Service (Optional)

1. **Download NSSM** from https://nssm.cc/download

2. **Install service**
   ```powershell
   nssm install Lanflix "C:\path\to\Lanflix.WebApi.exe"
   nssm set Lanflix AppDirectory "C:\path\to\publish\win-x64"
   nssm start Lanflix
   ```

---

## Linux Quick Start

### Prerequisites
- Linux (Ubuntu 20.04+, Debian 11+, CentOS 8+, etc.)
- FFmpeg installed

### Steps

1. **Install FFmpeg**
   ```bash
   # Ubuntu/Debian
   sudo apt update && sudo apt install ffmpeg
   
   # CentOS/RHEL
   sudo yum install epel-release && sudo yum install ffmpeg
   ```

2. **Build the executable**
   ```bash
   cd server/backend
   chmod +x publish-linux-x64.sh
   ./publish-linux-x64.sh
   ```

3. **Create application user**
   ```bash
   sudo useradd -r -s /bin/false lanflix
   sudo mkdir -p /opt/lanflix /var/lib/lanflix/data
   sudo chown -R lanflix:lanflix /opt/lanflix /var/lib/lanflix
   ```

4. **Deploy application**
   ```bash
   sudo cp -r WebApi/bin/Release/net9.0/publish/linux-x64/* /opt/lanflix/
   sudo chmod +x /opt/lanflix/Lanflix.WebApi
   ```

5. **Configure the server**
   ```bash
   sudo nano /opt/lanflix/appsettings.json
   ```
   
   Update:
   ```json
   {
     "Lanflix": {
       "MediaPaths": {
         "Movies": "/media/movies",
         "Series": "/media/series"
       },
       "ExternalApis": {
         "Tmdb": {
           "ApiKey": "your_tmdb_api_key"
         }
       }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=/var/lib/lanflix/data/lanflix.db"
     },
     "Jwt": {
       "Key": "YourSecureRandomKey32CharactersMin"
     }
   }
   ```

6. **Create systemd service**
   ```bash
   sudo nano /etc/systemd/system/lanflix.service
   ```
   
   Paste:
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
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://+:5000

   [Install]
   WantedBy=multi-user.target
   ```

7. **Start the service**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable lanflix
   sudo systemctl start lanflix
   ```

8. **Verify it's running**
   ```bash
   sudo systemctl status lanflix
   curl http://localhost:5000/health
   ```

**Done!** Server is running as a system service.

### Useful Commands

```bash
# View logs
sudo journalctl -u lanflix -f

# Stop server
sudo systemctl stop lanflix

# Restart server
sudo systemctl restart lanflix

# Check status
sudo systemctl status lanflix
```

---

## Post-Installation

### 1. Get TMDB API Key

1. Go to https://www.themoviedb.org/
2. Create an account
3. Go to Settings → API
4. Request an API key (free)
5. Add it to your configuration

### 2. Configure Media Paths

Ensure your media directories are accessible:

```bash
# Linux
sudo chown -R lanflix:lanflix /media/movies /media/series

# Windows
# Right-click folders → Properties → Security → Add user permissions
```

### 3. Test the Server

```bash
# Health check
curl http://localhost:5000/health

# API test
curl http://localhost:5000/api/library/items
```

### 4. Configure Firewall

```bash
# Linux (UFW)
sudo ufw allow 5000/tcp

# Linux (firewalld)
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --reload

# Windows
# Windows Defender Firewall → Advanced Settings → Inbound Rules → New Rule
# Port: 5000, Protocol: TCP, Allow connection
```

---

## Migration from Legacy Backend

If you're migrating from the old Node.js backend:

1. **Backup your data**
   ```bash
   cp /path/to/old/database.db /backup/database.db.backup
   ```

2. **Run migration tool**
   ```bash
   cd server/backend/MigrationTool
   dotnet run -- \
     --legacy-db /path/to/old/database.db \
     --new-db /path/to/new/lanflix.db \
     --config /path/to/old/.env
   ```

3. **Verify migration**
   ```bash
   # Check the migration report
   cat migration-report.json
   ```

4. **Start new backend**
   Follow the quick start guide above

For detailed migration instructions, see [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)

---

## Troubleshooting

### Server won't start

```bash
# Check if port is in use
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux

# Check logs
docker logs lanflix-server    # Docker
sudo journalctl -u lanflix    # Linux systemd
```

### FFmpeg not found

```bash
# Verify installation
ffmpeg -version

# Install if missing
sudo apt install ffmpeg       # Ubuntu/Debian
brew install ffmpeg           # macOS
# Download from ffmpeg.org    # Windows
```

### Can't access media files

```bash
# Check permissions
ls -l /media/movies           # Linux

# Fix permissions
sudo chown -R lanflix:lanflix /media/movies
```

### Database errors

```bash
# Check database file
ls -l /var/lib/lanflix/data/lanflix.db

# Fix permissions
sudo chown lanflix:lanflix /var/lib/lanflix/data/lanflix.db
sudo chmod 644 /var/lib/lanflix/data/lanflix.db
```

---

## Next Steps

- **Configure HTTPS**: See [DEPLOYMENT.md](DEPLOYMENT.md#security-considerations)
- **Set up monitoring**: See [DEPLOYMENT.md](DEPLOYMENT.md#monitoring)
- **Optimize performance**: See [DEPLOYMENT.md](DEPLOYMENT.md#performance-tuning)
- **Scale horizontally**: See [DEPLOYMENT.md](DEPLOYMENT.md#scaling)

---

## Getting Help

- **Documentation**: [DEPLOYMENT.md](DEPLOYMENT.md) - Full deployment guide
- **Configuration**: [CONFIGURATION.md](CONFIGURATION.md) - All configuration options
- **Migration**: [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md) - Migration from legacy backend
- **GitHub Issues**: Report bugs and request features
- **Community Forum**: Get help from the community

---

## Quick Reference

### Default Ports
- HTTP: 5000
- HTTPS: 5001
- Redis: 6379 (if using)

### Default Paths
- **Windows**: `C:\Program Files\Lanflix`
- **Linux**: `/opt/lanflix`
- **Docker**: `/app`

### Important Files
- Configuration: `appsettings.json`
- Database: `lanflix.db`
- Logs: `logs/lanflix-*.log`

### Health Check
```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "entries": {
    "database": { "status": "Healthy" },
    "ffmpeg": { "status": "Healthy" }
  }
}
```

---

**Congratulations!** Your Lanflix Server is now running. Enjoy streaming! 🎬🍿
