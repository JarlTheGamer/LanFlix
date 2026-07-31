/**
 * Content Modal Module
 * Displays detailed content information in a modal with ambilight background
 */

import apiClient from './api-client.js';
import stateManager from './data.js';

export class ContentModal {
    constructor(profileManager, navigation = null) {
        this.profileManager = profileManager;
        this.navigation = navigation;
        this.modal = null;
        this.currentContent = null;
    }

    /**
     * Show modal with content details
     */
    async show(contentId, contentType, isDiscovery = false) {
        try {
            // Validate parameters
            if (!contentId) {
                throw new Error('Content ID is required');
            }
            if (!contentType || contentType === 'undefined') {
                throw new Error('Content type is required (must be "movie" or "series")');
            }

            console.log('ContentModal.show:', { contentId, contentType, isDiscovery });

            // Fetch content details
            const profileId = this.profileManager.selectedProfileId;
            const content = isDiscovery
                ? await apiClient.getContentDetails(contentId, contentType, profileId)
                : await apiClient.getLibraryItem(contentId, profileId);

            // Fetch watch history for series to show episode progress
            if (contentType === 'series' && !isDiscovery) {
                try {
                    const watchHistory = await apiClient.getWatchHistory(profileId, 200);
                    // Filter for episodes of this series
                    const seriesHistory = watchHistory.filter(h => h.contentId == contentId && h.episodeId);

                    // Create a map of episodeId -> progress
                    this.episodeProgressMap = new Map();
                    this.lastInteractionEpisodeId = null;
                    let lastActivity = 0;

                    seriesHistory.forEach(h => {
                        const progressSeconds = h.positionTicks ? Math.floor(h.positionTicks / 10_000_000) : 0;
                        this.episodeProgressMap.set(h.episodeId, {
                            progressSeconds: progressSeconds,
                            watchedPercentage: h.watchedPercentage || 0,
                            completed: h.isCompleted || false
                        });

                        // Track last interaction
                        const activityDate = h.lastWatchedAt ? new Date(h.lastWatchedAt).getTime() : 0;
                        if (activityDate > lastActivity) {
                            lastActivity = activityDate;
                            this.lastInteractionEpisodeId = h.episodeId;
                        }
                    });
                } catch (error) {
                    console.error('Failed to fetch watch history:', error);
                    this.episodeProgressMap = new Map();
                    this.lastInteractionEpisodeId = null;
                }
            } else {
                this.episodeProgressMap = new Map();
                this.lastInteractionEpisodeId = null;
            }

            // Fetch season metadata for series (episodes loaded on demand)
            if (contentType === 'series' && isDiscovery) {
                try {
                    const episodesData = await apiClient.getSeriesEpisodes(contentId);
                    // Store season metadata, episodes will be loaded when season is selected
                    content.seasons = episodesData.seasons;
                    content.numberOfSeasons = episodesData.numberOfSeasons;
                    content.numberOfEpisodes = episodesData.numberOfEpisodes;
                    content.episodes = []; // Will be populated progressively
                    content.tmdbId = contentId; // Store for later episode fetching
                } catch (error) {
                    console.error('Failed to fetch season metadata:', error);
                    content.seasons = [];
                    content.episodes = [];
                }
            }

            this.currentContent = content;

            // Create modal
            this.createModal(content, isDiscovery);

            // Show modal with animation
            requestAnimationFrame(() => {
                this.modal.classList.add('visible');

                // Initialize TV navigation if available
                if (this.navigation && this.navigation.isAndroidTV) {
                    this.navigation.initializeModalNavigation();
                }
            });

            // Setup close handlers
            this.setupCloseHandlers();
        } catch (error) {
            console.error('Failed to load content details:', error);

            // Show detailed error message
            let errorMessage = 'Failed to load content details.';
            if (error.code === 'MISSING_TYPE_PARAMETER') {
                errorMessage = `Error: Content type is missing or invalid.\n\nDetails: ${error.details?.hint || 'Unknown error'}`;
            } else if (error.message) {
                errorMessage = `Error: ${error.message}`;
                if (error.details) {
                    errorMessage += `\n\nDetails: ${JSON.stringify(error.details, null, 2)}`;
                }
            }

            alert(errorMessage);
        }
    }

