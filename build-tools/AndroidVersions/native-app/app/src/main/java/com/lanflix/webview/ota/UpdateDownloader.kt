package com.lanflix.webview.ota

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.io.FileOutputStream
import java.security.MessageDigest
import java.util.concurrent.TimeUnit

sealed class DownloadState {
    object Idle : DownloadState()
    object Starting : DownloadState()
    data class Progress(val bytesDownloaded: Long, val totalBytes: Long, val percentage: Int) : DownloadState()
    data class Success(val file: File) : DownloadState()
    data class Error(val message: String, val exception: Throwable? = null) : DownloadState()
}

class UpdateDownloader(private val context: Context) {
    
    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()
    
    fun downloadUpdate(updateInfo: UpdateInfo): Flow<DownloadState> = flow {
        try {
            emit(DownloadState.Starting)
            
            val request = Request.Builder()
                .url(updateInfo.downloadUrl)
                .build()
            
            val response = httpClient.newCall(request).execute()
            
            if (!response.isSuccessful) {
                emit(DownloadState.Error("Download failed: ${response.code}"))
                return@flow
            }
            
            val body = response.body
            if (body == null) {
                emit(DownloadState.Error("Empty response body"))
                return@flow
            }
            
            val totalBytes = body.contentLength()
            val inputStream = body.byteStream()
            
            // Create download directory
            val downloadDir = File(context.cacheDir, "apk")
            if (!downloadDir.exists()) {
                downloadDir.mkdirs()
            }
            
            val apkFile = File(downloadDir, "lanflix-update-${updateInfo.versionName}.apk")
            
            // Delete existing file if it exists
            if (apkFile.exists()) {
                apkFile.delete()
            }
            
            val outputStream = FileOutputStream(apkFile)
            val buffer = ByteArray(8192)
            var bytesDownloaded = 0L
            var bytesRead: Int
            
            while (inputStream.read(buffer).also { bytesRead = it } != -1) {
                outputStream.write(buffer, 0, bytesRead)
                bytesDownloaded += bytesRead
                
                val percentage = if (totalBytes > 0) {
                    ((bytesDownloaded * 100) / totalBytes).toInt()
                } else {
                    0
                }
                
                emit(DownloadState.Progress(bytesDownloaded, totalBytes, percentage))
            }
            
            outputStream.close()
            inputStream.close()
            
            // Verify checksum if provided
            if (!updateInfo.checksum.isNullOrEmpty()) {
                val fileChecksum = calculateChecksum(apkFile)
                if (fileChecksum != updateInfo.checksum) {
                    apkFile.delete()
                    emit(DownloadState.Error("Checksum verification failed"))
                    return@flow
                }
            }
            
            emit(DownloadState.Success(apkFile))
            
        } catch (e: Exception) {
            emit(DownloadState.Error("Download error: ${e.message}", e))
        }
    }.flowOn(Dispatchers.IO)
    
    private fun calculateChecksum(file: File): String {
        val digest = MessageDigest.getInstance("SHA-256")
        val inputStream = file.inputStream()
        val buffer = ByteArray(8192)
        var bytesRead: Int
        
        while (inputStream.read(buffer).also { bytesRead = it } != -1) {
            digest.update(buffer, 0, bytesRead)
        }
        
        inputStream.close()
        
        return digest.digest().joinToString("") { "%02x".format(it) }
    }
}