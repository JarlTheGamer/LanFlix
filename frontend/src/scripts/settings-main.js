import { SettingsManager } from '../modules/settings-manager.js';

// Initialize settings page
document.addEventListener('DOMContentLoaded', () => {
  const settingsManager = new SettingsManager();
  settingsManager.initialize();
});
