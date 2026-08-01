package com.lanflix.offline

import android.content.Context
import com.lanflix.models.ContentItem
import com.lanflix.webview.ServerManager
import com.lanflix.api.LanflixApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.security.MessageDigest
import java.util.concurrent.TimeUnit

/** Downloads a server-available movie/episode to app-private storage for later playback. */
class OfflineDownloadManager(context: Context) {
    private val appContext = context.applicationContext
    private val store = OfflineMediaStore(context)
    private val api = LanflixApiClient(context)
    private val client = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()

    suspend fun download(item: ContentItem): ContentItem? = withContext(Dispatchers.IO) {
        if (!ServerManager.isOnline) return@withContext null
        val manifest = api.getDownloadManifest(item) ?: return@withContext null
        val request = Request.Builder()
            .url("${ServerManager.activeServerUrl}${manifest.downloadUrl}")
            .apply { api.sessions.accessToken?.let { header("Authorization", "Bearer $it") } }
            .get()
            .build()
        var temp: File? = null
        try {
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext null
                val body = response.body ?: return@withContext null
                temp = File.createTempFile("lanflix-download-", ".${extension(item)}", appContext.cacheDir)
                body.byteStream().use { input -> temp!!.outputStream().use { output -> input.copyTo(output) } }
                if (temp!!.length() != manifest.fileSize || sha256(temp!!) != manifest.sha256.lowercase()) return@withContext null
                store.saveDownloaded(item, temp!!)
            }
        } catch (_: Exception) {
            null
        } finally {
            temp?.delete()
        }
    }

    private fun extension(item: ContentItem): String = if (item.type.equals("episode", true)) "mp4" else "mp4"

    private fun sha256(file: File): String {
        val digest = MessageDigest.getInstance("SHA-256")
        file.inputStream().use { input ->
            val buffer = ByteArray(128 * 1024)
            while (true) {
                val read = input.read(buffer)
                if (read < 0) break
                digest.update(buffer, 0, read)
            }
        }
        return digest.digest().joinToString("") { "%02x".format(it) }
    }
}
