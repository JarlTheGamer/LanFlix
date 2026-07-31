import apiClient from './api-client.js';
import stateManager from './data.js';
import { appUpdater } from './app-updater.js';

export class SettingsManager {
  constructor() {
    this.focusedArea = 'back';
    this.focusedNavIndex = 0;
    this.focusedContentIndex = 0;
    this.selectMode = false;
    this.currentSelectElement = null;
    this.selectOptionIndex = 0;
    this.modalActive = false;
    this.modalFocusIndex = 0;
    this.selectedColor = null;
    this.currentProfileCard = null;
    this.settings = {};
    this.profiles = [];
  }

  async initialize() {
    // Load settings and profiles from backend
    await this.loadSettings();
    await this.loadProfiles();

    this.setupNavigation();
    this.initializeCustomSelects();
    this.setupToggles();
    this.setupModals();
    this.setupProfiles();
    this.setupUpdateChecker();
    this.updateVersionDisplay();
    this.updateFocus();

    const pairBtn = document.getElementById('pair-device-btn');
    const pairInput = document.getElementById('pair-code-input');
    if (pairBtn && pairInput) {
      pairBtn.addEventListener('click', async () => {
        const code = pairInput.value.trim();
        if (!code) return;
        try {
          await apiClient.pairDevice(code);
          pairInput.value = '';
          alert('Device successfully paired!');
          this.loadDevices();
        } catch (e) {
          alert('Failed to pair device: ' + e.message);
        }
      });
    }

    document.addEventListener('keydown', (e) => this.handleKeyboard(e));
    document.addEventListener('click', (e) => {
      if (!e.target.closest('.custom-select-wrapper')) {
        this.closeAllDropdowns();
      }
    });
  }

  /**
   * Load settings from backend
   */
  async loadSettings() {
    try {
      const profileId = stateManager.currentProfileId;
      
      // Load per-profile settings
      const settingKey = `userSettings_${profileId}`;
      
      try {
        const response = await apiClient.getCustomSetting(settingKey);
        const savedSettings = typeof response.value === 'string'
          ? JSON.parse(response.value)
          : response.value;
        this.settings = savedSettings;
        console.log('Loaded saved settings for profile', profileId, this.settings);
      } catch (error) {
        if (error.statusCode === 404) {
          console.log('No saved settings found for profile', profileId, '- using defaults');
        } else {
          console.error('Error loading settings:', error);
        }
        
        // Default settings
        this.settings = {
          'language': 'en',
          'timezone': 'utc',
          'auto-play-next': true,
          'skip-intro': true,
          'quality': 'auto',
          'data-saver': false,
          'audio-lang': 'en',
          'theme': 'dark',
          'show-backdrop': true
        };
      }
      
      this.applySettings();
    } catch (error) {
      console.error('Failed to load settings:', error);
      this.settings = {
        'language': 'en',
        'timezone': 'utc',
        'auto-play-next': true,
        'skip-intro': true,
        'quality': 'auto',
        'data-saver': false,
        'audio-lang': 'en',
        'theme': 'dark',
        'show-backdrop': true
      };
    }
  }

  /**
   * Load profiles from backend
   */
  async loadProfiles() {
    try {
      const response = await stateManager.getProfiles();
      this.profiles = response || [];
      this.renderProfiles();
    } catch (error) {
      console.error('Failed to load profiles:', error);
      this.profiles = [];
    }
  }

  /**
   * Apply loaded settings to UI
   */
  applySettings() {
    // Apply settings to form elements
    Object.keys(this.settings).forEach(key => {
      const element = document.getElementById(key);
      if (element) {
        if (element.type === 'checkbox') {
          element.checked = this.settings[key];
        } else {
          element.value = this.settings[key];
        }
      }
    });

    // Load streaming preferences for current profile
    this.loadStreamingPreferences();

    // Trigger custom select updates for transcoding settings
    this.updateCustomSelectDisplays();
  }

  /**
   * Update custom select displays after loading settings
   */
  updateCustomSelectDisplays() {
    document.querySelectorAll('.custom-select-wrapper').forEach(wrapper => {
      const nativeSelect = wrapper._nativeSelect;
      if (nativeSelect) {
        const trigger = wrapper.querySelector('.custom-select-trigger');
        const selectedText = trigger?.querySelector('.custom-select-text');
        if (selectedText) {
          selectedText.textContent = nativeSelect.options[nativeSelect.selectedIndex]?.text || 'Select...';
        }

        // Update selected option in dropdown
        const dropdown = wrapper.querySelector('.custom-select-dropdown');
        dropdown?.querySelectorAll('.custom-select-option').forEach((opt, index) => {
          if (index === nativeSelect.selectedIndex) {
            opt.classList.add('selected');
          } else {
            opt.classList.remove('selected');
          }
        });
      }
    });
  }

