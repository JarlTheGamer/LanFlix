import apiClient from '../modules/api-client.js';

// Load current settings
async function loadSettings() {
  try {
    const response = await apiClient.getSettings();
    console.log('Loaded settings:', response);

    // The response is the settings object directly (not nested in a settings property)
    const settings = response;

    // Populate form fields from nested structure
    const moviesPath = settings.mediaPaths?.movies || '';
    const seriesPath = settings.mediaPaths?.series || '';
    const tmdbKey = settings.externalApis?.tmdb?.apiKey || '';
    const sonarrUrl = settings.externalApis?.sonarr?.url || '';
    const sonarrKey = settings.externalApis?.sonarr?.apiKey || '';
    const radarrUrl = settings.externalApis?.radarr?.url || '';
    const radarrKey = settings.externalApis?.radarr?.apiKey || '';
    const prowlarrUrl = settings.externalApis?.prowlarr?.url || '';
    const prowlarrKey = settings.externalApis?.prowlarr?.apiKey || '';
    const bazarrUrl = settings.externalApis?.subtitles?.bazarr?.url || '';
    const bazarrKey = settings.externalApis?.subtitles?.bazarr?.apiKey || '';
    const subtitleLanguage = settings.externalApis?.subtitles?.preferredLanguage || 'eng';
    const autoDownloadSubtitles = settings.externalApis?.subtitles?.autoDownload !== false;

    console.log('Setting form values:', {
      moviesPath,
      seriesPath,
      tmdbKey: tmdbKey ? '***' : '(empty)',
      sonarrUrl,
      sonarrKey: sonarrKey ? '***' : '(empty)',
      radarrUrl,
      radarrKey: radarrKey ? '***' : '(empty)',
      prowlarrUrl,
      prowlarrKey: prowlarrKey ? '***' : '(empty)',
      bazarrUrl,
      bazarrKey: bazarrKey ? '***' : '(empty)',
      subtitleLanguage,
      autoDownloadSubtitles
    });

    document.getElementById('movies-path').value = moviesPath;
    document.getElementById('series-path').value = seriesPath;
    document.getElementById('tmdb-key').value = tmdbKey;

    // External services
    document.getElementById('sonarr-url').value = sonarrUrl;
    document.getElementById('sonarr-key').value = sonarrKey;
    document.getElementById('radarr-url').value = radarrUrl;
    document.getElementById('radarr-key').value = radarrKey;
    document.getElementById('prowlarr-url').value = prowlarrUrl;
    document.getElementById('prowlarr-key').value = prowlarrKey;
    document.getElementById('bazarr-url').value = bazarrUrl;
    document.getElementById('bazarr-key').value = bazarrKey;

    // Subtitle settings
    document.getElementById('subtitle-language').value = subtitleLanguage;
    document.getElementById('auto-download-subtitles').checked = autoDownloadSubtitles;

    // Metadata settings (not stored in backend yet)
    document.getElementById('auto-metadata').checked = true;
    document.getElementById('download-images').checked = true;
    document.getElementById('metadata-language').value = 'en';

    // Load root folders after settings are loaded
    await loadRootFolders();

    console.log('Settings loaded successfully');
  } catch (error) {
    console.error('Failed to load settings:', error);
    showStatus('Failed to load current settings', 'error');
  }
}

// Load root folders from Radarr and Sonarr
async function loadRootFolders() {
  // Load Radarr root folders
  try {
    const radarrFolders = await apiClient.getRadarrRootFolders();
    const moviesSelect = document.getElementById('movies-path-select');
    moviesSelect.innerHTML = '<option value="">Select from Radarr folders...</option>';
    
    radarrFolders.forEach(folder => {
      const option = document.createElement('option');
      option.value = folder.path;
      option.textContent = `${folder.path} (${folder.freeSpace || 'Unknown'} free)`;
      moviesSelect.appendChild(option);
    });
  } catch (error) {
    console.error('Failed to load Radarr root folders:', error);
    const moviesSelect = document.getElementById('movies-path-select');
    moviesSelect.innerHTML = '<option value="">Failed to load Radarr folders</option>';
  }

  // Load Sonarr root folders
  try {
    const sonarrFolders = await apiClient.getSonarrRootFolders();
    const seriesSelect = document.getElementById('series-path-select');
    seriesSelect.innerHTML = '<option value="">Select from Sonarr folders...</option>';
    
    sonarrFolders.forEach(folder => {
      const option = document.createElement('option');
      option.value = folder.path;
      option.textContent = `${folder.path} (${folder.freeSpace || 'Unknown'} free)`;
      seriesSelect.appendChild(option);
    });
  } catch (error) {
    console.error('Failed to load Sonarr root folders:', error);
    const seriesSelect = document.getElementById('series-path-select');
    seriesSelect.innerHTML = '<option value="">Failed to load Sonarr folders</option>';
  }
}

