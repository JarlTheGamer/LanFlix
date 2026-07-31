import { ProfileManager } from '../modules/profile-manager.js';
import stateManager from '../modules/data.js';
import { devicePairingManager } from '../modules/device-pairing.js';

// Initialize profiles page
document.addEventListener('DOMContentLoaded', async () => {
  const isPaired = await devicePairingManager.checkAndEnforcePairing();
  if (!isPaired) return;
  try {
    const profileManager = new ProfileManager();
    await profileManager.initialize();

    // Always show profile selection on this page
    profileManager.show();

    // Add keyboard event listener
    document.addEventListener('keydown', (e) => profileManager.handleKeyboard(e));

  } catch (error) {
    console.error('Failed to initialize profiles:', error);
    document.body.innerHTML = `
      <div style="display: flex; align-items: center; justify-content: center; height: 100vh; color: white; text-align: center; padding: 20px;">
        <div>
          <h1>Failed to load profiles</h1>
          <p>Please check your connection and try again.</p>
          <button onclick="location.reload()" style="margin-top: 20px; padding: 10px 20px; font-size: 16px; cursor: pointer;">
            Retry
          </button>
        </div>
      </div>
    `;
  }
});
