import { ProfileManager } from '../modules/profile-manager.js';
import { ContentDisplay } from '../modules/content-display.js';
import { Navigation } from '../modules/navigation.js';
import searchModule from '../modules/search.js';
import stateManager from '../modules/data.js';
import { notificationBadge } from '../modules/notification-badge.js';
import { checkFirstRun, isNativeApp } from '../modules/first-run.js';
import { devicePairingManager } from '../modules/device-pairing.js';

// Initialize application
document.addEventListener('DOMContentLoaded', async () => {
  // Check device pairing status first
  const isPaired = await devicePairingManager.checkAndEnforcePairing();
  if (!isPaired) return;

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
    const navigation = new Navigation(null, profileManager); // Will be set after contentDisplay creation
    const contentDisplay = new ContentDisplay(profileManager, navigation);
    
    // Set the contentDisplay reference in navigation
    navigation.contentDisplay = contentDisplay;

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

    // Refresh content when navigating back to this page (e.g., from player)
    window.addEventListener('pageshow', (event) => {
      if (event.persisted || performance.navigation.type === 2) {
        // Page was loaded from cache (back button)
        contentDisplay.refreshContent();
      }
    });

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