// Update movies path when dropdown selection changes
window.updateMoviesPath = function() {
  const select = document.getElementById('movies-path-select');
  const input = document.getElementById('movies-path');
  if (select.value) {
    input.value = select.value;
  }
};

// Update series path when dropdown selection changes
window.updateSeriesPath = function() {
  const select = document.getElementById('series-path-select');
  const input = document.getElementById('series-path');
  if (select.value) {
    input.value = select.value;
  }
};

// Save settings
async function saveSettings() {
  const saveBtn = document.getElementById('save-btn');
  saveBtn.textContent = '⏳ Saving...';
  saveBtn.disabled = true;

  try {
    // Build settings object with proper nested structure matching ServerSettingsDto
    const settings = {
      mediaPaths: {
        movies: document.getElementById('movies-path').value || '',
        series: document.getElementById('series-path').value || '',
        posterCache: '',
        backdropCache: ''
      },
      transcoding: {
        enableHardwareAcceleration: true,
        preferredHwAccel: 'auto',
        maxConcurrentTranscodes: 2,
        tempPath: '',
        defaultBitrate: 8000000,
        hlsSegmentDuration: 6
      },
      streaming: {
        enableDirectPlay: true,
        enableDirectStream: true,
        chunkSize: 81920
      },
      cache: {
        redis: {
          enabled: false,
          connectionString: '',
          instanceName: 'lanflix:'
        },
        memory: {
          sizeLimit: 512
        }
      },
      externalApis: {
        tmdb: {
          apiKey: document.getElementById('tmdb-key').value || '',
          baseUrl: 'https://api.themoviedb.org/3/'
        },
        sonarr: {
          url: document.getElementById('sonarr-url').value || '',
          apiKey: document.getElementById('sonarr-key').value || ''
        },
        radarr: {
          url: document.getElementById('radarr-url').value || '',
          apiKey: document.getElementById('radarr-key').value || ''
        },
        prowlarr: {
          url: document.getElementById('prowlarr-url').value || '',
          apiKey: document.getElementById('prowlarr-key').value || ''
        },
        subtitles: {
          preferredLanguage: document.getElementById('subtitle-language').value || 'eng',
          autoDownload: document.getElementById('auto-download-subtitles').checked,
          bazarr: {
            url: document.getElementById('bazarr-url').value || '',
            apiKey: document.getElementById('bazarr-key').value || ''
          }
        }
      }
    };

    console.log('Saving settings:', settings);
    await apiClient.updateSettings(settings);
    showStatus('✅ Configuration saved successfully!', 'success');
    saveBtn.textContent = '💾 Save Configuration';
    saveBtn.disabled = false;
  } catch (error) {
    console.error('Failed to save settings:', error);
    showStatus('❌ Failed to save configuration: ' + error.message, 'error');
    saveBtn.textContent = '💾 Save Configuration';
    saveBtn.disabled = false;
  }
}

// Test service connection
async function testConnection(service) {
  const statusEl = document.getElementById('status');
  statusEl.textContent = `Testing ${service} connection...`;
  statusEl.className = '';
  statusEl.style.display = 'block';

  try {
    const response = await apiClient.testServiceConnection(service);
    if (response.connected) {
      showStatus(`✅ ${service} connected successfully!`, 'success');
    } else {
      showStatus(`❌ ${service} connection failed: ${response.error || 'Unknown error'}`, 'error');
    }
  } catch (error) {
    showStatus(`❌ ${service} connection failed: ${error.message}`, 'error');
  }
}

// Toggle password visibility
window.togglePassword = function (fieldId) {
  const field = document.getElementById(fieldId);
  field.type = field.type === 'password' ? 'text' : 'password';
};

