/**
 * Video Player Module
 * Handles video playback with controls, progress tracking, and subtitle support
 */

import apiClient from './api-client.js';
import stateManager from './data.js';
import Hls from 'https://cdn.jsdelivr.net/npm/hls.js@1.5.6/dist/hls.mjs';

export class VideoPlayer {
  constructor(videoElement, profileId) {
    this.videoElement = videoElement;
    this.profileId = profileId;
    this.contentId = null;
    this.episodeId = null;
    this.contentType = null;
    this.isTranscoding = false;
    this.streamType = 'file';
    this.hls = null;
    this.activeSessionId = null;

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
    
    // Transcoding offset - tracks where we started in the original video
    this.startOffset = 0;

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

    // Setup event listeners first (to catch any errors)
    this.setupEventListeners();

    // Setup controls
    this.setupControls();

    // Ensure audio is enabled - CRITICAL: Set these BEFORE setting src
    this.videoElement.muted = false;
    this.videoElement.volume = 1.0;

    // Set video attributes for audio playback
    this.videoElement.setAttribute('muted', 'false');
    this.videoElement.removeAttribute('muted');

    // Build stream URL (include start offset when resuming)
    const streamUrl = apiClient.getStreamUrl(
      contentId,
      episodeId,
      this.profileId,
      startPosition > 0 ? startPosition : null
    );
    console.log('Setting video source:', streamUrl);

    let headResponse;
    try {
      headResponse = await fetch(streamUrl, { method: 'HEAD' });
      console.log('Stream HEAD response status:', headResponse.status);
      console.log('Stream HEAD response headers:', Object.fromEntries(headResponse.headers.entries()));

      if (!headResponse.ok) {
        console.error('Stream URL returned error:', headResponse.status, headResponse.statusText);
        const errorText = await fetch(streamUrl).then(r => r.text());
        console.error('Error response:', errorText);
        this.showNotification(`Stream error: ${headResponse.status} ${headResponse.statusText}`);
        return;
      }
    } catch (error) {
      console.error('Failed to test stream URL:', error);
      this.showNotification('Cannot connect to streaming server');
      return;
    }

    await this.detectTranscodingMode(headResponse.headers);

    const streamType = headResponse.headers.get('X-Stream-Type') || 'file';
    const sessionId = headResponse.headers.get('X-Transcode-Session');

    if (streamType === 'hls') {
      if (!sessionId) {
        console.error('HLS stream requested but no session id returned');
        this.showNotification('Streaming session could not be created');
        return;
      }

      this.streamType = 'hls';
      this.isTranscoding = true;
      this.activeSessionId = sessionId;
      this.startOffset = startPosition || 0;

      try {
        const manifestUrl = this.appendQueryParam(streamUrl, 'session', sessionId);
        await this.loadHlsStream(manifestUrl);
      } catch (error) {
        console.error('Failed to initialize HLS stream:', error);
        this.showNotification('Failed to start transcoded stream');
        return;
      }
    } else {
      this.streamType = 'file';
      this.isTranscoding = false;
      this.activeSessionId = null;
      this.startOffset = 0;
      this.destroyHls();
      this.videoElement.src = streamUrl;
      this.videoElement.load();
    }

    // Load content metadata to get duration (async, non-blocking)
    this.loadContentMetadata().catch(err => {
      console.warn('Failed to load metadata:', err);
    });

    // Force unmute after source is set (some browsers auto-mute)
    this.videoElement.addEventListener('loadedmetadata', () => {
      this.videoElement.muted = false;
      this.videoElement.volume = 1.0;

      // If duration is still not set from probe, use video element duration
      if (!this.duration || this.duration === 0) {
        this.duration = this.videoElement.duration;
        console.log(`Duration fallback from video element: ${this.duration}s`);
      }

      // Update duration display once we have it
      this.updateDurationDisplay();
    }, { once: true });

    // Load subtitles (async, non-blocking)
    this.loadSubtitles().catch(err => {
      console.warn('Failed to load subtitles:', err);
    });

    if (startPosition > 0 && this.streamType !== 'hls') {
      this.videoElement.addEventListener('loadedmetadata', () => {
        this.videoElement.currentTime = startPosition;
      }, { once: true });
    }

    // Start playing once video is ready
    this.videoElement.addEventListener('canplay', () => {
      this.play();
    }, { once: true });
  }

