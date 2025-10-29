// Settings page navigation
let focusedArea = 'back'; // 'back', 'nav', or 'content'
let focusedNavIndex = 0;
let focusedContentIndex = 0;

document.addEventListener('DOMContentLoaded', () => {
  const navItems = document.querySelectorAll('.settings-nav-item');

  // Click handlers for nav items
  navItems.forEach(item => {
    item.addEventListener('click', () => {
      switchToSection(item.dataset.section);
    });
  });

  // Initialize custom dropdowns
  initializeCustomSelects();

  // Initialize focus
  updateSettingsFocus();

  // Handle keyboard navigation
  document.addEventListener('keydown', handleSettingsKeyboard);

  // Add smooth transitions for toggles
  const toggles = document.querySelectorAll('.settings-toggle input');
  toggles.forEach(toggle => {
    toggle.addEventListener('change', (e) => {
      console.log(`Toggle ${e.target.id || 'unnamed'} changed to:`, e.target.checked);
    });
  });

  // Close dropdowns when clicking outside
  document.addEventListener('click', (e) => {
    if (!e.target.closest('.custom-select-wrapper')) {
      closeAllDropdowns();
    }
  });

  // Modal event listeners
  document.getElementById('cancel-profile').addEventListener('click', closeModal);
  document.getElementById('cancel-add-profile').addEventListener('click', closeModal);

  document.getElementById('save-profile').addEventListener('click', () => {
    const name = document.getElementById('profile-name').value;
    console.log('Saving profile:', name, 'with color:', selectedColor);
    closeModal();
  });

  document.getElementById('create-profile').addEventListener('click', () => {
    const name = document.getElementById('new-profile-name').value;
    console.log('Creating profile:', name, 'with color:', selectedColor);
    closeModal();
  });

  // Close modal when clicking overlay
  document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', (e) => {
      if (e.target === overlay) {
        closeModal();
      }
    });
  });

  // Close buttons
  document.querySelectorAll('.modal-close').forEach(btn => {
    btn.addEventListener('click', closeModal);
  });

  // Color picker
  document.querySelectorAll('.color-option').forEach(option => {
    option.addEventListener('click', () => {
      selectColor(option);
    });
  });

  // Profile card click handlers
  document.querySelectorAll('.profile-card').forEach(card => {
    card.addEventListener('click', () => {
      if (card.classList.contains('add-profile')) {
        showAddProfileModal();
      } else {
        showEditProfileModal(card);
      }
    });
  });

  // Profile card edit button handlers
  document.querySelectorAll('.profile-card-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      const card = btn.closest('.profile-card');
      if (card) {
        showEditProfileModal(card);
      }
    });
  });
});

function switchToSection(sectionId) {
  const navItems = document.querySelectorAll('.settings-nav-item');
  const sections = document.querySelectorAll('.settings-section');

  // Update active nav item
  navItems.forEach(nav => nav.classList.remove('active'));
  const targetNav = document.querySelector(`[data-section="${sectionId}"]`);
  if (targetNav) targetNav.classList.add('active');

  // Update active section
  sections.forEach(section => section.classList.remove('active'));
  const targetSection = document.getElementById(sectionId);
  if (targetSection) targetSection.classList.add('active');

  // Reset content focus when switching sections
  focusedContentIndex = 0;

  // Scroll to top of content
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

let selectMode = false;
let currentSelectElement = null;
let selectOptionIndex = 0;

// Custom dropdown functions
function initializeCustomSelects() {
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
        selectOption(wrapper, select, optionBtn, index);
      });

      dropdown.appendChild(optionBtn);
    });

    wrapper.appendChild(trigger);
    wrapper.appendChild(dropdown);

    select.parentNode.insertBefore(wrapper, select);

    trigger.addEventListener('click', (e) => {
      e.stopPropagation();
      toggleDropdown(wrapper);
    });

    wrapper._nativeSelect = select;
  });
}

function toggleDropdown(wrapper) {
  const trigger = wrapper.querySelector('.custom-select-trigger');
  const dropdown = wrapper.querySelector('.custom-select-dropdown');
  const isActive = trigger.classList.contains('active');

  closeAllDropdowns();

  if (!isActive) {
    wrapper.classList.add('active');
    trigger.classList.add('active');
    dropdown.classList.add('active');
    currentSelectElement = wrapper;
    selectMode = true;
    selectOptionIndex = 0;

    const selectedOption = dropdown.querySelector('.custom-select-option.selected');
    if (selectedOption) {
      selectOptionIndex = parseInt(selectedOption.dataset.index);
      updateDropdownFocus(dropdown);
    }
  }
}