    /**
     * Create modal HTML
     */
    createModal(content, isDiscovery) {
        // Remove existing modal if any
        this.close();

        const modal = document.createElement('div');
        modal.className = 'content-modal';
        modal.id = 'content-modal';

        const backdropUrl = content.backdropUrl || content.posterUrl || '';
        const posterUrl = content.posterUrl || '';
        // Handle genres - they can be strings or objects with name property
        const genres = Array.isArray(content.genres)
            ? content.genres.map(g => typeof g === 'string' ? g : g.name || g.Name).filter(Boolean).join(', ')
            : '';
        const year = content.releaseDate ? new Date(content.releaseDate).getFullYear() : '';
        const rating = content.voteAverage ? `★ ${content.voteAverage.toFixed(1)}` : '';

        // Format runtime properly
        let runtime = '';
        if (content.runtime && content.runtime > 0) {
            const hours = Math.floor(content.runtime / 60);
            const minutes = content.runtime % 60;
            if (hours > 0) {
                runtime = `${hours}h ${minutes}m`;
            } else {
                runtime = `${minutes}m`;
            }
        }

        // For series, show episode list
        const episodes = content.episodes || [];
        const episodeCount = content.numberOfEpisodes || episodes.length;
        // Show episodes section if it's a series (even if episodes not loaded yet)
        const hasEpisodes = content.type === 'series' && (content.seasons?.length > 0 || episodes.length > 0);

        // Calculate resume info for series
        let resumeInfo = null;
        if (content.type === 'series' && !isDiscovery) {
            resumeInfo = this.getSeriesResumeInfo(content);
        }

        modal.innerHTML = `
      <div class="modal-ambilight"></div>
      <div class="modal-overlay"></div>
      
      <div class="modal-content">
        <button class="modal-close" aria-label="Close">
          <svg viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
        </button>

        <div class="modal-header">
          <div class="modal-poster">
            <img src="${posterUrl}" alt="${content.title}" />
          </div>
          <div class="modal-info">
            <h1 class="modal-title">${content.title}</h1>
            <div class="modal-meta">
              ${year ? `<span>${year}</span>` : ''}
              ${rating ? `<span>${rating}</span>` : ''}
              ${runtime ? `<span>${runtime}</span>` : ''}
              ${content.type === 'series' && episodeCount > 0 ? `<span>${episodeCount} Episodes</span>` : ''}
            </div>
            <div class="modal-genres">${genres}</div>
            <p class="modal-description">${content.overview || 'No description available.'}</p>
            
            <div class="modal-actions">
              ${isDiscovery ? `
                <button class="modal-btn primary" data-action="queue-all">
                  <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                  Download ${content.type === 'series' ? 'All Episodes' : 'Movie'}
                </button>
              ` : `
                <button class="modal-btn primary" data-action="play" ${resumeInfo && resumeInfo.targetEpisodeId ? `data-episode-id="${resumeInfo.targetEpisodeId}"` : ''}>
                  <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                  ${content.type === 'series'
                ? (resumeInfo ? resumeInfo.text : 'Play')
                : (content.watchProgress && !content.watchProgress.completed && content.watchProgress.progressSeconds > 30 ? 'Resume' : 'Play')}
                </button>
              `}
              <button class="modal-btn secondary" data-action="watchlist">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                My List
              </button>
              ${!isDiscovery ? `
                <button class="modal-btn secondary" data-action="watchparty" title="Start Watch Party">
                  <svg viewBox="0 0 24 24"><path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg>
                  Watch Party
                </button>
              ` : ''}
            </div>
          </div>
        </div>

        ${hasEpisodes ? `
          <div class="modal-episodes">
            <h2>Episodes</h2>
            <div class="episodes-list" id="episodes-list"></div>
          </div>
        ` : ''}
      </div>
    `;

        document.body.appendChild(modal);
        this.modal = modal;

        // Render episodes if available
        if (hasEpisodes) {
            this.renderEpisodes(episodes, isDiscovery);
        }

        // Setup action handlers
        this.setupActionHandlers(content, isDiscovery);

        // Apply ambilight effect
        this.applyAmbilightEffect(backdropUrl);
    }

