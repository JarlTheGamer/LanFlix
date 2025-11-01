import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface ContentAttributes {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  originalTitle?: string;
  overview?: string;
  releaseDate?: Date;
  posterPath?: string;
  backdropPath?: string;
  voteAverage?: number;
  voteCount?: number;
  genres?: string;
  runtime?: number;
  status?: string;
  filePath?: string;
  addedAt?: Date;
  updatedAt?: Date;
}

interface ContentCreationAttributes extends Optional<ContentAttributes, 'id' | 'addedAt' | 'updatedAt'> {}

class Content extends Model<ContentAttributes, ContentCreationAttributes> implements ContentAttributes {
  public id!: number;
  public tmdbId!: number;
  public type!: 'movie' | 'series';
  public title!: string;
  public originalTitle?: string;
  public overview?: string;
  public releaseDate?: Date;
  public posterPath?: string;
  public backdropPath?: string;
  public voteAverage?: number;
  public voteCount?: number;
  public genres?: string;
  public runtime?: number;
  public status?: string;
  public filePath?: string;
  public readonly addedAt!: Date;
  public readonly updatedAt!: Date;
}

Content.init(
  {
    id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true
    },
    tmdbId: {
      type: DataTypes.INTEGER,
      allowNull: false,
      unique: true,
      field: 'tmdb_id'
    },
    type: {
      type: DataTypes.STRING(20),
      allowNull: false,
      validate: {
        isIn: [['movie', 'series']]
      }
    },
    title: {
      type: DataTypes.STRING(255),
      allowNull: false
    },
    originalTitle: {
      type: DataTypes.STRING(255),
      allowNull: true,
      field: 'original_title'
    },
    overview: {
      type: DataTypes.TEXT,
      allowNull: true
    },
    releaseDate: {
      type: DataTypes.DATEONLY,
      allowNull: true,
      field: 'release_date'
    },
    posterPath: {
      type: DataTypes.STRING(255),
      allowNull: true,
      field: 'poster_path'
    },
    backdropPath: {
      type: DataTypes.STRING(255),
      allowNull: true,
      field: 'backdrop_path'
    },
    voteAverage: {
      type: DataTypes.DECIMAL(3, 1),
      allowNull: true,
      field: 'vote_average'
    },
    voteCount: {
      type: DataTypes.INTEGER,
      allowNull: true,
      field: 'vote_count'
    },
    genres: {
      type: DataTypes.TEXT,
      allowNull: true
    },
    runtime: {
      type: DataTypes.INTEGER,
      allowNull: true
    },
    status: {
      type: DataTypes.STRING(50),
      allowNull: true
    },
    filePath: {
      type: DataTypes.STRING(500),
      allowNull: true,
      field: 'file_path'
    },
    addedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'added_at'
    },
    updatedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'updated_at'
    }
  },
  {
    sequelize,
    tableName: 'content',
    timestamps: false
  }
);

export default Content;
