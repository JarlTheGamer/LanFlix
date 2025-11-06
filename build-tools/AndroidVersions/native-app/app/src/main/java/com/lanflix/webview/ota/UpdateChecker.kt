package com.lanflix.webview.ota

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import com.google.gson.Gson
import com.lanflix.webview.R
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
            
            val request = Request.Builder()
                .url("$updateEndpoint?currentVersion=${currentVersion.versionCode}&platform=android")
                .addHeader("User-Agent", "LanflixApp/${currentVersion.versionName}")
                .build()
            
            val response = httpClient.newCall(request).execute()
            
            if (response.isSuccessful) {
                val responseBody = response.body?.string()
                if (!responseBody.isNullOrEmpty()) {
                    val updateInfo = gson.fromJson(responseBody, UpdateInfo::class.java)
                    
                    // Check if update is available
                    val hasUpdate = updateInfo.versionCode > currentVersion.versionCode
                    
                    return@withContext UpdateResponse(
                        hasUpdate = hasUpdate,
                        updateInfo = if (hasUpdate) updateInfo else null
                    )
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