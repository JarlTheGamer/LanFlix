package com.lanflix.webview.ota

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import com.google.gson.Gson
import com.lanflix.webview.R
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

class UpdateChecker(private val context: Context) {
    
    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()
    
    private val gson = Gson()
    
    // Get server URL from resources
    private val serverUrl = context.getString(R.string.update_server_url)
    private val updateEndpoint = serverUrl + context.getString(R.string.update_endpoint)
    
    suspend fun checkForUpdate(): UpdateResponse = withContext(Dispatchers.IO) {
        try {
            val currentVersion = getCurrentVersionInfo()
            val serverHost = ServerManager.getSavedServer(context).trimEnd('/')
            val endpoint = "$serverHost/api/app/update-check?currentVersion=${currentVersion.versionCode}&platform=android"
            
            val request = Request.Builder()
                .url(endpoint)
                .addHeader("User-Agent", "LanflixApp/${currentVersion.versionName}")
                .build()
            
            val response = httpClient.newCall(request).execute()
            
            if (response.isSuccessful) {
                val responseBody = response.body?.string()
                if (!responseBody.isNullOrEmpty()) {
                    val jsonObject = gson.fromJson(responseBody, com.google.gson.JsonObject::class.java)
                    val hasUpdate = jsonObject.get("hasUpdate")?.asBoolean ?: false
                    
                    if (hasUpdate) {
                        val updateInfo = UpdateInfo(
                            versionName = jsonObject.get("versionName")?.asString ?: "1.0.0",
                            versionCode = jsonObject.get("versionCode")?.asInt ?: (currentVersion.versionCode + 1),
                            downloadUrl = jsonObject.get("downloadUrl")?.asString ?: "",
                            releaseNotes = jsonObject.get("releaseNotes")?.asString,
                            mandatory = jsonObject.get("mandatory")?.asBoolean ?: false,
                            fileSize = jsonObject.get("fileSize")?.asLong ?: 0L,
                            checksum = jsonObject.get("checksum")?.asString
                        )
                        return@withContext UpdateResponse(hasUpdate = true, updateInfo = updateInfo)
                    }
                }
            }
            
            UpdateResponse(hasUpdate = false, updateInfo = null)
            
        } catch (e: Exception) {
            e.printStackTrace()
            UpdateResponse(hasUpdate = false, updateInfo = null)
        }
    }
    
    private fun getCurrentVersionInfo(): UpdateInfo {
        return try {
            val packageInfo = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                context.packageManager.getPackageInfo(
                    context.packageName,
                    PackageManager.PackageInfoFlags.of(0)
                )
            } else {
                @Suppress("DEPRECATION")
                context.packageManager.getPackageInfo(context.packageName, 0)
            }
            
            UpdateInfo(
                versionName = packageInfo.versionName ?: "1.0.0",
                versionCode = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                    packageInfo.longVersionCode.toInt()
                } else {
                    @Suppress("DEPRECATION")
                    packageInfo.versionCode
                },
                downloadUrl = "",
                releaseNotes = null,
                mandatory = false
            )
        } catch (e: Exception) {
            UpdateInfo(
                versionName = "1.0.0",
                versionCode = 1,
                downloadUrl = "",
                releaseNotes = null,
                mandatory = false
            )
        }
    }
}