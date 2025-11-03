/**
 * Modern Video Player for Lanflix Transcoding System
 * Clean, well-structured implementation designed for the new transcoding backend
 */

import apiClient from './api-client.js';

export class VideoPlayer {
  constructor(videoElement, profileId) {
    this.videoElement = videoElement;
    this.profileId = profileId;

    // Content info
    this.contentId = null;
    this.episodeId = null;
    this.contentType = null;

    // Playback state
    this.isPlaying = false;
    this.currentTime = 0;
    this.duration = 0;
    this.volume = 1.0;
    this.isMuted = false;
    this.isFullscreen = false;

    // Transcoding state
    this.isTranscoding = false;
    this.playbackMode = 'unknown';
    this.startOffset = 0; // For transcoded streams that start at a specific time

    // Progress tracking
    this.progressInterval = null;
    this.progressUpdateFrequency = 10000; // 10 seconds
    this.lastSavedProgress = 0;

    // UI state
    this.controlsVisible = true;
    this.controlsTimeout = null;

    // Initialization state
    this.isInitialized = false;
    this.isDestroyed = false;
  }

  /**
   * Initialize the video player with content
   */
  async initialize(contentId, contentType, episodeId = null, startPosition = 0) {
    if (this.isInitialized) {
      console.warn('Video player already initialized');
      return;
    }

    console.log(`🎬 Initializing video player: contentId=${contentId}, type=${contentType}, episode=${episodeId}, start=${startPosition}s`);

    this.contentId = contentId;
    this.contentType = contentType;
    this.episodeId = episodeId;
    this.startOffset = startPosition;

    try {
      // Setup video element
      this.setupVideoElement();

      // Setup event listeners
      this.setupEventListeners();

      // Setup controls UI
      this.setupControls();

      // Load media metadata first to get duration (with Fire TV fallback)
      await this.loadMediaMetadataWithFallback();

      // Detect playback mode and setup stream (with retries for Fire TV)
      await this.setupStreamWithRetry(startPosition);

      this.isInitialized = true;
      console.log('✅ Video player initialized successfully');

    } catch (error) {
      console.error('❌ Failed to initialize video player:', error);
      
      // Fire TV specific error handling
      const userAgent = navigator.userAgent.toLowerCase();
      const isFireTV = userAgent.includes('aftm') || userAgent.includes('aftb') || userAgent.includes('afts');
      
      if (isFireTV) {
        console.log('🔥 Fire TV initialization failed, trying fallback method...');
        await this.initializeFireTVFallback(startPosition);
      } else {
        this.showNotification('Failed to initialize video player: ' + error.message);
        throw error;
      }
    }
  }

  /**
   * Setup video element attributes
   */
  setupVideoElement() {
    // Detect Fire TV and other TV platforms
    const userAgent = navigator.userAgent.toLowerCase();
    const isFireTV = userAgent.includes('aftm') || userAgent.includes('aftb') || userAgent.includes('afts');
    const isTV = isFireTV || userAgent.includes('tv') || userAgent.includes('androidtv');

    console.log('🔍 Platform detection:', { userAgent, isFireTV, isTV });

    // Essential video attributes
    this.videoElement.setAttribute('playsinline', '');
    this.videoElement.setAttribute('webkit-playsinline', '');
    
    // Fire TV specific settings
    if (isFireTV) {
      console.log('🔥 Fire TV detected - applying specific settings');
      this.videoElement.setAttribute('preload', 'metadata'); // Less aggressive preloading
      this.videoElement.removeAttribute('crossorigin'); // Remove CORS for Fire TV
    } else {
      this.videoElement.setAttribute('preload', 'auto');
      this.videoElement.setAttribute('crossorigin', 'anonymous');
    }

    // TV-specific settings
    if (isTV) {
      // Disable picture-in-picture for TV platforms
      this.videoElement.setAttribute('disablepictureinpicture', '');
      // Ensure controls are disabled (we handle them ourselves)
      this.videoElement.removeAttribute('controls');
    }

    // Ensure audio is enabled
    this.videoElement.muted = false;
    this.videoElement.volume = 1.0;

    console.log('📺 Video element configured for platform:', isFireTV ? 'Fire TV' : isTV ? 'TV' : 'Web');
  }