  /**
   * Load streaming preferences for current profile
   */
  async loadStreamingPreferences() {
    try {
      const profileId = stateManager.currentProfileId;
      if (!profileId) {
        console.log('No profile selected, using default transcoding settings');
        return;
      }

      const settingKey = `streamingPreferences_${profileId}`;

      try {
        const response = await apiClient.getCustomSetting(settingKey);
        const prefs = typeof response.value === 'string'
          ? JSON.parse(response.value)
          : response.value;

        console.log('Loaded streaming preferences:', prefs);

        // Apply to UI
        const transcodingMode = document.getElementById('transcoding-mode');
        if (transcodingMode) {
          transcodingMode.value = prefs.transcodingMode || 'direct-play';
          this.settings['transcoding-mode'] = transcodingMode.value;
        }

        const hwAccel = document.getElementById('use-hardware-accel');
        if (hwAccel) {
          hwAccel.checked = prefs.useHardwareAccel !== false;
          this.settings['use-hardware-accel'] = hwAccel.checked;
        }

        const preset = document.getElementById('transcode-preset');
        if (preset) {
          preset.value = prefs.preset || 'p4';
          this.settings['transcode-preset'] = preset.value;
        }

        const audioTranscoding = document.getElementById('audio-transcoding');
        if (audioTranscoding) {
          audioTranscoding.checked = prefs.audioTranscoding !== false;
          this.settings['audio-transcoding'] = audioTranscoding.checked;
        }

        const videoTranscoding = document.getElementById('video-transcoding');
        if (videoTranscoding) {
          videoTranscoding.checked = prefs.videoTranscoding !== false;
          this.settings['video-transcoding'] = videoTranscoding.checked;
        }

        // Update custom select displays
        this.updateCustomSelectDisplays();
      } catch (error) {
        if (error.statusCode === 404) {
          console.log('No saved streaming preferences found for profile', profileId);
        } else {
          console.error('Error loading streaming preferences:', error);
        }
      }
    } catch (error) {
      console.error('Failed to load streaming preferences:', error);
    }
  }

  /**
   * Render profiles in settings
   */
  renderProfiles() {
    const profilesContainer = document.querySelector('.profiles-grid');
    if (!profilesContainer) return;

    // Clear existing profiles (except add button)
    const existingProfiles = profilesContainer.querySelectorAll('.profile-card:not(.add-profile)');
    existingProfiles.forEach(card => card.remove());

    // Add profile cards
    this.profiles.forEach(profile => {
      const profileCard = document.createElement('div');
      profileCard.className = 'profile-card';
      profileCard.dataset.profileId = profile.id;

      profileCard.innerHTML = `
        <div class="profile-card-avatar" style="background: linear-gradient(135deg, ${profile.avatarColorPrimary}, ${profile.avatarColorSecondary})"></div>
        <div class="profile-card-name">${profile.name}</div>
        <button class="profile-card-btn">Edit</button>
      `;

      // Insert before add button
      const addButton = profilesContainer.querySelector('.add-profile');
      if (addButton) {
        profilesContainer.insertBefore(profileCard, addButton);
      } else {
        profilesContainer.appendChild(profileCard);
      }
    });

    // Re-setup profile handlers
    this.setupProfiles();
  }

  setupNavigation() {
    const navItems = document.querySelectorAll('.settings-nav-item');
    navItems.forEach(item => {
      item.addEventListener('click', () => {
        this.switchToSection(item.dataset.section);
      });
    });
  }

  switchToSection(sectionId) {
    const navItems = document.querySelectorAll('.settings-nav-item');
    const sections = document.querySelectorAll('.settings-section');

    navItems.forEach(nav => nav.classList.remove('active'));
    const targetNav = document.querySelector(`[data-section="${sectionId}"]`);
    if (targetNav) targetNav.classList.add('active');

    sections.forEach(section => section.classList.remove('active'));
    const targetSection = document.getElementById(sectionId);
    if (targetSection) targetSection.classList.add('active');

    if (sectionId === 'devices') {
      this.loadDevices();
    }

    this.focusedContentIndex = 0;
  }

  async loadDevices() {
    try {
      const devices = await apiClient.getAllDevices();
      this.renderDevices(devices);

      const toggle = document.getElementById('require-pairing-toggle');
      if (toggle) {
        try {
          const res = await apiClient.request('/devices/require-pairing');
          toggle.checked = res.requirePairing !== false;
        } catch (e) {}

        if (!toggle.dataset.listenerAttached) {
          toggle.dataset.listenerAttached = 'true';
          toggle.addEventListener('change', async () => {
            try {
              await apiClient.request('/devices/require-pairing', {
                method: 'POST',
                body: JSON.stringify({ enabled: toggle.checked })
              });
            } catch (e) {
              console.error('Failed to save require pairing setting:', e);
            }
          });
        }
      }
    } catch (e) {
      console.error('Failed to load devices:', e);
    }
  }

