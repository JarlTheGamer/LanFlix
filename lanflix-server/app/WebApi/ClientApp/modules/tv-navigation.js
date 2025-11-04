/**
 * TV Navigation Module
 * Handles remote control navigation for Android TV, Fire TV, and other TV platforms
 * Works exactly like the existing arrow key navigation without any UI scaling
 */

export class TVNavigation {
  constructor() {
    this.isTV = this.detectTV();
    this.navigation = null; // Will be set by the main navigation module
  }

  /**
   * Detect if running on TV platform
   */
  detectTV() {
    const userAgent = navigator.userAgent.toLowerCase();

    // Fire TV specific detection
    const isFireTV = userAgent.includes('aftm') || userAgent.includes('aftb') || userAgent.includes('afts') || userAgent.includes('aftkmst12');

    // General TV detection
    const isTV = (
      isFireTV ||
      userAgent.includes('tv') ||
      userAgent.includes('googletv') ||
      userAgent.includes('androidtv') ||
      userAgent.includes('smarttv') ||
      userAgent.includes('web0s') || // LG webOS
      userAgent.includes('tizen') || // Samsung Tizen
      userAgent.includes('netcast') || // LG NetCast
      // Also detect Android WebView (for Android TV app)
      (userAgent.includes('android') && userAgent.includes('wv'))
    );

    console.log('TV Detection:', { userAgent, isTV, isFireTV });

    // Store Fire TV detection for later use
    this.isFireTV = isFireTV;

    return isTV;
  }

  /**
   * Initialize TV navigation
   */
  initialize(navigationInstance = null) {
    if (!this.isTV) {
      console.log('Not a TV platform, skipping TV navigation');
      return;
    }

    console.log('🎮 TV platform detected - enabling remote control navigation');

    // Store reference to main navigation instance
    this.navigation = navigationInstance;

    // NO custom CSS - use exact same styling as website
    // Just add a class for detection purposes only
    document.body.classList.add('tv-mode');

    // Setup remote control event listeners
    this.setupRemoteControlListeners();

    // Setup gamepad support (some TV remotes register as gamepads)
    this.setupGamepadSupport();
  }

  /**
   * Setup remote control event listeners
   */
  setupRemoteControlListeners() {
    // Listen for all key events and map remote control buttons
    document.addEventListener('keydown', (e) => this.handleRemoteControl(e), true);

    // Also listen for media key events
    document.addEventListener('keyup', (e) => this.handleMediaKeys(e), true);
  }

