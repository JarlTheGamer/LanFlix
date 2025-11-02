import Profile from './Profile';
import Content from './Content';
import SeriesEpisode from './SeriesEpisode';
import WatchHistory from './WatchHistory';
import Watchlist from './Watchlist';
import DownloadQueue from './DownloadQueue';
import Settings from './Settings';
import AutoDeleteSchedule from './AutoDeleteSchedule';
import DeviceToken from './DeviceToken';

// Define associations
Profile.hasMany(WatchHistory, { foreignKey: 'profileId', as: 'watchHistory' });
Profile.hasMany(Watchlist, { foreignKey: 'profileId', as: 'watchlist' });
Profile.hasMany(DownloadQueue, { foreignKey: 'profileId', as: 'downloads' });
Profile.hasMany(DeviceToken, { foreignKey: 'profileId', as: 'deviceTokens' });

Content.hasMany(SeriesEpisode, { foreignKey: 'contentId', as: 'episodes' });
Content.hasMany(WatchHistory, { foreignKey: 'contentId', as: 'watchHistory' });
Content.hasMany(Watchlist, { foreignKey: 'contentId', as: 'watchlists' });
Content.hasMany(DownloadQueue, { foreignKey: 'contentId', as: 'downloads' });
Content.hasOne(AutoDeleteSchedule, { foreignKey: 'contentId', as: 'deleteSchedule' });

SeriesEpisode.belongsTo(Content, { foreignKey: 'contentId', as: 'series' });
SeriesEpisode.hasMany(WatchHistory, { foreignKey: 'episodeId', as: 'watchHistory' });

WatchHistory.belongsTo(Profile, { foreignKey: 'profileId', as: 'profile' });
WatchHistory.belongsTo(Content, { foreignKey: 'contentId', as: 'content' });
WatchHistory.belongsTo(SeriesEpisode, { foreignKey: 'episodeId', as: 'episode' });

Watchlist.belongsTo(Profile, { foreignKey: 'profileId', as: 'profile' });
Watchlist.belongsTo(Content, { foreignKey: 'contentId', as: 'content' });

DownloadQueue.belongsTo(Profile, { foreignKey: 'profileId', as: 'profile' });
DownloadQueue.belongsTo(Content, { foreignKey: 'contentId', as: 'content' });

AutoDeleteSchedule.belongsTo(Content, { foreignKey: 'contentId', as: 'content' });

DeviceToken.belongsTo(Profile, { foreignKey: 'profileId', as: 'profile' });

export {
  Profile,
  Content,
  SeriesEpisode,
  WatchHistory,
  Watchlist,
  DownloadQueue,
  Settings,
  AutoDeleteSchedule,
  DeviceToken
};
