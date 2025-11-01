import sequelize from '../utils/database';
import {
  Profile,
  Content,
  SeriesEpisode,
  WatchHistory,
  Watchlist,
  DownloadQueue,
  Settings,
  AutoDeleteSchedule,
  DeviceToken
} from '../models';

async function verifyDatabase() {
  try {
    console.log('Connecting to database...');
    await sequelize.authenticate();
    console.log('✓ Database connection successful\n');

    // Check profiles
    const profiles = await Profile.findAll();
    console.log(`✓ Profiles table: ${profiles.length} profiles found`);
    profiles.forEach(p => console.log(`  - ${p.name} (${p.avatarColorPrimary})`));

    // Check settings
    const settings = await Settings.findAll();
    console.log(`\n✓ Settings table: ${settings.length} settings found`);
    settings.slice(0, 5).forEach(s => console.log(`  - ${s.key}: ${s.value}`));
    if (settings.length > 5) console.log(`  ... and ${settings.length - 5} more`);

    // Verify all tables exist by counting
    console.log('\n✓ Verifying all tables exist:');
    
    const contentCount = await Content.count();
    console.log(`  - Content: ${contentCount} records`);
    
    const episodeCount = await SeriesEpisode.count();
    console.log(`  - SeriesEpisode: ${episodeCount} records`);
    
    const historyCount = await WatchHistory.count();
    console.log(`  - WatchHistory: ${historyCount} records`);
    
    const watchlistCount = await Watchlist.count();
    console.log(`  - Watchlist: ${watchlistCount} records`);
    
    const downloadCount = await DownloadQueue.count();
    console.log(`  - DownloadQueue: ${downloadCount} records`);
    
    const scheduleCount = await AutoDeleteSchedule.count();
    console.log(`  - AutoDeleteSchedule: ${scheduleCount} records`);
    
    const tokenCount = await DeviceToken.count();
    console.log(`  - DeviceToken: ${tokenCount} records`);

    console.log('\n✅ Database verification complete!');
    console.log('\nAll models and migrations are working correctly.');
    
    await sequelize.close();
  } catch (error) {
    console.error('❌ Database verification failed:', error);
    process.exit(1);
  }
}

verifyDatabase();
