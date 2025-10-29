/**
 * Video Player Module
 * Handles video playback with controls, progress tracking, and subtitle support
 */

import apiClient from './api-client.js';
import stateManager from './data.js';

export class VideoPlayer {
  constructor(videoElement, profileId) {
    this.videoElement = videoElement;
    this.profileId = profileId;
    this.contentId = null;
    this.episodeId = null;
    this.contentType = null;
    
    // Progress tracking
    this.progressInterval = null;
    this.progressUpdateFrequency = 10000; // 10 seconds
    this.lastSavedProgress = 0;
    
    // Playback state
    this.isPlaying = false;
    this.currentTime = 0;
    this.duration = 0;
    this.volume = 1.0;
    this.isMuted = false;
    this.isFullscreen = false;
    
    // Subtitles
    this.availableSubtitles = [];
    this.currentSubtitleTrack = null;
    
    // Controls
    this.controlsTimeout = null;
    this.controlsVisible = true;
    this.controlsHideDelay = 3000; // 3 seconds
  }

  /**
   * Initialize the video player
   */
  async initialize(contentId, contentType, episodeId = null, startPosition = 0) {
    this.contentId = contentId;
    this.contentType = contentType;
    this.episodeId = episodeId;
    
    // Set video source
    const streamUrl = apiClient.getStreamUrl(contentId, episodeId);
    this.videoElement.src = streamUrl;
    
    // Load subtitles
    await this.loadSubtitles();
    
    // Setup event listeners
    this.setupEventListeners();
    
    // Setup controls
    this.setupControls();
    
    // Start at saved position
    if (startPosition > 0) {
      this.videoElement.currentTime = startPosition;
    }
    
    // Start playing
    this.play();
  }

  /**
   * Setup video element event listeners
   */
  setupEventListeners() {
    // Playback events
    this.videoElement.addEventListener('play', () => {
      this.isPlaying = true;
      this.startProgressTracking();
      this.updatePlayPauseButton();
    });

    this.videoElement.addEventListener('pause', () => {
      this.isPlaying = false;
      this.stopProgressTracking();
      this.updatePlayPauseButton();
    });

    this.videoElement.addEventListener('ended', () => {
      this.isPlaying = false;
      this.stopProgressTracking();
      this.saveProgress(true); // Mark as completed
    });

    this.videoElement.addEventListener('timeupdate', () => {
      this.currentTime = this.videoElement.currentTime;
      this.updateProgressBar();
    });

    this.videoElement.addEventListener('loadedmetadata', () => {
      this.duration = this.videoElement.duration;
      this.updateDurationDisplay();
    });

    this.videoElement.addEventListener('volumechange', () => {
      this.volume = this.videoElement.volume;
      this.isMuted = this.videoElement.muted;
      this.updateVolumeDisplay();
    });

    // Fullscreen events
    document.addEventListener('fullscreenchange', () => {
      this.isFullscreen = !!document.fullscreenElement;
      this.updateFullscreenButton();
    });

    // Mouse/touch events for controls
    this.videoElement.addEventListener('click', () => {
      this.togglePlayPause();
    });

    this.videoElement.addEventListener('mousemove', () => {
      this.showControls();
    });

    // Keyboard controls
    document.addEventListener('keydown', (e) => {
      this.handleKeyboard(e);
    });
  }

  /**
   * Setup player controls UI
   */
  setupControls() {
    const controlsContainer = document.getElementById('player-controls');
    if (!controlsContainer) return;

    controlsContainer.innerHTML = `
      <div class="player-controls-overlay">
        <div class="player-progress-container">
          <input type="range" class="player-progress-bar" min="0" max="100" value="0" step="0.1">
          <div class="player-time-display">
            <span class="current-time">0:00</span>
            <span class="duration">0:00</span>
          </div>
        </div>
        <div class="player-controls-buttons">
          <button class="player-btn play-pause-btn" title="Play/Pause">
            <span class="play-icon">▶</span>
            <span class="pause-icon" style="display: none;">⏸</span>
          </button>
          <button class="player-btn rewind-btn" title="Rewind 10s">⏪</button>
          <button class="player-btn forward-btn" title="Forward 10s">⏩</button>
          <div class="player-volume-control">
            <button class="player-btn volume-btn" title="Mute/Unmute">🔊</button>
            <input type="range" class="volume-slider" min="0" max="100" value="100">
          </div>
          <button class="player-btn subtitles-btn" title="Subtitles">CC</button>
          <button class="player-btn fullscreen-btn" title="Fullscreen">⛶</button>
        </div>
      </div>
    `;

    // Attach control event listeners
    this.attachControlListeners();
  }

