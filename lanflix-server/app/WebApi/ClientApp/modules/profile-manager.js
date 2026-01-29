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

    await this.createBackgroundTiles();
    this.updateFocus();

    if (this.profiles.length > 0) {
      this.updateBackground(this.profiles[0]);
    }

    // Check if we have a saved profile and set focus to it
    const savedProfileId = stateManager.currentProfileId;
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
      console.log(`Fetching watch history for ${profilesToCheck.length} profiles:`, profilesToCheck.map(p => p.name));
      
      const historyPromises = profilesToCheck.map(p => stateManager.getWatchHistory(p.id, false, 10));

      const histories = await Promise.all(historyPromises);
      console.log('Watch histories received:', histories.map((h, i) => `Profile ${i}: ${h?.length || 0} items`));
      
      const allHistoryItems = histories.flat(); // Flatten array of arrays
      console.log(`Total history items: ${allHistoryItems.length}`);

      if (allHistoryItems.length > 0) {
        backdrops = allHistoryItems
          .map(item => {
            // Use content object if available
            const content = item.content || item;
            
            // Debug log to see what we're getting
            if (backdrops.length < 3) {
              console.log('Profile background - history item:', {
                hasContent: !!content,
                contentId: content?.id,
                backdropUrl: content?.backdropUrl,
                backdropPath: content?.backdropPath,
                posterUrl: content?.posterUrl
              });
            }
            
            // For library content with an ID, try to use local backdrop first
            if (content?.id) {
              // Use the content ID to get the backdrop from the library
              // This will serve the local image if it exists, otherwise fall back to TMDB
              return content.backdropUrl || (content.backdropPath ? apiClient.getImageUrl(content.backdropPath, 'original') : null);
            }
            return null;
          })
          .filter(url => url !== null);
        
        console.log(`Profile backgrounds: Found ${backdrops.length} backdrops from ${allHistoryItems.length} history items`);
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
      console.warn('No backdrops found from watch history - this should not happen if you have watched content');
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
    this.selectedProfileId = profileId;
    const selectedProfile = this.profiles.find(p => p.id === profileId);
    console.log('Selected profile:', selectedProfile);

    // Save selected profile to state
    stateManager.currentProfileId = profileId;
    stateManager.saveState();

    const profileButton = document.querySelector('.profile');
    const profileAvatar = document.querySelector('.profile-avatar');
    if (profileButton && profileAvatar && selectedProfile) {
      profileAvatar.style.background = `linear-gradient(135deg, ${selectedProfile.avatarColorPrimary}, ${selectedProfile.avatarColorSecondary})`;
    }

    this.hide();
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
