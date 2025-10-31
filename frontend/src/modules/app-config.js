// App configuration for Android/device-specific settings
export class AppConfig {
  constructor() {
    this.storageKey = 'lanflix_config';
    this.config = this.load();
  }

  load() {
    const stored = localStorage.getItem(this.storageKey);
    if (stored) {
      return JSON.parse(stored);
    }
    
    // Default configuration - empty URL forces first-run setup
    return {
      backendUrl: '', // User must configure on first run
      autoPlay: true,
      quality: 'auto',
      subtitles: true
    };
  }

  save() {
    localStorage.setItem(this.storageKey, JSON.stringify(this.config));
  }

  getBackendUrl() {
    return this.config.backendUrl.replace(/\/$/, ''); // Remove trailing slash
  }

  setBackendUrl(url) {
    this.config.backendUrl = url;
    this.save();
  }

  get(key) {
    return this.config[key];
  }

  set(key, value) {
    this.config[key] = value;
    this.save();
  }

  getAll() {
    return { ...this.config };
  }
}

export const appConfig = new AppConfig();