  /**
   * Attach event listeners to control buttons
   */
  attachControlListeners() {
    // Play/Pause
    document.querySelector('.play-pause-btn')?.addEventListener('click', () => {
      this.togglePlayPause();
    });

    // Rewind
    document.querySelector('.rewind-btn')?.addEventListener('click', () => {
      this.seek(this.currentTime - 10);
    });

    // Forward
    document.querySelector('.forward-btn')?.addEventListener('click', () => {
      this.seek(this.currentTime + 10);
    });

    // Volume
    document.querySelector('.volume-btn')?.addEventListener('click', () => {
      this.toggleMute();
    });

    document.querySelector('.volume-slider')?.addEventListener('input', (e) => {
      this.setVolume(e.target.value / 100);
    });

    // Progress bar
    document.querySelector('.player-progress-bar')?.addEventListener('input', (e) => {
      const seekTime = (e.target.value / 100) * this.duration;
      this.seek(seekTime);
    });

    // Subtitles
    document.querySelector('.subtitles-btn')?.addEventListener('click', () => {
      this.showSubtitleMenu();
    });

    // Fullscreen
    document.querySelector('.fullscreen-btn')?.addEventListener('click', () => {
      this.toggleFullscreen();
    });
  }

  /**
   * Play video
   */
  play() {
    this.videoElement.play().catch(error => {
      console.error('Failed to play video:', error);
    });
  }

  /**
   * Pause video
   */
  pause() {
    this.videoElement.pause();
  }

  /**
   * Toggle play/pause
   */
  togglePlayPause() {
    if (this.isPlaying) {
      this.pause();
    } else {
      this.play();
    }
  }

  /**
   * Seek to specific time
   */
  seek(time) {
    this.videoElement.currentTime = Math.max(0, Math.min(time, this.duration));
  }

  /**
   * Set volume (0.0 to 1.0)
   */
  setVolume(volume) {
    this.videoElement.volume = Math.max(0, Math.min(1, volume));
  }

  /**
   * Toggle mute
   */
  toggleMute() {
    this.videoElement.muted = !this.videoElement.muted;
  }

  /**
   * Toggle fullscreen
   */
  toggleFullscreen() {
    if (!this.isFullscreen) {
      if (this.videoElement.requestFullscreen) {
        this.videoElement.requestFullscreen();
      } else if (this.videoElement.webkitRequestFullscreen) {
        this.videoElement.webkitRequestFullscreen();
      }
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if (document.webkitExitFullscreen) {
        document.webkitExitFullscreen();
      }
    }
  }

  /**
   * Load available subtitles
   */
  async loadSubtitles() {
    try {
      const response = await apiClient.getSubtitles(this.contentId, this.episodeId);
      this.availableSubtitles = response.subtitles || [];
      
      // Add subtitle tracks to video element
      this.availableSubtitles.forEach((subtitle, index) => {
        const track = document.createElement('track');
        track.kind = 'subtitles';
        track.label = subtitle.language.toUpperCase();
        track.srclang = subtitle.language;
        track.src = subtitle.path;
        
        if (index === 0) {
          track.default = true;
        }
        
        this.videoElement.appendChild(track);
      });
    } catch (error) {
      console.error('Failed to load subtitles:', error);
    }
  }

  /**
   * Show subtitle selection menu
   */
  showSubtitleMenu() {
    // Simple implementation - cycle through subtitles
    const tracks = this.videoElement.textTracks;
    
    if (tracks.length === 0) {
      alert('No subtitles available');
      return;
    }

    // Find current active track
    let currentIndex = -1;
    for (let i = 0; i < tracks.length; i++) {
      if (tracks[i].mode === 'showing') {
        currentIndex = i;
        tracks[i].mode = 'hidden';
      }
    }

    // Activate next track (or turn off if at end)
    const nextIndex = (currentIndex + 1) % (tracks.length + 1);
    
    if (nextIndex < tracks.length) {
      tracks[nextIndex].mode = 'showing';
      this.showNotification(`Subtitles: ${tracks[nextIndex].label}`);
    } else {
      this.showNotification('Subtitles: Off');
    }
  }

  /**
   * Start progress tracking
   */
  startProgressTracking() {
    if (this.progressInterval) return;
    
    this.progressInterval = setInterval(() => {
      this.saveProgress();
    }, this.progressUpdateFrequency);
  }

  /**
   * Stop progress tracking
   */
  stopProgressTracking() {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
    }
    
