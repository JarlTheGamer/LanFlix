import { SettingsManager } from '../modules/settings-manager.js';
import { appUpdater } from '../modules/app-updater.js';
import { devicePairingManager } from '../modules/device-pairing.js';
import stateManager from '../modules/data.js';

document.addEventListener('DOMContentLoaded', async () => {
  const isPaired = await devicePairingManager.checkAndEnforcePairing();
  if (!isPaired) return;

  const profileId = stateManager.currentProfileId;
  const profiles = await stateManager.getProfiles();
  const currentProfile = profiles.find(p => p.id === profileId);

  if (currentProfile && (currentProfile.isGuest || !currentProfile.canManageSettings)) {
    window.location.href = 'index.html';
    return;
  }

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