function closeAllDropdowns() {
  document.querySelectorAll('.custom-select-wrapper').forEach(wrapper => {
    wrapper.classList.remove('active');
  });
  document.querySelectorAll('.custom-select-trigger').forEach(trigger => {
    trigger.classList.remove('active');
  });
  document.querySelectorAll('.custom-select-dropdown').forEach(dropdown => {
    dropdown.classList.remove('active');
  });
  selectMode = false;
  currentSelectElement = null;
}

function selectOption(wrapper, nativeSelect, optionBtn, index) {
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

  console.log(`${nativeSelect.id} changed to:`, nativeSelect.value);

  closeAllDropdowns();
}

function updateDropdownFocus(dropdown) {
  const options = dropdown.querySelectorAll('.custom-select-option');
  options.forEach(opt => opt.classList.remove('focused'));

  if (options[selectOptionIndex]) {
    options[selectOptionIndex].classList.add('focused');
    options[selectOptionIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }
}

function handleSettingsKeyboard(e) {
  const navItems = Array.from(document.querySelectorAll('.settings-nav-item'));
  const activeSection = document.querySelector('.settings-section.active');

  // Handle modal navigation
  if (modalActive) {
    const elements = getModalInteractiveElements();

    if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      e.preventDefault();
      modalFocusIndex = (modalFocusIndex + 1) % elements.length;
      updateModalFocus();
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      e.preventDefault();
      modalFocusIndex = (modalFocusIndex - 1 + elements.length) % elements.length;
      updateModalFocus();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const element = elements[modalFocusIndex];
      if (element) {
        if (element.classList.contains('color-option')) {
          selectColor(element);
        } else if (element.classList.contains('modal-close')) {
          closeModal();
        } else {
          element.click();
        }
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      closeModal();
    }
    return;
  }

  // Handle select dropdown mode
  if (selectMode && currentSelectElement) {
    const dropdown = currentSelectElement.querySelector('.custom-select-dropdown');
    const options = dropdown.querySelectorAll('.custom-select-option');

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      selectOptionIndex = Math.min(selectOptionIndex + 1, options.length - 1);
      updateDropdownFocus(dropdown);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      selectOptionIndex = Math.max(selectOptionIndex - 1, 0);
      updateDropdownFocus(dropdown);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const selectedOption = options[selectOptionIndex];
      if (selectedOption) {
        const nativeSelect = currentSelectElement._nativeSelect;
        selectOption(currentSelectElement, nativeSelect, selectedOption, selectOptionIndex);
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      closeAllDropdowns();
    }
    return;
  }

  if (focusedArea === 'back') {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      focusedArea = 'nav';
      focusedNavIndex = 0;
      updateSettingsFocus();
    } else if (e.key === 'ArrowRight') {
      e.preventDefault();
      focusedArea = 'content';
      focusedContentIndex = 0;
      updateSettingsFocus();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      window.location.href = 'index.html';
    }
  } else if (focusedArea === 'nav') {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      focusedNavIndex = (focusedNavIndex + 1) % navItems.length;
      updateSettingsFocus();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      if (focusedNavIndex === 0) {
        focusedArea = 'back';
        updateSettingsFocus();
      } else {
        focusedNavIndex = (focusedNavIndex - 1 + navItems.length) % navItems.length;
        updateSettingsFocus();
      }
    } else if (e.key === 'ArrowRight') {
      e.preventDefault();
      focusedArea = 'content';
      focusedContentIndex = 0;
      updateSettingsFocus();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      navItems[focusedNavIndex].click();
    }
  } else if (focusedArea === 'content') {
    const interactiveElements = getInteractiveElements(activeSection);

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      focusedContentIndex = Math.min(focusedContentIndex + 1, interactiveElements.length - 1);
      updateSettingsFocus();
      scrollToFocusedElement(interactiveElements[focusedContentIndex]);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      focusedContentIndex = Math.max(focusedContentIndex - 1, 0);
      updateSettingsFocus();
      scrollToFocusedElement(interactiveElements[focusedContentIndex]);
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      focusedArea = 'nav';
      updateSettingsFocus();
    } else if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      const element = interactiveElements[focusedContentIndex];
      if (element) {
        if (element.classList.contains('settings-toggle')) {
          const checkbox = element.querySelector('input[type="checkbox"]');
          if (checkbox) {
            checkbox.checked = !checkbox.checked;
            checkbox.dispatchEvent(new Event('change'));
          }
        } else if (element.classList.contains('custom-select-wrapper')) {
          toggleDropdown(element);
        } else if (element.tagName === 'BUTTON') {
          element.click();
        } else if (element.classList.contains('profile-card')) {
          // Handle profile card click
          if (element.classList.contains('add-profile')) {
            showAddProfileModal();
          } else {
            const editBtn = element.querySelector('.profile-card-btn');
            if (editBtn) {
              showEditProfileModal(element);
            }
          }
        }
      }
    }
  }
}



