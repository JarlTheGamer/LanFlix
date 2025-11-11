/**
 * App Updater Module
 * Checks for and installs app updates
 */

export class AppUpdater {
  constructor() {
    this.currentVersion = '1.2.6'; // Will be read from package.json
    this.checkInterval = 24 * 60 * 60 * 1000; // Check once per day
    this.lastCheckKey = 'lanflix_last_update_check';
    this.skipVersionKey = 'lanflix_skip_version';
  }

  /**
   * Initialize updater - check version and set up periodic checks
   */
  async initialize() {
    // Load current version from package.json or manifest
    await this.loadCurrentVersion();

    // Check if we should check for updates
    const shouldCheck = this.shouldCheckForUpdates();
    if (shouldCheck) {
      await this.checkForUpdates(false); // Silent check
    }

    // Set up periodic checks
    this.startPeriodicChecks();
  }

  /**
   * Load current version from app
   */
  async loadCurrentVersion() {
    try {
      // For web app, try to get from meta tag or API
      const metaVersion = document.querySelector('meta[name="app-version"]');
      if (metaVersion) {
        this.currentVersion = metaVersion.content;
      }
    } catch (error) {
      console.warn('Could not load app version:', error);
    }
  }

  /**
   * Check if we should check for updates (not checked recently)
   */
  shouldCheckForUpdates() {
    const lastCheck = localStorage.getItem(this.lastCheckKey);
    if (!lastCheck) return true;

    const lastCheckTime = parseInt(lastCheck, 10);
    const now = Date.now();
    return (now - lastCheckTime) > this.checkInterval;
  }

  /**
   * Start periodic update checks
   */
  startPeriodicChecks() {
    setInterval(() => {
      this.checkForUpdates(false); // Silent check
    }, this.checkInterval);
  }

  /**
   * Check for updates from server API (not GitHub)
   * Note: This is now handled by settings-main.js using /api/app/update-check
   * This method is kept for compatibility but should not be used directly
   */
  async checkForUpdates(showNoUpdateMessage = true) {
    console.log('⚠️ Direct update check called - this should be handled by settings page');
    if (showNoUpdateMessage) {
      this.showNoUpdateMessage();
    }
    return null;
  }

  /**
   * Compare version numbers
   */
  isNewerVersion(latest, current) {
    const latestParts = latest.split('.').map(Number);
    const currentParts = current.split('.').map(Number);

    for (let i = 0; i < Math.max(latestParts.length, currentParts.length); i++) {
      const latestPart = latestParts[i] || 0;
      const currentPart = currentParts[i] || 0;

      if (latestPart > currentPart) return true;
      if (latestPart < currentPart) return false;
    }

    return false;
  }



  getVersionCode(version) {
    try {
      if (!version) {
        return 0;
      }

      const parts = version.split('.').map(part => parseInt(part, 10) || 0);
      const major = parts[0] || 0;
      const minor = parts[1] || 0;

      if (version === '4.0.0') {
        return 4;
      }

      return major * 10 + minor;
    } catch (error) {
      console.warn('Failed to calculate version code:', error);
      return 0;
    }
  }