  /**
   * Load media metadata (duration, codecs, etc.)
   */
  async loadMediaMetadata() {
    console.log('📊 Loading media metadata...');

    try {
      const mediaInfo = await apiClient.getMediaInfo(this.contentId, this.episodeId);

      // Handle both Duration (capital D) and duration (lowercase d) for compatibility
      const duration = mediaInfo?.Duration || mediaInfo?.duration;

      if (mediaInfo && typeof duration === 'number' && duration > 0) {
        this.duration = duration;
        console.log(`✅ Media duration loaded: ${this.duration}s (${Math.floor(this.duration / 60)}:${Math.floor(this.duration % 60).toString().padStart(2, '0')})`);
        this.updateDurationDisplay();
      } else {
        console.warn('⚠️ Invalid media info response:', mediaInfo);
        console.warn('Expected duration field but got:', { Duration: mediaInfo?.Duration, duration: mediaInfo?.duration });
        // Don't throw - we can still play without duration info
        this.showNotification('Could not load video duration - some features may be limited');
      }

    } catch (error) {
      console.error('❌ Failed to load media metadata:', error);
      // Don't throw - we'll try to get duration from video element later
      this.showNotification('Could not load video duration - will try to detect from stream');
    }
  }

  /**
   * Load media metadata with Fire TV fallback
   */
  async loadMediaMetadataWithFallback() {
    try {
      await this.loadMediaMetadata();
    } catch (error) {
      console.warn('⚠️ Standard metadata loading failed, trying Fire TV fallback');
      
      // Fire TV fallback - try to get basic info from library
      try {
        const content = await apiClient.getLibraryItem(this.contentId, this.profileId);
        if (content && content.runtime) {
          this.duration = content.runtime * 60; // Convert minutes to seconds
          console.log(`🔥 Fire TV fallback: Using runtime from library: ${this.duration}s`);
          this.updateDurationDisplay();
        }
      } catch (fallbackError) {
        console.warn('⚠️ Fire TV fallback also failed:', fallbackError);
        // Continue without duration
      }
    }
  }

  /**
   * Setup video stream and detect playback mode
   */
  async setupStream(startPosition = 0) {
    console.log('🔧 Setting up video stream...');

    try {
      // Get stream URL
      const streamUrl = this.getStreamUrl(startPosition);
      console.log('🔗 Initial stream URL:', streamUrl);

      // Test stream availability and get playback mode
      await this.detectPlaybackMode(streamUrl);

      // Set video source
      this.videoElement.src = streamUrl;

      // Wait for video to be ready
      await this.waitForVideoReady();

      console.log('✅ Video stream setup complete');

    } catch (error) {
      console.error('❌ Failed to setup video stream:', error);
      this.showNotification('Failed to load video stream');
      throw error;
    }
  }

  /**
   * Detect playback mode from server headers
   */
  async detectPlaybackMode(streamUrl) {
    console.log('🔍 Detecting playback mode...');

    try {
      const response = await fetch(streamUrl, { method: 'HEAD' });

      if (!response.ok) {
        throw new Error(`Stream not available: ${response.status} ${response.statusText}`);
      }

      // Get playback mode headers
      const playbackMode = response.headers.get('X-Playback-Mode');
      const transcodeMode = response.headers.get('X-Transcode-Mode');
      const directPlay = response.headers.get('X-Direct-Play');

      console.log('📋 Playback headers:', {
        'X-Playback-Mode': playbackMode,
        'X-Transcode-Mode': transcodeMode,
        'X-Direct-Play': directPlay
      });

      // Determine playback mode
      this.playbackMode = playbackMode || 'unknown';
      this.isTranscoding = directPlay !== 'true';

      // Log playback mode
      const modeEmojis = {
        'direct-play': '▶️',
        'remux': '📦',
        'direct-stream': '🎵',
        'transcode': '🎬'
      };

      const emoji = modeEmojis[this.playbackMode] || '❓';
      console.log(`${emoji} Playback mode: ${this.playbackMode} (transcoding: ${this.isTranscoding})`);

      if (this.isTranscoding) {
        console.log('⚠️ Transcoded stream - seeking will reload stream at new position');
      }

    } catch (error) {
      console.error('❌ Failed to detect playback mode:', error);
      // Default to safe assumptions
      this.playbackMode = 'unknown';
      this.isTranscoding = true;
    }
  }

