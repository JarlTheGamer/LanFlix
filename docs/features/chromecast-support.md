# Chromecast & Smart TV Casting Support

## Overview
Added native Google Cast support to stream content directly to Chromecast devices and Smart TVs.

## Features

### Cast Button
- Appears in video player controls when Cast API is available
- Shows connection status with visual feedback (red pulsing icon when connected)
- One-click casting to available devices

### Functionality
- **Device Discovery**: Automatically detects Chromecast and Cast-enabled devices on the network
- **Seamless Handoff**: Transfers playback from browser to Cast device
- **Progress Sync**: Maintains watch progress when casting
- **Resume Position**: Starts casting from current playback position
- **Remote Control**: Control playback from the browser while casting

### Supported Devices
- Chromecast (all generations)
- Chromecast Ultra
- Chromecast with Google TV
- Smart TVs with built-in Chromecast
- Android TV devices
- Google Nest Hub displays

## Technical Implementation

### Cast SDK Integration
```html
<!-- Google Cast SDK loaded in player.html -->
<script src="https://www.gstatic.com/cv/js/sender/v1/cast_sender.js?loadCastFramework=1"></script>
```

### Key Methods
- `initializeCastAPI()` - Initialize Cast framework
- `setupCastAPI()` - Configure Cast context and event listeners
- `initiateCast()` - Start/stop casting
- `loadMediaToCast()` - Load video stream to Cast device
- `handleCastStateChange()` - Handle connection state changes

### Cast States
1. **NOT_CONNECTED** - No Cast device connected
2. **CONNECTING** - Attempting to connect
3. **CONNECTED** - Successfully connected and casting

## User Experience

### Starting Cast
1. Click the Cast button in video player controls
2. Select a Cast device from the dialog
3. Video automatically transfers to the selected device
4. Browser shows "Now playing on Cast device" notification

### During Cast
- Local video pauses automatically
- Progress bar syncs with Cast device playback
- All controls remain functional in browser
- Watch progress continues to be tracked

### Stopping Cast
- Click Cast button again to disconnect
- Playback returns to browser
- Resumes from current position

## Limitations

### Current Limitations
- Cast button only appears when Cast API is available (Chrome/Edge browsers)
- Requires devices to be on the same network
- Some transcoding modes may have compatibility issues

### Browser Support
- ✅ Chrome (Desktop & Mobile)
- ✅ Edge (Chromium-based)
- ✅ Opera
- ❌ Firefox (no native Cast support)
- ❌ Safari (no native Cast support)

## Future Enhancements

### Planned Features
- [ ] AirPlay support for Apple devices
- [ ] DLNA support for broader device compatibility
- [ ] Cast queue management
- [ ] Multi-room audio sync
- [ ] Cast device volume control
- [ ] Subtitle support during casting
- [ ] Quality selection for Cast streams

## Troubleshooting

### Cast Button Not Appearing
- Ensure you're using a supported browser (Chrome/Edge)
- Check that Cast devices are on the same network
- Verify Cast SDK loaded successfully (check browser console)

### Connection Issues
- Restart Cast device
- Check firewall settings
- Ensure network allows multicast/mDNS traffic
- Try refreshing the page

### Playback Issues
- Check stream format compatibility
- Verify network bandwidth
- Try direct play mode instead of transcoding
- Check Cast device firmware is up to date

## Testing

### Test Checklist
- [ ] Cast button appears in supported browsers
- [ ] Device discovery works
- [ ] Connection establishes successfully
- [ ] Video starts playing on Cast device
- [ ] Progress syncs correctly
- [ ] Disconnection works properly
- [ ] Watch progress is saved
- [ ] Multiple cast sessions handled correctly
