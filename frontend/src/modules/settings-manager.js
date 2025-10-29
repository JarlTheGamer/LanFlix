import apiClient from './api-client.js';
import stateManager from './data.js';

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
    this.updateFocus();
    
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
      const response = await apiClient.getSettings();
      this.settings = response.settings || {};
      this.applySettings();
    } catch (error) {
      console.error('Failed to load settings:', error);
      this.settings = {};
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

    this.focusedContentIndex = 0;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  initializeCustomSelects() {
    const selects = document.querySelectorAll('.settings-select');

    selects.forEach(select => {
      const wrapper = document.createElement('div');
      wrapper.className = 'custom-select-wrapper';

      const trigger = document.createElement('div');
      trigger.className = 'custom-select-trigger';

      const selectedText = document.createElement('span');
      selectedText.className = 'custom-select-text';
      selectedText.textContent = select.options[select.selectedIndex].text;

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
        
        // Update local settings
        this.settings[settingKey] = settingValue;
        
        // Save to backend
        await this.saveSettings();
      });
    });
  }

  setupModals() {
    document.getElementById('cancel-profile')?.addEventListener('click', () => this.closeModal());
    document.getElementById('cancel-add-profile')?.addEventListener('click', () => this.closeModal());

    document.getElementById('save-profile')?.addEventListener('click', async () => {
      const name = document.getElementById('profile-name').value;
      const profileId = this.currentProfileCard?.dataset.profileId;
      
      if (!name || !profileId || !this.selectedColor) {
        alert('Please fill in all fields');
        return;
      }

      const [primary, secondary] = this.selectedColor.split(',');
      await this.updateExistingProfile(parseInt(profileId), {
        name,
        avatarColorPrimary: primary,
        avatarColorSecondary: secondary
      });
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
   * Save settings to backend
   */
  async saveSettings() {
    try {
      await apiClient.updateSettings(this.settings);
      console.log('Settings saved successfully');
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
      await this.loadProfiles();
      this.closeModal();
      alert('Profile updated successfully!');
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
}
