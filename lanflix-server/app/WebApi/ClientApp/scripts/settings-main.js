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