// Show status message
function showStatus(message, type) {
  const statusEl = document.getElementById('status');
  statusEl.textContent = message;
  statusEl.className = type;
  statusEl.style.display = 'block';

  if (type === 'success') {
    setTimeout(() => {
      statusEl.style.display = 'none';
    }, 5000);
  }
}

// Scan library
async function scanLibrary() {
  const scanBtn = document.getElementById('scan-library-btn');
  const scanStatus = document.getElementById('scan-status');

  scanBtn.textContent = '⏳ Scanning...';
  scanBtn.disabled = true;
  scanStatus.textContent = 'Scanning media folders for new content...';
  scanStatus.className = '';
  scanStatus.style.color = '#666';

  try {
    const response = await apiClient.request('/jobs/library-scan/trigger', {
      method: 'POST'
    });

    if (response.result) {
      const { added, updated, removed, errors } = response.result;
      let statusText = `✅ Library scan completed! Added: ${added}, Updated: ${updated}, Removed: ${removed}`;
      
      if (errors && errors.length > 0) {
        statusText += ` (${errors.length} errors - check console)`;
        console.warn('Library scan errors:', errors);
      }
      
      scanStatus.textContent = statusText;
    } else {
      scanStatus.textContent = '✅ Library scan completed! Check the logs for details.';
    }
    
    scanStatus.style.color = '#4caf50';
    scanBtn.textContent = '🔍 Scan Library Now';
    scanBtn.disabled = false;
  } catch (error) {
    console.error('Failed to scan library:', error);
    scanStatus.textContent = '❌ Failed to scan library: ' + (error.message || 'Unknown error');
    scanStatus.style.color = '#f44336';
    scanBtn.textContent = '🔍 Scan Library Now';
    scanBtn.disabled = false;
  }
}

// Make functions available globally
window.testConnection = testConnection;
window.scanLibrary = scanLibrary;

// Tab switching
function switchTab(tabName) {
  // Update tab buttons
  document.querySelectorAll('.admin-tab').forEach(tab => {
    tab.classList.remove('active');
  });
  document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');

  // Update tab content
  document.querySelectorAll('.admin-tab-content').forEach(content => {
    content.classList.remove('active');
  });
  document.getElementById(`${tabName}-tab`).classList.add('active');

  // Load media if switching to media tab
  if (tabName === 'media') {
    loadMediaLibrary();
  }
}

// Load media library
async function loadMediaLibrary() {
  await Promise.all([loadMovies(), loadSeries()]);
}

// Load movies
async function loadMovies() {
  const moviesList = document.getElementById('movies-list');
  if (!moviesList) {
    console.error('movies-list element not found');
    return;
  }

  moviesList.innerHTML = '<div class="loading">Loading movies...</div>';

  try {
    console.log('Fetching movies from API...');
    const response = await apiClient.getLibraryMovies();
    console.log('Movies response:', response);
    const movies = response.items || response.content || [];

    if (movies.length === 0) {
      moviesList.innerHTML = '<div class="loading">No movies found in library. Add some movies to your library first!</div>';
      return;
    }

    moviesList.innerHTML = movies.map(movie => `
      <div class="media-item" data-id="${movie.id}">
        <img src="${movie.posterUrl || '/placeholder.jpg'}" alt="${movie.title}" class="media-poster" onerror="this.src='/placeholder.jpg'" />
        <div class="media-info">
          <div class="media-title">${movie.title} ${movie.year ? `(${movie.year})` : ''}</div>
          <div class="media-meta">
            ${movie.runtime ? `${movie.runtime} min` : ''} 
            ${movie.genres ? `• ${movie.genres.slice(0, 2).join(', ')}` : ''}
          </div>
          <div class="media-path" title="${movie.filePath || 'No file path'}">${movie.filePath || 'No file path'}</div>
        </div>
        <div class="media-actions">
          <button class="media-btn transcode" onclick="showTranscodeModal(${movie.id}, 'movie', '${movie.title}')">🎬 Transcode</button>
          <button class="media-btn edit" onclick="showEditModal(${movie.id}, 'movie')">✏️ Edit</button>
          <button class="media-btn delete" onclick="deleteMedia(${movie.id}, 'movie', '${movie.title}')">🗑️ Delete</button>
        </div>
      </div>
    `).join('');
  } catch (error) {
    console.error('Failed to load movies:', error);
    moviesList.innerHTML = '<div class="loading">Failed to load movies</div>';
  }
}

