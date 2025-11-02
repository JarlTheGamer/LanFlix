# Server Auto-Update System

The Lanflix server includes an automatic update system that can check for new versions and apply them automatically.

## Features

- **Automatic Update Checks**: Background service checks for updates every 6 hours (configurable)
- **Manual Updates**: Check and apply updates via the Admin Dashboard
- **GitHub Releases Integration**: Fetches updates from GitHub releases
- **Platform Detection**: Automatically downloads the correct package for your OS (Windows/Linux/macOS)
- **Backup on Update**: Creates a backup of the current version before updating
- **Graceful Restart**: Server restarts automatically after update

## Configuration

Edit `appsettings.json`:

```json
{
  "Lanflix": {
    "ServerUpdates": {
      "EnableAutoUpdate": false,
      "CheckIntervalHours": 6,
      "UpdateCheckUrl": "https://api.github.com/repos/YOUR_USERNAME/YOUR_REPO/releases/latest"
    }
  }
}
```

### Settings

- **EnableAutoUpdate**: Set to `true` to automatically download and install updates
- **CheckIntervalHours**: How often to check for updates (default: 6 hours)
- **UpdateCheckUrl**: GitHub API URL for your releases

## Using Manual Updates

1. Navigate to **Admin Dashboard** → **Server Updates**
2. Click **Check for Updates**
3. If an update is available, review the release notes
4. Click **Download and Install Update**
5. Server will restart automatically

## API Endpoints

### Get Current Version
```http
GET /api/server-update/version
```

Response:
```json
{
  "version": "1.2.6"
}
```

### Check for Updates
```http
GET /api/server-update/check
```

Response (update available):
```json
{
  "updateAvailable": true,
  "currentVersion": "1.2.6",
  "latestVersion": "1.3.0",
  "releaseDate": "2024-01-15T10:30:00Z",
  "downloadUrl": "https://github.com/user/repo/releases/download/v1.3.0/lanflix-win-x64.zip",
  "fileSize": 52428800,
  "releaseNotes": "## What's New\n- Feature 1\n- Bug fix 2"
}
```

Response (up to date):
```json
{
  "updateAvailable": false,
  "currentVersion": "1.2.6",
  "message": "Server is up to date"
}
```

### Apply Update
```http
POST /api/server-update/apply
Content-Type: application/json

{
  "downloadUrl": "https://github.com/user/repo/releases/download/v1.3.0/lanflix-win-x64.zip"
}
```

Response:
```json
{
  "message": "Update is being applied. Server will restart shortly.",
  "success": true
}
```

## Publishing Releases

To make updates available:

1. **Build your release packages** for each platform:
   - `lanflix-win-x64.zip` (Windows)
   - `lanflix-linux-x64.tar.gz` (Linux)
   - `lanflix-osx-x64.tar.gz` (macOS)

2. **Create a GitHub Release**:
   - Tag version (e.g., `v1.3.0`)
   - Upload the platform packages as release assets
   - Add release notes

3. **Update the version** in your project:
   - Update `AssemblyVersion` in your `.csproj` file
   - Rebuild the project

## How It Works

1. **Background Service**: Runs every N hours checking GitHub API
2. **Version Comparison**: Compares current version with latest release
3. **Download**: Downloads the appropriate package for the platform
4. **Extract**: Extracts to temporary directory
5. **Backup**: Backs up current installation
6. **Preserve Data**: Saves database and settings files
7. **Update Script**: Creates platform-specific update script
8. **Restart**: Launches update script and exits
9. **Script Execution**: Script waits, copies files, restores data, restarts server

## Data Preservation

The update process automatically preserves:
- **Database**: `lanflix.db` (and SQLite journal files)
- **Settings**: `appsettings.json`
- **Logs**: Existing log files are kept

These files are temporarily saved during the update and restored after the new version is installed.

## Backup Location

Backups are stored in: `{ServerDirectory}/backup/`

## Troubleshooting

### Update fails to apply
- Check server logs in `logs/lanflix-errors-*.log`
- Ensure server has write permissions to its directory
- Manually restore from `backup/` folder if needed

### Update check fails
- Verify `UpdateCheckUrl` is correct
- Check internet connectivity
- Ensure GitHub API is accessible (rate limits apply)

### Server doesn't restart after update
- Check if update script has execute permissions (Linux/macOS)
- Manually restart the server
- Check system logs for script execution errors

## Security Notes

- Updates are downloaded over HTTPS
- No automatic execution without `EnableAutoUpdate: true`
- Backups are created before each update
- Update scripts are temporary and self-deleting

## Disabling Updates

To completely disable the update system:

1. Set `EnableAutoUpdate: false` in config
2. Don't use the manual update feature in Admin Dashboard
3. The background check service will still run but won't apply updates
