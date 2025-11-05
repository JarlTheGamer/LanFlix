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

    // Add TV mode class and platform-specific classes
    document.body.classList.add('tv-mode');

    if (this.isFireTV) {
      document.body.classList.add('fire-tv');
    } else if (this.detectAndroidTV()) {
      document.body.classList.add('android-tv');
    }

    // Setup remote control event listeners
    this.setupRemoteControlListeners();

    // Setup gamepad support (some TV remotes register as gamepads)
    this.setupGamepadSupport();

    // Setup focus management for better TV experience
    this.setupFocusManagement();

    // Setup scroll management for carousels
    this.setupScrollManagement();
  }

  /**
   * Setup remote control event listeners
   */
  setupRemoteControlListeners() {
    // Listen for all key events and map remote control buttons
    document.addEventListener('keydown', (e) => {
      // Try enhanced handler first
      if (!this.handleEnhancedRemoteControl(e)) {
        // Fall back to original handler
        this.handleRemoteControl(e);
      }
    }, true);

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
      'KEYCODE_MEDIA_REWIND': 'ArrowLeft'
    };

    // Standard remote control mappings
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

    // Try Fire TV specific mapping first if on Fire TV
    let mappedKey = null;
    if (this.isFireTV) {
      mappedKey = fireTVKeyMap[e.code] || fireTVKeyMap[e.key];
    }

    // Fall back to standard mapping
    if (!mappedKey) {
      mappedKey = remoteKeyMap[e.key] || remoteKeyMap[e.code];
    }

    if (mappedKey) {
      // Prevent the original event
      e.preventDefault();
      e.stopPropagation();

      console.log(`🎮 Remote control: ${e.key}/${e.code} -> ${mappedKey}`);

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
   * Detect Android TV specifically
   */
  detectAndroidTV() {
    const userAgent = navigator.userAgent.toLowerCase();
    return userAgent.includes('android') &&
      (userAgent.includes('tv') || userAgent.includes('wv'));
  }

  /**
   * Setup focus management for better TV experience
   */
  setupFocusManagement() {
    // Ensure focused elements are always visible
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const element = mutation.target;
          if (element.classList.contains('focused')) {
            this.ensureElementVisible(element);
          }
        }
      });
    });

    // Observe all elements for class changes
    observer.observe(document.body, {
      attributes: true,
      subtree: true,
      attributeFilter: ['class']
    });
  }

  /**
   * Ensure focused element is visible on screen
   */
  ensureElementVisible(element) {
    if (!element) return;

    // Use smooth scrolling to bring element into view
    element.scrollIntoView({
      behavior: 'smooth',
      block: 'nearest',
      inline: 'nearest'
    });

    // For carousel items, also scroll the carousel container
    if (element.classList.contains('movie-card')) {
      const carousel = element.closest('.carousel-row');
      if (carousel) {
        this.scrollCarouselToElement(carousel, element);
      }
    }
  }

  /**
   * Setup scroll management for carousels
   */
  setupScrollManagement() {
    // Improve carousel scrolling behavior
    const carousels = document.querySelectorAll('.carousel-row');
    carousels.forEach(carousel => {
      // Ensure smooth scrolling
      carousel.style.scrollBehavior = 'smooth';

      // Add scroll snap for better navigation
      carousel.style.scrollSnapType = 'x mandatory';

      // Ensure cards snap properly
      const cards = carousel.querySelectorAll('.movie-card');
      cards.forEach(card => {
        card.style.scrollSnapAlign = 'start';
        card.style.scrollSnapStop = 'normal';
      });
    });
  }

  /**
   * Scroll carousel to show focused element
   */
  scrollCarouselToElement(carousel, element) {
    if (!carousel || !element) return;

    const carouselRect = carousel.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();

    // Calculate if element is outside visible area
    const isLeftOutside = elementRect.left < carouselRect.left;
    const isRightOutside = elementRect.right > carouselRect.right;

    if (isLeftOutside || isRightOutside) {
      // Calculate scroll position to center the element
      const elementCenter = element.offsetLeft + (element.offsetWidth / 2);
      const carouselCenter = carousel.offsetWidth / 2;
      const scrollPosition = elementCenter - carouselCenter;

      carousel.scrollTo({
        left: Math.max(0, scrollPosition),
        behavior: 'smooth'
      });
    }
  }

  /**
   * Enhanced remote control handling with better key mapping
   */
  handleEnhancedRemoteControl(e) {
    // Enhanced key mappings for better TV experience
    const enhancedKeyMap = {
      // Standard navigation
      'ArrowUp': 'ArrowUp',
      'ArrowDown': 'ArrowDown',
      'ArrowLeft': 'ArrowLeft',
      'ArrowRight': 'ArrowRight',
      'Enter': 'Enter',
      'Escape': 'Escape',

      // Fire TV specific
      'KEYCODE_DPAD_UP': 'ArrowUp',
      'KEYCODE_DPAD_DOWN': 'ArrowDown',
      'KEYCODE_DPAD_LEFT': 'ArrowLeft',
      'KEYCODE_DPAD_RIGHT': 'ArrowRight',
      'KEYCODE_DPAD_CENTER': 'Enter',
      'KEYCODE_BACK': 'Escape',

      // Media controls
      'MediaPlayPause': ' ',
      'MediaPlay': ' ',
      'MediaPause': ' ',
      'MediaStop': 'Escape',
      'MediaTrackNext': 'ArrowRight',
      'MediaTrackPrevious': 'ArrowLeft',

      // Menu controls
      'Menu': 'm',
      'Home': 'h',
      'Back': 'Escape',

      // Number keys for quick navigation
      'Digit1': '1', 'Digit2': '2', 'Digit3': '3',
      'Digit4': '4', 'Digit5': '5', 'Digit6': '6',
      'Digit7': '7', 'Digit8': '8', 'Digit9': '9', 'Digit0': '0'
    };

    const mappedKey = enhancedKeyMap[e.key] || enhancedKeyMap[e.code];

    if (mappedKey) {
      e.preventDefault();
      e.stopPropagation();

      console.log(`🎮 Enhanced remote: ${e.key}/${e.code} -> ${mappedKey}`);

      // Create synthetic event
      const syntheticEvent = new KeyboardEvent('keydown', {
        key: mappedKey,
        code: mappedKey,
        bubbles: true,
        cancelable: true,
        composed: true
      });

      document.dispatchEvent(syntheticEvent);
      return true;
    }

    return false;
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