// Load series
async function loadSeries() {
  const seriesList = document.getElementById('series-list');
  if (!seriesList) {
    console.error('series-list element not found');
    return;
  }

  seriesList.innerHTML = '<div class="loading">Loading series...</div>';

  try {
    console.log('Fetching series from API...');
    const response = await apiClient.getLibrarySeries();
    console.log('Series response:', response);
    const series = response.items || response.content || [];

    if (series.length === 0) {
      seriesList.innerHTML = '<div class="loading">No series found in library. Add some series to your library first!</div>';
      return;
    }

    seriesList.innerHTML = series.map(show => `
      <div class="media-item series-item" data-id="${show.id}">
        <img src="${show.posterUrl || '/placeholder.jpg'}" alt="${show.title}" class="media-poster" onerror="this.src='/placeholder.jpg'" />
        <div class="media-info">
          <div class="media-title">${show.title} ${show.year ? `(${show.year})` : ''}</div>
          <div class="media-meta">
            ${show.numberOfSeasons ? `${show.numberOfSeasons} seasons` : ''} 
            ${show.genres ? `• ${show.genres.slice(0, 2).join(', ')}` : ''}
          </div>
          <div class="media-path" title="${show.folderPath || 'No folder path'}">${show.folderPath || 'No folder path'}</div>
        </div>
        <div class="media-actions">
          <button class="media-btn" onclick="toggleEpisodes(${show.id}, '${show.title}')">📺 Episodes</button>
          <button class="media-btn edit" onclick="showEditModal(${show.id}, 'series')">✏️ Edit</button>
          <button class="media-btn delete" onclick="deleteMedia(${show.id}, 'series', '${show.title}')">🗑️ Delete</button>
        </div>
      </div>
      <div class="episodes-container" id="episodes-${show.id}" style="display: none;"></div>
    `).join('');
  } catch (error) {
    console.error('Failed to load series:', error);
    seriesList.innerHTML = '<div class="loading">Failed to load series</div>';
  }
}

// Toggle episodes view
window.toggleEpisodes = async function (seriesId, seriesTitle) {
  const episodesContainer = document.getElementById(`episodes-${seriesId}`);

  if (episodesContainer.style.display === 'none') {
    // Load and show episodes
    episodesContainer.innerHTML = '<div class="loading" style="padding: 20px;">Loading episodes...</div>';
    episodesContainer.style.display = 'block';

    try {
      // Get series details which includes episodes
      const series = await apiClient.getLibraryItem(seriesId);
      const episodes = series.episodes || [];

      if (episodes.length === 0) {
        episodesContainer.innerHTML = '<div class="loading" style="padding: 20px;">No episodes found</div>';
        return;
      }

      // Group episodes by season
      const seasons = {};
      episodes.forEach(ep => {
        if (!seasons[ep.seasonNumber]) {
          seasons[ep.seasonNumber] = [];
        }
        seasons[ep.seasonNumber].push(ep);
      });

      // Render episodes grouped by season
      let html = '<div class="episodes-list">';
      Object.keys(seasons).sort((a, b) => a - b).forEach(seasonNum => {
        html += `
          <div class="season-group">
            <h3 class="season-title">Season ${seasonNum}</h3>
            <div class="season-episodes">
        `;

        seasons[seasonNum].sort((a, b) => a.episodeNumber - b.episodeNumber).forEach(episode => {
          html += `
            <div class="episode-item">
              <div class="episode-info">
                <div class="episode-title">
                  ${episode.episodeNumber}. ${episode.title || `Episode ${episode.episodeNumber}`}
                </div>
                <div class="episode-meta">
                  ${episode.runtime ? `${episode.runtime} min` : ''}
                  ${episode.airDate ? `• Aired: ${new Date(episode.airDate).toLocaleDateString()}` : ''}
                </div>
                <div class="media-path" title="${episode.filePath || 'No file'}">${episode.filePath || 'No file'}</div>
              </div>
              <div class="episode-actions">
                ${episode.filePath ? `<button class="media-btn transcode" onclick="showTranscodeModal(${episode.id}, 'episode', 'S${seasonNum}E${episode.episodeNumber} - ${episode.title || 'Episode'}')">🎬 Transcode</button>` : ''}
              </div>
            </div>
          `;
        });

        html += `
            </div>
          </div>
        `;
      });
      html += '</div>';

      episodesContainer.innerHTML = html;
    } catch (error) {
      console.error('Failed to load episodes:', error);
      episodesContainer.innerHTML = '<div class="loading" style="padding: 20px; color: #f44336;">Failed to load episodes</div>';
    }
  } else {
    // Hide episodes
    episodesContainer.style.display = 'none';
  }
};

