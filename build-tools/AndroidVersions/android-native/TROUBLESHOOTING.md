# Android App Connection Troubleshooting

## Common Connection Issues

### 1. Server Not Found
If the app shows "Connection Error" or "Cannot connect to server":

1. **Check Server Status**: Ensure the Lanflix server is running on your network
2. **Verify Server IP**: The server logs show it's running on `192.168.178.13:5037`
3. **Network Connection**: Make sure your Android device is on the same network

### 2. Manual Server Configuration

If automatic server discovery fails, you can manually configure the server:

1. The app will try to connect to `192.168.178.13:5037` by default (based on server logs)
2. If your server is on a different IP, you'll need to modify the code or wait for the settings screen

### 3. Debug Information

The app now includes detailed logging:
- Check Android Studio logcat for connection attempts
- Look for messages starting with "HomeViewModel:", "NetworkModule:", or "ServerDiscovery:"

### 4. Network Requirements

- **Same Network**: Android device and server must be on the same local network
- **Port Access**: Port 5037 must be accessible
- **Firewall**: Check if Windows Firewall is blocking the connection

### 5. Server Discovery Process

The app now:
1. First tries the known server IP: `192.168.178.13:5037`
2. If that fails, scans the local network for Lanflix servers
3. Tests multiple common ports: 5037, 8080, 3000, 5000, 8000, 5001

### 6. Error Messages

- **"Cannot connect to server"**: Network connectivity issue
- **"Connection timeout"**: Server is slow or unreachable
- **"Server not found"**: DNS/IP resolution issue

### 7. Quick Fixes

1. **Restart the app** - Sometimes helps with network state
2. **Check server logs** - Ensure the server is responding to requests
3. **Try from browser** - Test `http://192.168.178.13:5037/api/profiles` in a browser
4. **Network reset** - Disconnect and reconnect WiFi on Android device

## Server Configuration

Based on the logs, your server is configured as:
- IP: `192.168.178.13`
- Port: `5037`
- API Base: `http://192.168.178.13:5037/api/`

The app has been updated to use this configuration by default.
## ✅ 
Profile Data Structure Issues Fixed (Latest Update)

### 🔧 **Root Cause Identified:**
The app was crashing after connection because of a **data structure mismatch** between server and Android models:

**Server Profile (C#):**
- `Id`: `int` 
- `Name`: `string`
- `AvatarPath`: `string?` (optional)
- `IsKidsProfile`: `bool`
- `Preferences`: `UserPreferences?` (optional)

**Android Profile (Kotlin - FIXED):**
- `id`: `Int` (was `String` - FIXED)
- `name`: `String`
- `avatarPath`: `String?` (was missing - ADDED)
- `isKidsProfile`: `Boolean` (was missing - ADDED)
- `preferences`: `UserPreferences?` (was missing - ADDED)
- `avatarColorPrimary/Secondary`: Generated from name for UI

### 🔧 **API Request Structure Fixed:**
**Server expects (CreateProfileCommand):**
```json
{
  "name": "Profile Name",
  "avatarPath": null,
  "isKidsProfile": false,
  "preferences": null
}
```

**Android was sending (FIXED):**
- ❌ Old: `{name, avatarColorPrimary, avatarColorSecondary}`
- ✅ New: `{name, avatarPath, isKidsProfile, preferences}`

### 🔧 **Changes Made:**
1. **Profile Model**: Updated to match server structure exactly
2. **API Requests**: Fixed CreateProfileRequest and UpdateProfileRequest
3. **Repository**: Updated method signatures to use correct parameters
4. **ViewModel**: Fixed to handle Int IDs instead of String IDs
5. **Avatar Colors**: Now generated client-side from profile name
6. **Debugging**: Added comprehensive logging for profile operations

### 📱 **Expected Behavior Now:**
1. **Connection**: App connects to server successfully ✅
2. **Profile Loading**: Server returns profiles with correct structure ✅
3. **JSON Parsing**: Android correctly deserializes profile data ✅
4. **Navigation**: App proceeds to profile selection without crashing ✅
5. **Profile Creation**: Uses correct API structure ✅

### 🐛 **If Still Crashing:**
Check Android Studio logcat for:
- `ProfileRepository:` messages showing profile data
- `ProfileViewModel:` messages showing loading status
- JSON parsing errors or network exceptions
- Any remaining type mismatches

The app should now successfully load and display profiles from your server.