require('dotenv').config();
const path = require('path');

module.exports = {
  development: {
    dialect: 'sqlite',
    storage: process.env.DATABASE_PATH || path.join(__dirname, '../../data/lanflix.db'),
    logging: false
  },
  production: {
    dialect: 'sqlite',
    storage: process.env.DATABASE_PATH || path.join(__dirname, '../../data/lanflix.db'),
    logging: false
  }
};