    // Save final progress
    this.saveProgress();
  }

  /**
   * Save watch progress to backend
   */
  async saveProgress(completed = false) {
    // Only save if progress has changed significantly (more than 5 seconds)
    if (Math.abs(this.currentTime - this.lastSavedProgress) < 5 && !completed) {
      return;
    }

    try {
      await apiClient.updateWatchProgress(
        this.contentId,
        this.profileId,
        Math.floor(this.currentTime),
        Math.floor(this.duration),
        this.episodeId
      );
      
      this.lastSavedProgress = this.currentTime;
    } catch (error) {
      console.error('Failed to save watch progress:', error);
    }
  }

  /**
   * Update progress bar
   */
  updateProgressBar() {
    const progressBar = document.querySelector('.player-progress-bar');
    if (progressBar && this.duration > 0) {
      progressBar.value = (this.currentTime / this.duration) * 100;
    }

    const currentTimeDisplay = document.querySelector('.current-time');
    if (currentTimeDisplay) {
      currentTimeDisplay.textContent = this.formatTime(this.currentTime);
    }
  }

  /**
   * Update duration display
   */
  updateDurationDisplay() {
    const durationDisplay = document.querySelector('.duration');
    if (durationDisplay) {
      durationDisplay.textContent = this.formatTime(this.duration);
    }
  }

  /**
   * Update play/pause button
   */
  updatePlayPauseButton() {
    const playIcon = document.querySelector('.play-icon');
    const pauseIcon = document.querySelector('.pause-icon');
    
    if (this.isPlaying) {
      if (playIcon) playIcon.style.display = 'none';
      if (pauseIcon) pauseIcon.style.display = 'inline';
    } else {
      if (playIcon) playIcon.style.display = 'inline';
      if (pauseIcon) pauseIcon.style.display = 'none';
    }
  }

  /**
   * Update volume display
   */
  updateVolumeDisplay() {
    const volumeSlider = document.querySelector('.volume-slider');
    if (volumeSlider) {
      volumeSlider.value = this.isMuted ? 0 : this.volume * 100;
    }

    const volumeBtn = document.querySelector('.volume-btn');
    if (volumeBtn) {
      volumeBtn.textContent = this.isMuted ? '🔇' : this.volume > 0.5 ? '🔊' : '🔉';
    }
  }

  /**
   * Update fullscreen button
   */
  updateFullscreenButton() {
    const fullscreenBtn = document.querySelector('.fullscreen-btn');
    if (fullscreenBtn) {
      fullscreenBtn.textContent = this.isFullscreen ? '⛶' : '⛶';
    }
  }

  /**
   * Show/hide controls
   */
  showControls() {
    const controls = document.getElementById('player-controls');
    if (controls) {
      controls.style.opacity = '1';
      this.controlsVisible = true;
    }

    // Reset hide timer
    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
    }

    this.controlsTimeout = setTimeout(() => {
      if (this.isPlaying) {
        this.hideControls();
      }
    }, this.controlsHideDelay);
  }

  /**
   * Hide controls
   */
  hideControls() {
    const controls = document.getElementById('player-controls');
    if (controls) {
      controls.style.opacity = '0';
      this.controlsVisible = false;
    }
  }

  /**
   * Handle keyboard controls
   */
  handleKeyboard(e) {
    switch (e.key) {
      case ' ':
      case 'k':
        e.preventDefault();
        this.togglePlayPause();
        break;
      case 'ArrowLeft':
        e.preventDefault();
        this.seek(this.currentTime - 10);
        break;
      case 'ArrowRight':
        e.preventDefault();
        this.seek(this.currentTime + 10);
        break;
      case 'ArrowUp':
        e.preventDefault();
        this.setVolume(this.volume + 0.1);
        break;
      case 'ArrowDown':
        e.preventDefault();
        this.setVolume(this.volume - 0.1);
        break;
      case 'm':
        e.preventDefault();
        this.toggleMute();
        break;
      case 'f':
        e.preventDefault();
        this.toggleFullscreen();
        break;
      case 'c':
        e.preventDefault();
        this.showSubtitleMenu();
        break;
      case 'Escape':
        if (this.isFullscreen) {
          e.preventDefault();
          this.toggleFullscreen();
        }
        break;
    }
  }

  /**
   * Format time in MM:SS or HH:MM:SS
   */
  formatTime(seconds) {
    if (isNaN(seconds)) return '0:00';
    
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    
    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    } else {
      return `${minutes}:${secs.toString().padStart(2, '0')}`;
    }
  }

  /**
   * Show notification overlay
   */
  showNotification(message) {
    let notification = document.getElementById('player-notification');
    
    if (!notification) {
      notification = document.createElement('div');
      notification.id = 'player-notification';
      notification.style.cssText = `
        position: fixed;
        top: 20px;
        left: 50%;
        transform: translateX(-50%);
        background: rgba(0, 0, 0, 0.8);
        color: white;
        padding: 10px 20px;
        border-radius: 5px;
        z-index: 10000;
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

  /**
   * Cleanup and destroy player
   */
  destroy() {
    this.stopProgressTracking();
    this.videoElement.pause();
    this.videoElement.src = '';
    
    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
    }
  }
}

export default VideoPlayer;
