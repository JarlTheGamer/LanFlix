import { Sequelize } from 'sequelize';
import { config } from '../config/env';
import logger from './logger';
import fs from 'fs';
import path from 'path';

const dbPath = config.database.path;
const dbDir = path.dirname(dbPath);

if (!fs.existsSync(dbDir)) {
  fs.mkdirSync(dbDir, { recursive: true });
  logger.info(`Created database directory: ${dbDir}`);
}

const sequelize = new Sequelize({
  dialect: 'sqlite',
  storage: dbPath,
  logging: (msg) => logger.debug(msg)
});

export const initializeDatabase = async (): Promise<void> => {
  try {
    await sequelize.authenticate();
    logger.info('Database connection established successfully');

    // Import models to ensure they're registered
    const models = await import('../models');

    // Sync database schema (creates tables if they don't exist)
    // Use alter: true in development to update existing tables
    await sequelize.sync({ alter: config.server.nodeEnv === 'development' });
    logger.info('Database models synchronized');

    // Create default profiles if none exist
    const { Profile } = models;
    const profileCount = await Profile.count();
    if (profileCount === 0) {
      await Profile.bulkCreate([
        {
          name: 'Default',
          avatarColorPrimary: '#e50914',
          avatarColorSecondary: '#b20710'
        },
        {
          name: 'Kids',
          avatarColorPrimary: '#46d369',
          avatarColorSecondary: '#2ea84e'
        },
        {
          name: 'Guest',
          avatarColorPrimary: '#ffa00a',
          avatarColorSecondary: '#cc8008'
        }
      ]);
      logger.info('Created default profiles');
    }
  } catch (error) {
    logger.error('Unable to connect to database:', error);
    throw error;
  }
};

export default sequelize;
