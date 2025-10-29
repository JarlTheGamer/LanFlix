'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('watch_history', {
      id: {
        type: Sequelize.INTEGER,
        autoIncrement: true,
        primaryKey: true
      },
      profile_id: {
        type: Sequelize.INTEGER,
        allowNull: false,
        references: {
          model: 'profiles',
          key: 'id'
        },
        onDelete: 'CASCADE'
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
      episode_id: {
        type: Sequelize.INTEGER,
        allowNull: true,
        references: {
          model: 'series_episodes',
          key: 'id'
        },
        onDelete: 'CASCADE'
      },
      progress_seconds: {
        type: Sequelize.INTEGER,
        allowNull: false,
        defaultValue: 0
      },
      duration_seconds: {
        type: Sequelize.INTEGER,
        allowNull: true
      },
      completed: {
        type: Sequelize.BOOLEAN,
        allowNull: false,
        defaultValue: false
      },
      last_watched_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      }
    });

    // Add indexes
    await queryInterface.addIndex('watch_history', ['profile_id'], {
      name: 'idx_watch_history_profile_id'
    });
    await queryInterface.addIndex('watch_history', ['content_id'], {
      name: 'idx_watch_history_content_id'
    });
    await queryInterface.addIndex('watch_history', ['profile_id', 'content_id'], {
      name: 'idx_watch_history_profile_content'
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('watch_history');
  }
};
