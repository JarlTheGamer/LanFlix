# Android Connection Issues - Troubleshooting Guide

## "Failed to Fetch" Error

If your Android app shows "failed to fetch" when trying to connect to the backend server, follow these steps:

### 1. Verify Backend Server is Running

Make sure your backend server is running and accessible:

```bash
# Start the backend server
cd backend
npm start
```

The server should show:
```
Server running on port 6129
Server accessible on all network interfaces (0.0.0.0)
```

### 2. Check Network Configuration

**Both devices must be on the same network:**
- Your computer running the backend
- Your Android device

**Find your computer's local IP address:**

Windows:
```cmd
ipconfig
```
Look for "IPv4 Address" under your active network adapter (usually starts with 192.168.x.x or 10.0.x.x)

Mac/Linux:
```bash
ifconfig
# or
ip addr show
```

### 3. Test Backend Accessibility

From your Android device's browser, try accessing:
```
http://YOUR_COMPUTER_IP:6129/health
```

Example: `http://192.168.1.100:6129/health`

You should see:
```json
{
  "status": "ok",
  "timestamp": "...",
  "port": 6129,
  "version": "1.0.0",
  "name": "Lanflix"
}
```

### 4. Firewall Settings

**Windows Firewall:**
1. Open Windows Defender Firewall
2. Click "Allow an app or feature through Windows Defender Firewall"
3. Find "Node.js" or add a new rule for port 6129
4. Enable for both Private and Public networks

**Mac Firewall:**
1. System Preferences → Security & Privacy → Firewall
2. Click "Firewall Options"
3. Add Node.js or allow incoming connections

### 5. Rebuild Android App

After fixing network issues, rebuild the APK:

```cmd
cd frontend\build-tools\android
build-apk.bat
```

The updated network security config will allow cleartext HTTP traffic to your local server.

### 6. Manual Server Configuration

If auto-discovery fails, manually enter your server URL in the app:

1. Open the Lanflix app
2. On the configuration screen, enter: `http://YOUR_COMPUTER_IP:6129`
3. Example: `http://192.168.1.100:6129`
4. Click "Connect"

### Common Issues

**Issue: "Network request failed"**
- Backend server not running
- Wrong IP address
- Firewall blocking port 6129

**Issue: "Connection timeout"**
- Devices on different networks
- VPN interfering with local network
- Router blocking local traffic

**Issue: Auto-discovery not finding server**
- Server not bound to 0.0.0.0 (should be fixed now)
- Non-standard network configuration
- Use manual configuration instead

### Network Security Config

The Android app now allows cleartext HTTP traffic for all local connections. This is configured in:
```
frontend/build-tools/android/android/app/src/main/res/xml/network_security_config.xml
```

### Port Information

The backend uses port **6129** (spells "FLIX" on a phone keypad):
- 6 = F
- 1 = L
- 2 = I
- 9 = X

### Still Having Issues?

1. Check backend logs for connection attempts
2. Try connecting from another device on the same network
3. Temporarily disable firewall to test
4. Verify no VPN is active on either device
5. Check router settings for AP isolation (disable if enabled)
