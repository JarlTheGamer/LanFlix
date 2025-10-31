/**
 * TV Navigation Module
 * Handles D-pad navigation for Android TV, Fire TV, and other TV platforms
 */

export class TVNavigation {
  constructor() {
    this.isTV = this.detectTV();
    this.focusedElement = null;
    this.focusableElements = [];
    this.currentIndex = 0;
    
    // Grid navigation state
    this.gridRows = [];
    this.currentRow = 0;
    this.currentCol = 0;
  }

  /**
   * Detect if running on TV platform
   */
  detectTV() {
    const userAgent = navigator.userAgent.toLowerCase();
    return (
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
      userAgent.includes('netcast') // LG NetCast
    );
  }

  /**
   * Initialize TV navigation
   */
  initialize() {
    if (!this.isTV) {
      console.log('Not a TV platform, skipping TV navigation');
      return;
    }

    console.log('🎮 TV platform detected - enabling D-pad navigation');
    document.body.classList.add('tv-mode');

    // Setup keyboard event listener for D-pad
    document.addEventListener('keydown', (e) => this.handleDPad(e));

    // Initial focus
    this.updateFocusableElements();
    this.focusFirst();
  }

  /**
   * Update list of focusable elements
   */
  updateFocusableElements() {
    // Get all interactive elements
    const selectors = [
      '.menu-item',
      '.tab',
      '.movie-card',
      '.hero',
      '.profile',
      '.settings-btn',
      '.notifications-btn',
      'button:not([disabled])',
      'a[href]',
      'input[type="text"]',
      'input[type="url"]',
      'input[type="checkbox"]',
      'select',
      '.setting-input',
      '.player-btn'
    ];

    this.focusableElements = Array.from(
      document.querySelectorAll(selectors.join(','))
    ).filter(el => {
      // Filter out hidden elements
      const style = window.getComputedStyle(el);
      return style.display !== 'none' && style.visibility !== 'hidden';
    });
  }

  /**
   * Focus first element
   */
  focusFirst() {
    this.updateFocusableElements();
    if (this.focusableElements.length > 0) {
      this.setFocus(0);
    }
  }

