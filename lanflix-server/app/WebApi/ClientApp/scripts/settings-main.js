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
