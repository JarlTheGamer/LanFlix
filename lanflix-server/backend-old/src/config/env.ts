import dotenv from 'dotenv';
import path from 'path';

dotenv.config();

export const config = {
  server: {
    port: parseInt(process.env.PORT || '6129', 10),
    nodeEnv: process.env.NODE_ENV || 'development',
    logLevel: process.env.LOG_LEVEL || 'info'
  },
  database: {
    path: process.env.DATABASE_PATH || path.join(__dirname, '../../data/lanflix.db')
  },
  media: {
    rootPath: process.env.MEDIA_ROOT_PATH || '/path/to/media',
    posterCachePath: process.env.POSTER_CACHE_PATH || path.join(__dirname, '../../data/posters'),
    backdropCachePath: process.env.BACKDROP_CACHE_PATH || path.join(__dirname, '../../data/backdrops')
  },
  externalServices: {
    sonarr: {
      url: process.env.SONARR_URL || 'http://localhost:8989',
      apiKey: process.env.SONARR_API_KEY || ''
    },
    radarr: {
      url: process.env.RADARR_URL || 'http://localhost:7878',
      apiKey: process.env.RADARR_API_KEY || ''
    },
    prowlarr: {
      url: process.env.PROWLARR_URL || 'http://localhost:9696',
      apiKey: process.env.PROWLARR_API_KEY || ''
    },
    tmdb: {
      apiKey: process.env.TMDB_API_KEY || ''
    }
  },
  redis: {
    url: process.env.REDIS_URL
  }
};