let modalActive = false;
let modalFocusIndex = 0;
let selectedColor = null;
let currentProfileCard = null;

function showAddProfileModal() {
  const modal = document.getElementById('add-profile-modal');
  modal.classList.add('active');
  modalActive = true;
  modalFocusIndex = 0;
  selectedColor = null;

  // Clear input
  document.getElementById('new-profile-name').value = '';

  // Select first color by default
  const firstColor = modal.querySelector('.color-option');
  if (firstColor) {
    selectColor(firstColor);
  }

  updateModalFocus();
}

function showEditProfileModal(profileCard) {
  const modal = document.getElementById('edit-profile-modal');
  const profileName = profileCard.querySelector('.profile-card-name').textContent;

  modal.classList.add('active');
  modalActive = true;
  modalFocusIndex = 0;
  currentProfileCard = profileCard;

  // Set current profile name
  document.getElementById('profile-name').value = profileName;

  // Select current color
  const avatar = profileCard.querySelector('.profile-card-avatar');
  const bgStyle = avatar.style.background;
  const colorOption = modal.querySelector(`[data-color]`);
  if (colorOption) {
    selectColor(colorOption);
  }

  updateModalFocus();
}

function closeModal() {
  const modals = document.querySelectorAll('.modal-overlay');
  modals.forEach(modal => modal.classList.remove('active'));
  modalActive = false;
  modalFocusIndex = 0;
  selectedColor = null;
  currentProfileCard = null;
}

function selectColor(colorOption) {
  const modal = colorOption.closest('.modal-overlay');
  modal.querySelectorAll('.color-option').forEach(opt => opt.classList.remove('selected'));
  colorOption.classList.add('selected');
  selectedColor = colorOption.dataset.color;
}

function getModalInteractiveElements() {
  const activeModal = document.querySelector('.modal-overlay.active');
  if (!activeModal) return [];

  const elements = [];

  // Input field
  const input = activeModal.querySelector('.modal-input');
  if (input) elements.push(input);

  // Color options
  activeModal.querySelectorAll('.color-option').forEach(opt => elements.push(opt));

  // Buttons
  activeModal.querySelectorAll('.modal-btn').forEach(btn => elements.push(btn));

  // Close button
  const closeBtn = activeModal.querySelector('.modal-close');
  if (closeBtn) elements.push(closeBtn);

  return elements;
}

function updateModalFocus() {
  const elements = getModalInteractiveElements();
  elements.forEach(el => el.classList.remove('focused'));

  if (elements[modalFocusIndex]) {
    elements[modalFocusIndex].classList.add('focused');
    elements[modalFocusIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }
}

function getInteractiveElements(section) {
  if (!section) return [];

  const elements = [];

  // Get all settings items in DOM order
  const settingsItems = section.querySelectorAll('.settings-item');
  
  settingsItems.forEach((item) => {
    // Find interactive elements within each settings item (wrapper or toggle)
    const wrapper = item.querySelector('.custom-select-wrapper');
    const toggle = item.querySelector('.settings-toggle');
    
    if (wrapper) {
      elements.push(wrapper);
    } else if (toggle) {
      elements.push(toggle);
    }
  });

  // Add other buttons that aren't in settings-item containers
  section.querySelectorAll('.settings-link-btn, .profile-card-btn, .device-remove, .profile-card').forEach(el => {
    if (!elements.includes(el)) {
      elements.push(el);
    }
  });

  return elements;
}

function updateSettingsFocus() {
  const navItems = Array.from(document.querySelectorAll('.settings-nav-item'));
  const activeSection = document.querySelector('.settings-section.active');
  const backBtn = document.querySelector('.back-btn');

  // Clear all focus states
  navItems.forEach(item => item.classList.remove('focused'));
  if (backBtn) backBtn.classList.remove('focused');

  // Clear all settings groups z-index boost
  document.querySelectorAll('.settings-group').forEach(group => {
    group.style.zIndex = '';
  });

  if (activeSection) {
    const interactiveElements = getInteractiveElements(activeSection);
    interactiveElements.forEach(el => el.classList.remove('focused'));

    if (focusedArea === 'content' && interactiveElements[focusedContentIndex]) {
      interactiveElements[focusedContentIndex].classList.add('focused');
      
      // Boost z-index of parent settings-group when dropdown is focused
      const parentGroup = interactiveElements[focusedContentIndex].closest('.settings-group');
      if (parentGroup) {
        parentGroup.style.zIndex = '100';
      }
    }
  }

  if (focusedArea === 'back' && backBtn) {
    backBtn.classList.add('focused');
  } else if (focusedArea === 'nav') {
    navItems[focusedNavIndex].classList.add('focused');
  }
}

function scrollToFocusedElement(element) {
  if (element) {
    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
}
