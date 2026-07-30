/**
 * App & Server Updater Module
 * Checks for and installs app & server updates
 */

export class AppUpdater {
  constructor() {
    this.currentVersion = '1.2.6';
    this.checkInterval = 24 * 60 * 60 * 1000; // Check once per day
    this.lastCheckKey = 'lanflix_last_update_check';
    this.skipVersionKey = 'lanflix_skip_version';
  }

  async initialize() {
    await this.loadCurrentVersion();
  }

  async loadCurrentVersion() {
    try {
      const response = await fetch('/api/server-update/version');
      if (response.ok) {
        const data = await response.json();
        if (data && data.version) {
          this.currentVersion = data.version;
        }
      }
    } catch (error) {
      console.warn('Could not load server version:', error);
    }
  }

  showUpdateNotification(updateInfo) {
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
    const targetName = updateInfo.isServerUpdate ? 'Lanflix Server' : 'Lanflix App';

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
          <h2 style="color: #fff; font-size: 28px; margin: 0 0 10px 0;">${targetName} Update Available!</h2>
          <p style="color: #999; font-size: 16px; margin: 0;">
            Version ${updateInfo.version} is available
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
          ">
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
          ">
            Later
          </button>
        </div>
      </div>
    `;

    document.body.appendChild(modal);

    document.getElementById('update-now-btn').addEventListener('click', () => {
      this.startUpdate(updateInfo);
    });

    document.getElementById('update-later-btn').addEventListener('click', () => {
      this.hideUpdateNotification();
    });
  }

  formatReleaseNotes(notes) {
    if (!notes) return '<p>Bug fixes and performance improvements.</p>';
    return notes
      .replace(/\n/g, '<br>')
      .replace(/•/g, '&bull;');
  }

  hideUpdateNotification() {
    const modal = document.getElementById('update-notification-modal');
    if (modal) {
      modal.remove();
    }
  }

  async startUpdate(updateInfo) {
    const hasNativeBridge = typeof window !== 'undefined' && window.Android;

    if (hasNativeBridge && !updateInfo.isServerUpdate) {
      // Native Android App update
      const nativeUpdateInfo = {
        versionName: updateInfo.version,
        versionCode: updateInfo.versionCode || 40,
        downloadUrl: updateInfo.downloadUrl,
        releaseNotes: updateInfo.releaseNotes || '',
        mandatory: false,
        fileSize: updateInfo.fileSize || 0,
        checksum: updateInfo.checksum || ''
      };

      if (typeof window.Android.triggerUpdateWithInfo === 'function') {
        window.Android.triggerUpdateWithInfo(JSON.stringify(nativeUpdateInfo));
        this.hideUpdateNotification();
      } else if (typeof window.Android.triggerUpdate === 'function') {
        window.Android.triggerUpdate();
        this.hideUpdateNotification();
      }
    } else {
      // Server update (Web Browser / In-App Server update)
      this.hideUpdateNotification();
      this.showServerUpdateOverlay();
      
      try {
        const response = await fetch('/api/server-update/apply', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ downloadUrl: updateInfo.downloadUrl })
        });

        if (!response.ok) {
          throw new Error('Failed to initiate server update');
        }

        // Poll for server restart completion
        this.pollServerReboot();
      } catch (error) {
        console.error('Error applying server update:', error);
        this.showErrorMessage('Failed to start server update. Please try again.');
      }
    }
  }

  showServerUpdateOverlay() {
    let overlay = document.getElementById('server-update-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'server-update-overlay';
      overlay.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: #000;
        color: #fff;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        z-index: 99999;
        font-family: 'Poppins', sans-serif;
        text-align: center;
        padding: 20px;
      `;

      overlay.innerHTML = `
        <div style="font-size: 64px; margin-bottom: 20px; animation: pulse 1.5s infinite;">🚀</div>
        <h1 style="font-size: 28px; margin-bottom: 10px; color: #fff;">Server Updating</h1>
        <p style="color: #aaa; max-width: 450px; font-size: 16px; margin-bottom: 30px;">
          Please wait while Lanflix Server downloads the update, installs improvements, and restarts...
        </p>
        <div style="width: 300px; height: 6px; background: rgba(255,255,255,0.1); border-radius: 3px; overflow: hidden; margin-bottom: 15px;">
          <div id="update-progress-bar" style="width: 30%; height: 100%; background: #e50914; border-radius: 3px; animation: loading 2s infinite linear;"></div>
        </div>
        <span id="update-status-text" style="color: #888; font-size: 14px;">Downloading & Applying Update...</span>
        
        <style>
          @keyframes loading {
            0% { transform: translateX(-100%); }
            100% { transform: translateX(200%); }
          }
          @keyframes pulse {
            0% { transform: scale(1); }
            50% { transform: scale(1.1); }
            100% { transform: scale(1); }
          }
        </style>
      `;
      document.body.appendChild(overlay);
    }
  }

  pollServerReboot() {
    let attempts = 0;
    const interval = setInterval(async () => {
      attempts++;
      try {
        const response = await fetch('/api/server-update/version', { cache: 'no-store' });
        if (response.ok) {
          clearInterval(interval);
          const statusText = document.getElementById('update-status-text');
          if (statusText) statusText.textContent = 'Server updated! Reloading...';
          setTimeout(() => {
            window.location.reload();
          }, 1000);
        }
      } catch (e) {
        // Server is rebooting
        const statusText = document.getElementById('update-status-text');
        if (statusText && attempts > 5) {
          statusText.textContent = 'Restarting Lanflix Server...';
        }
      }
    }, 2000);
  }

  showNoUpdateMessage() {
    this.showInfoMessage("You're running the latest version!");
  }

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
    `;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
  }

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
    `;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
  }
}

export const appUpdater = new AppUpdater();