  /**
   * Get stream URL for current content
   */
  getStreamUrl(startTime = 0) {
    const params = new URLSearchParams();
    params.append('profileId', this.profileId.toString());

    if (this.episodeId) {
      params.append('episodeId', this.episodeId.toString());
    }

    // Always include startTime parameter for consistent server behavior
    params.append('startTime', startTime.toString());

    return `${apiClient.baseURL}/transcoding/stream/${this.contentId}?${params.toString()}`;
  }

  /**
   * Wait for video to be ready for playback
   */
  async waitForVideoReady() {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error('Video loading timeout'));
      }, 30000); // 30 second timeout

      const onReady = () => {
        clearTimeout(timeout);
        this.videoElement.removeEventListener('canplay', onReady);
        this.videoElement.removeEventListener('error', onError);
        resolve();
      };

      const onError = (event) => {
        clearTimeout(timeout);
        this.videoElement.removeEventListener('canplay', onReady);
        this.videoElement.removeEventListener('error', onError);
        reject(new Error(`Video loading error: ${this.videoElement.error?.message || 'Unknown error'}`));
      };

      this.videoElement.addEventListener('canplay', onReady, { once: true });
      this.videoElement.addEventListener('error', onError, { once: true });

      // If already ready, resolve immediately
      if (this.videoElement.readyState >= 3) {
        onReady();
      }
    });
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
      // Start auto-hiding controls when playing
      this.showControls();
      console.log('▶️ Video playing');
    });

    this.videoElement.addEventListener('pause', () => {
      this.isPlaying = false;
      this.stopProgressTracking();
      this.updatePlayPauseButton();
      // Show controls when paused (and keep them visible)
      this.showControls();
      console.log('⏸️ Video paused');
    });

    this.videoElement.addEventListener('ended', () => {
      // For transcoded streams, check if we've actually reached the end
      if (this.isTranscoding && this.duration > 0) {
        const actualProgress = (this.currentTime / this.duration) * 100;
        console.log(`🔍 Video 'ended' event - Progress: ${actualProgress.toFixed(1)}% (${this.currentTime}s / ${this.duration}s)`);

        // Only consider it ended if we're at least 95% through
        if (actualProgress < 95) {
          console.log(`⚠️ Premature 'ended' event detected - attempting stream reload to continue playback`);
          
          // Try to reload the stream at the current position to continue playback
          this.reloadStreamAtTime(this.currentTime).catch(error => {
            console.error('❌ Failed to reload stream after premature end:', error);
            this.showNotification('Video ended unexpectedly - try seeking to continue');
          });
          return;
        }
      }

      this.isPlaying = false;
      this.stopProgressTracking();
      this.saveProgress(true); // Mark as completed
      console.log('🏁 Video ended');
    });

    // Time updates
    this.videoElement.addEventListener('timeupdate', () => {
      // For transcoded streams, add the start offset to get actual position
      const rawTime = this.videoElement.currentTime;
      this.currentTime = rawTime + this.startOffset;
      this.updateProgressBar();
    });

    // Duration updates
    this.videoElement.addEventListener('loadedmetadata', () => {
      const elementDuration = this.videoElement.duration;
      console.log(`📏 Video element duration: ${elementDuration}s`);

      // For transcoded streams, video element duration is often incorrect
      if (this.isTranscoding) {
        console.log(`⚠️ Ignoring video element duration for transcoded stream (${elementDuration}s) - using API duration (${this.duration}s)`);
        return;
      }

      // Only use video element duration if we don't have it from API
      if (!this.duration || this.duration <= 0) {
        if (Number.isFinite(elementDuration) && elementDuration > 0) {
          this.duration = elementDuration;
          console.log(`✅ Using video element duration: ${this.duration}s`);
          this.updateDurationDisplay();
        }
      }
    });

    // Volume changes
    this.videoElement.addEventListener('volumechange', () => {
      this.volume = this.videoElement.volume;
      this.isMuted = this.videoElement.muted;
      this.updateVolumeDisplay();
    });

    // Buffering events
    this.videoElement.addEventListener('waiting', () => {
      console.log('⏳ Video buffering...');
      this.showLoadingSpinner();
    });

    this.videoElement.addEventListener('playing', () => {
      console.log('▶️ Video playing (buffering complete)');
      this.hideLoadingSpinner();
    });

    // Error handling
    this.videoElement.addEventListener('error', (event) => {
      const error = this.videoElement.error;
      console.error('❌ Video error:', error);

      const errorMessages = {
        1: 'MEDIA_ERR_ABORTED - Video loading was aborted',
        2: 'MEDIA_ERR_NETWORK - Network error occurred',
        3: 'MEDIA_ERR_DECODE - Video is corrupted or unsupported',
        4: 'MEDIA_ERR_SRC_NOT_SUPPORTED - Video format not supported'
      };

      const message = errorMessages[error?.code] || 'Unknown video error';
      this.showNotification(`Video Error: ${message}`);
    });

    // Fullscreen events
    document.addEventListener('fullscreenchange', () => {
      this.isFullscreen = !!document.fullscreenElement;
      this.updateFullscreenButton();
    });

    // Keyboard controls
    document.addEventListener('keydown', (event) => {
      if (this.isInitialized && !this.isDestroyed) {
        this.handleKeyboard(event);
      }
    });

    console.log('🎧 Event listeners setup complete');
  }

  /**
   * Setup player controls UI
   */
  setupControls() {
    const controlsContainer = document.getElementById('player-controls');
    if (!controlsContainer) {
      console.warn('⚠️ Player controls container not found');
      return;
    }

    controlsContainer.innerHTML = `
      <div class="player-controls-overlay">
        <div class="player-progress-container">
          <div class="player-progress-wrapper">
            <div class="player-progress-buffered"></div>
            <div class="player-progress-bar">
              <div class="player-progress-thumb"></div>
            </div>
          </div>
        </div>
        <div class="player-controls-bottom">
          <div class="player-controls-left">
            <button class="player-btn play-pause-btn" title="Play/Pause (Space)">
              <svg class="play-icon" viewBox="0 0 24 24">
                <path d="M8 5v14l11-7z"/>
              </svg>
              <svg class="pause-icon" viewBox="0 0 24 24" style="display: none;">
                <path d="M6 4h4v16H6V4zm8 0h4v16h-4V4z"/>
              </svg>
            </button>
            <button class="player-btn skip-back-btn" title="Rewind 10s (←)">
              <svg viewBox="0 0 24 24">
                <path d="M11.99 5V1l-5 5 5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6h-2c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z"/>
                <text x="12" y="16" text-anchor="middle" font-size="8" fill="currentColor">10</text>
              </svg>
            </button>
            <button class="player-btn skip-forward-btn" title="Forward 10s (→)">
              <svg viewBox="0 0 24 24">
                <path d="M12 5V1l5 5-5 5V7c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6h2c0 4.42-3.58 8-8 8s-8-3.58-8-8 3.58-8 8-8z"/>
                <text x="12" y="16" text-anchor="middle" font-size="8" fill="currentColor">10</text>
              </svg>
            </button>
            <div class="player-volume-control">
              <button class="player-btn volume-btn" title="Mute (M)">
                <svg class="volume-high-icon" viewBox="0 0 24 24">
                  <path d="M3 9v6h4l5 5V4L7 9H3zm13.5 3c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM14 3.23v2.06c2.89.86 5 3.54 5 6.71s-2.11 5.85-5 6.71v2.06c4.01-.91 7-4.49 7-8.77s-2.99-7.86-7-8.77z"/>
                </svg>
                <svg class="volume-muted-icon" viewBox="0 0 24 24" style="display: none;">
                  <path d="M16.5 12c0-1.77-1.02-3.29-2.5-4.03v2.21l2.45 2.45c.03-.2.05-.41.05-.63zm2.5 0c0 .94-.2 1.82-.54 2.64l1.51 1.51C20.63 14.91 21 13.5 21 12c0-4.28-2.99-7.86-7-8.77v2.06c2.89.86 5 3.54 5 6.71zM4.27 3L3 4.27 7.73 9H3v6h4l5 5v-6.73l4.25 4.25c-.67.52-1.42.93-2.25 1.18v2.06c1.38-.31 2.63-.95 3.69-1.81L19.73 21 21 19.73l-9-9L4.27 3zM12 4L9.91 6.09 12 8.18V4z"/>
                </svg>
              </button>
              <div class="volume-slider-container">
                <input type="range" class="volume-slider" min="0" max="100" value="100">
              </div>
            </div>
            <span class="player-time">
              <span class="current-time">0:00</span> / <span class="duration">--:--</span>
            </span>
          </div>
          <div class="player-controls-right">
            <button class="player-btn fullscreen-btn" title="Fullscreen (F)">
              <svg class="fullscreen-enter-icon" viewBox="0 0 24 24">
                <path d="M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"/>
              </svg>
              <svg class="fullscreen-exit-icon" viewBox="0 0 24 24" style="display: none;">
                <path d="M5 16h3v3h2v-5H5v2zm3-8H5v2h5V5H8v3zm6 11h2v-3h3v-2h-5v5zm2-11V5h-2v5h5V8h-3z"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
    `;

    // Add center play button
    const centerPlayBtn = document.createElement('div');
    centerPlayBtn.className = 'center-play-button';
    centerPlayBtn.innerHTML = `
      <svg viewBox="0 0 24 24">
        <path d="M8 5v14l11-7z"/>
      </svg>
    `;
    document.querySelector('.player-container').appendChild(centerPlayBtn);

    // Attach control event listeners
    this.attachControlListeners();

    console.log('🎮 Controls setup complete');
  }

  /**
   * Attach event listeners to control buttons
   */
  attachControlListeners() {
    // Play/Pause
    document.querySelector('.play-pause-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.togglePlayPause();
    });

    document.querySelector('.center-play-button')?.addEventListener('click', () => {
      this.togglePlayPause();
    });

    // Skip buttons
    document.querySelector('.skip-back-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.seek(this.currentTime - 10);
    });

    document.querySelector('.skip-forward-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.seek(this.currentTime + 10);
    });

    // Volume controls
    document.querySelector('.volume-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.toggleMute();
    });

    document.querySelector('.volume-slider')?.addEventListener('input', (e) => {
      this.setVolume(e.target.value / 100);
    });

    // Progress bar seeking
    const progressContainer = document.querySelector('.player-progress-container');
    progressContainer?.addEventListener('click', (e) => {
      if (!this.duration || this.duration <= 0) {
        this.showNotification('Video duration not available yet');
        return;
      }

      const rect = progressContainer.getBoundingClientRect();
      const percent = (e.clientX - rect.left) / rect.width;
      const seekTime = percent * this.duration;
      this.seek(seekTime);
    });

    // Fullscreen
    document.querySelector('.fullscreen-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.toggleFullscreen();
    });

    // Mouse controls for showing/hiding controls
    this.setupMouseControls();
  }

  /**
   * Setup mouse controls for showing/hiding player controls
   */
  setupMouseControls() {
    const playerContainer = document.querySelector('.player-container');

    if (!playerContainer) return;

    playerContainer.addEventListener('mousemove', () => this.showControls());
    playerContainer.addEventListener('mouseleave', () => this.hideControls());

    // Show controls initially
    this.showControls();
  }

  /**
   * Show player controls
   */
  showControls() {
    const controls = document.querySelector('.player-controls');
    const backButton = document.querySelector('.back-button');
    const playerContainer = document.querySelector('.player-container');

    if (controls) {
      controls.classList.add('visible');
    }
    if (backButton) {
      backButton.style.opacity = '1';
    }
    if (playerContainer) {
      playerContainer.classList.add('show-cursor');
    }

    this.controlsVisible = true;

    // Clear existing timeout
    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
      this.controlsTimeout = null;
    }

    // Hide controls after 3 seconds if playing
    if (this.isPlaying) {
      this.controlsTimeout = setTimeout(() => {
        this.hideControls();
      }, 3000);
    }
  }

  /**
   * Hide player controls
   */
  hideControls() {
    // Don't hide when paused or not initialized
    if (!this.isPlaying || !this.isInitialized) return;

    const controls = document.querySelector('.player-controls');
    const backButton = document.querySelector('.back-button');
    const playerContainer = document.querySelector('.player-container');

    if (controls) {
      controls.classList.remove('visible');
    }
    if (backButton) {
      backButton.style.opacity = '0';
    }
    if (playerContainer) {
      playerContainer.classList.remove('show-cursor');
    }

    this.controlsVisible = false;

    // Clear timeout
    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
      this.controlsTimeout = null;
    }
  }

  // ==================== PLAYBACK CONTROLS ====================

  /**
   * Play video
   */
  async play() {
    try {
      await this.videoElement.play();
    } catch (error) {
      console.error('❌ Failed to play video:', error);

      if (error.name === 'NotAllowedError') {
        this.showNotification('Click to play - browser requires user interaction');
      } else {
        this.showNotification('Failed to play video');
      }
    }
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
  async seek(targetTime) {
    if (!this.duration || this.duration <= 0) {
      console.warn('⚠️ Cannot seek - duration not available');
      return;
    }

    const clampedTime = Math.max(0, Math.min(targetTime, this.duration));
    console.log(`⏭️ Seeking to ${clampedTime.toFixed(1)}s (mode: ${this.playbackMode})`);

    if (this.isTranscoding) {
      // For transcoded streams, reload stream at new position
      await this.reloadStreamAtTime(clampedTime);
    } else {
      // For direct play, use normal seeking
      this.videoElement.currentTime = clampedTime;
    }
  }

  /**
   * Reload transcoded stream at specific time
   */
  async reloadStreamAtTime(time) {
    console.log(`🔄 Reloading transcoded stream at ${time}s`);

    const wasPlaying = this.isPlaying;

    try {
      // Pause and show loading
      this.pause();
      this.showLoadingSpinner();

      // Stop progress tracking during reload
      this.stopProgressTracking();

      // Update start offset
      this.startOffset = time;

      // Get new stream URL with start time
      const newStreamUrl = this.getStreamUrl(time);
      console.log('🔗 Seek stream URL:', newStreamUrl);

      // Load new source
      this.videoElement.src = newStreamUrl;

      // Wait for video to be ready
      await this.waitForVideoReady();

      console.log(`✅ Stream reloaded at ${time}s`);

      // Resume playback if it was playing
      if (wasPlaying) {
        await this.play();
      }

    } catch (error) {
      console.error('❌ Failed to reload stream:', error);
      this.showNotification('Failed to seek in video');
    } finally {
      this.hideLoadingSpinner();
    }
  }

  /**
   * Set volume (0.0 to 1.0)
   */
  setVolume(volume) {
    const clampedVolume = Math.max(0, Math.min(1, volume));
    this.videoElement.volume = clampedVolume;
    this.volume = clampedVolume;

    // Unmute if setting volume > 0
    if (clampedVolume > 0 && this.isMuted) {
      this.videoElement.muted = false;
    }
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
    const playerContainer = document.querySelector('.player-container');

    if (!this.isFullscreen) {
      // Enter fullscreen
      if (playerContainer.requestFullscreen) {
        playerContainer.requestFullscreen();
      } else if (playerContainer.webkitRequestFullscreen) {
        playerContainer.webkitRequestFullscreen();
      } else if (playerContainer.mozRequestFullScreen) {
        playerContainer.mozRequestFullScreen();
      } else if (playerContainer.msRequestFullscreen) {
        playerContainer.msRequestFullscreen();
      }
    } else {
      // Exit fullscreen
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if (document.webkitExitFullscreen) {
        document.webkitExitFullscreen();
      } else if (document.mozCancelFullScreen) {
        document.mozCancelFullScreen();
      } else if (document.msExitFullscreen) {
        document.msExitFullscreen();
      }
    }
  }

  // ==================== PROGRESS TRACKING ====================

  /**
   * Start progress tracking
   */
  startProgressTracking() {
    if (this.progressInterval) return;

    this.progressInterval = setInterval(() => {
      this.saveProgress();
    }, this.progressUpdateFrequency);

    console.log('📊 Progress tracking started');
  }

  /**
   * Stop progress tracking
   */
  stopProgressTracking() {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
      console.log('📊 Progress tracking stopped');
    }

    // Save final progress
    this.saveProgress();
  }

  /**
   * Save watch progress to backend
   */
  async saveProgress(completed = false) {
    // Only save if progress has changed significantly (more than 5 seconds)
    if (!completed && Math.abs(this.currentTime - this.lastSavedProgress) < 5) {
      return;
    }

    try {
      await apiClient.updateWatchProgress(
        this.contentId,
        this.profileId,
        Math.floor(this.currentTime),
        this.duration ? Math.floor(this.duration) : null,
        this.episodeId
      );

      this.lastSavedProgress = this.currentTime;

      if (completed) {
        console.log('✅ Watch progress saved (completed)');
      }

    } catch (error) {
      console.error('❌ Failed to save watch progress:', error);
    }
  }

  // ==================== UI UPDATES ====================

  /**
   * Update progress bar
   */
  updateProgressBar() {
    if (!this.duration || this.duration <= 0) return;

    const progressBar = document.querySelector('.player-progress-bar');
    const currentTimeDisplay = document.querySelector('.current-time');

    if (progressBar) {
      const percent = (this.currentTime / this.duration) * 100;
      progressBar.style.width = `${Math.min(percent, 100)}%`;
    }

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
      durationDisplay.textContent = this.duration > 0 ? this.formatTime(this.duration) : '--:--';
    }
  }

  /**
   * Update play/pause button
   */
  updatePlayPauseButton() {
    const playIcon = document.querySelector('.play-pause-btn .play-icon');
    const pauseIcon = document.querySelector('.play-pause-btn .pause-icon');
    const centerPlayBtn = document.querySelector('.center-play-button');
    const playerContainer = document.querySelector('.player-container');

    if (this.isPlaying) {
      if (playIcon) playIcon.style.display = 'none';
      if (pauseIcon) pauseIcon.style.display = 'block';
      if (centerPlayBtn) centerPlayBtn.classList.remove('visible');
      if (playerContainer) playerContainer.classList.remove('paused');
    } else {
      if (playIcon) playIcon.style.display = 'block';
      if (pauseIcon) pauseIcon.style.display = 'none';
      if (centerPlayBtn) centerPlayBtn.classList.add('visible');
      if (playerContainer) playerContainer.classList.add('paused');
    }
  }

  /**
   * Update volume display
   */
  updateVolumeDisplay() {
    const volumeSlider = document.querySelector('.volume-slider');
    const volumeHighIcon = document.querySelector('.volume-high-icon');
    const volumeMutedIcon = document.querySelector('.volume-muted-icon');

    if (volumeSlider) {
      volumeSlider.value = this.isMuted ? 0 : this.volume * 100;
    }

    if (this.isMuted || this.volume === 0) {
      if (volumeHighIcon) volumeHighIcon.style.display = 'none';
      if (volumeMutedIcon) volumeMutedIcon.style.display = 'block';
    } else {
      if (volumeHighIcon) volumeHighIcon.style.display = 'block';
      if (volumeMutedIcon) volumeMutedIcon.style.display = 'none';
    }
  }

  /**
   * Update fullscreen button
   */
  updateFullscreenButton() {
    const enterIcon = document.querySelector('.fullscreen-enter-icon');
    const exitIcon = document.querySelector('.fullscreen-exit-icon');

    if (this.isFullscreen) {
      if (enterIcon) enterIcon.style.display = 'none';
      if (exitIcon) exitIcon.style.display = 'block';
    } else {
      if (enterIcon) enterIcon.style.display = 'block';
      if (exitIcon) exitIcon.style.display = 'none';
    }
  }

  // ==================== KEYBOARD CONTROLS ====================

  /**
   * Handle keyboard controls
   */
  handleKeyboard(event) {
    // Don't handle if user is typing in an input
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') {
      return;
    }

    switch (event.key) {
      case ' ':
      case 'k':
        event.preventDefault();
        this.togglePlayPause();
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.seek(this.currentTime - 10);
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.seek(this.currentTime + 10);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.setVolume(this.volume + 0.1);
        break;
      case 'ArrowDown':
        event.preventDefault();
        this.setVolume(this.volume - 0.1);
        break;
      case 'm':
        event.preventDefault();
        this.toggleMute();
        break;
      case 'f':
        event.preventDefault();
        this.toggleFullscreen();
        break;
      case 'Escape':
        if (this.isFullscreen) {
          event.preventDefault();
          this.toggleFullscreen();
        }
        break;
    }
  }

  // ==================== UTILITY METHODS ====================

  /**
   * Format time in MM:SS or HH:MM:SS
   */
  formatTime(seconds) {
    if (!Number.isFinite(seconds) || seconds < 0) return '0:00';

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
  showNotification(message, duration = 3000) {
    let notification = document.getElementById('player-notification');

    if (!notification) {
      notification = document.createElement('div');
      notification.id = 'player-notification';
      notification.style.cssText = `
        position: fixed;
        top: 20px;
        left: 50%;
        transform: translateX(-50%);
        background: rgba(0, 0, 0, 0.9);
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        z-index: 10000;
        font-size: 14px;
        font-weight: 500;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
        transition: opacity 0.3s ease;
        max-width: 400px;
        text-align: center;
      `;
      document.body.appendChild(notification);
    }

    notification.textContent = message;
    notification.style.opacity = '1';

    setTimeout(() => {
      notification.style.opacity = '0';
    }, duration);
  }

  /**
   * Show loading spinner
   */
  showLoadingSpinner() {
    let spinner = document.getElementById('player-loading-spinner');

    if (!spinner) {
      spinner = document.createElement('div');
      spinner.id = 'player-loading-spinner';
      spinner.style.cssText = `
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        z-index: 1000;
        pointer-events: none;
      `;
      spinner.innerHTML = `
        <div style="
          width: 40px;
          height: 40px;
          border: 3px solid rgba(255, 255, 255, 0.3);
          border-top: 3px solid white;
          border-radius: 50%;
          animation: spin 1s linear infinite;
        "></div>
        <style>
          @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
          }
        </style>
      `;
      document.querySelector('.player-container').appendChild(spinner);
    }

    spinner.style.display = 'block';
  }

  /**
   * Hide loading spinner
   */
  hideLoadingSpinner() {
    const spinner = document.getElementById('player-loading-spinner');
    if (spinner) {
      spinner.style.display = 'none';
    }
  }

  // ==================== CLEANUP ====================

  /**
   * Destroy the video player and cleanup resources
   */
  destroy() {
    if (this.isDestroyed) return;

    console.log('🧹 Destroying video player...');

    // Stop progress tracking
    this.stopProgressTracking();

    // Pause and clear video
    this.videoElement.pause();
    this.videoElement.src = '';

    // Clear timeouts
    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
    }

    // Remove event listeners (they'll be cleaned up when elements are removed)

    // Clean up UI elements
    const notification = document.getElementById('player-notification');
    if (notification) {
      notification.remove();
    }

    const spinner = document.getElementById('player-loading-spinner');
    if (spinner) {
      spinner.remove();
    }

    // Mark as destroyed
    this.isDestroyed = true;
    this.isInitialized = false;

    console.log('✅ Video player destroyed');
  }
}

export default VideoPlayer;