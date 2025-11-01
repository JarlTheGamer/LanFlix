'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('device_tokens', {
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
      device_token: {
        type: Sequelize.STRING(500),
        allowNull: false,
        unique: true
      },
      platform: {
        type: Sequelize.STRING(50),
        allowNull: false
      },
      registered_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      },
      last_used_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      }
    });

    // Add indexes
    await queryInterface.addIndex('device_tokens', ['profile_id'], {
      name: 'idx_device_tokens_profile_id'
    });
    await queryInterface.addIndex('device_tokens', ['device_token'], {
      name: 'idx_device_tokens_device_token',
      unique: true
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('device_tokens');
  }
};
