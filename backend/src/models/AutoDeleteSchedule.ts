import { DataTypes, Model, Optional } from 'sequelize';
import sequelize from '../utils/database';

interface AutoDeleteScheduleAttributes {
  id: number;
  contentId: number;
  scheduledDeleteAt: Date;
  notificationSent: boolean;
  notificationSentAt?: Date;
  userResponse?: 'keep' | 'delete';
  responseAt?: Date;
  deleted: boolean;
  deletedAt?: Date;
}

interface AutoDeleteScheduleCreationAttributes extends Optional<AutoDeleteScheduleAttributes, 'id' | 'notificationSent' | 'deleted'> {}

class AutoDeleteSchedule extends Model<AutoDeleteScheduleAttributes, AutoDeleteScheduleCreationAttributes> implements AutoDeleteScheduleAttributes {
  public id!: number;
  public contentId!: number;
  public scheduledDeleteAt!: Date;
  public notificationSent!: boolean;
  public notificationSentAt?: Date;
  public userResponse?: 'keep' | 'delete';
  public responseAt?: Date;
  public deleted!: boolean;
  public deletedAt?: Date;
}

AutoDeleteSchedule.init(
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
    scheduledDeleteAt: {
      type: DataTypes.DATE,
      allowNull: false,
      field: 'scheduled_delete_at'
    },
    notificationSent: {
      type: DataTypes.BOOLEAN,
      allowNull: false,
      defaultValue: false,
      field: 'notification_sent'
    },
    notificationSentAt: {
      type: DataTypes.DATE,
      allowNull: true,
      field: 'notification_sent_at'
    },
    userResponse: {
      type: DataTypes.STRING(20),
      allowNull: true,
      field: 'user_response',
      validate: {
        isIn: [['keep', 'delete']]
      }
    },
    responseAt: {
      type: DataTypes.DATE,
      allowNull: true,
      field: 'response_at'
    },
    deleted: {
      type: DataTypes.BOOLEAN,
      allowNull: false,
      defaultValue: false
    },
    deletedAt: {
      type: DataTypes.DATE,
      allowNull: true,
      field: 'deleted_at'
    }
  },
  {
    sequelize,
    tableName: 'auto_delete_schedule',
    timestamps: false,
    underscored: true
  }
);

export default AutoDeleteSchedule;
