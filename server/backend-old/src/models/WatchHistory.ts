import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface WatchHistoryAttributes {
  id: number;
  profileId: number;
  contentId: number;
  episodeId?: number;
  progressSeconds: number;
  durationSeconds?: number;
  completed: boolean;
  lastWatchedAt: Date;
}

interface WatchHistoryCreationAttributes extends Optional<WatchHistoryAttributes, 'id' | 'progressSeconds' | 'completed' | 'lastWatchedAt'> {}

class WatchHistory extends Model<WatchHistoryAttributes, WatchHistoryCreationAttributes> implements WatchHistoryAttributes {
  public id!: number;
  public profileId!: number;
  public contentId!: number;
  public episodeId?: number;
  public progressSeconds!: number;
  public durationSeconds?: number;
  public completed!: boolean;
  public lastWatchedAt!: Date;
}

WatchHistory.init(
  {
    id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true
    },
    profileId: {
      type: DataTypes.INTEGER,
      allowNull: false,
      field: 'profile_id',
      references: {
        model: 'profiles',
        key: 'id'
      },
      onDelete: 'CASCADE'
    },
    contentId: {
      type: DataTypes.INTEGER,
      allowNull: false,
      field: 'content_id',
      references: {
        model: 'content',
        key: 'id'
      },
      onDelete: 'CASCADE'
    },
    episodeId: {
      type: DataTypes.INTEGER,
      allowNull: true,
      field: 'episode_id',
      references: {
        model: 'series_episodes',
        key: 'id'
      },
      onDelete: 'CASCADE'
    },
    progressSeconds: {
      type: DataTypes.INTEGER,
      allowNull: false,
      defaultValue: 0,
      field: 'progress_seconds'
    },
    durationSeconds: {
      type: DataTypes.INTEGER,
      allowNull: true,
      field: 'duration_seconds'
    },
    completed: {
      type: DataTypes.BOOLEAN,
      allowNull: false,
      defaultValue: false
    },
    lastWatchedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'last_watched_at'
    }
  },
  {
    sequelize,
    tableName: 'watch_history',
    timestamps: false,
    underscored: true
  }
);

export default WatchHistory;
