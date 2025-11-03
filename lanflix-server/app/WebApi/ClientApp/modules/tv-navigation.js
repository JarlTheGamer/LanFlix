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
    const isTV = (
      userAgent.includes('tv') ||
      userAgent.includes('aftm') || // Fire TV
      userAgent.includes('aftb') || // Fire TV Stick
      userAgent.includes('afts') || // Fire TV Stick 4K
      userAgent.includes('aftkmst12') || // Fire TV Stick Lite
      userAgent.includes('googletv') ||
      userAgent.includes('androidtv') ||
      userAgent.includes('smarttv') ||
      userAgent.includes('web0s') || // LG webOS
      userAgent.includes('tizen') || // Samsung Tizen
      userAgent.includes('netcast') || // LG NetCast
      // Also detect Android WebView (for Android TV app)
      (userAgent.includes('android') && userAgent.includes('wv'))
    );
    
    console.log('TV Detection:', { userAgent, isTV });
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
    // Map remote control buttons to standard keyboard events
    const remoteKeyMap = {
      // D-pad navigation
      'ArrowUp': 'ArrowUp',
      'ArrowDown': 'ArrowDown', 
      'ArrowLeft': 'ArrowLeft',
      'ArrowRight': 'ArrowRight',
      
      // Center/OK button
      'Enter': 'Enter',
      'Select': 'Enter',
      'OK': 'Enter',
      
      // Back button
      'Back': 'Escape',
      'Escape': 'Escape',
      'Backspace': 'Escape',
      
      // Media keys
      'MediaPlay': ' ',
      'MediaPause': ' ',
      'MediaPlayPause': ' ',
      'MediaStop': 'Escape',
      'MediaTrackNext': 'ArrowRight',
      'MediaTrackPrevious': 'ArrowLeft',
      
      // Number keys (for direct navigation)
      'Digit0': '0', 'Digit1': '1', 'Digit2': '2', 'Digit3': '3', 'Digit4': '4',
      'Digit5': '5', 'Digit6': '6', 'Digit7': '7', 'Digit8': '8', 'Digit9': '9',
      
      // Color buttons (common on TV remotes)
      'ColorF0Red': 'r',
      'ColorF1Green': 'g', 
      'ColorF2Yellow': 'y',
      'ColorF3Blue': 'b',
      
      // Menu/Options
      'Menu': 'm',
      'Options': 'o',
      'Info': 'i',
      'Guide': 'g',
      'Home': 'h'
    };

    const mappedKey = remoteKeyMap[e.key] || remoteKeyMap[e.code];
    
    if (mappedKey) {
      // Prevent the original event
      e.preventDefault();
      e.stopPropagation();
      
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
    }
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
      0: 'Enter',    // A button / OK
      1: 'Escape',   // B button / Back
      2: ' ',        // X button / Play/Pause
      3: 'i',        // Y button / Info
      12: 'ArrowUp',    // D-pad up
      13: 'ArrowDown',  // D-pad down
      14: 'ArrowLeft',  // D-pad left
      15: 'ArrowRight', // D-pad right
      8: 'Escape',   // Select / Back
      9: 'm',        // Start / Menu
      16: 'h'        // Home button
    };
    
    const mappedKey = buttonMap[buttonIndex];
    if (mappedKey) {
      const syntheticEvent = new KeyboardEvent('keydown', {
        key: mappedKey,
        code: mappedKey,
        bubbles: true,
        cancelable: true,
        composed: true
      });
      
      document.dispatchEvent(syntheticEvent);
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