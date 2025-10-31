import express from 'express';
import cors from 'cors';
import { config } from './config/env';
import logger from './utils/logger';
import { initializeDatabase } from './utils/database';
import { cacheManager } from './utils/cache-manager';
import { jobScheduler } from './jobs/scheduler';
import { errorHandler, notFoundHandler } from './middleware/error-handler';
import { injectApiStatus } from './middleware/api-status.middleware';
import apiRoutes from './routes';
import fs from 'fs';
import path from 'path';

const app = express();

// Configure CORS to allow requests from any origin (for local network access)
app.use(cors({
  origin: '*', // Allow all origins (safe for local network)
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'HEAD'],
  allowedHeaders: ['Content-Type', 'Authorization'],
  credentials: false,
  maxAge: 86400 // 24 hours
}));

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Serve cached images as static files
app.use('/images/posters', express.static(config.media.posterCachePath));
app.use('/images/backdrops', express.static(config.media.backdropCachePath));

// Serve media files (posters, backdrops, metadata) directly from media folders
app.use('/media', express.static(config.media.rootPath));

// Handle preflight requests for all routes
app.options('*', cors());

// Inject API status into all responses
app.use(injectApiStatus);

app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// Mount API routes
app.use('/api', apiRoutes);

app.use(notFoundHandler);
app.use(errorHandler);

const ensureDirectories = () => {
  const dirs = [
    path.dirname(config.database.path),
    config.media.posterCachePath,
    config.media.backdropCachePath,
    path.join(__dirname, '../logs')
  ];

  dirs.forEach(dir => {
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
      logger.info(`Created directory: ${dir}`);
    }
  });
};

const startServer = async () => {
  try {
    ensureDirectories();

    await initializeDatabase();

    // Load API keys from database
    const { loadApiKeysFromDatabase } = await import('./clients');
    await loadApiKeysFromDatabase();

    // Initialize cache manager
    await cacheManager.initialize();

    // Start job scheduler
    jobScheduler.start();

    app.listen(config.server.port, () => {
      logger.info(`Server running on port ${config.server.port}`);
      logger.info(`Environment: ${config.server.nodeEnv}`);
    });
  } catch (error) {
    logger.error('Failed to start server:', error);
    process.exit(1);
  }
};

// Graceful shutdown
process.on('SIGTERM', async () => {
  logger.info('SIGTERM signal received: closing HTTP server');
  jobScheduler.stop();
  await cacheManager.shutdown();
  process.exit(0);
});

process.on('SIGINT', async () => {
  logger.info('SIGINT signal received: closing HTTP server');
  jobScheduler.stop();
  await cacheManager.shutdown();
  process.exit(0);
});

startServer();

export default app;
