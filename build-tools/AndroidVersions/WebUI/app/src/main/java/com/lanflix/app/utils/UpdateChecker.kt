package com.lanflix.app.utils

import android.app.AlertDialog
import android.content.Context
import android.content.Intent
import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.net.URL

class UpdateChecker(private val context: Context) {
    
    companion object {
        private const val GITHUB_API_URL = "https://api.github.com/repos/JarlTheGamer/Applications./releases/latest"
        private const val PREF_LAST_CHECK = "last_update_check"
        private const val PREF_SKIP_VERSION = "skip_version"
        private const val CHECK_INTERVAL = 24 * 60 * 60 * 1000L // 24 hours
    }
    
    private val prefs = context.getSharedPreferences("lanflix_prefs", Context.MODE_PRIVATE)
    
    suspend fun checkForUpdates(force: Boolean = false): UpdateInfo? {
        return withContext(Dispatchers.IO) {
            try {
                // Check if we should skip this check
                if (!force) {
                    val lastCheck = prefs.getLong(PREF_LAST_CHECK, 0)
                    val now = System.currentTimeMillis()
                    if (now - lastCheck < CHECK_INTERVAL) {
                        return@withContext null
                    }
                }
                
                // Fetch latest release from GitHub
                val response = URL(GITHUB_API_URL).readText()
                val json = JSONObject(response)
                
                val latestVersion = json.getString("tag_name").removePrefix("v")
                val currentVersion = getCurrentVersion()
                
                // Save last check time
                prefs.edit().putLong(PREF_LAST_CHECK, System.currentTimeMillis()).apply()
                
                // Check if update is available
                if (isNewerVersion(latestVersion, currentVersion)) {
                    val skipVersion = prefs.getString(PREF_SKIP_VERSION, "")
                    if (skipVersion == latestVersion && !force) {
                        return@withContext null
                    }
                    
                    val downloadUrl = json.getJSONArray("assets")
                        .let { assets ->
                            for (i in 0 until assets.length()) {
                                val asset = assets.getJSONObject(i)
                                val name = asset.getString("name")
                                if (name.endsWith(".apk")) {
                                    return@let asset.getString("browser_download_url")
                                }
                            }
                            null
                        }
                    
                    if (downloadUrl != null) {
                        return@withContext UpdateInfo(
                            version = latestVersion,
                            downloadUrl = downloadUrl,
                            releaseNotes = json.optString("body", ""),
                            publishedAt = json.getString("published_at")
                        )
                    }
                }
                
                null
            } catch (e: Exception) {
                e.printStackTrace()
                null
            }
        }
    }
    
    fun showUpdateDialog(updateInfo: UpdateInfo) {
        AlertDialog.Builder(context)
            .setTitle("Update Available")
            .setMessage(
                "Version ${updateInfo.version} is available!\n\n" +
                "Current version: ${getCurrentVersion()}\n\n" +
                "What's new:\n${updateInfo.releaseNotes.take(200)}"
            )
            .setPositiveButton("Download") { _, _ ->
                downloadUpdate(updateInfo.downloadUrl)
            }
            .setNegativeButton("Later", null)
            .setNeutralButton("Skip This Version") { _, _ ->
                skipVersion(updateInfo.version)
            }
            .show()
    }
    
    private fun downloadUpdate(url: String) {
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse(url))
        context.startActivity(intent)
    }
    
    private fun skipVersion(version: String) {
        prefs.edit().putString(PREF_SKIP_VERSION, version).apply()
    }
    
    private fun getCurrentVersion(): String {
        return try {
            val packageInfo = context.packageManager.getPackageInfo(context.packageName, 0)
            packageInfo.versionName
        } catch (e: Exception) {
            "1.0.0"
        }
    }
    
    private fun isNewerVersion(latest: String, current: String): Boolean {
        val latestParts = latest.split(".").map { it.toIntOrNull() ?: 0 }
        val currentParts = current.split(".").map { it.toIntOrNull() ?: 0 }
        
        for (i in 0 until maxOf(latestParts.size, currentParts.size)) {
            val latestPart = latestParts.getOrNull(i) ?: 0
            val currentPart = currentParts.getOrNull(i) ?: 0
            
            if (latestPart > currentPart) return true
            if (latestPart < currentPart) return false
        }
        
        return false
    }
    
    data class UpdateInfo(
        val version: String,
        val downloadUrl: String,
        val releaseNotes: String,
        val publishedAt: String
    )
}
