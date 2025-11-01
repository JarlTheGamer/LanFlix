// First-run configuration check for Android app ONLY
// Web version served from backend doesn't need this
export function checkFirstRun() {
  // Skip first-run check if running as web app (served from backend)
  if (!isNativeApp()) {
    return true;
  }
  
  const config = localStorage.getItem('lanflix_config');
  
  // If no config exists, redirect to app configuration
  if (!config) {
    // Check if we're already on the config page
    if (!window.location.pathname.includes('app-config.html')) {
      window.location.replace('app-config.html');
      return false;
    }
  }
  
  return true;
}

// Check if running in Capacitor (native app)
export function isNativeApp() {
  return window.Capacitor !== undefined;
}

// Get platform info
export function getPlatform() {
  if (window.Capacitor) {
    return window.Capacitor.getPlatform();
  }
  return 'web';
}
