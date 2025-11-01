'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('content', {
      id: {
        type: Sequelize.INTEGER,
        autoIncrement: true,
        primaryKey: true
      },
      tmdb_id: {
        type: Sequelize.INTEGER,
        allowNull: false,
        unique: true
      },
      type: {
        type: Sequelize.STRING(20),
        allowNull: false
      },
      title: {
        type: Sequelize.STRING(255),
        allowNull: false
      },
      original_title: {
        type: Sequelize.STRING(255),
        allowNull: true
      },
      overview: {
        type: Sequelize.TEXT,
        allowNull: true
      },
      release_date: {
        type: Sequelize.DATEONLY,
        allowNull: true
      },
      poster_path: {
        type: Sequelize.STRING(255),
        allowNull: true
      },
      backdrop_path: {
        type: Sequelize.STRING(255),
        allowNull: true
      },
      vote_average: {
        type: Sequelize.DECIMAL(3, 1),
        allowNull: true
      },
      vote_count: {
        type: Sequelize.INTEGER,
        allowNull: true
      },
      genres: {
        type: Sequelize.TEXT,
        allowNull: true
      },
      runtime: {
        type: Sequelize.INTEGER,
        allowNull: true
      },
      status: {
        type: Sequelize.STRING(50),
        allowNull: true
      },
      file_path: {
        type: Sequelize.STRING(500),
        allowNull: true
      },
      added_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      },
      updated_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      }
    });

    // Add indexes
    await queryInterface.addIndex('content', ['tmdb_id'], {
      name: 'idx_content_tmdb_id'
    });
    await queryInterface.addIndex('content', ['type'], {
      name: 'idx_content_type'
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('content');
  }
};