  renderDevices(devices) {
    const container = document.getElementById('device-list-container');
    if (!container) return;

    if (!devices || devices.length === 0) {
      container.innerHTML = '<div style="color: #888; padding: 12px 0;">No devices registered yet.</div>';
      return;
    }

    const currentDeviceId = apiClient.getDeviceId();

    container.innerHTML = devices.map(dev => `
      <div class="device-item" style="display: flex; align-items: center; justify-content: space-between; padding: 14px 18px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.08); border-radius: 12px;">
        <div style="display: flex; align-items: center; gap: 14px;">
          <div style="width: 42px; height: 42px; background: ${dev.isPaired ? 'rgba(38,222,129,0.15)' : 'rgba(255,167,38,0.15)'}; border-radius: 10px; display: flex; align-items: center; justify-content: center; color: ${dev.isPaired ? '#26de81' : '#ffa726'};">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
          </div>
          <div>
            <div style="font-weight: 600; color: #fff; font-size: 0.95rem; display: flex; align-items: center; gap: 8px;">
              ${dev.deviceName}
              ${dev.deviceId === currentDeviceId ? '<span style="background: rgba(229,9,20,0.2); color: #e50914; font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: 700;">THIS DEVICE</span>' : ''}
              ${dev.isPaired ? '<span style="background: rgba(38,222,129,0.2); color: #26de81; font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: 700;">PAIRED</span>' : '<span style="background: rgba(255,167,38,0.2); color: #ffa726; font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: 700;">CODE: ' + dev.pairingCode + '</span>'}
            </div>
            <div style="font-size: 0.8rem; color: #888; margin-top: 2px;">
              IP: ${dev.ipAddress} &bull; Code: <strong style="color: #fff; font-family: monospace;">${dev.pairingCode}</strong> &bull; Last Seen: ${new Date(dev.lastSeen).toLocaleTimeString()}
            </div>
          </div>
        </div>
        <div>
          ${!dev.isPaired ? `<button class="action-btn pair-code-btn" data-code="${dev.pairingCode}" style="background: #26de81; color: #000; border: none; padding: 8px 16px; font-weight: 700; border-radius: 6px; cursor: pointer; margin-right: 8px; height: 34px; display: inline-flex; align-items: center; white-space: nowrap;">Approve</button>` : ''}
          <button class="action-btn unpair-device-btn" data-device-id="${dev.deviceId}" style="background: rgba(255,255,255,0.08); color: #ff6b6b; border: 1px solid rgba(255,107,107,0.3); padding: 8px 16px; font-weight: 600; border-radius: 6px; cursor: pointer; height: 34px; display: inline-flex; align-items: center; white-space: nowrap;">${dev.isPaired ? 'Unpair' : 'Reject'}</button>
        </div>
      </div>
    `).join('');

    // Attach button events
    container.querySelectorAll('.pair-code-btn').forEach(btn => {
      btn.addEventListener('click', async () => {
        const code = btn.dataset.code;
        try {
          await apiClient.pairDevice(code);
          this.loadDevices();
        } catch (e) {
          alert('Failed to pair device: ' + e.message);
        }
      });
    });

    container.querySelectorAll('.unpair-device-btn').forEach(btn => {
      btn.addEventListener('click', async () => {
        const devId = btn.dataset.deviceId;
        if (confirm('Are you sure you want to unpair/remove this device?')) {
          try {
            await apiClient.unpairDevice(devId);
            this.loadDevices();
          } catch (e) {
            alert('Failed to unpair device: ' + e.message);
          }
        }
      });
    });
  }

  initializeCustomSelects() {
    const selects = document.querySelectorAll('.settings-select');

    selects.forEach(select => {
      // Skip if already wrapped
      if (select.parentElement?.classList.contains('custom-select-wrapper')) {
        return;
      }

      const wrapper = document.createElement('div');
      wrapper.className = 'custom-select-wrapper';

      const trigger = document.createElement('div');
      trigger.className = 'custom-select-trigger';

      const selectedText = document.createElement('span');
      selectedText.className = 'custom-select-text';
      selectedText.textContent = select.options?.[select.selectedIndex]?.text || 'Select...';

      const arrow = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      arrow.setAttribute('class', 'custom-select-arrow');
      arrow.setAttribute('viewBox', '0 0 24 24');
      const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('d', 'M7 10l5 5 5-5z');
      arrow.appendChild(path);

      trigger.appendChild(selectedText);
      trigger.appendChild(arrow);

      const dropdown = document.createElement('div');
      dropdown.className = 'custom-select-dropdown';

      Array.from(select.options).forEach((option, index) => {
        const optionBtn = document.createElement('button');
        optionBtn.className = 'custom-select-option';
        optionBtn.textContent = option.text;
        optionBtn.dataset.value = option.value;
        optionBtn.dataset.index = index;

        if (index === select.selectedIndex) {
          optionBtn.classList.add('selected');
        }

        optionBtn.addEventListener('click', (e) => {
          e.stopPropagation();
          this.selectOption(wrapper, select, optionBtn, index);
        });

        dropdown.appendChild(optionBtn);
      });

      wrapper.appendChild(trigger);
      wrapper.appendChild(dropdown);
      select.parentNode.insertBefore(wrapper, select);

      trigger.addEventListener('click', (e) => {
        e.stopPropagation();
        this.toggleDropdown(wrapper);
      });

      wrapper._nativeSelect = select;
    });
  }

