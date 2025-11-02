import apiClient from './api-client.js';

class NotificationsManager {
  constructor() {
    this.currentTab = 'notifications';
    this.notifications = [];
    this.downloads = [];
    this.jobs = [];
    this.refreshInterval = null;
  }

  init() {
    this.setupTabs();
    this.setupEventListeners();
    this.loadNotifications();
    this.loadJobs();
    
    // Auto-refresh every 5 seconds for downloads and jobs
    this.refreshInterval = setInterval(() => {
      if (this.currentTab === 'downloads') {
        this.loadDownloads();
      } else if (this.currentTab === 'jobs') {
        this.loadJobs();
      }
    }, 5000);
  }

  setupTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    tabButtons.forEach(btn => {
      btn.addEventListener('click', () => {
        const tab = btn.dataset.tab;
        this.switchTab(tab);
      });
    });
  }

  switchTab(tab) {
    this.currentTab = tab;

    // Update button states
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.tab === tab);
    });

    // Update content visibility
    document.querySelectorAll('.tab-content').forEach(content => {
      content.classList.toggle('active', content.id === `${tab}-tab`);
    });

    // Load data for the tab
    if (tab === 'notifications') {
      this.loadNotifications();
    } else if (tab === 'downloads') {
      this.loadDownloads();
    } else if (tab === 'jobs') {
      this.loadJobs();
    }
  }

  setupEventListeners() {
    document.getElementById('clear-notifications')?.addEventListener('click', () => {
      this.clearNotifications();
    });

    document.getElementById('refresh-downloads')?.addEventListener('click', () => {
      this.loadDownloads();
    });

    document.getElementById('refresh-jobs')?.addEventListener('click', () => {
      this.loadJobs();
    });
  }

  async loadNotifications() {
    try {
      const profileId = this.getCurrentProfileId();
      const response = await apiClient.getNotifications(profileId);
      
      this.notifications = response.notifications || [];
      this.renderNotifications();
      this.updateBadge();
    } catch (error) {
      console.error('Failed to load notifications:', error);
      this.renderEmptyState('notifications-list', 'Failed to load notifications');
    }
  }

  async loadDownloads() {
    try {
      // This would connect to a real downloads API
      // For now, showing placeholder
      this.downloads = [];
      this.renderDownloads();
    } catch (error) {
      console.error('Failed to load downloads:', error);
      this.renderEmptyState('downloads-list', 'Failed to load downloads');
    }
  }

  async loadJobs() {
    try {
      const response = await apiClient.get('/jobs/status');
      
      this.jobs = response.jobs || [];
      this.renderJobs();
    } catch (error) {
      console.error('Failed to load jobs:', error);
      this.renderEmptyState('jobs-list', 'Failed to load jobs');
    }
  }

  renderNotifications() {
    const container = document.getElementById('notifications-list');
    
    if (!this.notifications.length) {
      this.renderEmptyState('notifications-list', 'No notifications');
      return;
    }

    container.innerHTML = this.notifications.map(notification => `
      <div class="notification-item" data-id="${notification.id}">
        <div class="notification-header">
          <div class="notification-title">${notification.contentTitle || 'Content'}</div>
          <div class="notification-time">${this.formatTime(notification.notificationSentAt)}</div>
        </div>
        <div class="notification-message">
          ${this.getNotificationMessage(notification)}
        </div>
        ${notification.userResponse ? `
          <div class="notification-actions">
            <span class="status-badge ${notification.userResponse === 'keep' ? 'running' : 'error'}">
              ${notification.userResponse === 'keep' ? 'Kept' : 'Deleted'}
            </span>
          </div>
        ` : `
          <div class="notification-actions">
            <button class="btn-action btn-keep" onclick="notificationsManager.respondToNotification(${notification.id}, 'keep')">
              Keep Watching
            </button>
            <button class="btn-action btn-delete" onclick="notificationsManager.respondToNotification(${notification.id}, 'delete')">
              Delete
            </button>
          </div>
        `}
      </div>
    `).join('');
  }

  renderDownloads() {
    const container = document.getElementById('downloads-list');
    
    if (!this.downloads.length) {
      this.renderEmptyState('downloads-list', 'No active downloads');
      return;
    }

    container.innerHTML = this.downloads.map(download => `
      <div class="download-item" data-id="${download.id}">
        <div class="download-header">
          <div class="download-title">${download.title}</div>
          <div class="download-status">${download.status}</div>
        </div>
        <div class="download-info">
          ${download.progress}% complete • ${download.speed || 'Calculating...'} • ETA: ${download.eta || 'Unknown'}
        </div>
        <div class="progress-bar">
          <div class="progress-fill" style="width: ${download.progress}%"></div>
        </div>
        ${download.status === 'downloading' ? `
          <div class="download-actions">
            <button class="btn-action btn-cancel" onclick="notificationsManager.cancelDownload(${download.id})">
              Cancel
            </button>
          </div>
        ` : ''}
      </div>
    `).join('');
  }

  renderJobs() {
    const container = document.getElementById('jobs-list');
    
    if (!this.jobs.length) {
      this.renderEmptyState('jobs-list', 'No jobs configured');
      return;
    }

    container.innerHTML = this.jobs.map(job => `
      <div class="job-item" data-name="${job.name}">
        <div class="job-header">
          <div class="job-title">${this.formatJobName(job.name)}</div>
          <span class="status-badge ${job.running ? 'running' : 'idle'}">
            ${job.running ? 'Running' : 'Idle'}
          </span>
        </div>
        <div class="job-info">
          ${job.description || 'Background job'}
        </div>
        <div class="job-schedule">
          Schedule: ${job.schedule || 'Manual'} • Last run: ${job.lastRun ? this.formatTime(job.lastRun) : 'Never'}
        </div>
        <div class="job-actions">
          <button class="btn-trigger" onclick="notificationsManager.triggerJob('${job.name}')" ${job.running ? 'disabled' : ''}>
            Trigger Now
          </button>
        </div>
      </div>
    `).join('');
  }

  renderEmptyState(containerId, message) {
    const container = document.getElementById(containerId);
    const icon = this.getEmptyStateIcon(containerId);
    
    container.innerHTML = `
      <div class="empty-state">
        ${icon}
        <p>${message}</p>
      </div>
    `;
  }

  getEmptyStateIcon(containerId) {
    if (containerId === 'notifications-list') {
      return `<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z" />
      </svg>`;
    } else if (containerId === 'downloads-list') {
      return `<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
      </svg>`;
    } else {
      return `<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
      </svg>`;
    }
  }

  async respondToNotification(notificationId, response) {
    try {
      await apiClient.respondToNotification(notificationId, response);
      await this.loadNotifications();
    } catch (error) {
      console.error('Failed to respond to notification:', error);
      alert('Failed to respond to notification');
    }
  }

  async cancelDownload(downloadId) {
    try {
      // This would connect to a real downloads API
      console.log('Cancelling download:', downloadId);
      await this.loadDownloads();
    } catch (error) {
      console.error('Failed to cancel download:', error);
      alert('Failed to cancel download');
    }
  }

  async triggerJob(jobName) {
    try {
      await apiClient.post(`/jobs/${jobName}/trigger`);
      alert(`Job "${this.formatJobName(jobName)}" triggered successfully`);
      await this.loadJobs();
    } catch (error) {
      console.error('Failed to trigger job:', error);
      alert('Failed to trigger job');
    }
  }

  async clearNotifications() {
    if (!confirm('Clear all notifications?')) return;
    
    try {
      // This would connect to a real API to clear notifications
      this.notifications = [];
      this.renderNotifications();
      this.updateBadge();
    } catch (error) {
      console.error('Failed to clear notifications:', error);
      alert('Failed to clear notifications');
    }
  }

  updateBadge() {
    const badge = document.getElementById('notification-badge');
    const unreadCount = this.notifications.filter(n => !n.userResponse).length;
    
    if (badge) {
      if (unreadCount > 0) {
        badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
        badge.classList.add('active');
      } else {
        badge.classList.remove('active');
      }
    }
  }

  getNotificationMessage(notification) {
    if (notification.type === 'keep_watching') {
      const deleteDate = new Date(notification.scheduledDeleteAt);
      return `This content is scheduled for deletion on ${deleteDate.toLocaleDateString()}. Would you like to keep watching?`;
    }
    return notification.message || 'Notification';
  }

  formatJobName(name) {
    return name
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  formatTime(timestamp) {
    if (!timestamp) return 'Unknown';
    
    const date = new Date(timestamp);
    const now = new Date();
    const diff = now - date;
    
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);
    
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days < 7) return `${days}d ago`;
    
    return date.toLocaleDateString();
  }

  getCurrentProfileId() {
    // Get from localStorage or default to 1
    return parseInt(localStorage.getItem('selectedProfileId') || '1', 10);
  }

  destroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }
}

export const notificationsManager = new NotificationsManager();

// Make it globally accessible for onclick handlers
window.notificationsManager = notificationsManager;
