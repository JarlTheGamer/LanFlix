/**
 * Content Modal Module
 * Displays detailed content information in a modal with ambilight background
 */

import apiClient from './api-client.js';
import stateManager from './data.js';

export class ContentModal {
    constructor(profileManager) {
        this.profileManager = profileManager;
        this.modal = null;
        this.currentContent = null;
    }

    /**
     * Show modal with content details
     */
    async show(contentId, contentType, isDiscovery = false) {
        try {
            // Fetch content details
            const profileId = this.profileManager.selectedProfileId;
            const content = isDiscovery
                ? await apiClient.getContentDetails(contentId, contentType, profileId)
                : await apiClient.getLibraryItem(contentId, profileId);

            this.currentContent = content;

            // Create modal
            this.createModal(content, isDiscovery);

            // Show modal with animation
            requestAnimationFrame(() => {
                this.modal.classList.add('visible');
            });

            // Setup close handlers
            this.setupCloseHandlers();
        } catch (error) {
            console.error('Failed to load content details:', error);
            alert('Failed to load content details.');
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
        const genres = Array.isArray(content.genres) ? content.genres.join(', ') : '';
        const year = content.releaseDate ? new Date(content.releaseDate).getFullYear() : '';
        const rating = content.voteAverage ? `★ ${content.voteAverage.toFixed(1)}` : '';
        const runtime = content.runtime ? `${Math.floor(content.runtime / 60)}h ${content.runtime % 60}m` : '';

        // For series, show episode list
        const episodes = content.episodes || [];
        const hasEpisodes = content.type === 'series' && episodes.length > 0;

        modal.innerHTML = `
      <div class="modal-backdrop" style="background-image: url(${backdropUrl})"></div>
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
              ${content.type === 'series' ? `<span>${episodes.length} Episodes</span>` : ''}
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
                <button class="modal-btn primary" data-action="play">
                  <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                  Play
                </button>
              `}
              <button class="modal-btn secondary" data-action="watchlist">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                My List
              </button>
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
     * Render episode list
     */
    renderEpisodes(episodes, isDiscovery) {
        const episodesList = document.getElementById('episodes-list');
        if (!episodesList) return;

        // Group by season
        const seasons = {};
        episodes.forEach(ep => {
            if (!seasons[ep.seasonNumber]) {
                seasons[ep.seasonNumber] = [];
            }
            seasons[ep.seasonNumber].push(ep);
        });

        // Render each season
        Object.keys(seasons).sort((a, b) => a - b).forEach(seasonNum => {
            const seasonEpisodes = seasons[seasonNum];

            const seasonSection = document.createElement('div');
            seasonSection.className = 'season-section';
            seasonSection.innerHTML = `
        <div class="season-header">
          <h3>Season ${seasonNum}</h3>
          ${isDiscovery ? `
            <button class="season-download-btn" data-season="${seasonNum}">
              <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
              Download Season
            </button>
          ` : ''}
        </div>
        <div class="episodes-grid"></div>
      `;

            const episodesGrid = seasonSection.querySelector('.episodes-grid');

            seasonEpisodes.forEach(episode => {
                const episodeCard = document.createElement('div');
                episodeCard.className = 'episode-card';
                episodeCard.dataset.episodeId = episode.id;
                episodeCard.dataset.seasonNumber = episode.seasonNumber;
                episodeCard.dataset.episodeNumber = episode.episodeNumber;

                const stillUrl = episode.stillPath || this.currentContent.backdropUrl || '';
                const watched = episode.watched || false;

                episodeCard.innerHTML = `
          <div class="episode-thumbnail">
            <img src="${stillUrl}" alt="Episode ${episode.episodeNumber}" />
            ${watched ? '<div class="watched-badge">✓</div>' : ''}
            ${!isDiscovery ? `
              <button class="episode-play-btn">
                <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
              </button>
            ` : ''}
          </div>
          <div class="episode-info">
            <div class="episode-number">Episode ${episode.episodeNumber}</div>
            <div class="episode-title">${episode.title || `Episode ${episode.episodeNumber}`}</div>
            <div class="episode-overview">${episode.overview || 'No description available.'}</div>
            ${isDiscovery ? `
              <button class="episode-download-btn" data-episode-id="${episode.id}">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                Download
              </button>
            ` : ''}
          </div>
        `;

                episodesGrid.appendChild(episodeCard);

                // Add play handler for library content
                if (!isDiscovery) {
                    const playBtn = episodeCard.querySelector('.episode-play-btn');
                    playBtn?.addEventListener('click', (e) => {
                        e.stopPropagation();
                        this.playEpisode(episode.id);
                    });
                }

                // Add download handler for discovery content
                if (isDiscovery) {
                    const downloadBtn = episodeCard.querySelector('.episode-download-btn');
                    downloadBtn?.addEventListener('click', (e) => {
                        e.stopPropagation();
                        this.downloadEpisode(episode.seasonNumber, episode.episodeNumber);
                    });
                }
            });

            episodesList.appendChild(seasonSection);

            // Add season download handler
            if (isDiscovery) {
                const seasonDownloadBtn = seasonSection.querySelector('.season-download-btn');
                seasonDownloadBtn?.addEventListener('click', () => {
                    this.downloadSeason(seasonNum);
                });
            }
        });
    }

    /**
     * Setup action button handlers
     */
    setupActionHandlers(content, isDiscovery) {
        const modal = this.modal;

        // Play button
        modal.querySelector('[data-action="play"]')?.addEventListener('click', () => {
            window.location.href = `player.html?contentId=${content.id}&type=${content.type}`;
        });

        // Queue/Download all button
        modal.querySelector('[data-action="queue-all"]')?.addEventListener('click', async () => {
            await this.queueDownload(content);
        });

        // Watchlist button
        modal.querySelector('[data-action="watchlist"]')?.addEventListener('click', async () => {
            await this.toggleWatchlist(content.id);
        });
    }

    /**
     * Queue download for content
     */
    async queueDownload(content) {
        try {
            const profileId = this.profileManager.selectedProfileId;
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
        console.log(`Download episode S${seasonNumber}E${episodeNumber}`);
        // This would trigger a specific episode download
        alert(`Downloading Season ${seasonNumber} Episode ${episodeNumber}`);
    }

    /**
     * Download entire season
     */
    async downloadSeason(seasonNumber) {
        console.log(`Download season ${seasonNumber}`);
        alert(`Downloading Season ${seasonNumber}`);
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
            ambilight.style.backgroundImage = `url(${imageUrl})`;
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