  /**
   * Show update notification modal
   */
  showUpdateNotification(updateInfo) {
    // Remove existing notification if any
    this.hideUpdateNotification();

    const modal = document.createElement('div');
    modal.id = 'update-notification-modal';
    modal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.85);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10000;
      animation: fadeIn 0.3s ease-out;
    `;

    const releaseNotes = this.formatReleaseNotes(updateInfo.releaseNotes);

    modal.innerHTML = `
      <div style="
        background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);
        border-radius: 12px;
        padding: 40px;
        max-width: 600px;
        width: 90%;
        max-height: 80vh;
        overflow-y: auto;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
      ">
        <div style="text-align: center; margin-bottom: 30px;">
          <div style="font-size: 48px; margin-bottom: 10px;">🎉</div>
          <h2 style="color: #fff; font-size: 28px; margin: 0 0 10px 0;">Update Available!</h2>
          <p style="color: #999; font-size: 16px; margin: 0;">
            Version ${updateInfo.version} is now available
          </p>
          <p style="color: #666; font-size: 14px; margin: 5px 0 0 0;">
            Current version: ${updateInfo.currentVersion}
          </p>
        </div>

        <div style="
          background: rgba(255, 255, 255, 0.05);
          border-radius: 8px;
          padding: 20px;
          margin-bottom: 30px;
          max-height: 300px;
          overflow-y: auto;
        ">
          <h3 style="color: #fff; font-size: 18px; margin: 0 0 15px 0;">What's New:</h3>
          <div style="color: #ccc; font-size: 14px; line-height: 1.6;">
            ${releaseNotes}
          </div>
        </div>

        <div style="display: flex; gap: 12px; justify-content: center; flex-wrap: wrap;">
          <button id="update-now-btn" style="
            background: #e50914;
            color: white;
            border: none;
            padding: 14px 32px;
            font-size: 16px;
            font-weight: 600;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
            flex: 1;
            min-width: 140px;
          " onmouseover="this.style.background='#f40612'; this.style.transform='scale(1.05)'" 
             onmouseout="this.style.background='#e50914'; this.style.transform='scale(1)'">
            Update Now
          </button>
          <button id="update-later-btn" style="
            background: rgba(255, 255, 255, 0.1);
            color: white;
            border: 1px solid rgba(255, 255, 255, 0.2);
            padding: 14px 32px;
            font-size: 16px;
            font-weight: 600;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
            flex: 1;
            min-width: 140px;
          " onmouseover="this.style.background='rgba(255, 255, 255, 0.15)'" 
             onmouseout="this.style.background='rgba(255, 255, 255, 0.1)'">
            Later
          </button>
          <button id="update-skip-btn" style="
            background: transparent;
            color: #999;
            border: none;
            padding: 14px 20px;
            font-size: 14px;
            cursor: pointer;
            transition: color 0.2s;
          " onmouseover="this.style.color='#fff'" 
             onmouseout="this.style.color='#999'">
            Skip This Version
          </button>
        </div>
      </div>
    `;

    document.body.appendChild(modal);

    // Add animation
    const style = document.createElement('style');
    style.textContent = `
      @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
      }
    `;
    document.head.appendChild(style);

    // Add event listeners
    document.getElementById('update-now-btn').addEventListener('click', () => {
      this.startUpdate(updateInfo);
    });

    document.getElementById('update-later-btn').addEventListener('click', () => {
      this.hideUpdateNotification();
    });

    document.getElementById('update-skip-btn').addEventListener('click', () => {
      localStorage.setItem(this.skipVersionKey, updateInfo.version);
      this.hideUpdateNotification();
    });
  }

  /**
   * Format release notes for display
   */
  formatReleaseNotes(notes) {
    if (!notes) return '<p>No release notes available.</p>';

    // Convert markdown-style lists to HTML
    let formatted = notes
      .replace(/^### (.+)$/gm, '<h4 style="color: #fff; margin: 15px 0 10px 0;">$1</h4>')
      .replace(/^## (.+)$/gm, '<h3 style="color: #fff; margin: 20px 0 10px 0;">$1</h3>')
      .replace(/^- (.+)$/gm, '<li style="margin: 5px 0;">$1</li>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n\n/g, '</p><p style="margin: 10px 0;">')
      .replace(/^(?!<[hl]|<li)/gm, '<p style="margin: 10px 0;">');

    // Wrap lists
    formatted = formatted.replace(/(<li[^>]*>.*<\/li>)/s, '<ul style="margin: 10px 0; padding-left: 20px;">$1</ul>');

    return formatted;
  }

  /**
   * Hide update notification
   */
  hideUpdateNotification() {
    const modal = document.getElementById('update-notification-modal');
    if (modal) {
      modal.style.animation = 'fadeOut 0.3s ease-out';
      setTimeout(() => modal.remove(), 300);
    }
  }

  /**
   * Start update process
   */
  async startUpdate(updateInfo) {
    const isAndroid = /android/i.test(navigator.userAgent);
    const hasNativeBridge = typeof window !== 'undefined' && window.Android;

    // ONLY support native Android app updates - no web fallback
    if (isAndroid && hasNativeBridge) {
      // Prepare update info for native app
      const nativeUpdateInfo = {
        versionName: updateInfo.version,
        versionCode: updateInfo.versionCode || this.getVersionCode(updateInfo.version),
        downloadUrl: updateInfo.downloadUrl,
        releaseNotes: updateInfo.releaseNotes || '',
        mandatory: false,
        fileSize: updateInfo.downloadSize || 0,
        checksum: updateInfo.checksum || ''
      };

      // Trigger native update with full info
      if (typeof window.Android.triggerUpdateWithInfo === 'function') {
        window.Android.triggerUpdateWithInfo(JSON.stringify(nativeUpdateInfo));
        this.hideUpdateNotification();
        console.log('✅ Native OTA update started - UpdateActivity will show progress');
      } else if (typeof window.Android.triggerUpdate === 'function') {
        window.Android.triggerUpdate();
        this.hideUpdateNotification();
        console.log('✅ Native OTA update started');
      } else {
        console.error('❌ Native update bridge not available');
        this.showErrorMessage('Update system not available. Please contact support.');
      }
    } else {
      // Not a native Android app - updates not supported
      console.warn('Updates are only available in the native Android app');
      this.showErrorMessage('Updates are only available in the native Android app.');
    }
  }



  /**
   * Show "no update available" message
   */
  showNoUpdateMessage() {
    this.showInfoMessage('You\'re running the latest version!');
  }

  /**
   * Show info message
   */
  showInfoMessage(message) {
    const toast = document.createElement('div');
    toast.style.cssText = `
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(255, 255, 255, 0.95);
      color: #000;
      padding: 16px 32px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 500;
      z-index: 10002;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `;
    toast.textContent = message;

    document.body.appendChild(toast);

    setTimeout(() => {
      toast.style.animation = 'slideUp 0.3s ease-out';
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  }

  /**
   * Show error message
   */
  showErrorMessage(message) {
    const toast = document.createElement('div');
    toast.style.cssText = `
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(229, 9, 20, 0.95);
      color: white;
      padding: 16px 32px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 500;
      z-index: 10002;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `;
    toast.textContent = message;

    document.body.appendChild(toast);

    setTimeout(() => {
      toast.style.animation = 'slideUp 0.3s ease-out';
      setTimeout(() => toast.remove(), 300);
    }, 4000);
  }

  /**
   * Manual update check (triggered by user)
   */
  async checkNow() {
    return await this.checkForUpdates(true);
  }
}

// Create singleton instance
export const appUpdater = new AppUpdater();
