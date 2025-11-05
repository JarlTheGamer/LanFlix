/**
 * Player Navigation Module for TV Remote Control
 * Handles TV remote navigation for the video player interface
 */

export class PlayerNavigation {
  constructor(videoPlayer) {
    this.videoPlayer = videoPlayer;

    // Navigation state
    this.focusedElement = 'play-pause'; // 'play-pause', 'skip-back', 'skip-forward', 'volume', 'progress', 'fullscreen', 'back'
    this.isControlsVisible = false;

    // Detect TV platform
    this.isAndroidTV = this.detectAndroidTV();
    this.isFireTV = this.detectFireTV();
    this.isTVPlatform = this.isAndroidTV || this.isFireTV;

    // Control elements mapping
    this.controlElements = {
      'back': '.back-button',
      'play-pause': '.play-pause-btn',
      'skip-back': '.skip-back-btn',
      'skip-forward': '.skip-forward-btn',
      'volume': '.volume-btn',
      'progress': '.player-progress-container',
      'fullscreen': '.fullscreen-btn'
    };

    // Control order for navigation
    this.controlOrder = ['play-pause', 'skip-back', 'skip-forward', 'volume', 'progress', 'fullscreen'];
    this.currentControlIndex = 0;
  }

  /**
   * Detect Android TV platform
   */
  detectAndroidTV() {
    const userAgent = navigator.userAgent.toLowerCase();
    return (
      userAgent.includes('tv') ||
      userAgent.includes('googletv') ||
      userAgent.includes('androidtv') ||
      userAgent.includes('smarttv') ||
      userAgent.includes('web0s') || // LG webOS
      userAgent.includes('tizen') || // Samsung Tizen
      userAgent.includes('netcast') || // LG NetCast
      (userAgent.includes('android') && userAgent.includes('wv'))
    );
  }

  /**
   * Detect Fire TV platform
   */
  detectFireTV() {
    const userAgent = navigator.userAgent.toLowerCase();
    return userAgent.includes('aftm') || userAgent.includes('aftb') ||
      userAgent.includes('afts') || userAgent.includes('aftkmst12');
  }

  /**
   * Initialize TV navigation
   */
  initialize() {
    if (!this.isTVPlatform) {
      console.log('📱 Non-TV platform detected - skipping player TV navigation');
      return;
    }

    console.log('🎮 TV platform detected - enabling player remote control navigation');

    // Add TV mode class
    document.body.classList.add('tv-mode');

    // Setup event listeners
    this.setupEventListeners();

    // Initialize focus
    this.updateFocus();
  }

