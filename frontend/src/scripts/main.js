import { ProfileManager } from '../modules/profile-manager.js';
import { ContentDisplay } from '../modules/content-display.js';
import { Navigation } from '../modules/navigation.js';

// Initialize application
document.addEventListener('DOMContentLoaded', () => {
  // Create instances
  const profileManager = new ProfileManager();
  const contentDisplay = new ContentDisplay();
  const navigation = new Navigation(contentDisplay, profileManager);

  // Initialize modules
  profileManager.initialize();
  contentDisplay.initialize();
  navigation.initialize();

  // Start with profile selection
  profileManager.show();
});
