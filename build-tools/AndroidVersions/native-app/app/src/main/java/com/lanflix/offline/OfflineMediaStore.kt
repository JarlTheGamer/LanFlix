package com.lanflix.offline

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.models.ContentItem
import com.lanflix.api.DiscoveryPage
import java.io.File

/** Room-backed catalog and app-private media store for server-independent playback. */
class OfflineMediaStore(context: Context) {
    private val appContext = context.applicationContext
    private val gson = Gson()
    private val dao = OfflineCatalogDatabase.get(appContext).catalog()
    private val legacyCatalogFile = File(appContext.filesDir, "offline-catalog.json")
    private val discoveryCacheFile = File(appContext.filesDir, "discovery-cache.json")
    private val mediaDir = File(appContext.filesDir, "offline-media").apply { mkdirs() }

    fun readDiscoveryPage(): DiscoveryPage? {
        if (!discoveryCacheFile.isFile) return null
        return runCatching { gson.fromJson(discoveryCacheFile.readText(), DiscoveryPage::class.java) }.getOrNull()
    }

    fun cacheDiscoveryPage(page: DiscoveryPage) {
        runCatching { discoveryCacheFile.writeText(gson.toJson(page)) }
    }

    suspend fun readCatalog(): List<ContentItem> {
        migrateLegacyCatalogIfNeeded()
        return dao.getAll().mapNotNull { entity ->
            runCatching { gson.fromJson(entity.payloadJson, ContentItem::class.java) }
                .getOrNull()
                ?.copy(localFilePath = entity.localFilePath)
        }
    }

    suspend fun cacheLibrary(items: List<ContentItem>) {
        if (items.isEmpty()) return
        val existing = dao.getAll().associateBy { it.mediaKey }
        val now = System.currentTimeMillis()
        dao.upsertAll(items.map { item ->
            val key = mediaKey(item)
            val preservedPath = existing[key]?.localFilePath?.takeIf { File(it).isFile }
            entity(item.copy(localFilePath = preservedPath), now)
        })
    }

    suspend fun saveDownloaded(item: ContentItem, sourceFile: File): ContentItem {
        val extension = sourceFile.extension.takeIf { it.isNotBlank() }?.let { ".$it" }.orEmpty()
        val target = File(mediaDir, "${safeName(item.type)}-${item.id}-${safeName(item.displayTitle)}$extension")
        sourceFile.copyTo(target, overwrite = true)
        val saved = item.copy(localFilePath = target.absolutePath)
        dao.upsertAll(listOf(entity(saved, System.currentTimeMillis())))
        return saved
    }

    fun localFile(item: ContentItem): File? = item.localFilePath?.let(::File)?.takeIf { it.isFile }

    suspend fun removeDownload(item: ContentItem) {
        localFile(item)?.delete()
        val key = mediaKey(item)
        dao.get(key)?.let { dao.upsertAll(listOf(it.copy(localFilePath = null, updatedAtUtc = System.currentTimeMillis()))) }
    }

    suspend fun clearMetadataCache() = dao.clearMetadataOnly()

    private suspend fun migrateLegacyCatalogIfNeeded() {
        if (!legacyCatalogFile.isFile || dao.getAll().isNotEmpty()) return
        val type = object : TypeToken<List<ContentItem>>() {}.type
        val legacy = runCatching {
            gson.fromJson<List<ContentItem>>(legacyCatalogFile.readText(), type) ?: emptyList()
        }.getOrDefault(emptyList())
        if (legacy.isNotEmpty()) dao.upsertAll(legacy.map { entity(it, System.currentTimeMillis()) })
        legacyCatalogFile.renameTo(File(legacyCatalogFile.parentFile, "${legacyCatalogFile.name}.migrated"))
    }

    private fun entity(item: ContentItem, updatedAt: Long) = OfflineCatalogEntity(
        mediaKey = mediaKey(item),
        payloadJson = gson.toJson(item.copy(localFilePath = null)),
        localFilePath = item.localFilePath,
        updatedAtUtc = updatedAt
    )

    private fun mediaKey(item: ContentItem): String = "${item.type?.lowercase()}:${item.id}"

    private fun safeName(value: String?): String = (value ?: "video")
        .replace(Regex("[^A-Za-z0-9._-]"), "-").take(80)
}
