'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('download_queue', {
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
      type: {
        type: Sequelize.STRING(20),
        allowNull: false
      },
      external_id: {
        type: Sequelize.INTEGER,
        allowNull: true
      },
      status: {
        type: Sequelize.STRING(50),
        allowNull: false,
        defaultValue: 'queued'
      },
      progress_percent: {
        type: Sequelize.INTEGER,
        allowNull: false,
        defaultValue: 0
      },
      error_message: {
        type: Sequelize.TEXT,
        allowNull: true
      },
      queued_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      },
      completed_at: {
        type: Sequelize.DATE,
        allowNull: true
      }
    });

    // Add indexes
    await queryInterface.addIndex('download_queue', ['profile_id'], {
      name: 'idx_download_queue_profile_id'
    });
    await queryInterface.addIndex('download_queue', ['content_id'], {
      name: 'idx_download_queue_content_id'
    });
    await queryInterface.addIndex('download_queue', ['status'], {
      name: 'idx_download_queue_status'
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('download_queue');
  }
};
