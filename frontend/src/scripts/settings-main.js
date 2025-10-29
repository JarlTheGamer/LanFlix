import { SettingsManager } from '../modules/settings-manager.js';

// Initialize settings page
document.addEventListener('DOMContentLoaded', async () => {
  try {
    const settingsManager = new SettingsManager();
    await settingsManager.initialize();
  } catch (error) {
    console.error('Failed to initialize settings:', error);
    alert('Failed to load settings. Please refresh the page.');
  }
});
