package com.lanflix.offline

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.models.ContentItem
import java.io.File

/** Persistent catalog and local media files for server-independent playback. */
class OfflineMediaStore(context: Context) {
    private val gson = Gson()
    private val catalogFile = File(context.applicationContext.filesDir, "offline-catalog.json")
    private val mediaDir = File(context.applicationContext.filesDir, "offline-media").apply { mkdirs() }

    @Synchronized
    fun readCatalog(): List<ContentItem> = if (!catalogFile.exists()) emptyList() else runCatching {
        val type = object : TypeToken<List<ContentItem>>() {}.type
        gson.fromJson<List<ContentItem>>(catalogFile.readText(), type) ?: emptyList()
    }.getOrDefault(emptyList())

    @Synchronized
    fun cacheLibrary(items: List<ContentItem>) {
        writeCatalog((items + readCatalog().filter { it.isOfflinePlayable }).distinctBy { it.id })
    }

    @Synchronized
    fun saveDownloaded(item: ContentItem, sourceFile: File): ContentItem {
        val target = File(mediaDir, "${safeName(item.type)}-${item.id}-${safeName(item.displayTitle)}${sourceFile.extension}")
        sourceFile.copyTo(target, overwrite = true)
        val saved = item.copy(localFilePath = target.absolutePath)
        writeCatalog((readCatalog().filterNot { it.id == item.id } + saved).distinctBy { it.id })
        return saved
    }

    fun localFile(item: ContentItem): File? = item.localFilePath?.let(::File)?.takeIf { it.isFile }

    private fun writeCatalog(items: List<ContentItem>) {
        val temp = File(catalogFile.parentFile, "${catalogFile.name}.tmp")
        temp.writeText(gson.toJson(items))
        if (!temp.renameTo(catalogFile)) {
            catalogFile.delete()
            temp.renameTo(catalogFile)
        }
    }

    private fun safeName(value: String?): String = (value ?: "video")
        .replace(Regex("[^A-Za-z0-9._-]"), "-").take(80)
}
