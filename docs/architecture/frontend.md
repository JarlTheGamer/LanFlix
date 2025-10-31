# Frontend Architecture

Deep dive into Lanflix frontend structure and modules.

## Technology Stack

- **Build Tool**: Vite
- **Language**: Vanilla JavaScript (ES6+)
- **Video Player**: Video.js
- **HTTP Client**: Axios
- **Styling**: CSS3 with custom properties
- **Module System**: ES Modules

## Project Structure

```
frontend/src/
├── pages/                      # HTML pages
│   ├── index.html             # Home/browse page
│   ├── player.html            # Video player page
│   ├── profiles.html          # Profile management
│   ├── settings.html          # Settings page
│   ├── admin.html             # Admin panel
│   └── my-list.html           # Watchlist page
├── scripts/                    # Page entry points
│   ├── main.js                # Home page script
│   ├── profiles-main.js       # Profiles page script
│   ├── settings-main.js       # Settings page script
│   └── admin-main.js          # Admin page script
├── modules/                    # Reusable modules
│   ├── api-client.js          # API communication
│   ├── data.js                # Data management
│   ├── navigation.js          # Navigation handling
│   ├── profile-manager.js     # Profile operations
│   ├── settings-manager.js    # Settings operations
│   ├── content-display.js     # Content rendering
│   ├── content-modal.js       # Content detail modal
│   ├── search.js              # Search functionality
│   └── video-player.js        # Video player logic
├── styles/                     # CSS stylesheets
│   ├── main.css               # Global styles
│   ├── page-layouts.css       # Page layouts
│   ├── player.css             # Video player styles
│   ├── settings.css           # Settings page styles
│   ├── admin.css              # Admin page styles
│   ├── content-modal.css      # Modal styles
│   └── search.css             # Search styles
└── public/                     # Static assets
    └── (images, icons, etc.)
```

## Core Modules

### API Client Module (`api-client.js`)

Centralized API communication layer.

```javascript
class APIClient {
  constructor(baseURL) {
    this.baseURL = baseURL;
    this.axios = axios.create({ baseURL });
  }

  // Content endpoints
  async discoverContent(type, filters) {}
  async searchContent(query, type) {}
  async getContentDetails(id) {}
  
  // Library endpoints
  async getLibrary() {}
  async scanLibrary() {}
  async refreshMetadata(id) {}
  
  // Profile endpoints
  async getProfiles() {}
  async createProfile(data) {}
  async updateProfile(id, data) {}
  
  // Streaming endpoints
  async getStreamURL(contentId) {}
  async updateProgress(contentId, progress) {}
  
  // Settings endpoints
  async getSettings() {}
  async updateSetting(key, value) {}
  
  // Watchlist endpoints
  async getWatchlist(profileId) {}
  async addToWatchlist(contentId) {}
  async removeFromWatchlist(contentId) {}
}
```

### Data Module (`data.js`)

State management and data caching.

```javascript
class DataManager {
  constructor() {
    this.cache = new Map();
    this.currentProfile = null;
    this.settings = {};
  }

  // Profile management
  setCurrentProfile(profile) {}
  getCurrentProfile() {}
  
  // Cache operations
  cacheContent(key, data, ttl) {}
  getCachedContent(key) {}
  clearCache() {}
  
  // Settings
  loadSettings() {}
  getSetting(key) {}
  updateSetting(key, value) {}
}
```

### Navigation Module (`navigation.js`)

Page navigation and routing.

```javascript
class Navigation {
  // Navigate to page
  navigateTo(page, params) {}
  
  // Update URL
  updateURL(path) {}
  
  // Handle back button
  handleBackButton() {}
  
  // Active page tracking
  getCurrentPage() {}
}
```

### Profile Manager Module (`profile-manager.js`)

Profile selection and management.

```javascript
class ProfileManager {
  // Profile selection
  async selectProfile(profileId) {}
  
  // Profile CRUD
  async createProfile(name, avatar) {}
  async updateProfile(id, data) {}
  async deleteProfile(id) {}
  
  // Profile UI
  renderProfileSelector() {}
  showProfileModal() {}
}
```

### Settings Manager Module (`settings-manager.js`)

Application settings management.

```javascript
class SettingsManager {
  // Load settings
  async loadSettings() {}
  
  // Update settings
  async updateTranscodingSettings(settings) {}
  async updateLibrarySettings(settings) {}
  async updateStreamingSettings(settings) {}
  
  // UI rendering
  renderSettingsForm() {}
  validateSettings(settings) {}
}
```

