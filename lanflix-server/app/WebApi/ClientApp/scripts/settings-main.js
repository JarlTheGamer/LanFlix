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
        await appUpdater.checkNow();

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
