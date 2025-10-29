import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface SettingsAttributes {
  key: string;
  value: string;
  updatedAt: Date;
}

interface SettingsCreationAttributes extends Optional<SettingsAttributes, 'updatedAt'> {}

class Settings extends Model<SettingsAttributes, SettingsCreationAttributes> implements SettingsAttributes {
  public key!: string;
  public value!: string;
  public updatedAt!: Date;
}

Settings.init(
  {
    key: {
      type: DataTypes.STRING(255),
      primaryKey: true,
      allowNull: false
    },
    value: {
      type: DataTypes.TEXT,
      allowNull: false
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
    tableName: 'settings',
    timestamps: false,
    underscored: true
  }
);

export default Settings;
