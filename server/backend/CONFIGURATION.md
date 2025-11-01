# Lanflix Server Configuration Guide

This document provides comprehensive information about all configuration options available in the Lanflix Server.

## Configuration Files

The server uses the standard ASP.NET Core configuration system with the following files:

- `appsettings.json` - Base configuration (development defaults)
- `appsettings.Production.json` - Production overrides
- `appsettings.Development.json` - Development overrides (optional)
- Environment variables - Override any setting

## Configuration Sections

### 1. CORS Configuration

Controls Cross-Origin Resource Sharing for web clients.

```json
"Lanflix": {
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-domain.com"
    ],
    "ProductionOrigins": [
      "https://your-domain.com"
    ]
  }
}
```

**Options:**
- `AllowedOrigins` (array): List of allowed origins for CORS requests
- `ProductionOrigins` (array): Additional origins allowed only in production

**Environment Variables:**
- `Lanflix__Cors__AllowedOrigins__0=http://localhost:3000`
- `Lanflix__Cors__AllowedOrigins__1=https://your-domain.com`

---

### 2. Media Paths Configuration

Defines where media files and cache are stored.

```json
"MediaPaths": {
  "Movies": "/media/movies",
  "Series": "/media/series",
  "PosterCache": "/data/cache/posters",
  "BackdropCache": "/data/cache/backdrops"
}
```

**Options:**
- `Movies` (string): Path to movie files directory
- `Series` (string): Path to TV series files directory
- `PosterCache` (string): Path to store cached poster images
- `BackdropCache` (string): Path to store cached backdrop images

**Environment Variables:**
- `Lanflix__MediaPaths__Movies=/media/movies`
- `Lanflix__MediaPaths__Series=/media/series`

**Notes:**
- Paths should be absolute
- Ensure the application has read access to media directories
- Ensure the application has read/write access to cache directories

---

### 3. Transcoding Configuration

Controls FFmpeg transcoding behavior.

```json
"Transcoding": {
  "EnableHardwareAcceleration": true,
  "PreferredHwAccel": "auto",
  "MaxConcurrentTranscodes": 2,
  "TempPath": "/temp/transcoding",
  "DefaultBitrate": 8000000,
  "HlsSegmentDuration": 6
}
```

**Options:**
- `EnableHardwareAcceleration` (bool): Enable GPU-accelerated transcoding
- `PreferredHwAccel` (string): Preferred hardware acceleration method
  - `auto` - Automatically detect best available
  - `nvenc` - NVIDIA NVENC
  - `qsv` - Intel QuickSync
  - `amf` - AMD AMF
  - `vaapi` - VAAPI (Linux)
- `MaxConcurrentTranscodes` (int): Maximum number of simultaneous transcoding sessions
- `TempPath` (string): Directory for temporary transcoding files
- `DefaultBitrate` (int): Default video bitrate in bits per second (8000000 = 8 Mbps)
- `HlsSegmentDuration` (int): HLS segment duration in seconds

**Environment Variables:**
- `Lanflix__Transcoding__EnableHardwareAcceleration=true`
- `Lanflix__Transcoding__MaxConcurrentTranscodes=3`

**Performance Notes:**
- Higher `MaxConcurrentTranscodes` requires more CPU/GPU resources
- Lower `DefaultBitrate` reduces quality but saves bandwidth
- Shorter `HlsSegmentDuration` improves seeking but increases overhead

---

### 4. Streaming Configuration

Controls media streaming behavior.

```json
"Streaming": {
  "EnableDirectPlay": true,
  "EnableDirectStream": true,
  "ChunkSize": 81920
}
```

**Options:**
- `EnableDirectPlay` (bool): Allow streaming without any transcoding
- `EnableDirectStream` (bool): Allow container remuxing without transcoding codecs
- `ChunkSize` (int): Size of streaming chunks in bytes (81920 = 80KB)

**Environment Variables:**
- `Lanflix__Streaming__EnableDirectPlay=true`
- `Lanflix__Streaming__ChunkSize=81920`

---

### 5. Cache Configuration

Controls caching behavior for metadata and API responses.

