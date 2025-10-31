import { ProfileManager } from '../modules/profile-manager.js';
import { ContentDisplay } from '../modules/content-display.js';
import { Navigation } from '../modules/navigation.js';
import searchModule from '../modules/search.js';
import stateManager from '../modules/data.js';
import { notificationBadge } from '../modules/notification-badge.js';
import { checkFirstRun, isNativeApp } from '../modules/first-run.js';
import tvNavigation from '../modules/tv-navigation.js';

// Initialize application
document.addEventListener('DOMContentLoaded', async () => {
  // Check for first-run configuration (Android app only)
  if (isNativeApp() && !checkFirstRun()) {
    return; // Will redirect to config page
  }
  
  try {
    // Check if we have a saved profile, if not redirect to profiles page
    if (!stateManager.currentProfileId) {
      window.location.replace('profiles.html');
      return;
    }

    // Create instances
    const profileManager = new ProfileManager();
    const contentDisplay = new ContentDisplay(profileManager);
    const navigation = new Navigation(contentDisplay, profileManager);

    // Initialize modules
    await profileManager.initialize();
    
    // Set the selected profile from state
    profileManager.selectedProfileId = stateManager.currentProfileId;
    const selectedProfile = profileManager.profiles.find(p => p.id === stateManager.currentProfileId);
    if (selectedProfile) {
      const profileAvatar = document.querySelector('.profile-avatar');
      if (profileAvatar) {
        profileAvatar.style.background = `linear-gradient(135deg, ${selectedProfile.avatarColorPrimary}, ${selectedProfile.avatarColorSecondary})`;
      }
    }
    
    await contentDisplay.initialize();
    navigation.initialize();
    
    // Initialize TV navigation if on TV platform
    tvNavigation.initialize();
    
    // Initialize search module
    searchModule.initialize();
    
    // Add search button handler
    const searchBtn = document.getElementById('search-btn');
    if (searchBtn) {
      searchBtn.addEventListener('click', () => {
        searchModule.open();
      });
    }

    // Initialize notification badge
    notificationBadge.init();

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
