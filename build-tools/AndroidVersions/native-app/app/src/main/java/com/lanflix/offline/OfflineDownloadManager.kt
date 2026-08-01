package com.lanflix.offline

import android.content.Context
import com.lanflix.models.ContentItem
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.util.concurrent.TimeUnit

/** Downloads a server-available movie/episode to app-private storage for later playback. */
class OfflineDownloadManager(context: Context) {
    private val appContext = context.applicationContext
    private val store = OfflineMediaStore(context)
    private val client = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()

    suspend fun download(item: ContentItem): ContentItem? = withContext(Dispatchers.IO) {
        if (!ServerManager.isOnline) return@withContext null
        val kind = if (item.type.equals("episode", true)) "episode" else "movie"
        val endpoint = "$kind/${item.id}/file"
        val request = Request.Builder()
            .url("${ServerManager.activeServerUrl}/api/stream/$endpoint")
            .get()
            .build()
        runCatching {
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext null
                val body = response.body ?: return@withContext null
                val temp = File.createTempFile("lanflix-download-", ".${extension(item)}", appContext.cacheDir)
                body.byteStream().use { input -> temp.outputStream().use { output -> input.copyTo(output) } }
                store.saveDownloaded(item, temp).also { temp.delete() }
            }
        }.getOrNull()
    }

    private fun extension(item: ContentItem): String = if (item.type.equals("episode", true)) "mp4" else "mp4"
}
