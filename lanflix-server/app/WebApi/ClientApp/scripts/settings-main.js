import { SettingsManager } from '../modules/settings-manager.js';
import { appUpdater } from '../modules/app-updater.js';

// Initialize settings page
document.addEventListener('DOMContentLoaded', async () => {
  try {
    const settingsManager = new SettingsManager();
    await settingsManager.initialize();

    // Initialize app updater
    await appUpdater.initialize();

    // Display current version
    const versionElement = document.getElementById('app-version');
    if (versionElement) {
      versionElement.textContent = appUpdater.currentVersion;
    }

    // Setup update check button
    const checkUpdatesBtn = document.getElementById('check-updates-btn');
    if (checkUpdatesBtn) {
      checkUpdatesBtn.addEventListener('click', async () => {
        // Disable button and show loading state
        checkUpdatesBtn.disabled = true;
        const originalHTML = checkUpdatesBtn.innerHTML;
        checkUpdatesBtn.innerHTML = `
          <svg viewBox="0 0 24 24" width="20" height="20" style="margin-right: 8px; animation: spin 1s linear infinite;">
            <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
          </svg>
          Checking...
        `;

        // Add spin animation
        if (!document.getElementById('spin-animation')) {
          const style = document.createElement('style');
          style.id = 'spin-animation';
          style.textContent = `
            @keyframes spin {
              from { transform: rotate(0deg); }
              to { transform: rotate(360deg); }
            }
          `;
          document.head.appendChild(style);
        }

        // Check for updates
        await checkForUpdates();

        // Restore button
        checkUpdatesBtn.disabled = false;
        checkUpdatesBtn.innerHTML = originalHTML;
      });
    }

    // Setup auto-update toggle
    const autoUpdateToggle = document.getElementById('auto-update-toggle');
    if (autoUpdateToggle) {
      // Load saved preference
      const autoUpdateEnabled = localStorage.getItem('lanflix_auto_update') !== 'false';
      autoUpdateToggle.checked = autoUpdateEnabled;

      // Save preference on change
      autoUpdateToggle.addEventListener('change', () => {
        localStorage.setItem('lanflix_auto_update', autoUpdateToggle.checked.toString());
        if (autoUpdateToggle.checked) {
          appUpdater.startPeriodicChecks();
        }
      });
    }
  } catch (error) {
    console.error('Failed to initialize settings:', error);
    alert('Failed to load settings. Please refresh the page.');
  }
});

// Update checking functionality
async function checkForUpdates(options = {}) {
  const { silent = false } = options;
  try {
    const currentVersion = getCurrentVersion();
    const currentVersionCode = getVersionCode(currentVersion);

    const response = await fetch(`/api/app/update-check?currentVersion=${currentVersionCode}&platform=android`);

    if (!response.ok) {
      throw new Error('Failed to check for updates');
    }

    const updateInfo = await response.json();

    if (hasUpdateAvailable(updateInfo, currentVersionCode)) {
      const modalData = mapUpdateInfoToModal(updateInfo, currentVersion);
      appUpdater.showUpdateNotification(modalData);
    } else {
      if (!silent) {
        appUpdater.showNoUpdateMessage();
      }
    }
  } catch (error) {
    console.error('Error checking for updates:', error);
    if (!silent) {
      appUpdater.showErrorMessage('Failed to check for updates. Please check your internet connection and try again.');
    }
  }
}

function hasUpdateAvailable(updateInfo, currentVersionCode) {
  if (!updateInfo || updateInfo.hasUpdate === false) {
    return false;
  }

  if (typeof updateInfo.versionCode === 'number') {
    return updateInfo.versionCode > currentVersionCode;
  }

  return false;
}

function mapUpdateInfoToModal(updateInfo, currentVersion) {
  const versionName = updateInfo.versionName || updateInfo.version || `v${updateInfo.versionCode}`;
  const releaseNotes = buildReleaseNotes(updateInfo);
  const downloadUrl = updateInfo.downloadUrl || updateInfo.htmlUrl || 'https://github.com/JarlTheGamer/Applications./releases/latest';

  return {
    version: versionName,
    currentVersion,
    releaseNotes,
    downloadUrl,
    htmlUrl: updateInfo.htmlUrl || downloadUrl
  };
}

function buildReleaseNotes(updateInfo) {
  const notes = updateInfo.releaseNotes && updateInfo.releaseNotes.trim().length > 0
    ? updateInfo.releaseNotes
    : 'Bug fixes and performance improvements.';

  const extraDetails = [];

  if (typeof updateInfo.fileSize === 'number' && updateInfo.fileSize > 0) {
    extraDetails.push(`Download size: ${formatFileSize(updateInfo.fileSize)}`);
  }

  if (updateInfo.mandatory) {
    extraDetails.push('This is a mandatory update.');
  }

  if (updateInfo.checksum) {
    extraDetails.push(`Checksum: ${updateInfo.checksum}`);
  }

  if (extraDetails.length === 0) {
    return notes;
  }

  return `${notes}\n\n${extraDetails.map(detail => `- ${detail}`).join('\n')}`;
}

function formatFileSize(bytes) {
  const megabytes = bytes / (1024 * 1024);
  if (megabytes >= 1) {
    return `${megabytes.toFixed(2)} MB`;
  }

  const kilobytes = bytes / 1024;
  if (kilobytes >= 1) {
    return `${kilobytes.toFixed(0)} KB`;
  }

  return `${bytes} B`;
}

function getCurrentVersion() {
  // Try to get version from native app first
  if (isNativeApp() && window.Android && window.Android.getAppVersion) {
    try {
      return window.Android.getAppVersion();
    } catch (e) {
      console.log('Failed to get native app version:', e);
    }
  }
  
  // Fallback to web app version
  const metaVersion = document.querySelector('meta[name="app-version"]');
  return metaVersion ? metaVersion.getAttribute('content') : '2.0.0';
}

function getVersionCode(versionName) {
  // Convert version name to version code (e.g., "2.0.0" -> 20, "3.9.0" -> 39)
  try {
    const parts = versionName.split('.');
    const major = parseInt(parts[0]) || 0;
    const minor = parseInt(parts[1]) || 0;
    const patch = parseInt(parts[2]) || 0;
    return major * 10 + minor;
  } catch (error) {
    return 20; // Default version code
  }
}

function isNativeApp() {
  // Detect if running in native app
  return window.Android !== undefined ||
         navigator.userAgent.includes('LanflixApp') ||
         window.location.protocol === 'file:';
}

// Auto-check for updates on page load (for native app)
document.addEventListener('DOMContentLoaded', () => {
  if (isNativeApp()) {
    // Check for updates 3 seconds after page load
    setTimeout(() => {
      checkForUpdates({ silent: true }).catch(console.error);
    }, 3000);
  }
});