  /**
   * Handle remote control input
   */
  handleRemoteControl(e) {
    // Fire TV specific key mappings
    const fireTVKeyMap = {
      // Fire TV remote specific codes
      'KEYCODE_DPAD_UP': 'ArrowUp',
      'KEYCODE_DPAD_DOWN': 'ArrowDown',
      'KEYCODE_DPAD_LEFT': 'ArrowLeft',
      'KEYCODE_DPAD_RIGHT': 'ArrowRight',
      'KEYCODE_DPAD_CENTER': 'Enter',
      'KEYCODE_BACK': 'Escape',
      'KEYCODE_MENU': 'm',
      'KEYCODE_MEDIA_PLAY': ' ',
      'KEYCODE_MEDIA_PAUSE': ' ',
      'KEYCODE_MEDIA_PLAY_PAUSE': ' ',
      'KEYCODE_MEDIA_STOP': 'Escape',
      'KEYCODE_MEDIA_FAST_FORWARD': 'ArrowRight',
      'KEYCODE_MEDIA_REWIND': 'ArrowLeft',
      // Additional Fire TV codes
      'KEYCODE_BUTTON_SELECT': 'Enter',
      'KEYCODE_BUTTON_A': 'Enter',
      'KEYCODE_BUTTON_B': 'Escape',
      'KEYCODE_BUTTON_X': ' ',
      'KEYCODE_BUTTON_Y': 'i'
    };

    // Standard remote control mappings
    const remoteKeyMap = {
      // D-pad navigation
      'ArrowUp': 'ArrowUp',
      'ArrowDown': 'ArrowDown',
      'ArrowLeft': 'ArrowLeft',
      'ArrowRight': 'ArrowRight',

      // Center/OK button variations
      'Enter': 'Enter',
      'Select': 'Enter',
      'OK': 'Enter',
      'Accept': 'Enter',
      'Confirm': 'Enter',

      // Back button variations
      'Back': 'Escape',
      'Escape': 'Escape',
      'Backspace': 'Escape',
      'Cancel': 'Escape',

      // Media keys
      'MediaPlay': ' ',
      'MediaPause': ' ',
      'MediaPlayPause': ' ',
      'MediaStop': 'Escape',
      'MediaTrackNext': 'ArrowRight',
      'MediaTrackPrevious': 'ArrowLeft',
      'MediaFastForward': 'ArrowRight',
      'MediaRewind': 'ArrowLeft',

      // Number keys (for direct navigation)
      'Digit0': '0', 'Digit1': '1', 'Digit2': '2', 'Digit3': '3', 'Digit4': '4',
      'Digit5': '5', 'Digit6': '6', 'Digit7': '7', 'Digit8': '8', 'Digit9': '9',
      'Numpad0': '0', 'Numpad1': '1', 'Numpad2': '2', 'Numpad3': '3', 'Numpad4': '4',
      'Numpad5': '5', 'Numpad6': '6', 'Numpad7': '7', 'Numpad8': '8', 'Numpad9': '9',

      // Color buttons (common on TV remotes)
      'ColorF0Red': 'r',
      'ColorF1Green': 'g',
      'ColorF2Yellow': 'y',
      'ColorF3Blue': 'b',
      'Red': 'r',
      'Green': 'g',
      'Yellow': 'y',
      'Blue': 'b',

      // Menu/Options variations
      'Menu': 'm',
      'Options': 'o',
      'Info': 'i',
      'Guide': 'g',
      'Home': 'h',
      'Settings': 's',
      'ContextMenu': 'm',

      // Additional TV remote buttons
      'ChannelUp': 'ArrowUp',
      'ChannelDown': 'ArrowDown',
      'VolumeUp': '+',
      'VolumeDown': '-',
      'VolumeMute': 'm',
      'Power': 'p',
      'Exit': 'Escape',
      'Last': 'Escape',
      'List': 'l',
      'Subtitle': 's',
      'Audio': 'a',
      'Zoom': 'z',
      'Record': 'r',
      'Pause': ' ',
      'Play': ' ',
      'Stop': 'Escape',
      'Rewind': 'ArrowLeft',
      'FastForward': 'ArrowRight'
    };

    // Android TV specific mappings
    const androidTVKeyMap = {
      'DPAD_UP': 'ArrowUp',
      'DPAD_DOWN': 'ArrowDown',
      'DPAD_LEFT': 'ArrowLeft',
      'DPAD_RIGHT': 'ArrowRight',
      'DPAD_CENTER': 'Enter',
      'BUTTON_A': 'Enter',
      'BUTTON_B': 'Escape',
      'BUTTON_X': ' ',
      'BUTTON_Y': 'i',
      'BUTTON_SELECT': 'Enter',
      'BUTTON_START': 'm'
    };

    // Try different mapping strategies
    let mappedKey = null;

    // 1. Fire TV specific mapping first if on Fire TV
    if (this.isFireTV) {
      mappedKey = fireTVKeyMap[e.code] || fireTVKeyMap[e.key];
    }

    // 2. Android TV mapping
    if (!mappedKey) {
      mappedKey = androidTVKeyMap[e.code] || androidTVKeyMap[e.key];
    }

    // 3. Standard remote mapping
    if (!mappedKey) {
      mappedKey = remoteKeyMap[e.key] || remoteKeyMap[e.code];
    }

    // 4. Handle special cases for different TV platforms
    if (!mappedKey) {
      // Samsung Tizen TV
      if (e.key.startsWith('ColorF')) {
        const colorMap = { 'ColorF0Red': 'r', 'ColorF1Green': 'g', 'ColorF2Yellow': 'y', 'ColorF3Blue': 'b' };
        mappedKey = colorMap[e.key];
      }
      // LG webOS TV
      else if (e.key === 'Return') {
        mappedKey = 'Escape';
      }
      // Generic gamepad buttons
      else if (e.key.startsWith('Gamepad')) {
        const gamepadMap = {
          'GamepadButton0': 'Enter',  // A button
          'GamepadButton1': 'Escape', // B button
          'GamepadButton2': ' ',      // X button
          'GamepadButton3': 'i',      // Y button
          'GamepadButton12': 'ArrowUp',    // D-pad up
          'GamepadButton13': 'ArrowDown',  // D-pad down
          'GamepadButton14': 'ArrowLeft',  // D-pad left
          'GamepadButton15': 'ArrowRight'  // D-pad right
        };
        mappedKey = gamepadMap[e.key];
      }
    }

    if (mappedKey) {
      // Prevent the original event
      e.preventDefault();
      e.stopPropagation();

      console.log(`🎮 Remote control: ${e.key}/${e.code} -> ${mappedKey}`);

      // Add visual feedback for button press
      this.showRemoteButtonFeedback(mappedKey);

      // Create a synthetic keyboard event that the existing navigation will handle
      const syntheticEvent = new KeyboardEvent('keydown', {
        key: mappedKey,
        code: mappedKey,
        bubbles: true,
        cancelable: true,
        composed: true
      });

      // Dispatch to the existing navigation system
      document.dispatchEvent(syntheticEvent);
    } else {
      // Log unmapped keys for debugging
      console.log(`🎮 Unmapped remote key: ${e.key}/${e.code}`);
    }
  }

