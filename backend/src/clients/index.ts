export { TMDBClient } from './tmdb.client';
export { SonarrClient } from './sonarr.client';
export { RadarrClient } from './radarr.client';
export { ProwlarrClient } from './prowlarr.client';

import { TMDBClient } from './tmdb.client';
import { SonarrClient } from './sonarr.client';
import { RadarrClient } from './radarr.client';
import { ProwlarrClient } from './prowlarr.client';

// Create singleton instances
export const tmdbClient = new TMDBClient();
export const sonarrClient = new SonarrClient();
export const radarrClient = new RadarrClient();
export const prowlarrClient = new ProwlarrClient();