```json
"Cache": {
  "Redis": {
    "Enabled": true,
    "ConnectionString": "redis:6379",
    "InstanceName": "lanflix:"
  },
  "Memory": {
    "SizeLimit": 512
  }
}
```

**Options:**
- `Redis.Enabled` (bool): Enable Redis distributed cache
- `Redis.ConnectionString` (string): Redis connection string
- `Redis.InstanceName` (string): Prefix for Redis keys
- `Memory.SizeLimit` (int): In-memory cache size limit in MB

**Environment Variables:**
- `Lanflix__Cache__Redis__Enabled=true`
- `Lanflix__Cache__Redis__ConnectionString=redis:6379`

**Notes:**
- Redis is recommended for production deployments
- Memory cache is used as L1 cache even when Redis is enabled
- Redis enables cache sharing across multiple server instances

---

### 6. SignalR Configuration

Controls real-time communication settings.

```json
"SignalR": {
  "UseRedisBackplane": true,
  "ConnectionLifetime": {
    "KeepAliveIntervalSeconds": 15,
    "ClientTimeoutSeconds": 30,
    "HandshakeTimeoutSeconds": 15
  },
  "Reconnection": {
    "EnableAutoReconnect": true,
    "ReconnectIntervalSeconds": 5,
    "MaxReconnectAttempts": 10
  }
}
```

**Options:**
- `UseRedisBackplane` (bool): Use Redis for SignalR message distribution
- `ConnectionLifetime.KeepAliveIntervalSeconds` (int): Interval for keep-alive pings
- `ConnectionLifetime.ClientTimeoutSeconds` (int): Timeout before disconnecting inactive clients
- `ConnectionLifetime.HandshakeTimeoutSeconds` (int): Timeout for initial handshake
- `Reconnection.EnableAutoReconnect` (bool): Enable automatic reconnection
- `Reconnection.ReconnectIntervalSeconds` (int): Interval between reconnection attempts
- `Reconnection.MaxReconnectAttempts` (int): Maximum reconnection attempts

**Environment Variables:**
- `Lanflix__SignalR__UseRedisBackplane=true`

---

### 7. External APIs Configuration

Configuration for external service integrations.

```json
"ExternalApis": {
  "Tmdb": {
    "ApiKey": "your_api_key_here",
    "BaseUrl": "https://api.themoviedb.org/3/"
  }
}
```

**Options:**
- `Tmdb.ApiKey` (string): The Movie Database (TMDB) API key
- `Tmdb.BaseUrl` (string): TMDB API base URL

**Environment Variables:**
- `Lanflix__ExternalApis__Tmdb__ApiKey=your_key`

**Notes:**
- Get a free TMDB API key at https://www.themoviedb.org/settings/api
- The API key is required for fetching movie/TV show metadata

---

### 8. App Updates Configuration

Controls Android app OTA update system.

```json
"AppUpdates": {
  "ApkStoragePath": "/data/apk-updates",
  "EnableAutoUpdate": true
}
```

**Options:**
- `ApkStoragePath` (string): Directory to store APK files
- `EnableAutoUpdate` (bool): Enable automatic update checks

**Environment Variables:**
- `Lanflix__AppUpdates__ApkStoragePath=/data/apk-updates`

---

### 9. Database Configuration