### Content Display Module (`content-display.js`)

Content grid and list rendering.

```javascript
class ContentDisplay {
  // Render content grid
  renderContentGrid(container, content) {}
  
  // Render content row
  renderContentRow(container, title, content) {}
  
  // Create content card
  createContentCard(item) {}
  
  // Handle card click
  handleCardClick(contentId) {}
  
  // Lazy loading
  setupIntersectionObserver() {}
}
```

### Content Modal Module (`content-modal.js`)

Content detail modal and actions.

```javascript
class ContentModal {
  // Show modal
  async show(contentId) {}
  
  // Hide modal
  hide() {}
  
  // Render content details
  renderDetails(content) {}
  
  // Action buttons
  handlePlayButton() {}
  handleAddToListButton() {}
  handleDownloadButton() {}
  
  // Episode selection (for series)
  renderEpisodeList(episodes) {}
  selectEpisode(episodeId) {}
}
```

### Search Module (`search.js`)

Search functionality and UI.

```javascript
class SearchManager {
  // Initialize search
  init(inputElement, resultsContainer) {}
  
  // Perform search
  async search(query) {}
  
  // Debounced search
  debouncedSearch(query, delay) {}
  
  // Render results
  renderResults(results) {}
  
  // Clear search
  clearSearch() {}
  
  // Search filters
  applyFilters(filters) {}
}
```

### Video Player Module (`video-player.js`)

Video.js player integration and controls.

```javascript
class VideoPlayer {
  // Initialize player
  init(videoElement, options) {}
  
  // Load video
  loadVideo(streamURL, metadata) {}
  
  // Playback controls
  play() {}
  pause() {}
  seek(time) {}
  setVolume(level) {}
  
  // Progress tracking
  trackProgress() {}
  saveProgress(time) {}
  resumeFromProgress() {}
  
  // Quality selection
  setQuality(quality) {}
  
  // Subtitles
  loadSubtitles(tracks) {}
  selectSubtitle(trackId) {}
  
  // Fullscreen
  enterFullscreen() {}
  exitFullscreen() {}
  
  // Chromecast
  initChromecast() {}
  castVideo() {}
  
  // Cleanup
  destroy() {}
}
```

## Page Scripts

### Home Page (`main.js`)

```javascript
// Initialize modules
const api = new APIClient('/api');
const data = new DataManager();
const contentDisplay = new ContentDisplay();
const search = new SearchManager();

// Load content
async function loadHomePage() {
  const profile = data.getCurrentProfile();
  if (!profile) {
    navigation.navigateTo('profiles');
    return;
  }
  
  // Load discover content
  const movies = await api.discoverContent('movie');
  const series = await api.discoverContent('tv');
  
  // Render content rows
  contentDisplay.renderContentRow('#movies', 'Movies', movies);
  contentDisplay.renderContentRow('#series', 'TV Series', series);
  
  // Continue watching
  const continueWatching = await api.getContinueWatching(profile.id);
  contentDisplay.renderContentRow('#continue', 'Continue Watching', continueWatching);
}

// Initialize search
search.init('#search-input', '#search-results');

// Load page
loadHomePage();
```

### Player Page (`video-player.js` usage)

```javascript
// Get content ID from URL
const contentId = new URLSearchParams(window.location.search).get('id');

// Initialize player
const player = new VideoPlayer();
player.init('#video-player', {
  controls: true,
  autoplay: true,
  fluid: true
});

// Load content
async function loadVideo() {
  const content = await api.getContentDetails(contentId);
  const streamURL = await api.getStreamURL(contentId);
  
  player.loadVideo(streamURL, content);
  
  // Resume from last position
  const progress = await api.getProgress(contentId);
  if (progress) {
    player.seek(progress.position);
  }
  
  // Track progress
  player.trackProgress();
}

loadVideo();
```

### Settings Page (`settings-main.js`)

```javascript
const settingsManager = new SettingsManager();

// Load settings
async function loadSettings() {
  const settings = await settingsManager.loadSettings();
  settingsManager.renderSettingsForm('#settings-form', settings);
}

// Save settings
async function saveSettings(formData) {
  await settingsManager.updateTranscodingSettings(formData.transcoding);
  await settingsManager.updateLibrarySettings(formData.library);
  showNotification('Settings saved successfully');
}

loadSettings();
```

### Admin Page (`admin-main.js`)