// Start transcoding immediately (no modal, no questions)
window.showTranscodeModal = async function (contentId, type, title) {
  // Confirm before starting
  if (!confirm(`Start maximum quality transcode for "${title}"?\n\n🎬 Settings:\n• GPU: RTX 4070 Ti (NVENC)\n• Quality: CQ 16 (near-lossless)\n• Preset: p7 (maximum)\n• Audio: 320k AAC 5.1\n\nOriginal will be backed up with .original extension.`)) {
    return;
  }

  try {
    const result = await apiClient.request('/transcode/offline', {
      method: 'POST',
      body: JSON.stringify({
        contentId,
        type
      })
    });

    alert(`✅ Transcoding started for "${title}"!\n\nRunning in background with maximum quality settings.\nOriginal will be backed up with .original extension.`);
  } catch (error) {
    console.error('Failed to start transcoding:', error);
    alert(`❌ Failed to start transcoding: ${error.message}`);
  }
};

// Show edit modal
window.showEditModal = async function (contentId, type) {
  try {
    const content = await apiClient.getLibraryItem(contentId);

    const modal = document.createElement('div');
    modal.className = 'edit-modal active';
    modal.innerHTML = `
      <div class="edit-modal-content">
        <div class="edit-modal-header">
          <h2 class="edit-modal-title">Edit: ${content.title}</h2>
          <button class="edit-modal-close" onclick="this.closest('.edit-modal').remove()">×</button>
        </div>
        <div class="edit-modal-body">
          <div class="admin-field">
            <label>Title</label>
            <input type="text" id="edit-title" value="${content.title}" />
          </div>
          <div class="admin-field">
            <label>Year</label>
            <input type="text" id="edit-year" value="${content.year || ''}" />
          </div>
          <div class="admin-field">
            <label>Overview</label>
            <textarea id="edit-overview" rows="4" style="width: 100%; background: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.2); border-radius: 4px; padding: 12px; color: #fff; font-family: 'Poppins', sans-serif;">${content.overview || ''}</textarea>
          </div>
        </div>
        <div class="edit-modal-footer">
          <button class="cancel-btn" onclick="this.closest('.edit-modal').remove()">Cancel</button>
          <button class="save-btn" onclick="saveMediaEdit(${contentId}, '${type}')">Save Changes</button>
        </div>
      </div>
    `;
    document.body.appendChild(modal);
  } catch (error) {
    console.error('Failed to load content details:', error);
    alert('Failed to load content details');
  }
};

// Save media edit
window.saveMediaEdit = async function (contentId, type) {
  const title = document.getElementById('edit-title').value;
  const year = document.getElementById('edit-year').value;
  const overview = document.getElementById('edit-overview').value;

  try {
    await apiClient.request(`/library/${contentId}`, {
      method: 'PUT',
      body: JSON.stringify({ title, year, overview })
    });

    alert('✅ Changes saved successfully!');
    document.querySelector('.edit-modal').remove();
    loadMediaLibrary();
  } catch (error) {
    console.error('Failed to save changes:', error);
    alert('❌ Failed to save changes: ' + error.message);
  }
};

// Delete media
window.deleteMedia = async function (contentId, type, title) {
  if (!confirm(`Are you sure you want to delete "${title}"?\n\nThis will remove it from the library and delete the file from disk.`)) {
    return;
  }

  try {
    await apiClient.removeFromLibrary(contentId);
    alert(`✅ "${title}" has been deleted`);
    loadMediaLibrary();
  } catch (error) {
    console.error('Failed to delete media:', error);
    alert('❌ Failed to delete: ' + error.message);
  }
};