  /**
   * Setup event listeners for TV navigation
   */
  setupEventListeners() {
    // Listen for keyboard events (including mapped remote control events)
    document.addEventListener('keydown', (e) => this.handleKeyboard(e), true);

    // Listen for controls visibility changes
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const controls = document.querySelector('.player-controls');
          if (controls) {
            const wasVisible = this.isControlsVisible;
            this.isControlsVisible = controls.classList.contains('visible');

            if (!wasVisible && this.isControlsVisible) {
              // Controls became visible - set initial focus
              this.focusedElement = 'play-pause';
              this.currentControlIndex = 0;
              this.updateFocus();
            }
          }
        }
      });
    });

    // Wait for controls to be created, then observe
    const waitForControls = () => {
      const controls = document.querySelector('.player-controls');
      if (controls) {
        observer.observe(controls, { attributes: true });
      } else {
        // Retry after a short delay
        setTimeout(waitForControls, 100);
      }
    };

    waitForControls();
  }

  /**
   * Handle keyboard input for TV navigation
   */
  handleKeyboard(e) {
    // Map remote control keys to standard keys (handled by main navigation)

    // Handle player-specific navigation
    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();
        this.handleArrowUp();
        break;
      case 'ArrowDown':
        e.preventDefault();
        this.handleArrowDown();
        break;
      case 'ArrowLeft':
        e.preventDefault();
        this.handleArrowLeft();
        break;
      case 'ArrowRight':
        e.preventDefault();
        this.handleArrowRight();
        break;
      case 'Enter':
        e.preventDefault();
        this.handleEnter();
        break;
      case 'Escape':
        e.preventDefault();
        this.handleEscape();
        break;
      case ' ': // Space bar
        e.preventDefault();
        this.videoPlayer.togglePlayPause();
        this.showControlsTemporarily();
        break;
    }
  }

  /**
   * Handle up arrow - show controls or navigate to back button
   */
  handleArrowUp() {
    if (!this.isControlsVisible) {
      this.showControlsTemporarily();
      return;
    }

    // If controls are visible, navigate to back button
    this.focusedElement = 'back';
    this.updateFocus();
  }

  /**
   * Handle down arrow - show controls or hide them
   */
  handleArrowDown() {
    if (!this.isControlsVisible) {
      this.showControlsTemporarily();
      return;
    }

    // If on back button, go to controls
    if (this.focusedElement === 'back') {
      this.focusedElement = 'play-pause';
      this.currentControlIndex = 0;
      this.updateFocus();
      return;
    }

    // Hide controls if already visible and focused on controls
    this.hideControls();
  }

  /**
   * Handle left arrow - navigate controls or seek backward
   */
  handleArrowLeft() {
    if (!this.isControlsVisible) {
      // Seek backward when controls are hidden
      this.videoPlayer.seek(this.videoPlayer.currentTime - 10);
      this.showControlsTemporarily();
      return;
    }

    if (this.focusedElement === 'back') {
      return; // Can't go left from back button
    }

    if (this.focusedElement === 'progress') {
      // Seek backward when progress bar is focused
      this.videoPlayer.seek(this.videoPlayer.currentTime - 10);
      return;
    }

    // Navigate to previous control
    this.currentControlIndex = Math.max(0, this.currentControlIndex - 1);
    this.focusedElement = this.controlOrder[this.currentControlIndex];
    this.updateFocus();
  }

  /**
   * Handle right arrow - navigate controls or seek forward
   */
  handleArrowRight() {
    if (!this.isControlsVisible) {
      // Seek forward when controls are hidden
      this.videoPlayer.seek(this.videoPlayer.currentTime + 10);
      this.showControlsTemporarily();
      return;
    }

    if (this.focusedElement === 'back') {
      return; // Can't go right from back button
    }

    if (this.focusedElement === 'progress') {
      // Seek forward when progress bar is focused
      this.videoPlayer.seek(this.videoPlayer.currentTime + 10);
      return;
    }

    // Navigate to next control
    this.currentControlIndex = Math.min(this.controlOrder.length - 1, this.currentControlIndex + 1);
    this.focusedElement = this.controlOrder[this.currentControlIndex];
    this.updateFocus();
  }

  /**
   * Handle enter key - activate focused control
   */
  handleEnter() {
    if (!this.isControlsVisible) {
      this.showControlsTemporarily();
      return;
    }

    const element = document.querySelector(this.controlElements[this.focusedElement]);
    if (element) {
      element.click();
    }
  }

  /**
   * Handle escape key - hide controls or go back
   */
  handleEscape() {
    if (this.isControlsVisible) {
      this.hideControls();
    } else {
      // Go back to previous page
      const backButton = document.querySelector('.back-button');
      if (backButton) {
        backButton.click();
      }
    }
  }

  /**
   * Show controls temporarily
   */
  showControlsTemporarily() {
    this.videoPlayer.showControls();
    this.focusedElement = 'play-pause';
    this.currentControlIndex = 0;
    this.updateFocus();
  }

  /**
   * Hide controls
   */
  hideControls() {
    this.videoPlayer.hideControls();
    this.clearFocus();
  }

  /**
   * Update focus styling
   */
  updateFocus() {
    // Clear all existing focus
    this.clearFocus();

    if (!this.isControlsVisible && this.focusedElement !== 'back') {
      return;
    }

    // Add focus to current element
    const selector = this.controlElements[this.focusedElement];
    if (!selector) return;

    const element = document.querySelector(selector);
    if (element) {
      element.classList.add('tv-focused');

      // Special handling for progress bar
      if (this.focusedElement === 'progress') {
        const progressWrapper = element.querySelector('.player-progress-wrapper');
        if (progressWrapper) {
          progressWrapper.classList.add('tv-focused');
        }
      }
    }
  }

  /**
   * Clear all focus styling
   */
  clearFocus() {
    document.querySelectorAll('.tv-focused').forEach(el => {
      el.classList.remove('tv-focused');
    });
  }

  /**
   * Handle controls visibility change
   */
  onControlsVisibilityChange(visible) {
    this.isControlsVisible = visible;

    if (visible) {
      this.focusedElement = 'play-pause';
      this.currentControlIndex = 0;
      this.updateFocus();
    } else {
      this.clearFocus();
    }
  }

  /**
   * Destroy navigation
   */
  destroy() {
    this.clearFocus();
    document.body.classList.remove('tv-mode');
  }
}

export default PlayerNavigation;