    /**
     * Render episode list with seasons sidebar (progressive loading)
     */
    renderEpisodes(episodes, isDiscovery) {
        const episodesList = document.getElementById('episodes-list');
        if (!episodesList) return;

        // Use seasons from content metadata if available (for discovery content)
        const seasons = this.currentContent.seasons || [];
        const hasSeasonMetadata = seasons.length > 0;

        // If we have season metadata but no episodes yet, use that
        let seasonNumbers;
        if (hasSeasonMetadata) {
            seasonNumbers = seasons.map(s => s.seasonNumber.toString()).sort((a, b) => parseInt(a) - parseInt(b));
        } else {
            // Group existing episodes by season (for library content)
            const seasonMap = {};
            episodes.forEach(ep => {
                if (!seasonMap[ep.seasonNumber]) {
                    seasonMap[ep.seasonNumber] = [];
                }
                seasonMap[ep.seasonNumber].push(ep);
            });
            seasonNumbers = Object.keys(seasonMap).sort((a, b) => parseInt(a) - parseInt(b));
        }

        // Create layout with sidebar
        episodesList.innerHTML = `
      <div class="episodes-layout">
        <div class="seasons-sidebar">
          ${seasonNumbers.map((seasonNum, index) => {
            const season = hasSeasonMetadata ? seasons.find(s => s.seasonNumber.toString() === seasonNum) : null;
            const episodeCount = season ? season.episodeCount : (episodes.filter(e => e.seasonNumber.toString() === seasonNum).length);
            return `
            <button class="season-tab ${index === 0 ? 'active' : ''}" data-season="${seasonNum}">
              <div class="season-tab-title">Season ${seasonNum}</div>
              <div class="season-tab-count">${episodeCount} episodes</div>
            </button>
          `;
        }).join('')}
        </div>
        <div class="episodes-content">
          ${seasonNumbers.map((seasonNum, index) => `
            <div class="season-episodes ${index === 0 ? 'active' : ''}" data-season="${seasonNum}">
              <div class="season-header">
                <h3>Season ${seasonNum}</h3>
                <button class="season-download-btn" data-season="${seasonNum}" style="display: none;">
                  <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                  <span class="download-btn-text">${isDiscovery ? 'Download Season' : 'Download Missing'}</span>
                </button>
              </div>
              <div class="episodes-list-vertical" data-season="${seasonNum}">
                ${index === 0 ? '' : '<div class="loading-placeholder">Loading episodes...</div>'}
              </div>
            </div>
          `).join('')}
        </div>
      </div>
    `;

        // Load all seasons progressively starting with Season 1
        if (seasonNumbers.length > 0) {
            this.loadAllSeasonsProgressively(seasonNumbers, isDiscovery);
        }

        // Setup season tab switching with progressive loading
        const seasonTabs = episodesList.querySelectorAll('.season-tab');
        seasonTabs.forEach(tab => {
            tab.addEventListener('click', async () => {
                const seasonNum = tab.dataset.season;

                // Update active tab
                seasonTabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');

                // Update active season content
                const seasonContents = episodesList.querySelectorAll('.season-episodes');
                seasonContents.forEach(content => {
                    content.classList.remove('active');
                    if (content.dataset.season === seasonNum) {
                        content.classList.add('active');
                    }
                });

                // Load episodes for this season if not already loaded
                await this.loadSeasonEpisodes(seasonNum, isDiscovery);
            });
        });
    }