  toggleDropdown(wrapper) {
    const trigger = wrapper.querySelector('.custom-select-trigger');
    const dropdown = wrapper.querySelector('.custom-select-dropdown');
    const isActive = trigger.classList.contains('active');

    this.closeAllDropdowns();

    if (!isActive) {
      wrapper.classList.add('active');
      trigger.classList.add('active');
      dropdown.classList.add('active');
      this.currentSelectElement = wrapper;
      this.selectMode = true;
      this.selectOptionIndex = 0;

      const selectedOption = dropdown.querySelector('.custom-select-option.selected');
      if (selectedOption) {
        this.selectOptionIndex = parseInt(selectedOption.dataset.index);
        this.updateDropdownFocus(dropdown);
      }
    }
  }

  closeAllDropdowns() {
    document.querySelectorAll('.custom-select-wrapper').forEach(wrapper => {
      wrapper.classList.remove('active');
    });
    document.querySelectorAll('.custom-select-trigger').forEach(trigger => {
      trigger.classList.remove('active');
    });
    document.querySelectorAll('.custom-select-dropdown').forEach(dropdown => {
      dropdown.classList.remove('active');
    });
    this.selectMode = false;
    this.currentSelectElement = null;
  }

  async selectOption(wrapper, nativeSelect, optionBtn, index) {
    const trigger = wrapper.querySelector('.custom-select-trigger');
    const selectedText = trigger.querySelector('.custom-select-text');
    const dropdown = wrapper.querySelector('.custom-select-dropdown');

    dropdown.querySelectorAll('.custom-select-option').forEach(opt => {
      opt.classList.remove('selected');
    });

    optionBtn.classList.add('selected');
    selectedText.textContent = optionBtn.textContent;

    nativeSelect.selectedIndex = index;
    nativeSelect.dispatchEvent(new Event('change'));

    const settingKey = nativeSelect.id;
    const settingValue = nativeSelect.value;

    console.log(`${settingKey} changed to:`, settingValue);

    // Update local settings
    this.settings[settingKey] = settingValue;

    // Save to backend
    await this.saveSettings();

    this.closeAllDropdowns();
  }

