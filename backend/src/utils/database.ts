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
    
    await sequelize.sync({ alter: false });
    logger.info('Database models synchronized');
  } catch (error) {
    logger.error('Unable to connect to database:', error);
    throw error;
  }
};

export default sequelize;
