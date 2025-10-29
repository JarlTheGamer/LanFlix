import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface DownloadQueueAttributes {
  id: number;
  profileId: number;
  contentId: number;
  type: 'movie' | 'series';
  externalId?: number;
  status: 'queued' | 'downloading' | 'completed' | 'failed';
  progressPercent: number;
  errorMessage?: string;
  queuedAt: Date;
  completedAt?: Date;
}

interface DownloadQueueCreationAttributes extends Optional<DownloadQueueAttributes, 'id' | 'status' | 'progressPercent' | 'queuedAt'> {}

class DownloadQueue extends Model<DownloadQueueAttributes, DownloadQueueCreationAttributes> implements DownloadQueueAttributes {
  public id!: number;
  public profileId!: number;
  public contentId!: number;
  public type!: 'movie' | 'series';
  public externalId?: number;
  public status!: 'queued' | 'downloading' | 'completed' | 'failed';
  public progressPercent!: number;
  public errorMessage?: string;
  public queuedAt!: Date;
  public completedAt?: Date;
}

DownloadQueue.init(
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
    type: {
      type: DataTypes.STRING(20),
      allowNull: false,
      validate: {
        isIn: [['movie', 'series']]
      }
    },
    externalId: {
      type: DataTypes.INTEGER,
      allowNull: true,
      field: 'external_id'
    },
    status: {
      type: DataTypes.STRING(50),
      allowNull: false,
      defaultValue: 'queued',
      validate: {
        isIn: [['queued', 'downloading', 'completed', 'failed']]
      }
    },
    progressPercent: {
      type: DataTypes.INTEGER,
      allowNull: false,
      defaultValue: 0,
      field: 'progress_percent'
    },
    errorMessage: {
      type: DataTypes.TEXT,
      allowNull: true,
      field: 'error_message'
    },
    queuedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'queued_at'
    },
    completedAt: {
      type: DataTypes.DATE,
      allowNull: true,
      field: 'completed_at'
    }
  },
  {
    sequelize,
    tableName: 'download_queue',
    timestamps: false,
    underscored: true
  }
);

export default DownloadQueue;
