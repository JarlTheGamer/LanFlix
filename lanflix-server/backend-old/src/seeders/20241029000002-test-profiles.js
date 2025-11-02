'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.bulkInsert('profiles', [
      {
        name: 'Default',
        avatar_color_primary: '#e50914',
        avatar_color_secondary: '#b20710',
        created_at: new Date(),
        updated_at: new Date()
      },
      {
        name: 'Kids',
        avatar_color_primary: '#46d369',
        avatar_color_secondary: '#2ea84e',
        created_at: new Date(),
        updated_at: new Date()
      },
      {
        name: 'Guest',
        avatar_color_primary: '#ffa00a',
        avatar_color_secondary: '#cc8008',
        created_at: new Date(),
        updated_at: new Date()
      }
    ], {});
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.bulkDelete('profiles', null, {});
  }
};