    /**
     * Load all seasons progressively (one at a time to avoid rate limits)
     * TMDB rate limit: 40 requests per 10 seconds
     */
    async loadAllSeasonsProgressively(seasonNumbers, isDiscovery) {
        // For library content, load all at once since episodes are already available
        if (!isDiscovery) {
            for (const seasonNum of seasonNumbers) {
                try {
                    await this.loadSeasonEpisodes(seasonNum, isDiscovery);
                } catch (error) {
                    console.error(`Failed to load season ${seasonNum}:`, error);
                }
            }
            return;
        }

        // For discovery content, load only the first season initially
        // Other seasons will be loaded on-demand when user clicks the tab
        if (seasonNumbers.length > 0) {
            try {
                await this.loadSeasonEpisodes(seasonNumbers[0], isDiscovery);
            } catch (error) {
                console.error(`Failed to load season ${seasonNumbers[0]}:`, error);
            }
        }
    }

    /**
     * Load episodes for a specific season
     */
    async loadSeasonEpisodes(seasonNum, isDiscovery) {
        const episodesContainer = document.querySelector(`.episodes-list-vertical[data-season="${seasonNum}"]`);
        if (!episodesContainer) return;

        // Check if already loaded
        if (episodesContainer.querySelector('.episode-card-horizontal')) {
            return; // Already loaded
        }

        // Show loading state
        episodesContainer.innerHTML = '<div class="loading-placeholder">Loading episodes...</div>';

        try {
            let seasonEpisodes = [];

            // For discovery content, fetch from API
            if (isDiscovery && this.currentContent.tmdbId) {
                const seasonData = await apiClient.getSeasonEpisodes(this.currentContent.tmdbId, parseInt(seasonNum));
                // API returns TmdbSeasonDetails directly with Episodes property
                seasonEpisodes = seasonData.episodes || seasonData.Episodes || [];
            } else {
                // For library content, use existing episodes
                const seasonNumInt = parseInt(seasonNum);
                seasonEpisodes = (this.currentContent.episodes || []).filter(ep =>
                    parseInt(ep.seasonNumber) === seasonNumInt
                );
            }

            // Clear loading state
            episodesContainer.innerHTML = '';

            // Render episodes
            seasonEpisodes.forEach(episode => {
                const episodeCard = this.createEpisodeCard(episode, isDiscovery);
                episodesContainer.appendChild(episodeCard);
            });

            // Check if there are any unavailable episodes (for library content)
            const hasUnavailableEpisodes = !isDiscovery && seasonEpisodes.some(ep => !ep.available);

            // Show/hide download button based on content type and availability
            const seasonDownloadBtn = document.querySelector(`.season-download-btn[data-season="${seasonNum}"]`);
            if (seasonDownloadBtn) {
                if (isDiscovery || hasUnavailableEpisodes) {
                    seasonDownloadBtn.style.display = 'flex';
                    seasonDownloadBtn.addEventListener('click', () => {
                        this.downloadSeason(seasonNum);
                    });
                } else {
                    seasonDownloadBtn.style.display = 'none';
                }
            }
        } catch (error) {
            console.error(`Failed to load season ${seasonNum}:`, error);
            episodesContainer.innerHTML = '<div class="error-placeholder">Failed to load episodes</div>';
        }
    }

