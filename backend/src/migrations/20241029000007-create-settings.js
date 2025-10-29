'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('settings', {
      key: {
        type: Sequelize.STRING(255),
        primaryKey: true,
        allowNull: false
      },
      value: {
        type: Sequelize.TEXT,
        allowNull: false
      },
      updated_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      }
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('settings');
  }
};
