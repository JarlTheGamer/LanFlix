# Offline Mode & Graceful Degradation

## Overview

The streaming media server frontend now includes robust offline mode support with graceful degradation. When the backend API is unavailable, the application automatically switches to offline mode and continues to function using cached data.

## Features

### 1. Automatic Offline Detection
- The API client automatically detects when the backend is unavailable
- After 3 retry attempts with exponential backoff, the system marks itself as offline
- All API calls are intercepted and cached data is returned immediately

### 2. Persistent Caching
- All fetched data is automatically cached in `localStorage`
- Cache persists across browser sessions
- Includes: profiles, library content, watchlists, recently added, and discovery content

### 3. Automatic Retry
- When offline, the system automatically retries connection every **10 minutes**
- Users can also manually retry using the "Retry Now" button
- When connection is restored, data is automatically refreshed

### 4. User Notifications
- A non-intrusive banner appears at the top when offline
- Shows status: "Discovery features are offline. Your downloaded content is still available."
- Automatically disappears when connection is restored

### 5. Feature Availability

#### Always Available (Offline Mode)
- ✅ Home page with downloaded content
- ✅ Movies library (downloaded movies)
- ✅ Series library (downloaded TV shows)
- ✅ My List (watchlist)
- ✅ Video playback of downloaded content
- ✅ Profile management (cached)
- ✅ Settings (cached)

#### Requires Connection (Online Only)
- ❌ Discovery page (browse new content)
- ❌ Search for new content
- ❌ Download new content
- ❌ Add to watchlist (new items)

## Technical Implementation

### API Client (`api-client.js`)

```javascript
// Offline state management
this.isOffline = false;
this.offlineRetryInterval = 10 * 60 * 1000; // 10 minutes

// Automatic offline detection
markOffline() {
  this.isOffline = true;
  window.dispatchEvent(new CustomEvent('api-offline'));
  this.scheduleOfflineRetry();
}

// Automatic retry
scheduleOfflineRetry() {
  setTimeout(async () => {
    await this.checkConnection();
  }, this.offlineRetryInterval);
}
```

### State Manager (`data.js`)

```javascript
// Check offline status before API calls
if (apiClient.isOffline && !forceRefresh) {
  console.log('📦 Using cached data (offline mode)');
  return this.cache[key] || defaultValue;
}

// Persist cache to localStorage
saveCacheToStorage(key, data) {
  localStorage.setItem(`cache_${key}`, JSON.stringify({
    data: data,
    timestamp: Date.now()
  }));
}
```

### Content Display (`content-display.js`)

```javascript
// Show offline message on discovery page
if (this.currentCategory === 'discover' && apiClient.isOffline) {
  // Display offline message with retry button
}

// Listen for online/offline events
window.addEventListener('api-offline', () => {
  this.showOfflineNotification();
});

window.addEventListener('api-online', () => {
  this.hideOfflineNotification();
  this.refreshContent();
});
```

## Events

### Custom Events

#### `api-offline`
Fired when the API becomes unavailable
```javascript
window.addEventListener('api-offline', () => {
  // Handle offline state
});
```

#### `api-online`
Fired when the API connection is restored
```javascript
window.addEventListener('api-online', () => {
  // Handle online state
  // Refresh data
});
```

#### `data-refresh-needed`
Fired when cached data should be refreshed
```javascript
window.addEventListener('data-refresh-needed', () => {
  // Refresh UI with new data
});
```

## User Experience

### Offline Scenario

1. **User is browsing** → Backend goes down
2. **System detects failure** → Switches to offline mode (3 retries)
3. **Banner appears** → "Discovery features are offline..."
4. **User continues** → Can still watch downloaded content
5. **Auto-retry** → System checks connection every 10 minutes
6. **Connection restored** → Banner disappears, data refreshes

### Discovery Page Offline

When on the Discovery page and offline:
- Shows friendly message with icon 📡
- Explains that discovery requires internet
- Provides "Retry Now" button
- Mentions automatic retry in 10 minutes
- Suggests other available sections

### Home Page Offline

When on Home page and offline:
- Shows downloaded content normally
- Hides discovery carousel (if no cached data)
- All downloaded content remains playable
- Full functionality for local library

## Configuration

### Retry Interval
Change the automatic retry interval in `api-client.js`:
```javascript
this.offlineRetryInterval = 10 * 60 * 1000; // 10 minutes (in milliseconds)
```

### Cache Duration
Change how long cached data is considered valid in `data.js`:
```javascript
this.cacheDuration = 5 * 60 * 1000; // 5 minutes (in milliseconds)
```

### Retry Attempts
Change the number of retry attempts before marking offline in `api-client.js`:
```javascript
this.retryAttempts = 3; // Number of retries
this.retryDelay = 1000; // Initial delay in ms (exponential backoff)
```

## Testing Offline Mode

### Method 1: Stop Backend
```bash
# Stop the backend server
# Frontend will automatically detect and switch to offline mode
```

### Method 2: Browser DevTools
1. Open Chrome DevTools (F12)
2. Go to Network tab
3. Select "Offline" from throttling dropdown
4. Refresh the page

### Method 3: Firewall
Temporarily block port 3000 to simulate backend unavailability

## Troubleshooting

### Cache Not Working
- Check browser console for localStorage errors
- Verify localStorage is not full (quota exceeded)
- Clear cache and reload: `localStorage.clear()`

### Offline Mode Not Triggering
- Check network tab for actual API responses
- Verify retry logic in console logs
- Look for "🔴 API is offline" message

### Data Not Refreshing When Online
- Check for "🟢 API is back online" message
- Verify `api-online` event is firing
- Check cache timestamps in localStorage

## Future Enhancements

- [ ] Service Worker for true offline support
- [ ] Background sync for queued actions
- [ ] Offline queue for downloads
- [ ] Progressive Web App (PWA) support
- [ ] IndexedDB for larger cache storage
- [ ] Smarter cache invalidation
- [ ] Partial data updates
