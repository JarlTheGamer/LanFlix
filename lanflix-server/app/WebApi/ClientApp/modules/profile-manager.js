import stateManager from './data.js';
import apiClient from './api-client.js';

// Global debug function for testing
window.debugProfileManager = null;

export class ProfileManager {
  constructor() {
    this.selectedProfileId = null;
    this.profileSelectionActive = true;
    this.focusedProfileIndex = 0;
    this.profiles = [];
  }

  async initialize() {
    // Load profiles from backend
    await this.loadProfiles();

    // Apply permissions for currently selected profile on load
    const savedProfileId = stateManager.currentProfileId;
    if (savedProfileId) {
      const activeProfile = this.profiles.find(p => p.id === savedProfileId);
      if (activeProfile) {
        this.applyProfilePermissions(activeProfile);
      }
    }

    // Make this instance available for debugging
    window.debugProfileManager = this;

    const profilesBar = document.getElementById('profiles-vertical-bar');

    // Only initialize UI if profile elements exist (on profiles page)
    if (!profilesBar) {
      return;
    }

    // Create profile items
    this.profiles.forEach((profile, index) => {
      const profileItem = document.createElement('div');
      profileItem.className = 'profile-item';
      profileItem.dataset.profileId = profile.id;
      profileItem.dataset.index = index;

      profileItem.innerHTML = `
        <div class="profile-avatar-large" style="background: linear-gradient(135deg, ${profile.avatarColorPrimary}, ${profile.avatarColorSecondary}); position: relative;">
          ${profile.hasPin ? `
            <div class="profile-lock-badge" title="PIN Protected" style="position: absolute; bottom: 8px; right: 8px; background: rgba(0,0,0,0.75); border-radius: 50%; width: 26px; height: 26px; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 6px rgba(0,0,0,0.5);">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="#ffffff">
                <path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z"/>
              </svg>
            </div>
          ` : ''}
        </div>
        <div class="profile-name">${profile.name}</div>
      `;

      // Add click/touch event handlers for all devices
      profileItem.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile clicked:', profile.name, profile.id);
        this.selectProfile(profile.id);
      });

      // Add touch event handlers for mobile devices
      profileItem.addEventListener('touchstart', (e) => {
        e.preventDefault();
        e.stopPropagation();
        // Update focus to show visual feedback
        this.focusedProfileIndex = index;
        this.updateFocus();
      });

      profileItem.addEventListener('touchend', (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile touch end:', profile.name, profile.id);
        this.selectProfile(profile.id);
      });

      // Add mouse hover for desktop
      profileItem.addEventListener('mouseenter', () => {
        this.focusedProfileIndex = index;
        this.updateFocus();
      });

      profilesBar.appendChild(profileItem);
    });

    await this.createBackgroundTiles();
    this.updateFocus();

    if (this.profiles.length > 0) {
      this.updateBackground(this.profiles[0]);
    }

    if (savedProfileId) {
      const profileIndex = this.profiles.findIndex(p => p.id === savedProfileId);
      if (profileIndex >= 0) {
        this.focusedProfileIndex = profileIndex;
        this.updateFocus();
      }
    }
  }

  async loadProfiles() {
    try {
      this.profiles = await stateManager.getProfiles();
    } catch (error) {
      console.error('Failed to load profiles:', error);
      this.profiles = [];
    }
  }

  async createBackgroundTiles() {
    const backgroundAnimation = document.getElementById('profile-background-animation');

    if (!backgroundAnimation) return;

    // Fetch real content for backdrops
    // We sort of iterate through profiles to get their watch history to make it personalized
    let backdrops = [];
    try {
      // Collect history from up to 3 profiles to get a good mix
      const profilesToCheck = this.profiles.slice(0, 3);
      const historyPromises = profilesToCheck.map(p => stateManager.getWatchHistory(p.id, false, 10));

      const histories = await Promise.all(historyPromises);
      const allHistoryItems = histories.flat(); // Flatten array of arrays

      if (allHistoryItems.length > 0) {
        backdrops = allHistoryItems
          .map(item => {
            const content = item.content || item;
            
            // Use backdropPath from content (TMDB path)
            if (content?.backdropPath) {
              return apiClient.getImageUrl(content.backdropPath, 'original');
            }
            return null;
          })
          .filter(url => url !== null);
      }

      // If history is empty (new install), fallback to Trending instead of Recently Added (as requested)
      if (backdrops.length === 0) {
        console.log('No watch history found for backgrounds, using trending content');
        const trending = await stateManager.getDiscoverContent(null); // null profile for generic trending
        if (trending && trending.trending && trending.trending.length > 0) {
          backdrops = trending.trending
            .filter(item => item.backdropPath)
            .map(item => apiClient.getImageUrl(item.backdropPath, 'original'));
        }
      }

    } catch (error) {
      console.warn('Failed to fetch history for profile background:', error);
    }

    // Fallback if no real content yet (or offline/fresh install)
    if (backdrops.length === 0) {
      console.warn('No backdrops found from watch history');
    }

    const rowCount = 20; // Reduced slightly for performance
    const tilesPerRow = 15;

    // Shuffle backdrops
    backdrops.sort(() => Math.random() - 0.5);

    for (let row = 0; row < rowCount; row++) {
      const rowElement = document.createElement('div');
      rowElement.className = 'background-row';

      for (let tile = 0; tile < tilesPerRow * 2; tile++) {
        const tileElement = document.createElement('div');
        tileElement.className = 'profile-background-tile';

        const imageIndex = (row * tilesPerRow + tile) % backdrops.length;
        const imageUrl = backdrops[imageIndex];

        tileElement.style.backgroundImage = `url(${imageUrl})`;
        rowElement.appendChild(tileElement);
      }

      backgroundAnimation.appendChild(rowElement);
    }
  }

  updateFocus() {
    const profileItems = document.querySelectorAll('.profile-item');
    profileItems.forEach((item, index) => {
      item.classList.toggle('focused', index === this.focusedProfileIndex);
    });

    const focusedProfile = this.profiles[this.focusedProfileIndex];
    if (focusedProfile) {
      this.updateBackground(focusedProfile);
    }
  }

  updateBackground(profile) {
    const backgroundTiles = document.querySelectorAll('.profile-background-tile');

    backgroundTiles.forEach((tile, index) => {
      if (index % 4 === 0) {
        tile.style.opacity = '0.6';
      } else {
        tile.style.opacity = '0.3';
      }
    });
  }

  selectProfile(profileId) {
    console.log('selectProfile called with:', profileId);
    const selectedProfile = this.profiles.find(p => p.id === profileId);
    if (!selectedProfile) return;

    if (selectedProfile.hasPin) {
      this.promptProfilePin(selectedProfile);
      return;
    }

    this.activateProfile(selectedProfile);
  }

  activateProfile(selectedProfile) {
    this.selectedProfileId = selectedProfile.id;
    stateManager.currentProfileId = selectedProfile.id;
    stateManager.saveState();
    sessionStorage.setItem('auth_profile_' + selectedProfile.id, 'true');

    const profileButton = document.querySelector('.profile');
    const profileAvatar = document.querySelector('.profile-avatar');
    if (profileButton && profileAvatar && selectedProfile) {
      profileAvatar.style.background = `linear-gradient(135deg, ${selectedProfile.avatarColorPrimary}, ${selectedProfile.avatarColorSecondary})`;
    }

    this.applyProfilePermissions(selectedProfile);
    this.hide();

    if (window.location.pathname.includes('profiles.html')) {
      window.location.href = 'index.html';
    }
  }

  promptProfilePin(selectedProfile) {
    let pinModal = document.getElementById('profile-pin-modal');
    if (!pinModal) {
      pinModal = document.createElement('div');
      pinModal.id = 'profile-pin-modal';
      pinModal.style.cssText = `
        position: fixed; inset: 0; z-index: 10000;
        background: rgba(0,0,0,0.85); backdrop-filter: blur(12px);
        display: flex; align-items: center; justify-content: center;
      `;
      document.body.appendChild(pinModal);
    }

    pinModal.innerHTML = `
      <div style="background: rgba(30,30,38,0.95); border: 1px solid rgba(255,255,255,0.15); border-radius: 20px; padding: 32px; width: 340px; text-align: center; color: #fff; box-shadow: 0 20px 40px rgba(0,0,0,0.6);">
        <div style="font-size: 1.3rem; font-weight: 700; margin-bottom: 8px;">🔒 Enter PIN</div>
        <p style="font-size: 0.85rem; color: rgba(255,255,255,0.7); margin-bottom: 24px;">Enter the 4-digit PIN for <strong>${selectedProfile.name}</strong></p>
        <div style="display: flex; gap: 12px; justify-content: center; margin-bottom: 24px;">
          <input type="password" id="pin-digit-1" maxlength="1" style="width: 48px; height: 56px; font-size: 1.5rem; text-align: center; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.2); border-radius: 12px; color: #fff; outline: none;">
          <input type="password" id="pin-digit-2" maxlength="1" style="width: 48px; height: 56px; font-size: 1.5rem; text-align: center; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.2); border-radius: 12px; color: #fff; outline: none;">
          <input type="password" id="pin-digit-3" maxlength="1" style="width: 48px; height: 56px; font-size: 1.5rem; text-align: center; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.2); border-radius: 12px; color: #fff; outline: none;">
          <input type="password" id="pin-digit-4" maxlength="1" style="width: 48px; height: 56px; font-size: 1.5rem; text-align: center; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.2); border-radius: 12px; color: #fff; outline: none;">
        </div>
        <div id="pin-error-text" style="color: #ff5252; font-size: 0.85rem; height: 20px; margin-bottom: 12px;"></div>
        <div style="display: flex; gap: 10px;">
          <button id="pin-cancel-btn" style="flex: 1; background: rgba(255,255,255,0.1); border: none; padding: 12px; border-radius: 10px; color: #fff; font-weight: 600; cursor: pointer;">Cancel</button>
        </div>
      </div>
    `;

    pinModal.style.display = 'flex';

    const inputs = [
      document.getElementById('pin-digit-1'),
      document.getElementById('pin-digit-2'),
      document.getElementById('pin-digit-3'),
      document.getElementById('pin-digit-4')
    ];

    inputs[0]?.focus();

    inputs.forEach((input, idx) => {
      input?.addEventListener('input', async (e) => {
        if (e.target.value.length === 1 && idx < 3) {
          inputs[idx + 1].focus();
        }

        const fullPin = inputs.map(i => i.value).join('');
        if (fullPin.length === 4) {
          try {
            const isValid = await apiClient.verifyProfilePin(selectedProfile.id, fullPin);
            if (isValid) {
              pinModal.style.display = 'none';
              this.activateProfile(selectedProfile);
            } else {
              const errEl = document.getElementById('pin-error-text');
              if (errEl) errEl.textContent = '❌ Incorrect PIN';
              inputs.forEach(i => i.value = '');
              inputs[0]?.focus();
            }
          } catch (err) {
            console.error('PIN verification error:', err);
          }
        }
      });

      input?.addEventListener('keydown', (e) => {
        if (e.key === 'Backspace' && !e.target.value && idx > 0) {
          inputs[idx - 1].focus();
        }
      });
    });

    document.getElementById('pin-cancel-btn')?.addEventListener('click', () => {
      pinModal.style.display = 'none';
    });
  }

  applyProfilePermissions(profile) {
    if (!profile) return;

    const isGuestOrNoSettings = profile.isGuest || !profile.canManageSettings;
    const isGuestOrNoDownload = profile.isGuest || !profile.canDownload;

    // 1. Settings / 3-dots button
    document.querySelectorAll('.settings-btn, .nav-settings, a[href*="settings.html"], a[href*="admin.html"]').forEach(el => {
      el.style.display = isGuestOrNoSettings ? 'none' : '';
    });

    // 2. Notifications button
    document.querySelectorAll('.notifications-btn, a[href*="notifications.html"]').forEach(el => {
      el.style.display = isGuestOrNoDownload ? 'none' : '';
    });

    // 3. Search magnifying glass button
    document.querySelectorAll('#search-btn, .search-home, .search-btn').forEach(el => {
      el.style.display = isGuestOrNoDownload ? 'none' : '';
    });

    // 4. Discover tab button
    document.querySelectorAll('[data-hero="discover"]').forEach(el => {
      el.style.display = isGuestOrNoDownload ? 'none' : '';
    });

    // 5. Download buttons
    document.querySelectorAll('.btn-download, .download-btn, .download-option, [data-action="queue"], [data-action="queue-all"], .season-download-btn, .episode-download-btn').forEach(el => {
      el.style.display = isGuestOrNoDownload ? 'none' : '';
    });
  }

  show() {
    this.profileSelectionActive = true;
    this.focusedProfileIndex = 0;
    const overlay = document.getElementById('profile-selection-overlay');
    const main = document.querySelector('main');
    const header = document.querySelector('header');

    if (overlay) overlay.classList.remove('hidden');
    if (main) main.style.display = 'none';
    if (header) header.style.display = 'none';

    this.updateFocus();
  }

  hide() {
    this.profileSelectionActive = false;
    const overlay = document.getElementById('profile-selection-overlay');
    const main = document.querySelector('main');
    const header = document.querySelector('header');

    if (overlay) overlay.classList.add('hidden');
    if (main) main.style.display = 'block';
    if (header) header.style.display = 'block';
  }

  handleKeyboard(e) {
    if (!this.profileSelectionActive) return;

    if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.focusedProfileIndex = this.focusedProfileIndex > 0 ? this.focusedProfileIndex - 1 : this.profiles.length - 1;
      this.updateFocus();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.focusedProfileIndex = this.focusedProfileIndex < this.profiles.length - 1 ? this.focusedProfileIndex + 1 : 0;
      this.updateFocus();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const selectedProfile = this.profiles[this.focusedProfileIndex];
      if (selectedProfile) {
        this.selectProfile(selectedProfile.id);
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      this.hide();
    }
  }

  /**
   * Get currently selected profile
   */
  getSelectedProfile() {
    return this.profiles.find(p => p.id === this.selectedProfileId);
  }

  /**
   * Refresh profiles from backend
   */
  async refreshProfiles() {
    await this.loadProfiles(true);

    // Re-render profile items
    const profilesBar = document.getElementById('profiles-vertical-bar');
    profilesBar.innerHTML = '';

    this.profiles.forEach((profile, index) => {
      const profileItem = document.createElement('div');
      profileItem.className = 'profile-item';
      profileItem.dataset.profileId = profile.id;
      profileItem.dataset.index = index;

      profileItem.innerHTML = `
        <div class="profile-avatar-large" style="background: linear-gradient(135deg, ${profile.avatarColorPrimary}, ${profile.avatarColorSecondary})">
        </div>
        <div class="profile-name">${profile.name}</div>
      `;

      // Add click/touch event handlers for all devices
      profileItem.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile clicked:', profile.name, profile.id);
        this.selectProfile(profile.id);
      });

      // Add touch event handlers for mobile devices
      profileItem.addEventListener('touchstart', (e) => {
        e.preventDefault();
        e.stopPropagation();
        // Update focus to show visual feedback
        this.focusedProfileIndex = index;
        this.updateFocus();
      });

      profileItem.addEventListener('touchend', (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile touch end:', profile.name, profile.id);
        this.selectProfile(profile.id);
      });

      // Add mouse hover for desktop
      profileItem.addEventListener('mouseenter', () => {
        this.focusedProfileIndex = index;
        this.updateFocus();
      });

      profilesBar.appendChild(profileItem);
    });

    this.updateFocus();
  }

  /**
   * Create a new profile
   */
  async createProfile(name, avatarColorPrimary, avatarColorSecondary) {
    try {
      const response = await apiClient.createProfile(name, avatarColorPrimary, avatarColorSecondary);

      // Refresh profiles
      await this.refreshProfiles();

      return response.profile;
    } catch (error) {
      console.error('Failed to create profile:', error);
      throw error;
    }
  }

  /**
   * Update an existing profile
   */
  async updateProfile(profileId, updates) {
    try {
      const response = await apiClient.updateProfile(profileId, updates);

      // Refresh profiles
      await this.refreshProfiles();

      // Update UI if this is the selected profile
      if (profileId === this.selectedProfileId) {
        const profileButton = document.querySelector('.profile');
        const profileAvatar = document.querySelector('.profile-avatar');
        if (profileButton && profileAvatar && updates.avatarColorPrimary && updates.avatarColorSecondary) {
          profileAvatar.style.background = `linear-gradient(135deg, ${updates.avatarColorPrimary}, ${updates.avatarColorSecondary})`;
        }
      }

      return response.profile;
    } catch (error) {
      console.error('Failed to update profile:', error);
      throw error;
    }
  }

  /**
   * Delete a profile
   */
  async deleteProfile(profileId) {
    try {
      await apiClient.deleteProfile(profileId);

      // If deleted profile was selected, clear selection
      if (profileId === this.selectedProfileId) {
        this.selectedProfileId = null;
        stateManager.currentProfileId = null;
        stateManager.saveState();
      }

      // Refresh profiles
      await this.refreshProfiles();

      return true;
    } catch (error) {
      console.error('Failed to delete profile:', error);
      throw error;
    }
  }

  /**
   * Show profile management UI
   */
  showProfileManagement() {
    // This would open a modal or navigate to profile management page
    console.log('Show profile management UI');
    // For now, just show the profile selection
    this.show();
  }

  /**
   * Debug function to test profile clicks
   */
  debugProfileClicks() {
    console.log('=== Profile Click Debug Info ===');
    console.log('Profiles loaded:', this.profiles.length);
    console.log('Profile selection active:', this.profileSelectionActive);

    const profileItems = document.querySelectorAll('.profile-item');
    console.log('Profile items found:', profileItems.length);

    profileItems.forEach((item, index) => {
      console.log(`Profile ${index}:`, {
        id: item.dataset.profileId,
        clickable: window.getComputedStyle(item).pointerEvents !== 'none',
        zIndex: window.getComputedStyle(item).zIndex,
        position: window.getComputedStyle(item).position
      });
    });

    // Test if we can manually trigger a click
    if (profileItems.length > 0) {
      console.log('Attempting to trigger click on first profile...');
      profileItems[0].click();
    }
  }
}
