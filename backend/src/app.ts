import express from 'express';
import cors from 'cors';
import { config } from './config/env';
import logger from './utils/logger';
import { initializeDatabase } from './utils/database';
import { errorHandler, notFoundHandler } from './middleware/error-handler';
import fs from 'fs';
import path from 'path';

const app = express();

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

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
    
    app.listen(config.server.port, () => {
      logger.info(`Server running on port ${config.server.port}`);
      logger.info(`Environment: ${config.server.nodeEnv}`);
    });
  } catch (error) {
    logger.error('Failed to start server:', error);
    process.exit(1);
  }
};

startServer();

export default app;
