import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface DeviceTokenAttributes {
  id: number;
  profileId: number;
  deviceToken: string;
  platform: 'android' | 'android-tv' | 'web';
  registeredAt: Date;
  lastUsedAt: Date;
}

interface DeviceTokenCreationAttributes extends Optional<DeviceTokenAttributes, 'id' | 'registeredAt' | 'lastUsedAt'> {}

class DeviceToken extends Model<DeviceTokenAttributes, DeviceTokenCreationAttributes> implements DeviceTokenAttributes {
  public id!: number;
  public profileId!: number;
  public deviceToken!: string;
  public platform!: 'android' | 'android-tv' | 'web';
  public registeredAt!: Date;
  public lastUsedAt!: Date;
}

DeviceToken.init(
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
    deviceToken: {
      type: DataTypes.STRING(500),
      allowNull: false,
      unique: true,
      field: 'device_token'
    },
    platform: {
      type: DataTypes.STRING(50),
      allowNull: false,
      validate: {
        isIn: [['android', 'android-tv', 'web']]
      }
    },
    registeredAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'registered_at'
    },
    lastUsedAt: {
      type: DataTypes.DATE,
      allowNull: false,
      defaultValue: DataTypes.NOW,
      field: 'last_used_at'
    }
  },
  {
    sequelize,
    tableName: 'device_tokens',
    timestamps: false,
    underscored: true
  }
);

export default DeviceToken;