  updateDropdownFocus(dropdown) {
    const options = dropdown.querySelectorAll('.custom-select-option');
    options.forEach(opt => opt.classList.remove('focused'));

    if (options[this.selectOptionIndex]) {
      options[this.selectOptionIndex].classList.add('focused');
      options[this.selectOptionIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  }

  setupToggles() {
    const toggles = document.querySelectorAll('.settings-toggle input');
    toggles.forEach(toggle => {
      toggle.addEventListener('change', async (e) => {
        const settingKey = e.target.id;
        const settingValue = e.target.checked;

        console.log(`Toggle ${settingKey} changed to:`, settingValue);

        // Handle streaming transcode toggles specially
        if (settingKey === 'audio-transcoding' || settingKey === 'video-transcoding' ||
          settingKey === 'use-hardware-accel') {
          await this.saveStreamingPreferences();
        } else {
          // Update local settings
          this.settings[settingKey] = settingValue;

          // Save to backend
          await this.saveSettings();
        }
      });
    });

    // Handle transcoding mode and preset select changes
    const transcodingModeSelect = document.getElementById('transcoding-mode');
    if (transcodingModeSelect) {
      transcodingModeSelect.addEventListener('change', async () => {
        this.handleTranscodingModeChange();
        await this.saveStreamingPreferences();
      });
      // Initialize on load
      this.handleTranscodingModeChange();
    }

    const presetSelect = document.getElementById('transcode-preset');
    if (presetSelect) {
      presetSelect.addEventListener('change', async () => {
        await this.saveStreamingPreferences();
      });
    }
  }

  /**
   * Handle transcoding mode change - show/hide custom warning and enable/disable toggles
   */
  handleTranscodingModeChange() {
    const transcodingMode = document.getElementById('transcoding-mode')?.value;
    const customWarning = document.getElementById('custom-mode-warning');
    const audioTranscodingToggle = document.getElementById('audio-transcoding');
    const videoTranscodingToggle = document.getElementById('video-transcoding');

    if (transcodingMode === 'custom') {
      // Show warning
      if (customWarning) customWarning.style.display = 'flex';
      
      // Enable toggles
      if (audioTranscodingToggle) audioTranscodingToggle.disabled = false;
      if (videoTranscodingToggle) videoTranscodingToggle.disabled = false;
    } else {
      // Hide warning
      if (customWarning) customWarning.style.display = 'none';
      
      // Disable toggles (they're controlled by the mode)
      if (audioTranscodingToggle) audioTranscodingToggle.disabled = true;
      if (videoTranscodingToggle) videoTranscodingToggle.disabled = true;
    }
  }

  /**
   * Save streaming preferences to profile settings
   */
  async saveStreamingPreferences() {
    try {
      const profileId = stateManager.currentProfileId;
      if (!profileId) {
        console.error('No profile selected, cannot save streaming preferences');
        alert('Please select a profile first');
        return;
      }

      const transcodingMode = document.getElementById('transcoding-mode')?.value ?? 'direct-play';
      const audioTranscoding = document.getElementById('audio-transcoding')?.checked ?? true;
      const videoTranscoding = document.getElementById('video-transcoding')?.checked ?? true;
      const useHardwareAccel = document.getElementById('use-hardware-accel')?.checked ?? true;
      const preset = document.getElementById('transcode-preset')?.value ?? 'p4';

      const streamingPreferences = {
        transcodingMode,
        audioTranscoding,
        videoTranscoding,
        useHardwareAccel,
        preset
      };

      console.log('Saving streaming preferences for profile', profileId, ':', streamingPreferences);

      // Save to custom settings
      const settingKey = `streamingPreferences_${profileId}`;
      await apiClient.saveCustomSetting(settingKey, JSON.stringify(streamingPreferences));

      // Update local settings cache
      this.settings['transcoding-mode'] = transcodingMode;
      this.settings['audio-transcoding'] = audioTranscoding;
      this.settings['video-transcoding'] = videoTranscoding;
      this.settings['use-hardware-accel'] = useHardwareAccel;
      this.settings['transcode-preset'] = preset;

      console.log('✅ Streaming preferences saved successfully');
      this.showSaveNotification('Transcoding settings saved!');
    } catch (error) {
      console.error('Failed to save streaming preferences:', error);
      alert('Failed to save streaming preferences. Please try again.');
    }
  }

  /**
   * Show save notification
   */
  showSaveNotification(message) {
    let notification = document.getElementById('settings-save-notification');

    if (!notification) {
      notification = document.createElement('div');
      notification.id = 'settings-save-notification';
      notification.style.cssText = `
        position: fixed;
        top: 80px;
        right: 20px;
        background: #4caf50;
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        z-index: 10000;
        font-size: 14px;
        font-weight: 500;
        opacity: 0;
        transition: opacity 0.3s;
      `;
      document.body.appendChild(notification);
    }

    notification.textContent = message;
    notification.style.opacity = '1';

    setTimeout(() => {
      notification.style.opacity = '0';
    }, 2000);
  }

  setupModals() {
    document.getElementById('cancel-profile')?.addEventListener('click', () => this.closeModal());
    document.getElementById('cancel-add-profile')?.addEventListener('click', () => this.closeModal());

    document.getElementById('save-profile')?.addEventListener('click', async () => {
      const name = document.getElementById('profile-name').value;
      const pinCode = document.getElementById('profile-pin')?.value?.trim() || '';
      const profileId = this.currentProfileCard?.dataset.profileId;

      if (!name || !profileId) {
        alert('Please fill in profile name');
        return;
      }

      if (pinCode && !/^\d{4}$/.test(pinCode)) {
        alert('PIN code must be exactly 4 digits (e.g. 1234)');
        return;
      }

      const updates = { name, pinCode };
      if (this.selectedColor) {
        const [primary, secondary] = this.selectedColor.split(',');
        updates.avatarColorPrimary = primary;
        updates.avatarColorSecondary = secondary;
      }

      await this.updateExistingProfile(parseInt(profileId), updates);
    });

    document.getElementById('create-profile')?.addEventListener('click', async () => {
      const name = document.getElementById('new-profile-name').value;

      if (!name || !this.selectedColor) {
        alert('Please fill in all fields');
        return;
      }

      const [primary, secondary] = this.selectedColor.split(',');
      await this.createNewProfile(name, primary, secondary);
    });

    document.querySelectorAll('.modal-overlay').forEach(overlay => {
      overlay.addEventListener('click', (e) => {
        if (e.target === overlay) {
          this.closeModal();
        }
      });
    });

    document.querySelectorAll('.modal-close').forEach(btn => {
      btn.addEventListener('click', () => this.closeModal());
    });

    document.querySelectorAll('.color-option').forEach(option => {
      option.addEventListener('click', () => {
        this.selectColor(option);
      });
    });
  }

  setupProfiles() {
    document.querySelectorAll('.profile-card').forEach(card => {
      card.addEventListener('click', () => {
        if (card.classList.contains('add-profile')) {
          this.showAddProfileModal();
        } else {
          this.showEditProfileModal(card);
        }
      });
    });

    document.querySelectorAll('.profile-card-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const card = btn.closest('.profile-card');
        if (card) {
          this.showEditProfileModal(card);
        }
      });
    });
  }

  showAddProfileModal() {
    const modal = document.getElementById('add-profile-modal');
    modal.classList.add('active');
    this.modalActive = true;
    this.modalFocusIndex = 0;
    this.selectedColor = null;

    document.getElementById('new-profile-name').value = '';

    const firstColor = modal.querySelector('.color-option');
    if (firstColor) {
      this.selectColor(firstColor);
    }

    this.updateModalFocus();
  }

  showEditProfileModal(profileCard) {
    const modal = document.getElementById('edit-profile-modal');
    const profileName = profileCard.querySelector('.profile-card-name').textContent;

    modal.classList.add('active');
    this.modalActive = true;
    this.modalFocusIndex = 0;
    this.currentProfileCard = profileCard;

    document.getElementById('profile-name').value = profileName;
    const pinInput = document.getElementById('profile-pin');
    if (pinInput) pinInput.value = '';

    const colorOption = modal.querySelector(`[data-color]`);
    if (colorOption) {
      this.selectColor(colorOption);
    }

    this.updateModalFocus();
  }

  closeModal() {
    const modals = document.querySelectorAll('.modal-overlay');
    modals.forEach(modal => modal.classList.remove('active'));
    this.modalActive = false;
    this.modalFocusIndex = 0;
    this.selectedColor = null;
    this.currentProfileCard = null;
  }

  selectColor(colorOption) {
    const modal = colorOption.closest('.modal-overlay');
    modal.querySelectorAll('.color-option').forEach(opt => opt.classList.remove('selected'));
    colorOption.classList.add('selected');
    // Store color as "primary,secondary" format
    this.selectedColor = colorOption.dataset.color;
  }

  getModalInteractiveElements() {
    const activeModal = document.querySelector('.modal-overlay.active');
    if (!activeModal) return [];

    const elements = [];

    const input = activeModal.querySelector('.modal-input');
    if (input) elements.push(input);

    activeModal.querySelectorAll('.color-option').forEach(opt => elements.push(opt));
    activeModal.querySelectorAll('.modal-btn').forEach(btn => elements.push(btn));

    const closeBtn = activeModal.querySelector('.modal-close');
    if (closeBtn) elements.push(closeBtn);

    return elements;
  }

  updateModalFocus() {
    const elements = this.getModalInteractiveElements();
    elements.forEach(el => el.classList.remove('focused'));

    if (elements[this.modalFocusIndex]) {
      elements[this.modalFocusIndex].classList.add('focused');
      elements[this.modalFocusIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  }

  getInteractiveElements(section) {
    if (!section) return [];

    const elements = [];
    const settingsItems = section.querySelectorAll('.settings-item');

    settingsItems.forEach((item) => {
      const wrapper = item.querySelector('.custom-select-wrapper');
      const toggle = item.querySelector('.settings-toggle');

      if (wrapper) {
        elements.push(wrapper);
      } else if (toggle) {
        elements.push(toggle);
      }
    });

    section.querySelectorAll('.settings-link-btn, .profile-card-btn, .device-remove, .profile-card').forEach(el => {
      if (!elements.includes(el)) {
        elements.push(el);
      }
    });

    return elements;
  }

  handleKeyboard(e) {
    const navItems = Array.from(document.querySelectorAll('.settings-nav-item'));
    const activeSection = document.querySelector('.settings-section.active');

    if (this.modalActive) {
      const elements = this.getModalInteractiveElements();

      if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
        e.preventDefault();
        this.modalFocusIndex = (this.modalFocusIndex + 1) % elements.length;
        this.updateModalFocus();
      } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
        e.preventDefault();
        this.modalFocusIndex = (this.modalFocusIndex - 1 + elements.length) % elements.length;
        this.updateModalFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const element = elements[this.modalFocusIndex];
        if (element) {
          if (element.classList.contains('color-option')) {
            this.selectColor(element);
          } else if (element.classList.contains('modal-close')) {
            this.closeModal();
          } else {
            element.click();
          }
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        this.closeModal();
      }
      return;
    }

    if (this.selectMode && this.currentSelectElement) {
      const dropdown = this.currentSelectElement.querySelector('.custom-select-dropdown');
      const options = dropdown.querySelectorAll('.custom-select-option');

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.selectOptionIndex = Math.min(this.selectOptionIndex + 1, options.length - 1);
        this.updateDropdownFocus(dropdown);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        this.selectOptionIndex = Math.max(this.selectOptionIndex - 1, 0);
        this.updateDropdownFocus(dropdown);
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const selectedOption = options[this.selectOptionIndex];
        if (selectedOption) {
          const nativeSelect = this.currentSelectElement._nativeSelect;
          this.selectOption(this.currentSelectElement, nativeSelect, selectedOption, this.selectOptionIndex);
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        this.closeAllDropdowns();
      }
      return;
    }

    if (this.focusedArea === 'back') {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedArea = 'nav';
        this.focusedNavIndex = 0;
        this.updateFocus();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        this.focusedArea = 'content';
        this.focusedContentIndex = 0;
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        window.location.href = 'index.html';
      }
    } else if (this.focusedArea === 'nav') {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedNavIndex = (this.focusedNavIndex + 1) % navItems.length;
        this.updateFocus();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (this.focusedNavIndex === 0) {
          this.focusedArea = 'back';
          this.updateFocus();
        } else {
          this.focusedNavIndex = (this.focusedNavIndex - 1 + navItems.length) % navItems.length;
          this.updateFocus();
        }
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        this.focusedArea = 'content';
        this.focusedContentIndex = 0;
        this.updateFocus();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        navItems[this.focusedNavIndex].click();
      }
    } else if (this.focusedArea === 'content') {
      const interactiveElements = this.getInteractiveElements(activeSection);

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        this.focusedContentIndex = Math.min(this.focusedContentIndex + 1, interactiveElements.length - 1);
        this.updateFocus();
        this.scrollToFocusedElement(interactiveElements[this.focusedContentIndex]);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        this.focusedContentIndex = Math.max(this.focusedContentIndex - 1, 0);
        this.updateFocus();
        this.scrollToFocusedElement(interactiveElements[this.focusedContentIndex]);
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        this.focusedArea = 'nav';
        this.updateFocus();
      } else if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        const element = interactiveElements[this.focusedContentIndex];
        if (element) {
          if (element.classList.contains('settings-toggle')) {
            const checkbox = element.querySelector('input[type="checkbox"]');
            if (checkbox) {
              checkbox.checked = !checkbox.checked;
              checkbox.dispatchEvent(new Event('change'));
            }
          } else if (element.classList.contains('custom-select-wrapper')) {
            this.toggleDropdown(element);
          } else if (element.tagName === 'BUTTON') {
            element.click();
          } else if (element.classList.contains('profile-card')) {
            if (element.classList.contains('add-profile')) {
              this.showAddProfileModal();
            } else {
              const editBtn = element.querySelector('.profile-card-btn');
              if (editBtn) {
                this.showEditProfileModal(element);
              }
            }
          }
        }
      }
    }
  }

  updateFocus() {
    const navItems = Array.from(document.querySelectorAll('.settings-nav-item'));
    const activeSection = document.querySelector('.settings-section.active');
    const backBtn = document.querySelector('.back-btn');

    navItems.forEach(item => item.classList.remove('focused'));
    if (backBtn) backBtn.classList.remove('focused');

    document.querySelectorAll('.settings-group').forEach(group => {
      group.style.zIndex = '';
    });

    if (activeSection) {
      const interactiveElements = this.getInteractiveElements(activeSection);
      interactiveElements.forEach(el => el.classList.remove('focused'));

      if (this.focusedArea === 'content' && interactiveElements[this.focusedContentIndex]) {
        interactiveElements[this.focusedContentIndex].classList.add('focused');

        const parentGroup = interactiveElements[this.focusedContentIndex].closest('.settings-group');
        if (parentGroup) {
          parentGroup.style.zIndex = '100';
        }
      }
    }

    if (this.focusedArea === 'back' && backBtn) {
      backBtn.classList.add('focused');
    } else if (this.focusedArea === 'nav') {
      navItems[this.focusedNavIndex].classList.add('focused');
    }
  }

  scrollToFocusedElement(element) {
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  /**
   * Save settings to backend (per-profile)
   */
  async saveSettings() {
    try {
      const profileId = stateManager.currentProfileId;
      if (!profileId) {
        console.warn('No profile selected, settings not saved');
        return;
      }

      // Save per-profile settings
      const settingKey = `userSettings_${profileId}`;
      const settingValue = JSON.stringify(this.settings);
      
      await apiClient.saveCustomSetting(settingKey, settingValue);

      console.log('Settings saved successfully for profile', profileId);
      this.showSaveNotification('Settings saved!');
    } catch (error) {
      console.error('Failed to save settings:', error);
      alert('Failed to save settings. Please try again.');
    }
  }

  /**
   * Create new profile
   */
  async createNewProfile(name, colorPrimary, colorSecondary) {
    try {
      await apiClient.createProfile(name, colorPrimary, colorSecondary);
      await this.loadProfiles();
      this.closeModal();
      alert('Profile created successfully!');
    } catch (error) {
      console.error('Failed to create profile:', error);
      alert('Failed to create profile. Please try again.');
    }
  }

  /**
   * Update existing profile
   */
  async updateExistingProfile(profileId, updates) {
    try {
      await apiClient.updateProfile(profileId, updates);
      this.closeModal();
      this.showSaveNotification('Profile updated successfully!');
      setTimeout(() => location.reload(), 500);
    } catch (error) {
      console.error('Failed to update profile:', error);
      alert('Failed to update profile. Please try again.');
    }
  }

  /**
   * Delete profile
   */
  async deleteExistingProfile(profileId) {
    if (!confirm('Are you sure you want to delete this profile?')) {
      return;
    }

    try {
      await apiClient.deleteProfile(profileId);
      await this.loadProfiles();
      alert('Profile deleted successfully!');
    } catch (error) {
      console.error('Failed to delete profile:', error);
      alert('Failed to delete profile. Please try again.');
    }
  }

  setupUpdateChecker() {
    const checkBtn = document.getElementById('check-updates-btn');
    if (!checkBtn) return;

    checkBtn.addEventListener('click', async () => {
      await appUpdater.checkForUpdates(true);
    });
  }

  async updateVersionDisplay() {
    try {
      const response = await fetch('/api/server-update/version', { cache: 'no-store' });
      if (response.ok) {
        const data = await response.json();
        const versionEl = document.getElementById('app-version');
        const buildEl = document.getElementById('app-build-number');
        if (versionEl && data.version) {
          versionEl.textContent = data.version;
        }
        if (buildEl && data.version) {
          buildEl.textContent = `v${data.version}`;
        }
      }
    } catch (e) {
      console.warn('Failed to fetch app version for settings page:', e);
    }
  }

  async checkForServerUpdates(userInitiated = true) {
    const checkBtn = document.getElementById('check-updates-btn');
    const originalHtml = checkBtn ? checkBtn.innerHTML : '';

    try {
      if (checkBtn && userInitiated) {
        checkBtn.disabled = true;
        checkBtn.innerHTML = `Checking...`;
      }

      const response = await fetch('/api/server-update/check');
      const data = await response.json();

      if (checkBtn && userInitiated) {
        checkBtn.disabled = false;
        checkBtn.innerHTML = originalHtml;
      }

      if (data.updateAvailable) {
        this.showOtaModal(data);
      } else {
        if (userInitiated) {
          alert(`Lanflix is up to date! (Current version: ${data.currentVersion || 'v1.2.6'})`);
        }
      }
    } catch (error) {
      console.error('Failed to check for updates:', error);
      if (checkBtn && userInitiated) {
        checkBtn.disabled = false;
        checkBtn.innerHTML = originalHtml;
        alert('Could not check for updates. Make sure server has internet access.');
      }
    }
  }

  showOtaModal(updateData) {
    const modal = document.getElementById('ota-update-modal');
    const versionText = document.getElementById('ota-version-text');
    const releaseNotes = document.getElementById('ota-release-notes');
    const applyBtn = document.getElementById('apply-ota-btn');
    const infoContainer = document.getElementById('ota-update-info');
    const progressContainer = document.getElementById('ota-progress-container');
    const modalFooter = document.getElementById('ota-modal-footer');

    if (!modal) return;

    if (versionText) versionText.textContent = `Version ${updateData.latestVersion}`;
    if (releaseNotes) releaseNotes.textContent = updateData.releaseNotes || 'New features, bug fixes, and performance enhancements.';

    if (infoContainer) infoContainer.style.display = 'block';
    if (progressContainer) progressContainer.style.display = 'none';
    if (modalFooter) modalFooter.style.display = 'flex';

    modal.classList.add('active');

    if (applyBtn) {
      applyBtn.onclick = async () => {
        await this.applyServerUpdate(updateData.downloadUrl);
      };
    }
  }

  hideOtaModal() {
    const modal = document.getElementById('ota-update-modal');
    if (modal) modal.classList.remove('active');
  }

  async applyServerUpdate(downloadUrl) {
    const infoContainer = document.getElementById('ota-update-info');
    const progressContainer = document.getElementById('ota-progress-container');
    const modalFooter = document.getElementById('ota-modal-footer');
    const progressBar = document.getElementById('ota-progress-bar');
    const progressStatus = document.getElementById('ota-progress-status');
    const progressSubtext = document.getElementById('ota-progress-subtext');

    if (infoContainer) infoContainer.style.display = 'none';
    if (modalFooter) modalFooter.style.display = 'none';
    if (progressContainer) progressContainer.style.display = 'block';

    if (progressBar) progressBar.style.width = '30%';
    if (progressStatus) progressStatus.textContent = 'Downloading update package from GitHub...';

    try {
      const response = await fetch('/api/server-update/apply', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ downloadUrl: downloadUrl })
      });

      if (progressBar) progressBar.style.width = '80%';
      if (progressStatus) progressStatus.textContent = 'Extracting and applying update...';

      if (response.ok) {
        if (progressBar) progressBar.style.width = '100%';
        if (progressStatus) progressStatus.textContent = 'Update Applied! Restarting Lanflix...';
        if (progressSubtext) progressSubtext.textContent = 'The app will reload in a few seconds.';

        setTimeout(() => {
          window.location.reload();
        }, 4000);
      } else {
        const errorData = await response.json();
        alert(`Update failed: ${errorData.error || 'Server error'}`);
        this.hideOtaModal();
      }
    } catch (error) {
      console.error('Error applying update:', error);
      alert('Failed to connect to server during update application.');
      this.hideOtaModal();
    }
  }
}
