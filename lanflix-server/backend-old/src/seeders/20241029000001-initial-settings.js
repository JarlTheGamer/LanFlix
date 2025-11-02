'use strict';

module.exports = {
  up: async (queryInterface, Sequelize) => {
    await queryInterface.bulkInsert('settings', [
      {
        key: 'app_version',
        value: '1.0.0',
        updated_at: new Date()
      },
      {
        key: 'language',
        value: 'en',
        updated_at: new Date()
      },
      {
        key: 'timezone',
        value: 'UTC',
        updated_at: new Date()
      },
      {
        key: 'video_quality',
        value: 'auto',
        updated_at: new Date()
      },
      {
        key: 'data_saver_mode',
        value: 'false',
        updated_at: new Date()
      },
      {
        key: 'audio_language',
        value: 'en',
        updated_at: new Date()
      },
      {
        key: 'subtitle_language',
        value: 'en',
        updated_at: new Date()
      },
      {
        key: 'theme',
        value: 'dark',
        updated_at: new Date()
      },
      {
        key: 'auto_delete_enabled',
        value: 'true',
        updated_at: new Date()
      },
      {
        key: 'auto_delete_days',
        value: '30',
        updated_at: new Date()
      },
      {
        key: 'notification_enabled',
        value: 'true',
        updated_at: new Date()
      },
      {
        key: 'notification_days_before_delete',
        value: '7',
        updated_at: new Date()
      }
    ], {});
  },

  down: async (queryInterface, Sequelize) => {
    await queryInterface.bulkDelete('settings', null, {});
  }
};
