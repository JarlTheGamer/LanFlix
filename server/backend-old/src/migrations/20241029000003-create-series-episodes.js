'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('series_episodes', {
      id: {
        type: Sequelize.INTEGER,
        autoIncrement: true,
        primaryKey: true
      },
      content_id: {
        type: Sequelize.INTEGER,
        allowNull: false,
        references: {
          model: 'content',
          key: 'id'
        },
        onDelete: 'CASCADE'
      },
      season_number: {
        type: Sequelize.INTEGER,
        allowNull: false
      },
      episode_number: {
        type: Sequelize.INTEGER,
        allowNull: false
      },
      title: {
        type: Sequelize.STRING(255),
        allowNull: true
      },
      overview: {
        type: Sequelize.TEXT,
        allowNull: true
      },
      air_date: {
        type: Sequelize.DATEONLY,
        allowNull: true
      },
      still_path: {
        type: Sequelize.STRING(255),
        allowNull: true
      },
      file_path: {
        type: Sequelize.STRING(500),
        allowNull: true
      }
    });

    // Add indexes
    await queryInterface.addIndex('series_episodes', ['content_id'], {
      name: 'idx_series_episodes_content_id'
    });
    await queryInterface.addIndex('series_episodes', ['content_id', 'season_number', 'episode_number'], {
      name: 'idx_series_episodes_content_season_episode'
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('series_episodes');
  }
};
