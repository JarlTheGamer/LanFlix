import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface ProfileAttributes {
  id: number;
  name: string;
  avatarColorPrimary: string;
  avatarColorSecondary: string;
  createdAt?: Date;
  updatedAt?: Date;
}

interface ProfileCreationAttributes extends Optional<ProfileAttributes, 'id' | 'createdAt' | 'updatedAt'> {}

class Profile extends Model<ProfileAttributes, ProfileCreationAttributes> implements ProfileAttributes {
  public id!: number;
  public name!: string;
  public avatarColorPrimary!: string;
  public avatarColorSecondary!: string;
  public readonly createdAt!: Date;
  public readonly updatedAt!: Date;
}

Profile.init(
  {
    id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true
    },
    name: {
      type: DataTypes.STRING(255),
      allowNull: false
    },
    avatarColorPrimary: {
      type: DataTypes.STRING(7),
      allowNull: false,
      field: 'avatar_color_primary'
    },
    avatarColorSecondary: {
      type: DataTypes.STRING(7),
      allowNull: false,
      field: 'avatar_color_secondary'
    }
  },
  {
    sequelize,
    tableName: 'profiles',
    timestamps: true,
    underscored: true
  }
);

export default Profile;
