'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('auto_delete_schedule', {
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
      scheduled_delete_at: {
        type: Sequelize.DATE,
        allowNull: false
      },
      notification_sent: {
        type: Sequelize.BOOLEAN,
        allowNull: false,
        defaultValue: false
      },
      notification_sent_at: {
        type: Sequelize.DATE,
        allowNull: true
      },
      user_response: {
        type: Sequelize.STRING(20),
        allowNull: true
      },
      response_at: {
        type: Sequelize.DATE,
        allowNull: true
      },
      deleted: {
        type: Sequelize.BOOLEAN,
        allowNull: false,
        defaultValue: false
      },
      deleted_at: {
        type: Sequelize.DATE,
        allowNull: true
      }
    });

    // Add indexes
    await queryInterface.addIndex('auto_delete_schedule', ['content_id'], {
      name: 'idx_auto_delete_schedule_content_id'
    });
    await queryInterface.addIndex('auto_delete_schedule', ['scheduled_delete_at'], {
      name: 'idx_auto_delete_schedule_scheduled_delete_at'
    });
    await queryInterface.addIndex('auto_delete_schedule', ['deleted'], {
      name: 'idx_auto_delete_schedule_deleted'
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('auto_delete_schedule');
  }
};