    /**
     * Create episode card element
     */
    createEpisodeCard(episode, isDiscovery) {
        const episodeCard = document.createElement('div');
        const isAvailable = episode.available !== false; // Default to true for discovery content
        const isLibraryContent = !isDiscovery;

        episodeCard.className = `episode-card-horizontal ${!isAvailable && isLibraryContent ? 'unavailable' : ''}`;
        episodeCard.dataset.episodeId = episode.id;
        episodeCard.dataset.seasonNumber = episode.seasonNumber;
        episodeCard.dataset.episodeNumber = episode.episodeNumber;

        // Use still URL - backend provides full URL
        const stillUrl = episode.stillUrl || episode.StillUrl || this.currentContent.backdropUrl || '';

        const watched = episode.watched || false;
        const runtime = episode.runtime ? `${episode.runtime}m` : '';

        // Get watch progress for this episode
        const progress = this.episodeProgressMap?.get(episode.id);
        const hasProgress = progress && progress.progressSeconds > 0 && !progress.completed;
        const progressPercent = progress?.watchedPercentage || 0;

        episodeCard.innerHTML = `
          <div class="episode-thumbnail-horizontal">
            <img src="${stillUrl}" alt="Episode ${episode.episodeNumber}" />
            ${progress?.completed ? '<div class="watched-badge">✓</div>' : ''}
            ${!isAvailable && isLibraryContent ? '<div class="unavailable-badge">Not Downloaded</div>' : ''}
            ${isAvailable && isLibraryContent ? `
              <button class="episode-play-btn">
                <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
              </button>
            ` : ''}
            ${hasProgress ? `
              <div class="episode-progress-bar">
                <div class="episode-progress-fill" style="width: ${Math.min(progressPercent, 100)}%"></div>
              </div>
            ` : ''}
          </div>
          <div class="episode-info-horizontal">
            <div class="episode-header-row">
              <div class="episode-number-title">
                <span class="episode-number">${episode.episodeNumber}.</span>
                <span class="episode-title">${episode.title || episode.name || episode.Name || `Episode ${episode.episodeNumber}`}</span>
              </div>
              ${runtime ? `<span class="episode-runtime">${runtime}</span>` : ''}
            </div>
            <div class="episode-overview">${episode.overview || 'No description available.'}</div>
            ${isDiscovery || (!isAvailable && isLibraryContent) ? `
              <button class="episode-download-btn" data-episode-id="${episode.id}">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                Download Episode
              </button>
            ` : ''}
          </div>
        `;

        // Add play handler for available library content
        if (isAvailable && isLibraryContent) {
            const playBtn = episodeCard.querySelector('.episode-play-btn');
            playBtn?.addEventListener('click', (e) => {
                e.stopPropagation();
                this.playEpisode(episode.id);
            });
        }

        // Add download handler for discovery content or unavailable library episodes
        if (isDiscovery || (!isAvailable && isLibraryContent)) {
            const downloadBtn = episodeCard.querySelector('.episode-download-btn');
            downloadBtn?.addEventListener('click', (e) => {
                e.stopPropagation();
                this.downloadEpisode(episode.seasonNumber, episode.episodeNumber);
            });
        }

        return episodeCard;
    }

    /**
     * Setup action button handlers
     */
    setupActionHandlers(content, isDiscovery) {
        const modal = this.modal;

        // Play button
        modal.querySelector('[data-action="play"]')?.addEventListener('click', (e) => {
            if (content.type === 'series') {
                // Check if we have a specific target episode (set during render)
                const targetEpisodeId = e.currentTarget.dataset.episodeId;

                if (targetEpisodeId) {
                    this.playEpisode(parseInt(targetEpisodeId));
                    return;
                }

                // Fallback: Find the first available episode to play
                const episodes = content.episodes || [];
                const firstAvailableEpisode = episodes.find(ep => ep.available);

                if (firstAvailableEpisode) {
                    this.playEpisode(firstAvailableEpisode.id);
                } else {
                    alert('No episodes available to play. Please download episodes first.');
                }
            } else {
                // For movies, check for watch progress and resume if available
                const startTime = content.watchProgress && !content.watchProgress.completed && content.watchProgress.progressSeconds > 30
                    ? content.watchProgress.progressSeconds
                    : 0;

                const url = `player.html?contentId=${content.id}&type=${content.type}${startTime > 0 ? `&startTime=${startTime}` : ''}`;
                window.location.href = url;
            }
        });

        // Hide download options for Guest / restricted profiles
        const selectedProfileId = this.profileManager?.selectedProfileId;
        const currentProfile = this.profileManager?.profiles?.find(p => p.id === selectedProfileId);
        if (currentProfile && (currentProfile.isGuest || !currentProfile.canDownload)) {
            modal.querySelectorAll('[data-action="queue-all"], .season-download-btn, .episode-download-btn').forEach(el => el.style.display = 'none');
        }

        // Queue/Download all button
        modal.querySelector('[data-action="queue-all"]')?.addEventListener('click', async () => {
            await this.queueDownload(content);
        });

        // Watchlist button
        modal.querySelector('[data-action="watchlist"]')?.addEventListener('click', async () => {
            await this.toggleWatchlist(content.id);
        });

        // Watch Party button
        modal.querySelector('[data-action="watchparty"]')?.addEventListener('click', () => {
            let url = `player.html?contentId=${content.id}&type=${content.type}&createSync=true`;
            if (content.type === 'series') {
                const targetEpId = modal.querySelector('[data-action="play"]')?.dataset.episodeId;
                if (targetEpId) {
                    url += `&episodeId=${targetEpId}`;
                }
            }
            window.location.href = url;
        });
    }

