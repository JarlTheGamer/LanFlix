import apiClient from './api-client.js';

class NotificationBadge {
  constructor() {
    this.badge = null;
    this.updateInterval = null;
  }

  init() {
    this.badge = document.getElementById('notification-badge');
    if (!this.badge) return;

    // Update immediately
    this.updateBadge();

    // Update every 30 seconds
    this.updateInterval = setInterval(() => {
      this.updateBadge();
    }, 30000);
  }

  async updateBadge() {
    try {
      const profileId = this.getCurrentProfileId();
      const response = await apiClient.get(`/api/notifications/${profileId}`);
      
      const unreadCount = (response.notifications || []).filter(n => !n.userResponse).length;
      
      if (this.badge) {
        if (unreadCount > 0) {
          this.badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
          this.badge.classList.add('active');
        } else {
          this.badge.classList.remove('active');
        }
      }
    } catch (error) {
      // Silently fail - don't show errors for badge updates
      console.debug('Failed to update notification badge:', error);
    }
  }

  getCurrentProfileId() {
    return parseInt(localStorage.getItem('selectedProfileId') || '1', 10);
  }

  destroy() {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
    }
  }
}

export const notificationBadge = new NotificationBadge();