```javascript
// Library management
async function scanLibrary() {
  showLoading();
  await api.scanLibrary();
  hideLoading();
  showNotification('Library scan completed');
}

// View logs
async function viewLogs() {
  const logs = await api.getLogs();
  renderLogs('#logs-container', logs);
}

// System status
async function loadSystemStatus() {
  const status = await api.getSystemStatus();
  renderStatus('#status-container', status);
}
```

## Styling Architecture

### CSS Custom Properties

```css
:root {
  /* Colors */
  --primary-color: #e50914;
  --secondary-color: #564d4d;
  --background-color: #141414;
  --surface-color: #2f2f2f;
  --text-color: #ffffff;
  --text-secondary: #b3b3b3;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 2rem;
  --spacing-xl: 4rem;
  
  /* Typography */
  --font-family: 'Helvetica Neue', Arial, sans-serif;
  --font-size-sm: 0.875rem;
  --font-size-md: 1rem;
  --font-size-lg: 1.25rem;
  --font-size-xl: 2rem;
  
  /* Transitions */
  --transition-fast: 0.15s ease;
  --transition-normal: 0.3s ease;
  --transition-slow: 0.5s ease;
}
```

### Component Styles

**Content Card**
```css
.content-card {
  position: relative;
  border-radius: 4px;
  overflow: hidden;
  transition: transform var(--transition-normal);
  cursor: pointer;
}

.content-card:hover {
  transform: scale(1.05);
  z-index: 10;
}

.content-card img {
  width: 100%;
  height: auto;
  display: block;
}
```

**Modal**
```css
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: var(--surface-color);
  border-radius: 8px;
  max-width: 850px;
  max-height: 90vh;
  overflow-y: auto;
}
```

## State Management

### Local Storage
```javascript
// Save profile selection
localStorage.setItem('currentProfile', JSON.stringify(profile));

// Save settings
localStorage.setItem('settings', JSON.stringify(settings));

// Save playback position
localStorage.setItem(`progress_${contentId}`, position);
```

### Session Storage
```javascript
// Temporary search results
sessionStorage.setItem('searchResults', JSON.stringify(results));

// Navigation history
sessionStorage.setItem('navigationHistory', JSON.stringify(history));
```

## Event Handling

### Custom Events
```javascript
// Dispatch custom event
const event = new CustomEvent('profileChanged', {
  detail: { profile }
});
document.dispatchEvent(event);

// Listen for custom event
document.addEventListener('profileChanged', (e) => {
  console.log('Profile changed:', e.detail.profile);
});
```

### Keyboard Shortcuts
```javascript
document.addEventListener('keydown', (e) => {
  if (e.key === ' ') {
    player.togglePlay();
  } else if (e.key === 'f') {
    player.toggleFullscreen();
  } else if (e.key === 'ArrowLeft') {
    player.seek(player.currentTime() - 10);
  } else if (e.key === 'ArrowRight') {
    player.seek(player.currentTime() + 10);
  }
});
```

## Performance Optimizations

### Lazy Loading
```javascript
const observer = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      const img = entry.target;
      img.src = img.dataset.src;
      observer.unobserve(img);
    }
  });
});

document.querySelectorAll('img[data-src]').forEach(img => {
  observer.observe(img);
});
```

### Debouncing
```javascript
function debounce(func, delay) {
  let timeoutId;
  return function(...args) {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => func.apply(this, args), delay);
  };
}

const debouncedSearch = debounce(search, 300);
```

### Virtual Scrolling
```javascript
// Render only visible items
function renderVisibleItems(container, items, itemHeight) {
  const scrollTop = container.scrollTop;
  const viewportHeight = container.clientHeight;
  
  const startIndex = Math.floor(scrollTop / itemHeight);
  const endIndex = Math.ceil((scrollTop + viewportHeight) / itemHeight);
  
  const visibleItems = items.slice(startIndex, endIndex);
  renderItems(visibleItems);
}
```

## Build Configuration

### Vite Config (`vite.config.js`)

```javascript
export default {
  root: 'src',
  build: {
    outDir: '../dist',
    rollupOptions: {
      input: {
        main: 'src/pages/index.html',
        player: 'src/pages/player.html',
        profiles: 'src/pages/profiles.html',
        settings: 'src/pages/settings.html',
        admin: 'src/pages/admin.html'
      }
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:3000',
        changeOrigin: true
      }
    }
  }
}
```

## Next Steps

- [Backend Architecture](./backend.md)
- [Database Schema](./database.md)
- [Video Player Guide](../features/video-player.md)

**Last Updated**: October 31, 2025