// Initialize
document.addEventListener('DOMContentLoaded', () => {
  console.log('Admin page loaded');

  loadSettings();
  document.getElementById('save-btn').addEventListener('click', saveSettings);

  // Setup tab switching
  document.querySelectorAll('.admin-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      console.log('Tab clicked:', tab.dataset.tab);
      switchTab(tab.dataset.tab);
    });
  });

  // Setup search and filters
  document.getElementById('movie-search')?.addEventListener('input', (e) => {
    filterMedia('movies', e.target.value);
  });

  document.getElementById('series-search')?.addEventListener('input', (e) => {
    filterMedia('series', e.target.value);
  });

  // Check if we should load media library on initial load
  // (in case user bookmarked the media tab or it's the default)
  const activeTab = document.querySelector('.admin-tab.active');
  if (activeTab && activeTab.dataset.tab === 'media') {
    console.log('Media tab is active on load, loading media library...');
    loadMediaLibrary();
  }
});

// Filter media
function filterMedia(type, query) {
  const listId = type === 'movies' ? 'movies-list' : 'series-list';
  const items = document.querySelectorAll(`#${listId} .media-item`);

  items.forEach(item => {
    const title = item.querySelector('.media-title').textContent.toLowerCase();
    if (title.includes(query.toLowerCase())) {
      item.style.display = 'flex';
    } else {
      item.style.display = 'none';
    }
  });
}


// Server Update Functions
let currentUpdateInfo = null;

async function loadCurrentVersion() {
  try {
    const response = await apiClient.get('/server-update/version');
    document.getElementById('current-version').textContent = response.version;
  } catch (error) {
    console.error('Failed to load version:', error);
    document.getElementById('current-version').textContent = 'Unknown';
  }
}

async function checkForUpdates() {
  const btn = document.getElementById('check-update-btn');
  btn.textContent = '⏳ Checking...';
  btn.disabled = true;

  try {
    const response = await apiClient.get('/server-update/check');
    
    if (response.updateAvailable) {
      currentUpdateInfo = response;
      document.getElementById('latest-version').textContent = response.latestVersion;
      document.getElementById('update-available-msg').style.display = 'block';
      document.getElementById('update-details').style.display = 'block';
      
      // Format release notes
      const releaseNotes = response.releaseNotes || 'No release notes available';
      document.getElementById('release-notes').innerHTML = releaseNotes.replace(/\n/g, '<br>');
      
      showStatus(`Update available: ${response.latestVersion}`, 'success');
    } else {
      document.getElementById('update-available-msg').style.display = 'none';
      document.getElementById('update-details').style.display = 'none';
      showStatus(response.message || 'Server is up to date', 'success');
    }
  } catch (error) {
    console.error('Failed to check for updates:', error);
    showStatus('Failed to check for updates', 'error');
  } finally {
    btn.textContent = 'Check for Updates';
    btn.disabled = false;
  }
}

async function applyUpdate() {
  if (!currentUpdateInfo || !currentUpdateInfo.downloadUrl) {
    showStatus('No update information available', 'error');
    return;
  }

  if (!confirm(`This will download and install version ${currentUpdateInfo.latestVersion}. The server will restart. Continue?`)) {
    return;
  }

  const btn = document.getElementById('apply-update-btn');
  btn.textContent = '⏳ Downloading...';
  btn.disabled = true;

  try {
    const response = await apiClient.post('/server-update/apply', {
      downloadUrl: currentUpdateInfo.downloadUrl
    });
    
    showStatus('Update is being applied. Server will restart shortly...', 'success');
    
    // Show a countdown or message
    setTimeout(() => {
      alert('Server is restarting. Please refresh the page in a moment.');
    }, 2000);
  } catch (error) {
    console.error('Failed to apply update:', error);
    showStatus('Failed to apply update: ' + (error.message || 'Unknown error'), 'error');
    btn.textContent = 'Download and Install Update';
    btn.disabled = false;
  }
}

// Make functions globally available
window.checkForUpdates = checkForUpdates;
window.applyUpdate = applyUpdate;

// Load version on page load
document.addEventListener('DOMContentLoaded', () => {
  loadCurrentVersion();
});
