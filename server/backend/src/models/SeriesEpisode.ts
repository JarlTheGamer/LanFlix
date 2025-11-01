import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface SeriesEpisodeAttributes {
  id: number;
  contentId: number;
  seasonNumber: number;
  episodeNumber: number;
  title?: string;
  overview?: string;
  airDate?: Date;
  stillPath?: string;
  filePath?: string;
}

interface SeriesEpisodeCreationAttributes extends Optional<SeriesEpisodeAttributes, 'id'> {}

class SeriesEpisode extends Model<SeriesEpisodeAttributes, SeriesEpisodeCreationAttributes> implements SeriesEpisodeAttributes {
  public id!: number;
  public contentId!: number;
  public seasonNumber!: number;
  public episodeNumber!: number;
  public title?: string;
  public overview?: string;
  public airDate?: Date;
  public stillPath?: string;
  public filePath?: string;
}

SeriesEpisode.init(
  {
    id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true
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
    seasonNumber: {
      type: DataTypes.INTEGER,
      allowNull: false,
      field: 'season_number'
    },
    episodeNumber: {
      type: DataTypes.INTEGER,
      allowNull: false,
      field: 'episode_number'
    },
    title: {
      type: DataTypes.STRING(255),
      allowNull: true
    },
    overview: {
      type: DataTypes.TEXT,
      allowNull: true
    },
    airDate: {
      type: DataTypes.DATEONLY,
      allowNull: true,
      field: 'air_date'
    },
    stillPath: {
      type: DataTypes.STRING(255),
      allowNull: true,
      field: 'still_path'
    },
    filePath: {
      type: DataTypes.STRING(500),
      allowNull: true,
      field: 'file_path'
    }
  },
  {
    sequelize,
    tableName: 'series_episodes',
    timestamps: false,
    underscored: true
  }
);

export default SeriesEpisode;