  /**
   * Show visual feedback for remote button presses
   */
  showRemoteButtonFeedback(key) {
    // Create or update feedback indicator
    let indicator = document.getElementById('remote-feedback');
    if (!indicator) {
      indicator = document.createElement('div');
      indicator.id = 'remote-feedback';
      indicator.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: rgba(0, 0, 0, 0.8);
        color: white;
        padding: 8px 16px;
        border-radius: 20px;
        font-size: 14px;
        font-weight: 600;
        z-index: 10000;
        opacity: 0;
        transition: opacity 0.2s ease;
        pointer-events: none;
        font-family: monospace;
      `;
      document.body.appendChild(indicator);
    }

    // Show the key that was pressed
    const keyNames = {
      'ArrowUp': '↑',
      'ArrowDown': '↓',
      'ArrowLeft': '←',
      'ArrowRight': '→',
      'Enter': 'OK',
      'Escape': 'Back',
      ' ': 'Play/Pause',
      'm': 'Menu',
      'i': 'Info',
      'h': 'Home',
      's': 'Settings'
    };

    indicator.textContent = keyNames[key] || key;
    indicator.style.opacity = '1';

    // Hide after a short delay
    clearTimeout(this.feedbackTimeout);
    this.feedbackTimeout = setTimeout(() => {
      indicator.style.opacity = '0';
    }, 1000);
  }

  /**
   * Handle media key events
   */
  handleMediaKeys(e) {
    // Handle media keys that might need special processing
    switch (e.key) {
      case 'MediaPlay':
      case 'MediaPause':
      case 'MediaPlayPause':
        // If we're on a video player page, let it handle media keys
        const videoPlayer = document.querySelector('video');
        if (videoPlayer) {
          if (e.key === 'MediaPlay' || (e.key === 'MediaPlayPause' && videoPlayer.paused)) {
            videoPlayer.play();
          } else if (e.key === 'MediaPause' || (e.key === 'MediaPlayPause' && !videoPlayer.paused)) {
            videoPlayer.pause();
          }
          e.preventDefault();
        }
        break;

      case 'MediaStop':
        // Stop video and go back
        const video = document.querySelector('video');
        if (video) {
          video.pause();
          video.currentTime = 0;
        }
        // Trigger back navigation
        window.history.back();
        e.preventDefault();
        break;

      case 'Home':
        // Go to home page
        window.location.href = '/';
        e.preventDefault();
        break;
    }
  }

  /**
   * Setup gamepad support for TV remotes that register as gamepads
   */
  setupGamepadSupport() {
    let gamepadIndex = -1;

    // Check for gamepad connection
    window.addEventListener('gamepadconnected', (e) => {
      console.log('Gamepad connected:', e.gamepad.id);
      gamepadIndex = e.gamepad.index;
      this.startGamepadPolling(gamepadIndex);
    });

    window.addEventListener('gamepaddisconnected', (e) => {
      console.log('Gamepad disconnected:', e.gamepad.id);
      gamepadIndex = -1;
    });
  }

  /**
   * Poll gamepad for input (for remotes that register as gamepads)
   */
  startGamepadPolling(gamepadIndex) {
    let lastButtons = [];
    let lastAxes = [];

    const poll = () => {
      if (gamepadIndex === -1) return;

      const gamepad = navigator.getGamepads()[gamepadIndex];
      if (!gamepad) return;

      // Check buttons
      gamepad.buttons.forEach((button, index) => {
        if (button.pressed && !lastButtons[index]) {
          this.handleGamepadButton(index);
        }
        lastButtons[index] = button.pressed;
      });

      // Check axes (for D-pad on some remotes)
      gamepad.axes.forEach((axis, index) => {
        const threshold = 0.5;
        const lastAxis = lastAxes[index] || 0;

        if (Math.abs(axis) > threshold && Math.abs(lastAxis) <= threshold) {
          this.handleGamepadAxis(index, axis);
        }
        lastAxes[index] = axis;
      });

      requestAnimationFrame(poll);
    };

    poll();
  }

  /**
   * Handle gamepad button press
   */
  handleGamepadButton(buttonIndex) {
    // Map common gamepad buttons to keyboard events
    const buttonMap = {
      0: 'Enter',       // A button / OK / Select
      1: 'Escape',      // B button / Back / Cancel
      2: ' ',           // X button / Play/Pause
      3: 'i',           // Y button / Info
      4: 'ArrowLeft',   // Left shoulder (L1) - Previous
      5: 'ArrowRight',  // Right shoulder (R1) - Next
      6: '-',           // Left trigger (L2) - Volume down
      7: '+',           // Right trigger (R2) - Volume up
      8: 'Escape',      // Select / Back
      9: 'm',           // Start / Menu
      10: ' ',          // Left stick click - Play/Pause
      11: 'i',          // Right stick click - Info
      12: 'ArrowUp',    // D-pad up
      13: 'ArrowDown',  // D-pad down
      14: 'ArrowLeft',  // D-pad left
      15: 'ArrowRight', // D-pad right
      16: 'h',          // Home button (Xbox guide, PS button)
      17: 's'           // Share/Options button
    };

    const mappedKey = buttonMap[buttonIndex];
    if (mappedKey) {
      console.log(`🎮 Gamepad button ${buttonIndex} -> ${mappedKey}`);
      
      // Show visual feedback
      this.showRemoteButtonFeedback(mappedKey);

      const syntheticEvent = new KeyboardEvent('keydown', {
        key: mappedKey,
        code: mappedKey,
        bubbles: true,
        cancelable: true,
        composed: true
      });

      document.dispatchEvent(syntheticEvent);
    } else {
      console.log(`🎮 Unmapped gamepad button: ${buttonIndex}`);
    }
  }

  /**
   * Handle gamepad axis movement (D-pad on some remotes)
   */
  handleGamepadAxis(axisIndex, value) {
    let key = null;

    if (axisIndex === 0) { // Horizontal axis
      key = value > 0 ? 'ArrowRight' : 'ArrowLeft';
    } else if (axisIndex === 1) { // Vertical axis
      key = value > 0 ? 'ArrowDown' : 'ArrowUp';
    }

    if (key) {
      const syntheticEvent = new KeyboardEvent('keydown', {
        key: key,
        code: key,
        bubbles: true,
        cancelable: true,
        composed: true
      });

      document.dispatchEvent(syntheticEvent);
    }
  }



  /**
   * Refresh method for compatibility
   */
  refresh() {
    // This method exists for compatibility with the old TV navigation
    // The new system doesn't need manual refresh as it uses the existing navigation
  }
}

export default new TVNavigation();