  /**
   * Load content metadata (runtime/duration)
   */
  async loadContentMetadata() {
    try {
      // Use the stream info endpoint to get actual media duration from ffprobe
      const data = await apiClient.getMediaInfo(this.contentId, this.episodeId);

      if (data.mediaInfo && data.mediaInfo.duration) {
        this.duration = data.mediaInfo.duration;
        console.log(`Duration from media probe: ${this.duration}s (${Math.floor(this.duration / 60)} minutes)`);
      }
    } catch (error) {
      console.warn('Failed to load media info, will use video element duration:', error);
    }
  }

  /**
   * Detect if stream is being transcoded by checking response headers
   */
  async detectTranscodingMode(preloadedHeaders = null) {
    try {
      let headers = preloadedHeaders;

      if (!headers) {
        const streamUrl = apiClient.getStreamUrl(this.contentId, this.episodeId, this.profileId);
        const response = await fetch(streamUrl, { method: 'HEAD' });
        headers = response.headers;
      }

      if (!headers) {
        this.isTranscoding = false;
        return;
      }

      const streamType = headers.get('X-Stream-Type');
      const transcodeMode = headers.get('X-Transcode-Mode');
      const directPlay = headers.get('X-Direct-Play');

      this.isTranscoding = streamType === 'hls' || (!!transcodeMode && directPlay !== 'true');

      console.log('=== Transcoding Detection ===');
      console.log('X-Stream-Type:', streamType);
      console.log('X-Transcode-Mode:', transcodeMode);
      console.log('X-Direct-Play:', directPlay);
      console.log('isTranscoding:', this.isTranscoding);

      if (this.isTranscoding) {
        console.log('🎬 Transcoding mode detected:', transcodeMode || 'hls');
        console.log('⚠️ Seeking will reload stream at new position');
      } else {
        console.log('▶️ Direct play mode - normal seeking enabled');
      }
    } catch (error) {
      console.warn('Failed to detect transcoding mode:', error);
      this.isTranscoding = false;
    }
  }