    /**
     * Queue download for content
     */
    async queueDownload(content) {
        try {
            const profileId = this.profileManager?.selectedProfileId;
            const currentProfile = this.profileManager?.profiles?.find(p => p.id === profileId);
            if (currentProfile && (currentProfile.isGuest || !currentProfile.canDownload)) {
                alert('Guest and restricted profiles are not permitted to request or download media.');
                return;
            }

            if (!profileId) {
                alert('Please select a profile first');
                return;
            }

            const btn = this.modal.querySelector('[data-action="queue-all"]');
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<svg viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/></svg> Added to Queue';
            }

            await apiClient.queueDownload(
                content.tmdbId || content.id,
                profileId,
                content.type,
                content.title,
                content.releaseDate ? new Date(content.releaseDate).getFullYear() : null
            );

            setTimeout(() => {
                if (btn) {
                    btn.innerHTML = '<svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg> Download ' + (content.type === 'series' ? 'All Episodes' : 'Movie');
                    btn.disabled = false;
                }
            }, 2000);
        } catch (error) {
            console.error('Failed to queue download:', error);
            alert('Failed to add to download queue.');
        }
    }

    /**
     * Download specific episode
     */
    async downloadEpisode(seasonNumber, episodeNumber) {
        try {
            const profileId = this.profileManager.selectedProfileId;
            if (!profileId) {
                alert('Please select a profile first');
                return;
            }

            const content = this.currentContent;

            // Queue the episode download
            await apiClient.queueEpisodeDownload(
                content.tmdbId || content.id,
                profileId,
                content.title,
                seasonNumber,
                episodeNumber,
                content.releaseDate ? new Date(content.releaseDate).getFullYear() : null
            );

            alert(`"${content.title}" S${seasonNumber}E${episodeNumber} has been added to your download queue!`);
        } catch (error) {
            console.error('Failed to queue episode download:', error);
            alert('Failed to add episode to download queue.');
        }
    }

    /**
     * Download entire season
     */
    async downloadSeason(seasonNumber) {
        try {
            const profileId = this.profileManager.selectedProfileId;
            if (!profileId) {
                alert('Please select a profile first');
                return;
            }

            const content = this.currentContent;

            // Queue the season download
            await apiClient.queueSeasonDownload(
                content.tmdbId || content.id,
                profileId,
                content.title,
                seasonNumber,
                content.releaseDate ? new Date(content.releaseDate).getFullYear() : null
            );

            alert(`"${content.title}" Season ${seasonNumber} has been added to your download queue!`);
        } catch (error) {
            console.error('Failed to queue season download:', error);
            alert('Failed to add season to download queue.');
        }
    }

    /**
     * Play episode
     */
    playEpisode(episodeId) {
        const contentId = this.currentContent.id;
        window.location.href = `player.html?contentId=${contentId}&type=series&episodeId=${episodeId}`;
    }

    /**
     * Toggle watchlist
     */
    async toggleWatchlist(contentId) {
        try {
            const profileId = this.profileManager.selectedProfileId;
            if (!profileId) {
                alert('Please select a profile first');
                return;
            }

            // Check if already in watchlist
            const watchlist = await apiClient.getWatchlist(profileId);
            const isInWatchlist = watchlist.items?.some(item => item.contentId === contentId);

            if (isInWatchlist) {
                await apiClient.removeFromWatchlist(profileId, contentId);
                alert('Removed from My List');
            } else {
                await apiClient.addToWatchlist(profileId, contentId);
                alert('Added to My List');
            }
        } catch (error) {
            console.error('Failed to toggle watchlist:', error);
            alert('Failed to update My List.');
        }
    }

    /**
     * Apply ambilight effect to backdrop
     */
    applyAmbilightEffect(imageUrl) {
        const ambilight = this.modal.querySelector('.modal-ambilight');
        if (ambilight && imageUrl) {
            ambilight.style.backgroundImage = `url('${imageUrl}')`;
        }
    }

    /**
     * Setup close handlers
     */
    setupCloseHandlers() {
        const closeBtn = this.modal.querySelector('.modal-close');
        const overlay = this.modal.querySelector('.modal-overlay');

        closeBtn?.addEventListener('click', () => this.close());
        overlay?.addEventListener('click', () => this.close());

        // ESC key
        const escHandler = (e) => {
            if (e.key === 'Escape') {
                this.close();
                document.removeEventListener('keydown', escHandler);
            }
        };
        document.addEventListener('keydown', escHandler);
    }

    /**
     * Get resume info for series (button text and target episode)
     */
    getSeriesResumeInfo(content) {
        const episodes = content.episodes || [];
        if (episodes.length === 0) return null;

        // Sort episodes to ensure order
        const sortedEpisodes = [...episodes].sort((a, b) => {
            if (a.seasonNumber !== b.seasonNumber) return a.seasonNumber - b.seasonNumber;
            return a.episodeNumber - b.episodeNumber;
        });

        // If we have history interaction
        if (this.lastInteractionEpisodeId) {
            const lastEpisode = sortedEpisodes.find(e => e.id === this.lastInteractionEpisodeId);

            if (lastEpisode) {
                const progress = this.episodeProgressMap.get(lastEpisode.id);

                // If in progress (not completed), resume it
                if (progress && !progress.completed) {
                    return {
                        text: `Resume S${lastEpisode.seasonNumber}:E${lastEpisode.episodeNumber}`,
                        targetEpisodeId: lastEpisode.id
                    };
                }

                // If completed, find next episode
                const lastIndex = sortedEpisodes.findIndex(e => e.id === lastEpisode.id);
                if (lastIndex !== -1 && lastIndex < sortedEpisodes.length - 1) {
                    const nextEpisode = sortedEpisodes[lastIndex + 1];
                    return {
                        text: `Play S${nextEpisode.seasonNumber}:E${nextEpisode.episodeNumber}`,
                        targetEpisodeId: nextEpisode.id
                    };
                }

                // If no next episode (finished series), maybe restart? or just show Play S1:E1
                const firstEp = sortedEpisodes[0];
                return {
                    text: 'Play Again',
                    targetEpisodeId: firstEp.id
                };
            }
        }

        // Default: Play first episode
        const firstEp = sortedEpisodes[0];
        return {
            text: `Play S${firstEp.seasonNumber}:E${firstEp.episodeNumber}`,
            targetEpisodeId: firstEp.id
        };
    }

    /**
     * Close modal
     */
    close() {
        if (this.modal) {
            this.modal.classList.remove('visible');
            setTimeout(() => {
                this.modal.remove();
                this.modal = null;
                this.currentContent = null;
            }, 300);
        }
    }
}

export default ContentModal;
