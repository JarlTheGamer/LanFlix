import stateManager from './data.js';
import apiClient from './api-client.js';

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

    const profilesBar = document.getElementById('profiles-vertical-bar');
    const backgroundAnimation = document.getElementById('profile-background-animation');

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

      profilesBar.appendChild(profileItem);
    });

    this.createBackgroundTiles();
    this.updateFocus();
    
    if (this.profiles.length > 0) {
      this.updateBackground(this.profiles[0]);
    }

    // Check if we have a saved profile
    const savedProfileId = stateManager.currentProfileId;
    if (savedProfileId) {
      const profileIndex = this.profiles.findIndex(p => p.id === savedProfileId);
      if (profileIndex >= 0) {
        this.focusedProfileIndex = profileIndex;
        this.selectProfile(savedProfileId);
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

  createBackgroundTiles() {
    const backgroundAnimation = document.getElementById('profile-background-animation');
    
    // Use placeholder images for background tiles
    const placeholderImages = [
      'https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg',
      'https://image.tmdb.org/t/p/w500/1M876KPjulVwppEpldhdc8V4o68.jpg',
      'https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg',
      'https://image.tmdb.org/t/p/w500/fqldf2t8ztc9aiwn3k6mlX3tvRT.jpg',
      'https://image.tmdb.org/t/p/w500/sWgBv7LV2PRoQgkxwlibdGXKz1S.jpg'
    ];

    const rowCount = 25;
    const tilesPerRow = 30;

    for (let row = 0; row < rowCount; row++) {
      const rowElement = document.createElement('div');
      rowElement.className = 'background-row';

      for (let tile = 0; tile < tilesPerRow * 2; tile++) {
        const tileElement = document.createElement('div');
        tileElement.className = 'profile-background-tile';
        const randomImage = placeholderImages[Math.floor(Math.random() * placeholderImages.length)];
        tileElement.style.backgroundImage = `url(${randomImage})`;
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
    this.selectedProfileId = profileId;
    const selectedProfile = this.profiles.find(p => p.id === profileId);

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

    overlay.classList.remove('hidden');
    main.style.display = 'none';
    header.style.display = 'none';

    this.updateFocus();
  }

  hide() {
    this.profileSelectionActive = false;
    const overlay = document.getElementById('profile-selection-overlay');
    const main = document.querySelector('main');
    const header = document.querySelector('header');

    overlay.classList.add('hidden');
    main.style.display = 'block';
    header.style.display = 'block';
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
}
