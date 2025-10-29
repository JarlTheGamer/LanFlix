import apiClient from '../modules/api-client.js';

// Load current settings
async function loadSettings() {
  try {
    const response = await apiClient.getSettings();
    const settings = response.settings || {};

    // Populate form fields
    document.getElementById('movies-path').value = settings.moviesPath || '/media/movies';
    document.getElementById('series-path').value = settings.seriesPath || '/media/series';
    document.getElementById('tmdb-key').value = settings.tmdbApiKey || '';
    document.getElementById('sonarr-url').value = settings.sonarrUrl || '';
    document.getElementById('sonarr-key').value = settings.sonarrApiKey || '';
    document.getElementById('radarr-url').value = settings.radarrUrl || '';
    document.getElementById('radarr-key').value = settings.radarrApiKey || '';
    document.getElementById('prowlarr-url').value = settings.prowlarrUrl || '';
    document.getElementById('prowlarr-key').value = settings.prowlarrApiKey || '';
    document.getElementById('auto-metadata').checked = settings.autoMetadata !== false;
    document.getElementById('download-images').checked = settings.downloadImages !== false;
    document.getElementById('metadata-language').value = settings.metadataLanguage || 'en';
  } catch (error) {
    console.error('Failed to load settings:', error);
    showStatus('Failed to load current settings', 'error');
  }
}

// Save settings
async function saveSettings() {
  const saveBtn = document.getElementById('save-btn');
  saveBtn.textContent = '⏳ Saving...';
  saveBtn.disabled = true;

  try {
    const settings = {
      moviesPath: document.getElementById('movies-path').value,
      seriesPath: document.getElementById('series-path').value,
      tmdbApiKey: document.getElementById('tmdb-key').value,
      sonarrUrl: document.getElementById('sonarr-url').value,
      sonarrApiKey: document.getElementById('sonarr-key').value,
      radarrUrl: document.getElementById('radarr-url').value,
      radarrApiKey: document.getElementById('radarr-key').value,
      prowlarrUrl: document.getElementById('prowlarr-url').value,
      prowlarrApiKey: document.getElementById('prowlarr-key').value,
      autoMetadata: document.getElementById('auto-metadata').checked,
      downloadImages: document.getElementById('download-images').checked,
      metadataLanguage: document.getElementById('metadata-language').value
    };

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
window.togglePassword = function(fieldId) {
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

// Make testConnection available globally
window.testConnection = testConnection;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
  loadSettings();
  document.getElementById('save-btn').addEventListener('click', saveSettings);
});
