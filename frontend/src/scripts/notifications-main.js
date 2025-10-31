import { notificationsManager } from '../modules/notifications-manager.js';

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  notificationsManager.init();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
  notificationsManager.destroy();
});