Connection string for the database.

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=/data/lanflix.db"
}
```

**SQLite (Default):**
```json
"DefaultConnection": "Data Source=/data/lanflix.db"
```

**PostgreSQL:**
```json
"DefaultConnection": "Host=localhost;Database=lanflix;Username=lanflix;Password=your_password"
```

**Environment Variables:**
- `ConnectionStrings__DefaultConnection=Data Source=/data/lanflix.db`

---

### 10. JWT Authentication Configuration

Controls JSON Web Token authentication.

```json
"Jwt": {
  "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
  "Issuer": "Lanflix",
  "Audience": "LanflixClient",
  "ExpirationMinutes": 43200
}
```

**Options:**
- `Key` (string): Secret key for signing JWTs (minimum 32 characters)
- `Issuer` (string): JWT issuer claim
- `Audience` (string): JWT audience claim
- `ExpirationMinutes` (int): Token expiration time in minutes (43200 = 30 days)

**Environment Variables:**
- `Jwt__Key=your_secret_key`
- `Jwt__ExpirationMinutes=43200`

**Security Notes:**
- Use a strong, randomly generated key in production
- Never commit the production key to source control
- Rotate keys periodically for enhanced security

---

### 11. Legacy JWT Configuration

For backward compatibility with old authentication tokens.

```json
"LegacyJwt": {
  "Key": "legacy_key_here",
  "Issuer": "LanflixLegacy",
  "Audience": "LanflixLegacyClient"
}
```

**Options:**
- `Key` (string): Secret key from the legacy backend
- `Issuer` (string): Legacy JWT issuer
- `Audience` (string): Legacy JWT audience

**Environment Variables:**
- `LegacyJwt__Key=legacy_key`

---

### 12. Logging Configuration (Serilog)

Controls application logging behavior.

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "WriteTo": [
    {
      "Name": "Console"
    },
    {
      "Name": "File",
      "Args": {
        "path": "/logs/lanflix-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 30
      }
    }
  ]
}
```

**Log Levels:**
- `Verbose` - Most detailed
- `Debug` - Debugging information
- `Information` - General information
- `Warning` - Warning messages
- `Error` - Error messages
- `Fatal` - Critical failures

**Environment Variables:**
- `Serilog__MinimumLevel__Default=Information`

---

## Environment Variable Override Examples

All configuration can be overridden using environment variables with the following pattern:

```bash
# Format: Section__SubSection__Property=Value

# Example: Set TMDB API Key
export Lanflix__ExternalApis__Tmdb__ApiKey="your_api_key"

# Example: Set Redis connection
export Lanflix__Cache__Redis__ConnectionString="redis:6379"

# Example: Set database connection
export ConnectionStrings__DefaultConnection="Data Source=/data/lanflix.db"

# Example: Set JWT key
export Jwt__Key="YourSecretKey"
```

## Docker Environment Variables

When using Docker, set environment variables in `docker-compose.yml`:

```yaml
environment:
  - Lanflix__ExternalApis__Tmdb__ApiKey=your_key
  - Lanflix__Cache__Redis__Enabled=true
  - ConnectionStrings__DefaultConnection=Data Source=/app/data/lanflix.db
```

Or use an `.env` file:

```bash
# .env file
TMDB_API_KEY=your_key
REDIS_ENABLED=true
DATABASE_PATH=/app/data/lanflix.db
```

## Configuration Validation

The server validates configuration on startup and will fail to start if:
- Required paths don't exist or aren't accessible
- JWT key is too short (< 32 characters)
- Invalid connection strings
- Missing required API keys (TMDB)

Check the logs for detailed validation error messages.

## Security Best Practices

1. **Never commit secrets to source control**
   - Use environment variables for sensitive data
   - Use `.env` files (add to `.gitignore`)
   - Use secret management tools in production

2. **Use strong JWT keys**
   - Minimum 32 characters
   - Use cryptographically random values
   - Rotate periodically

3. **Restrict file permissions**
   - Configuration files: 600 (owner read/write only)
   - Database files: 600
   - Log files: 644 (owner read/write, others read)

4. **Use HTTPS in production**
   - Configure SSL certificates
   - Redirect HTTP to HTTPS
   - Use HSTS headers

## Troubleshooting

### Configuration not loading
- Check file permissions
- Verify JSON syntax (use a JSON validator)
- Check environment variable names (case-sensitive)

### Paths not found
- Use absolute paths
- Verify directory exists and is accessible
- Check user permissions

### Redis connection fails
- Verify Redis is running
- Check connection string format
- Verify network connectivity

### FFmpeg not found
- Ensure FFmpeg is installed
- Add FFmpeg to system PATH
- Check FFmpeg version compatibility

## Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Serilog Configuration](https://github.com/serilog/serilog-settings-configuration)
- [Docker Environment Variables](https://docs.docker.com/compose/environment-variables/)
