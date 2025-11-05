# TV Navigation Guide

This guide explains how to use Lanflix with TV remote controls like Fire TV remotes, Android TV remotes, and other TV platform controllers.

## Overview

Lanflix now supports full navigation using TV remote controls. The interface automatically detects when running on TV platforms and enables remote control navigation with clear visual focus indicators.

## Supported Platforms

- **Fire TV** (Fire TV Stick, Fire TV Cube, etc.)
- **Android TV** (Android TV boxes, smart TVs)
- **Google TV** (Chromecast with Google TV)
- **Generic TV platforms** with D-pad remotes

## Remote Control Mapping

### Navigation Controls
- **D-pad Up/Down/Left/Right**: Navigate between interface elements
- **Center/OK/Enter**: Select focused item
- **Back**: Go back or close modals
- **Menu**: Open context menus (where applicable)
- **Home**: Return to home screen

### Media Controls
- **Play/Pause**: Control video playback
- **Stop**: Stop video and return to previous screen
- **Fast Forward**: Skip forward in video or navigate right
- **Rewind**: Skip backward in video or navigate left

### Quick Navigation
- **Number keys (0-9)**: Quick navigation shortcuts (where applicable)

## Navigation Flow

### 1. Main Menu Navigation
- Use **Left/Right** arrows to navigate between menu items (Home, Discover, Series, Films, My List)
- Use **Up/Down** arrows to move between menu bar and content areas
- Press **Enter** to select a menu item

### 2. Content Browsing
- Use **Up/Down** arrows to move between different content carousels
- Use **Left/Right** arrows to browse through movies/shows within a carousel
- Press **Enter** to select and view details of a movie/show

### 3. Video Playback
- All standard media keys work during video playback
- **Back** button returns to the previous screen
- **Play/Pause** controls playback
- **Left/Right** arrows seek backward/forward

## Visual Feedback

The interface provides clear visual feedback for TV navigation:

- **White outline**: Indicates the currently focused element
- **Glow effect**: Focused elements have a subtle glow
- **Scale animation**: Focused items slightly increase in size
- **Smooth scrolling**: Carousels automatically scroll to keep focused items visible

### Platform-Specific Styling
- **Fire TV**: Orange focus outline
- **Android TV**: Green focus outline
- **Generic TV**: White focus outline

## Technical Implementation

### Android App Integration
The Android WebView app automatically:
- Detects TV platforms
- Maps remote control keys to web navigation events
- Enables hardware acceleration for smooth performance
- Handles back button navigation

### Web Interface
The web interface:
- Automatically detects TV mode
- Applies appropriate focus styles
- Manages carousel scrolling
- Handles keyboard/remote events

## Troubleshooting

### Remote Not Working
1. Ensure the Android app is properly installed
2. Check that the device is recognized as a TV platform
3. Try restarting the app
4. Verify remote control batteries

### Navigation Issues
1. Check browser console for JavaScript errors
2. Ensure the TV navigation module is loaded
3. Verify focus styles are applied correctly
4. Test with keyboard navigation first

### Performance Issues
1. Ensure hardware acceleration is enabled
2. Check network connection stability
3. Clear app cache if needed
4. Restart the Android app

## Development Notes

### Key Files Modified
- `MainActivity.kt`: Enhanced remote control key mapping
- `tv-navigation.js`: Core TV navigation logic
- `navigation.js`: Enhanced focus management
- `main.css`: TV-specific focus styles

### CSS Classes
- `.tv-mode`: Applied when TV platform is detected
- `.fire-tv`: Applied specifically for Fire TV devices
- `.android-tv`: Applied specifically for Android TV devices
- `.focused`: Applied to currently focused elements

### JavaScript Events
The system uses synthetic keyboard events to integrate remote control input with existing web navigation:

```javascript
// Example: D-pad right becomes ArrowRight key event
window.dispatchEvent(new KeyboardEvent('keydown', {
  key: 'ArrowRight', 
  bubbles: true
}));
```

## Future Enhancements

Planned improvements include:
- Voice search integration
- Gesture support for advanced remotes
- Customizable key mappings
- Enhanced accessibility features
- Performance optimizations for older TV hardware

## Support

For issues or questions about TV navigation:
1. Check the troubleshooting section above
2. Review browser console logs
3. Test with different remote control types
4. Report issues with specific device models and remote types