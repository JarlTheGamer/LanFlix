import { ProfileManager } from '../modules/profile-manager.js';
import { ContentDisplay } from '../modules/content-display.js';
import { Navigation } from '../modules/navigation.js';
import stateManager from '../modules/data.js';

// Initialize application
document.addEventListener('DOMContentLoaded', async () => {
  try {
    // Create instances
    const profileManager = new ProfileManager();
    const contentDisplay = new ContentDisplay(profileManager);
    const navigation = new Navigation(contentDisplay, profileManager);

    // Initialize modules
    await profileManager.initialize();
    await contentDisplay.initialize();
    navigation.initialize();

    // Check if we have a saved profile, if not show profile selection
    if (!stateManager.currentProfileId) {
      profileManager.show();
    }
  } catch (error) {
    console.error('Failed to initialize application:', error);
    // Show error message to user
    document.body.innerHTML = `
      <div style="display: flex; align-items: center; justify-content: center; height: 100vh; color: white; text-align: center; padding: 20px;">
        <div>
          <h1>Failed to load application</h1>
          <p>Please check your connection and try again.</p>
          <button onclick="location.reload()" style="margin-top: 20px; padding: 10px 20px; font-size: 16px; cursor: pointer;">
            Retry
          </button>
        </div>
      </div>
    `;
  }
});
