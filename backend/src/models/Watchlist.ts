import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface WatchlistAttributes {
  id: number;
  profileId: number;
  contentId: number;
  addedAt: Date;
}

interface WatchlistCreationAttributes extends Optional<WatchlistAttributes, 'id' | 'addedAt'> {}

class Watchlist extends Model<WatchlistAttributes, WatchlistCreationAttributes> implements WatchlistAttributes {
  public id!: number;
  public profileId!: number;
  public contentId!: number;
  public addedAt!: Date;
}

Watchlist.init(
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
    addedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'added_at'
    }
  },
  {
    sequelize,
    tableName: 'watchlist',
    timestamps: false,
    underscored: true,
    indexes: [
      {
        unique: true,
        fields: ['profile_id', 'content_id']
      }
    ]
  }
);

export default Watchlist;
