# Lanflix Android App - OTA Update System

This document explains how to use the Over-The-Air (OTA) update system for the Lanflix Android app.

## Features

- **Automatic Update Checking**: Checks for updates every 6 hours in the background
- **Manual Update Check**: Users can manually check for updates via the menu
- **Automatic Download**: Downloads updates automatically when available
- **Silent Installation**: Prompts user to install downloaded updates
- **Version Skipping**: Users can skip non-mandatory updates
- **Mandatory Updates**: Force users to update for critical releases
- **Progress Notifications**: Shows download progress in notifications
- **Checksum Verification**: Verifies downloaded APK integrity

## How It Works

1. **Background Checks**: The app periodically checks your server for new versions
2. **Update Detection**: Compares current version code with server version
3. **Update Screen**: Launches full-screen update experience
4. **Download Process**: Downloads APK with real-time progress display
5. **Auto-Install**: Seamlessly launches Android package installer

## User Experience

### Update Flow:

1. **Update Detection Dialog** (for optional updates):
   ```
   Update Available
   A new version (3.9.0) is available!
   
   What's new:
   • Bug fixes and improvements
   • Better performance
   
   Download size: 15MB
   
   [Update Now] [Skip This Version] [Later]
   ```

2. **Full-Screen Update Experience**:
   - **Immersive update screen** with app logo and branding
   - **Real-time download progress** with percentage and data transferred
   - **Release notes display** showing what's new in this version
   - **Automatic installation** when download completes
   - **Error handling** with retry options

3. **Update Screen States**:
   - **Preparing**: "Preparing update..." with loading animation
   - **Downloading**: "Downloading update... 45%" with progress bar
   - **Complete**: "Download complete! Ready to install"
   - **Installing**: "Installing update..." with loading animation
   - **Error**: "Download failed" with retry button

4. **Installation**: Seamlessly launches Android package installer

### Features:
- **Immersive Experience**: Full-screen update with hidden system UI
- **Progress Tracking**: Real-time download progress with MB/percentage
- **Release Notes**: Shows what's new in the update
- **Error Recovery**: Retry failed downloads
- **Permission Handling**: Guides users through installation permissions
- **Mandatory Updates**: Cannot be skipped or cancelled

## Server Setup

### 1. Add the Update Controller

The `AppUpdateController.cs` has been created in your server project. It provides:

- `GET /api/app/update-check` - Check for available updates
- `GET /api/app/download/{fileName}` - Download APK files
- `GET /api/app/version` - Get server version info

### 2. Configure Update Information

Edit the `CheckForUpdate` method in `AppUpdateController.cs`:

```csharp
var latestVersion = new
{
    versionName = "3.9.0",           // Display version
    versionCode = 39,                // Numeric version for comparison
    downloadUrl = $"{Request.Scheme}://{Request.Host}/api/app/download/lanflix-native-webview-v3.9.0.apk",
    releaseNotes = "Bug fixes and improvements",
    mandatory = false,               // Set to true for forced updates
    fileSize = 15728640L,           // File size in bytes
    checksum = "sha256hash..."       // SHA-256 checksum for verification
};
```

### 3. Place APK Files

Ensure your APK files are in the `releases` directory of your server project.

## Building with OTA Support

### Using the Build Script

```powershell
# Navigate to the build-tools/scripts directory
cd build-tools/scripts

# Build a new version
./build-and-release-ota.ps1 -VersionName "3.9.0" -VersionCode 39 -ReleaseNotes "Bug fixes and improvements"

# Build a mandatory update
./build-and-release-ota.ps1 -VersionName "4.0.0" -VersionCode 40 -ReleaseNotes "Major update with new features" -Mandatory
```

### Manual Build Process

1. **Update Version**: Edit `app/build.gradle.kts`
   ```kotlin
   versionCode = 39
   versionName = "3.9.0"
   ```

2. **Build APK**:
   ```bash
   cd build-tools/AndroidVersions/native-app
   ./gradlew assembleRelease
   ```

3. **Copy to Releases**: Move APK to `releases` directory

4. **Update Server**: Modify `AppUpdateController.cs` with new version info

## Configuration

### App Configuration

Edit `app/src/main/res/values/update_config.xml`:

```xml
<resources>
    <string name="update_server_url">http://your-server:5037</string>
    <string name="update_endpoint">/api/app/update-check</string>
    <integer name="update_check_interval_hours">6</integer>
    <bool name="auto_download_updates">true</bool>
    <bool name="check_updates_on_startup">true</bool>
</resources>
```

### Server URL

Update the server URL in:
- `MainActivity.kt` (line with `serverUrl`)
- `update_config.xml` (update_server_url)

## User Experience

### Automatic Updates
- App checks for updates on startup (after 3 seconds)
- Background checks every 6 hours
- Downloads happen automatically
- User gets notification when ready to install

### Manual Updates
- Users can check via menu: "Check for Updates"
- Shows "No updates" dialog if current
- Presents update dialog with release notes

### Update Dialog Options
- **Download**: Start downloading the update
- **Skip This Version**: Don't show this version again (non-mandatory only)
- **Later**: Dismiss dialog, will check again later

## Permissions

The app requires these permissions for OTA updates:

```xml
<uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />
<uses-permission android:name="android.permission.DOWNLOAD_WITHOUT_NOTIFICATION" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

## Security Considerations

1. **HTTPS**: Use HTTPS in production for secure downloads
2. **Checksum Verification**: Always provide SHA-256 checksums
3. **Signed APKs**: Ensure APKs are properly signed
4. **Server Security**: Protect your update endpoints

## Troubleshooting

### Updates Not Working
1. Check server URL configuration
2. Verify server is running and accessible
3. Check app logs for error messages
4. Ensure APK files are in correct location

### Installation Fails
1. Check if "Install from Unknown Sources" is enabled
2. Verify APK is not corrupted (checksum)
3. Ensure sufficient storage space
4. Check Android version compatibility

### Background Checks Not Working
1. Verify WorkManager is properly configured
2. Check if battery optimization is disabled
3. Ensure network connectivity

## Testing

### Test Update Flow
1. Build app with version 1
2. Install on device
3. Build app with version 2
4. Update server configuration
5. Test manual update check
6. Test automatic background check

### Test Scenarios
- [ ] Manual update check with available update
- [ ] Manual update check with no update
- [ ] Automatic background update detection
- [ ] Download progress notifications
- [ ] Installation process
- [ ] Mandatory vs optional updates
- [ ] Version skipping functionality
- [ ] Network error handling
- [ ] Checksum verification

## Production Deployment

1. **Build Release APK**: Use release build configuration
2. **Sign APK**: Ensure proper code signing
3. **Upload to Server**: Place in releases directory
4. **Update Controller**: Configure version information
5. **Test**: Verify update flow works correctly
6. **Monitor**: Check server logs for update requests

## Version Management

### Version Numbering
- **versionName**: Human-readable (e.g., "3.9.0")
- **versionCode**: Integer for comparison (e.g., 39)

### Release Process
1. Increment version numbers
2. Build and test APK
3. Generate release notes
4. Update server configuration
5. Deploy APK to server
6. Announce update availability

This OTA system provides a seamless update experience for your Lanflix users while maintaining security and reliability.