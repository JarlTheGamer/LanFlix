import { SettingsManager } from '../modules/settings-manager.js';
import { appUpdater } from '../modules/app-updater.js';

document.addEventListener('DOMContentLoaded', async () => {
  try {
    const settingsManager = new SettingsManager();
    await settingsManager.initialize();
    await appUpdater.initialize();

    const versionElement = document.getElementById('app-version');
    if (versionElement) {
      versionElement.textContent = appUpdater.currentVersion;
    }

    const checkUpdatesBtn = document.getElementById('check-updates-btn');
    if (checkUpdatesBtn) {
      checkUpdatesBtn.addEventListener('click', async () => {
        checkUpdatesBtn.disabled = true;
        const originalHTML = checkUpdatesBtn.innerHTML;
        checkUpdatesBtn.innerHTML = `
          <svg viewBox="0 0 24 24" width="20" height="20" style="margin-right: 8px; animation: spin 1s linear infinite;">
            <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
          </svg>
          Checking...
        `;

        await checkForUpdates({ silent: false });

        checkUpdatesBtn.disabled = false;
        checkUpdatesBtn.innerHTML = originalHTML;
      });
    }

    // Load Telemetry Data
    loadTelemetry();
    setInterval(loadTelemetry, 10000);

  } catch (error) {
    console.error('Failed to initialize settings:', error);
  }
});

async function loadTelemetry() {
  try {
    const response = await fetch('/api/system/telemetry');
    if (!response.ok) return;

    const data = await response.json();
    
    // Update Network Section
    const speedEl = document.getElementById('network-speed');
    if (speedEl && data.network) {
      speedEl.textContent = `${data.network.downloadSpeedMbps || 100} Mbps`;
    }

    // Update Devices Section
    const container = document.getElementById('device-list-container');
    if (container && Array.isArray(data.devices)) {
      container.innerHTML = data.devices.map(device => `
        <div class="device-item">
          <div class="device-icon">
            <svg viewBox="0 0 24 24">
              <path fill="currentColor" d="M17 1.01L7 1c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-1.99-2-1.99zM17 19H7V5h10v14z"/>
            </svg>
          </div>
          <div class="device-info">
            <div class="device-name">${escapeHtml(device.name)} ${device.isCurrent ? '(This Device)' : ''}</div>
            <div class="device-meta">IP: ${escapeHtml(device.ipAddress)} &bull; Status: ${escapeHtml(device.status)}</div>
          </div>
        </div>
      `).join('');
    }
  } catch (error) {
    console.warn('Telemetry load failed:', error);
  }
}

async function checkForUpdates(options = {}) {
  const { silent = false } = options;
  const inNativeApp = isNativeApp();

  try {
    if (inNativeApp) {
      // Check for Android App update
      const currentVersionCode = 39;
      const response = await fetch(`/api/app/update-check?currentVersion=${currentVersionCode}&platform=android`);
      if (!response.ok) throw new Error('App update check failed');
      const data = await response.json();

      if (data && data.hasUpdate) {
        appUpdater.showUpdateNotification({
          version: data.versionName,
          currentVersion: '3.9.0',
          releaseNotes: data.releaseNotes,
          downloadUrl: data.downloadUrl,
          isServerUpdate: false
        });
      } else if (!silent) {
        appUpdater.showNoUpdateMessage();
      }
    } else {
      // Check for Server update (Web Browser)
      const response = await fetch('/api/server-update/check');
      if (!response.ok) throw new Error('Server update check failed');
      const data = await response.json();

      if (data && data.updateAvailable) {
        appUpdater.showUpdateNotification({
          version: data.latestVersion,
          currentVersion: data.currentVersion,
          releaseNotes: data.releaseNotes,
          downloadUrl: data.downloadUrl,
          isServerUpdate: true
        });
      } else if (!silent) {
        appUpdater.showNoUpdateMessage();
      }
    }
  } catch (error) {
    console.error('Update check error:', error);
    if (!silent) {
      appUpdater.showErrorMessage('Failed to check for updates. Please try again.');
    }
  }
}

function isNativeApp() {
  return window.Android !== undefined || navigator.userAgent.includes('LanflixApp');
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/[&<>"']/g, match => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[match]);
}