  /**
   * Setup video element event listeners
   */
  setupEventListeners() {
    // Error handling
    this.videoElement.addEventListener('error', (e) => {
      console.error('Video element error:', e);
      if (this.videoElement.error) {
        console.error('Error code:', this.videoElement.error.code);
        console.error('Error message:', this.videoElement.error.message);

        const errorMessages = {
          1: 'MEDIA_ERR_ABORTED - The video download was aborted',
          2: 'MEDIA_ERR_NETWORK - A network error occurred',
          3: 'MEDIA_ERR_DECODE - The video is corrupted or not supported',
          4: 'MEDIA_ERR_SRC_NOT_SUPPORTED - The video format is not supported'
        };

        const errorMsg = errorMessages[this.videoElement.error.code] || 'Unknown error';
        console.error('Error details:', errorMsg);
        this.showNotification(`Video Error: ${errorMsg}`);
      }
    });

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
      // For transcoded streams, add the start offset to get actual position
      const rawTime = this.videoElement.currentTime;
      this.currentTime = rawTime + this.startOffset;
      
      // Debug log every 5 seconds
      if (Math.floor(this.currentTime) % 5 === 0 && Math.floor(rawTime) !== Math.floor(rawTime - 0.1)) {
        console.log(`Time: ${this.currentTime.toFixed(1)}s (raw: ${rawTime.toFixed(1)}s + offset: ${this.startOffset}s)`);
      }
      
      this.updateProgressBar();
    });

    this.videoElement.addEventListener('loadedmetadata', () => {
      // Only use video element duration if we don't have it from probe
      if (!this.duration || this.duration === 0) {
        this.duration = this.videoElement.duration;
        console.log(`Duration from video element: ${this.duration}s`);
      }
      this.updateDurationDisplay();

      // Detect if transcoding (no duration or specific headers)
      this.isTranscoding = !this.videoElement.duration || this.videoElement.duration === Infinity;
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
          <div class="player-progress-wrapper">
            <div class="player-progress-buffered"></div>
            <div class="player-progress-bar">
              <div class="player-progress-thumb"></div>
            </div>
          </div>
        </div>
        <div class="player-controls-bottom">
          <div class="player-controls-left">
            <button class="player-btn play-pause-btn" title="Play/Pause (k)">
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
              <button class="player-btn volume-btn" title="Mute (m)">
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
              <span class="current-time">0:00</span> / <span class="duration">0:00</span>
            </span>
          </div>
          <div class="player-controls-right">
            <button class="player-btn subtitles-btn" title="Subtitles (c)">
              <svg viewBox="0 0 24 24">
                <path d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zM4 12h4v2H4v-2zm10 6H4v-2h10v2zm6 0h-4v-2h4v2zm0-4H10v-2h10v2z"/>
              </svg>
            </button>
            <button class="player-btn settings-btn" title="Settings">
              <svg viewBox="0 0 24 24">
                <path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/>
              </svg>
            </button>
            <button class="player-btn fullscreen-btn" title="Fullscreen (f)">
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

    // Center play button
    document.querySelector('.center-play-button')?.addEventListener('click', () => {
      this.togglePlayPause();
    });

    // Skip back
    document.querySelector('.skip-back-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.seek(this.currentTime - 10);
    });

    // Skip forward
    document.querySelector('.skip-forward-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.seek(this.currentTime + 10);
    });

    // Volume
    document.querySelector('.volume-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.toggleMute();
    });

    document.querySelector('.volume-slider')?.addEventListener('input', (e) => {
      this.setVolume(e.target.value / 100);
    });

    // Progress bar - click to seek
    const progressContainer = document.querySelector('.player-progress-container');
    progressContainer?.addEventListener('click', (e) => {
      const rect = progressContainer.getBoundingClientRect();
      const percent = (e.clientX - rect.left) / rect.width;
      const seekTime = percent * this.duration;
      this.seek(seekTime);
    });

    // Subtitles
    document.querySelector('.subtitles-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.showSubtitleMenu();
    });

    // Fullscreen
    document.querySelector('.fullscreen-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.toggleFullscreen();
    });

    // Show controls on mouse move
    let controlsTimeout;
    const playerContainer = document.querySelector('.player-container');
    const controls = document.querySelector('.player-controls');

    const showControls = () => {
      controls?.classList.add('visible');
      clearTimeout(controlsTimeout);

      if (this.isPlaying) {
        controlsTimeout = setTimeout(() => {
          controls?.classList.remove('visible');
        }, 3000);
      }
    };

    playerContainer?.addEventListener('mousemove', showControls);
    playerContainer?.addEventListener('mouseleave', () => {
      if (this.isPlaying) {
        controls?.classList.remove('visible');
      }
    });
  }

  /**
   * Play video
   */
  play() {
    this.videoElement.play().catch(error => {
      console.error('Failed to play video:', error);
      console.error('Video src:', this.videoElement.src);
      console.error('Video readyState:', this.videoElement.readyState);
      console.error('Video networkState:', this.videoElement.networkState);

      // Show user-friendly error
      this.showNotification('Failed to play video. Check console for details.');
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
    const targetTime = Math.max(0, Math.min(time, this.duration));
    console.log(`Seeking to ${targetTime}s (duration: ${this.duration}s, isTranscoding: ${this.isTranscoding})`);

    if (this.isTranscoding || this.streamType === 'hls') {
      // For transcoded streams, always reload at new position
      // This ensures proper seeking without buffering issues
      console.log('Using transcode seek (reload stream)');
      void this.reloadStreamAtTime(targetTime);
    } else {
      // For direct play, use normal seeking
      console.log('Using direct play seek');
      this.videoElement.currentTime = targetTime;
    }
  }

  /**
   * Reload stream at specific time (for transcoded streams)
   */
  async reloadStreamAtTime(time) {
    console.log(`Reloading stream at ${time}s (current type: ${this.streamType})`);
    const wasPlaying = !this.videoElement.paused;

    this.videoElement.pause();
    this.stopProgressTracking();

    const streamUrl = apiClient.getStreamUrl(this.contentId, this.episodeId, this.profileId, time);
    console.log('New stream URL:', streamUrl);

    let headResponse;
    try {
      headResponse = await fetch(streamUrl, { method: 'HEAD' });
      if (!headResponse.ok) {
        throw new Error(`HEAD request failed with status ${headResponse.status}`);
      }
    } catch (error) {
      console.error('Failed to start new streaming session for seek:', error);
      this.showNotification('Failed to seek - unable to restart stream');
      return;
    }

    await this.detectTranscodingMode(headResponse.headers);

    const streamType = headResponse.headers.get('X-Stream-Type') || 'file';
    const sessionId = headResponse.headers.get('X-Transcode-Session');
    this.streamType = streamType;

    if (streamType === 'hls') {
      if (!sessionId) {
        console.error('Missing session id for HLS reload');
        this.showNotification('Failed to resume transcoded stream');
        return;
      }

      this.activeSessionId = sessionId;
      this.startOffset = time;

      try {
        const manifestUrl = this.appendQueryParam(streamUrl, 'session', sessionId);
        await this.loadHlsStream(manifestUrl);
      } catch (error) {
        console.error('Failed to reload HLS stream:', error);
        this.showNotification('Failed to reload stream');
        return;
      }
    } else {
      this.destroyHls();
      this.activeSessionId = null;
      this.startOffset = 0;
      this.videoElement.src = streamUrl;
      this.videoElement.load();
      this.videoElement.addEventListener('loadedmetadata', () => {
        this.videoElement.currentTime = time;
      }, { once: true });
    }

    const resumePlayback = wasPlaying;
    this.videoElement.addEventListener('canplay', () => {
      if (resumePlayback) {
        this.play();
        this.startProgressTracking();
      }
    }, { once: true });
  }

  appendQueryParam(url, key, value) {
    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}${encodeURIComponent(key)}=${encodeURIComponent(value)}`;
  }

  async loadHlsStream(manifestUrl) {
    this.destroyHls();

    if (Hls.isSupported()) {
      this.hls = new Hls({
        enableWorker: true,
        lowLatencyMode: false,
        backBufferLength: 120
      });

      this.hls.on(Hls.Events.ERROR, (_event, data) => {
        if (!data) {
          return;
        }

        if (data.fatal) {
          console.error('Fatal HLS error:', data);
          switch (data.type) {
            case Hls.ErrorTypes.NETWORK_ERROR:
              this.hls?.startLoad();
              break;
            case Hls.ErrorTypes.MEDIA_ERROR:
              this.hls?.recoverMediaError();
              break;
            default:
              this.showNotification('Streaming error - please try again');
              this.destroyHls();
              break;
          }
        }
      });

      this.hls.attachMedia(this.videoElement);
      this.hls.on(Hls.Events.MEDIA_ATTACHED, () => {
        this.hls?.loadSource(manifestUrl);
      });
    } else if (this.videoElement.canPlayType('application/vnd.apple.mpegurl')) {
      this.videoElement.src = manifestUrl;
      this.videoElement.load();
    } else {
      throw new Error('HLS is not supported in this browser');
    }
  }

  destroyHls() {
    if (this.hls) {
      try {
        this.hls.destroy();
      } catch (error) {
        console.warn('Failed to destroy HLS instance:', error);
      }
      this.hls = null;
    }
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
      const percent = (this.currentTime / this.duration) * 100;
      progressBar.style.width = `${percent}%`;
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
    if (volumeSlider) {
      volumeSlider.value = this.isMuted ? 0 : this.volume * 100;
    }

    const volumeHighIcon = document.querySelector('.volume-high-icon');
    const volumeMutedIcon = document.querySelector('.volume-muted-icon');

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

  /**
   * Show/hide controls
   */
  showControls() {
    const controls = document.getElementById('player-controls');
    if (controls) {
      controls.classList.add('visible');
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
      controls.classList.remove('visible');
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
    this.destroyHls();
    this.activeSessionId = null;
    this.streamType = 'file';

    if (this.controlsTimeout) {
      clearTimeout(this.controlsTimeout);
    }
  }
}

export default VideoPlayer;
