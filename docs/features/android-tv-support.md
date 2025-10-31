# Android TV / Fire TV Support

## Overview

Lanflix now includes full support for Android TV, Fire TV Stick, and other TV platforms with D-pad navigation and optimized UI for 10-foot viewing experiences.

## Features

### 🎮 D-Pad Navigation
- **Arrow Keys**: Navigate through UI elements (up, down, left, right)
- **Enter Key**: Select/activate focused elements
- **Back/Escape**: Go back or close modals
- **Spatial Navigation**: Smart directional navigation that finds the closest element in the direction you're moving

### 📺 TV Platform Detection
Automatically detects and enables TV mode for:
- Android TV
- Fire TV / Fire TV Stick
- Google TV
- Samsung Tizen
- LG webOS
- Other smart TV platforms

### 🎨 TV-Optimized UI
- **Larger Touch Targets**: All interactive elements are at least 48x48px
- **Enhanced Focus Indicators**: Clear white outlines show which element is focused
- **Optimized Typography**: Larger fonts for comfortable viewing from a distance
- **Always-Visible Controls**: Video player controls remain visible in TV mode
- **Smooth Animations**: Focus transitions with subtle pulse animations

### 🎬 Video Player Enhancements
- **Native HTML5 Video**: Works on all mobile and TV devices
- **Inline Playback**: Videos play inline without fullscreen on mobile
- **D-Pad Controls**: Navigate player controls with remote
- **Keyboard Shortcuts**: Full keyboard support for TV remotes
  - Space/K: Play/Pause
  - Left/Right: Skip 10 seconds
  - Up/Down: Volume control
  - M: Mute/Unmute
  - F: Fullscreen
  - C: Subtitles

## Usage

### For Users

1. **Open Lanflix on your TV browser**
   - Fire TV: Use Silk Browser or Firefox
   - Android TV: Use Chrome or any browser
   - Smart TV: Use built-in browser

2. **Navigate with your remote**
   - Use D-pad to move between elements
   - Press Enter/OK to select
   - Press Back to go back

3. **Video Playback**
   - Navigate to a movie or show
   - Press Enter to play
   - Use D-pad to control playback

### For Developers

#### TV Navigation Module

The TV navigation is handled by `tv-navigation.js`:

```javascript
import tvNavigation from '../modules/tv-navigation.js';

// Initialize TV navigation
tvNavigation.initialize();

// Refresh after DOM changes
tvNavigation.refresh();
```

#### TV Mode Styles

TV-specific styles are in `tv-mode.css`:

```css
/* TV focus indicator */
.tv-mode .tv-focused {
  outline: 4px solid rgba(255, 255, 255, 0.95);
  transform: scale(1.05);
}
```

#### Making Elements TV-Navigable

Elements are automatically detected if they match these selectors:
- `.menu-item`
- `.tab`
- `.movie-card`
- `.hero`
- `button:not([disabled])`
- `a[href]`
- `.player-btn`

## Technical Details

### Platform Detection

```javascript
detectTV() {
  const userAgent = navigator.userAgent.toLowerCase();
  return (
    userAgent.includes('tv') ||
    userAgent.includes('aftm') || // Fire TV
    userAgent.includes('aftb') || // Fire TV Stick
    userAgent.includes('googletv') ||
    userAgent.includes('androidtv')
  );
}
```

### Spatial Navigation Algorithm

The navigation system uses a spatial algorithm that:
1. Calculates the position of all focusable elements
2. Finds elements in the requested direction
3. Prioritizes elements that are closer and more aligned
4. Smoothly scrolls the focused element into view

### Video Player Attributes

For mobile and TV compatibility:
```html
<video 
  playsinline
  webkit-playsinline
  preload="auto"
  crossorigin="anonymous"
></video>
```

## Browser Compatibility

### Tested Platforms
- ✅ Fire TV Stick (Silk Browser)
- ✅ Fire TV Stick 4K (Silk Browser)
- ✅ Android TV (Chrome)
- ✅ Google TV (Chrome)
- ✅ Mobile Android (Chrome, Firefox)
- ✅ Mobile iOS (Safari)

### Known Issues
- Some older Smart TV browsers may have limited HTML5 video support
- WebOS and Tizen browsers may require additional testing

## Performance Optimizations

### TV Mode Optimizations
- Disabled hover effects (not applicable on TV)
- Reduced animations for smoother performance
- Optimized font rendering for TV screens
- Hardware-accelerated transforms

### Video Playback
- Progressive loading for faster start times
- Adaptive bitrate streaming support
- Hardware-accelerated decoding when available

## Troubleshooting

### Video Not Playing
1. Check if the video format is supported (H.264/AAC recommended)
2. Ensure transcoding is enabled in settings
3. Try refreshing the page
4. Check network connection

### Navigation Not Working
1. Ensure you're using a compatible browser
2. Try refreshing the page
3. Check if TV mode is detected (body should have `tv-mode` class)

### Focus Not Visible
1. Check if CSS is loaded properly
2. Ensure `tv-mode.css` is included
3. Verify browser supports CSS outline

## Future Enhancements

- [ ] Voice search integration
- [ ] Picture-in-picture mode
- [ ] Multi-profile quick switch
- [ ] Parental controls
- [ ] Continue watching on TV
- [ ] TV-specific recommendations

## Contributing

To add TV support to a new page:

1. Include TV mode CSS:
```html
<link rel="stylesheet" href="../styles/tv-mode.css" />
```

2. Initialize TV navigation:
```javascript
import tvNavigation from '../modules/tv-navigation.js';
tvNavigation.initialize();
```

3. Test with keyboard navigation (arrow keys + Enter)

## Resources

- [Android TV Design Guidelines](https://developer.android.com/design/tv)
- [Fire TV Development](https://developer.amazon.com/fire-tv)
- [HTML5 Video Best Practices](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/video)
