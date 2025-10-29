import { PROFILES } from './data.js';

export class ProfileManager {
  constructor() {
    this.selectedProfileId = null;
    this.profileSelectionActive = true;
    this.focusedProfileIndex = 0;
  }

  initialize() {
    const profilesBar = document.getElementById('profiles-vertical-bar');
    const backgroundAnimation = document.getElementById('profile-background-animation');

    // Create profile items
    PROFILES.forEach((profile, index) => {
      const profileItem = document.createElement('div');
      profileItem.className = 'profile-item';
      profileItem.dataset.profileId = profile.id;
      profileItem.dataset.index = index;

      profileItem.innerHTML = `
        <div class="profile-avatar-large" style="background: linear-gradient(135deg, ${profile.avatar.primary}, ${profile.avatar.secondary})">
        </div>
        <div class="profile-name">${profile.name}</div>
      `;

      profilesBar.appendChild(profileItem);
    });

    this.createBackgroundTiles();
    this.updateFocus();
    this.updateBackground(PROFILES[0]);
  }

  createBackgroundTiles() {
    const backgroundAnimation = document.getElementById('profile-background-animation');
    const allShows = PROFILES.flatMap(profile => profile.watchedShows);

    const rowCount = 25;
    const tilesPerRow = 30;

    for (let row = 0; row < rowCount; row++) {
      const rowElement = document.createElement('div');
      rowElement.className = 'background-row';

      for (let tile = 0; tile < tilesPerRow * 2; tile++) {
        const tileElement = document.createElement('div');
        tileElement.className = 'profile-background-tile';
        const randomShow = allShows[Math.floor(Math.random() * allShows.length)];
        tileElement.style.backgroundImage = `url(${randomShow.image})`;
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

    const focusedProfile = PROFILES[this.focusedProfileIndex];
    if (focusedProfile) {
      this.updateBackground(focusedProfile);
    }
  }

  updateBackground(profile) {
    const backgroundTiles = document.querySelectorAll('.profile-background-tile');

    backgroundTiles.forEach((tile, index) => {
      if (index % 4 === 0) {
        const randomShow = profile.watchedShows[Math.floor(Math.random() * profile.watchedShows.length)];
        tile.style.backgroundImage = `url(${randomShow.image})`;
        tile.style.opacity = '0.6';
      } else {
        tile.style.opacity = '0.3';
      }
    });
  }

  selectProfile(profileId) {
    this.selectedProfileId = profileId;
    const selectedProfile = PROFILES.find(p => p.id === profileId);

    const profileButton = document.querySelector('.profile');
    const profileAvatar = document.querySelector('.profile-avatar');
    if (profileButton && profileAvatar && selectedProfile) {
      profileAvatar.style.background = `linear-gradient(135deg, ${selectedProfile.avatar.primary}, ${selectedProfile.avatar.secondary})`;
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
      this.focusedProfileIndex = this.focusedProfileIndex > 0 ? this.focusedProfileIndex - 1 : PROFILES.length - 1;
      this.updateFocus();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.focusedProfileIndex = this.focusedProfileIndex < PROFILES.length - 1 ? this.focusedProfileIndex + 1 : 0;
      this.updateFocus();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const selectedProfile = PROFILES[this.focusedProfileIndex];
      if (selectedProfile) {
        this.selectProfile(selectedProfile.id);
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      this.hide();
    }
  }
}
