'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.createTable('watchlist', {
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
      added_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal('CURRENT_TIMESTAMP')
      }
    });

    // Add indexes
    await queryInterface.addIndex('watchlist', ['profile_id'], {
      name: 'idx_watchlist_profile_id'
    });
    await queryInterface.addIndex('watchlist', ['content_id'], {
      name: 'idx_watchlist_content_id'
    });
    await queryInterface.addIndex('watchlist', ['profile_id', 'content_id'], {
      name: 'idx_watchlist_profile_content',
      unique: true
    });
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.dropTable('watchlist');
  }
};
