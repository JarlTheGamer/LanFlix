package com.lanflix.offline

import android.content.Context
import androidx.room.Dao
import androidx.room.Database
import androidx.room.Entity
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.PrimaryKey
import androidx.room.Query
import androidx.room.Room
import androidx.room.RoomDatabase

@Entity(tableName = "offline_catalog")
data class OfflineCatalogEntity(
    @PrimaryKey val mediaKey: String,
    val payloadJson: String,
    val localFilePath: String?,
    val updatedAtUtc: Long
)

@Dao
interface OfflineCatalogDao {
    @Query("SELECT * FROM offline_catalog ORDER BY updatedAtUtc DESC")
    suspend fun getAll(): List<OfflineCatalogEntity>

    @Query("SELECT * FROM offline_catalog WHERE mediaKey = :key LIMIT 1")
    suspend fun get(key: String): OfflineCatalogEntity?

    @Query("DELETE FROM offline_catalog WHERE localFilePath IS NULL")
    suspend fun clearMetadataOnly()

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(items: List<OfflineCatalogEntity>)
}

@Database(entities = [OfflineCatalogEntity::class], version = 1, exportSchema = false)
abstract class OfflineCatalogDatabase : RoomDatabase() {
    abstract fun catalog(): OfflineCatalogDao

    companion object {
        @Volatile private var instance: OfflineCatalogDatabase? = null

        fun get(context: Context): OfflineCatalogDatabase = instance ?: synchronized(this) {
            instance ?: Room.databaseBuilder(
                context.applicationContext,
                OfflineCatalogDatabase::class.java,
                "lanflix-offline.db"
            ).build().also { instance = it }
        }
    }
}