  /**
   * Set focus to element at index
   */
  setFocus(index) {
    // Remove previous focus
    if (this.focusedElement) {
      this.focusedElement.classList.remove('tv-focused');
    }

    // Set new focus
    this.currentIndex = Math.max(0, Math.min(index, this.focusableElements.length - 1));
    this.focusedElement = this.focusableElements[this.currentIndex];

    if (this.focusedElement) {
      this.focusedElement.classList.add('tv-focused');
      this.focusedElement.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
        inline: 'center'
      });
    }
  }

  /**
   * Handle D-pad input
   */
  handleDPad(e) {
    // Update focusable elements on each keypress (in case DOM changed)
    this.updateFocusableElements();

    const key = e.key;
    
    // Prevent default scrolling
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter'].includes(key)) {
      e.preventDefault();
    }

    switch (key) {
      case 'ArrowUp':
        this.navigateUp();
        break;
      case 'ArrowDown':
        this.navigateDown();
        break;
      case 'ArrowLeft':
        this.navigateLeft();
        break;
      case 'ArrowRight':
        this.navigateRight();
        break;
      case 'Enter':
        this.activateFocused();
        break;
      case 'Escape':
      case 'Back':
        this.handleBack();
        break;
    }
  }

  /**
   * Navigate up
   */
  navigateUp() {
    const current = this.focusedElement;
    if (!current) {
      this.focusFirst();
      return;
    }

    const currentRect = current.getBoundingClientRect();
    let bestMatch = null;
    let bestDistance = Infinity;

    // Find closest element above
    this.focusableElements.forEach(el => {
      if (el === current) return;
      
      const rect = el.getBoundingClientRect();
      
      // Must be above current element
      if (rect.bottom <= currentRect.top) {
        // Calculate distance (prefer elements in same column)
        const verticalDistance = currentRect.top - rect.bottom;
        const horizontalDistance = Math.abs(rect.left - currentRect.left);
        const distance = verticalDistance + horizontalDistance * 0.5;
        
        if (distance < bestDistance) {
          bestDistance = distance;
          bestMatch = el;
        }
      }
    });

    if (bestMatch) {
      const index = this.focusableElements.indexOf(bestMatch);
      this.setFocus(index);
    }
  }

  /**
   * Navigate down
   */
  navigateDown() {
    const current = this.focusedElement;
    if (!current) {
      this.focusFirst();
      return;
    }

    const currentRect = current.getBoundingClientRect();
    let bestMatch = null;
    let bestDistance = Infinity;

    // Find closest element below
    this.focusableElements.forEach(el => {
      if (el === current) return;
      
      const rect = el.getBoundingClientRect();
      
      // Must be below current element
      if (rect.top >= currentRect.bottom) {
        // Calculate distance (prefer elements in same column)
        const verticalDistance = rect.top - currentRect.bottom;
        const horizontalDistance = Math.abs(rect.left - currentRect.left);
        const distance = verticalDistance + horizontalDistance * 0.5;
        
        if (distance < bestDistance) {
          bestDistance = distance;
          bestMatch = el;
        }
      }
    });

    if (bestMatch) {
      const index = this.focusableElements.indexOf(bestMatch);
      this.setFocus(index);
    }
  }

  /**
   * Navigate left
   */
  navigateLeft() {
    const current = this.focusedElement;
    if (!current) {
      this.focusFirst();
      return;
    }

    const currentRect = current.getBoundingClientRect();
    let bestMatch = null;
    let bestDistance = Infinity;

    // Find closest element to the left
    this.focusableElements.forEach(el => {
      if (el === current) return;
      
      const rect = el.getBoundingClientRect();
      
      // Must be to the left of current element
      if (rect.right <= currentRect.left) {
        // Calculate distance (prefer elements in same row)
        const horizontalDistance = currentRect.left - rect.right;
        const verticalDistance = Math.abs(rect.top - currentRect.top);
        const distance = horizontalDistance + verticalDistance * 0.5;
        
        if (distance < bestDistance) {
          bestDistance = distance;
          bestMatch = el;
        }
      }
    });

    if (bestMatch) {
      const index = this.focusableElements.indexOf(bestMatch);
      this.setFocus(index);
    }
  }

  /**
   * Navigate right
   */
  navigateRight() {
    const current = this.focusedElement;
    if (!current) {
      this.focusFirst();
      return;
    }

    const currentRect = current.getBoundingClientRect();
    let bestMatch = null;
    let bestDistance = Infinity;

    // Find closest element to the right
    this.focusableElements.forEach(el => {
      if (el === current) return;
      
      const rect = el.getBoundingClientRect();
      
      // Must be to the right of current element
      if (rect.left >= currentRect.right) {
        // Calculate distance (prefer elements in same row)
        const horizontalDistance = rect.left - currentRect.right;
        const verticalDistance = Math.abs(rect.top - currentRect.top);
        const distance = horizontalDistance + verticalDistance * 0.5;
        
        if (distance < bestDistance) {
          bestDistance = distance;
          bestMatch = el;
        }
      }
    });

    if (bestMatch) {
      const index = this.focusableElements.indexOf(bestMatch);
      this.setFocus(index);
    }
  }

  /**
   * Activate focused element (Enter key)
   */
  activateFocused() {
    if (!this.focusedElement) return;

    // Handle input fields differently
    if (this.focusedElement.tagName === 'INPUT' || this.focusedElement.tagName === 'SELECT') {
      // For text inputs, focus them so user can type
      this.focusedElement.focus();
      
      // For checkboxes, toggle them
      if (this.focusedElement.type === 'checkbox') {
        this.focusedElement.checked = !this.focusedElement.checked;
        this.focusedElement.dispatchEvent(new Event('change', { bubbles: true }));
      }
      return;
    }

    // Trigger click event for buttons and links
    this.focusedElement.click();
  }

  /**
   * Handle back button
   */
  handleBack() {
    // Check if we're in a modal or overlay
    const modal = document.querySelector('.content-modal.active');
    if (modal) {
      const closeBtn = modal.querySelector('.close-modal');
      if (closeBtn) {
        closeBtn.click();
        return;
      }
    }

    // Otherwise go back in history
    if (window.history.length > 1) {
      window.history.back();
    }
  }

  /**
   * Refresh focusable elements (call after DOM changes)
   */
  refresh() {
    this.updateFocusableElements();
    
    // Try to maintain focus on same element
    if (this.focusedElement && this.focusableElements.includes(this.focusedElement)) {
      const index = this.focusableElements.indexOf(this.focusedElement);
      this.setFocus(index);
    } else {
      this.focusFirst();
    }
  }
}

export default new TVNavigation();
