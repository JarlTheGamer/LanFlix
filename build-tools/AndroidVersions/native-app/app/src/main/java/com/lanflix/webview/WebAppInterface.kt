package com.lanflix.webview

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.webkit.JavascriptInterface
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.ota.UpdateManager
import kotlinx.coroutines.launch
import org.json.JSONObject

class WebAppInterface(private val context: Context, private val updateManager: UpdateManager) {
    
    @JavascriptInterface
    fun getAppVersion(): String {
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
            packageInfo.versionName ?: "2.0.0"
        } catch (e: Exception) {
            "2.0.0"
        }
    }
    
    @JavascriptInterface
    fun getVersionCode(): Int {
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
            
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                packageInfo.longVersionCode.toInt()
            } else {
                @Suppress("DEPRECATION")
                packageInfo.versionCode
            }
        } catch (e: Exception) {
            2
        }
    }
    
    @JavascriptInterface
    fun triggerUpdate() {
        if (context is MainActivity) {
            context.lifecycleScope.launch {
                try {
                    val hasUpdate = updateManager.checkForUpdateManually(showNoUpdateDialog = false)
                    if (!hasUpdate) {
                        // If no update found via OTA, show message
                        context.runOnUiThread {
                            android.widget.Toast.makeText(
                                context, 
                                "No updates available", 
                                android.widget.Toast.LENGTH_SHORT
                            ).show()
                        }
                    }
                } catch (e: Exception) {
                    context.runOnUiThread {
                        android.widget.Toast.makeText(
                            context, 
                            "Failed to check for updates", 
                            android.widget.Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            }
        }
    }

    @JavascriptInterface
    fun triggerUpdateWithInfo(updateJson: String?) {
        if (context !is MainActivity) {
            triggerUpdate()
            return
        }

        if (updateJson.isNullOrBlank()) {
            triggerUpdate()
            return
        }

        context.lifecycleScope.launch {
            try {
                val payload = JSONObject(updateJson)
                val versionName = payload.optString("versionName")
                val downloadUrl = payload.optString("downloadUrl")

                if (versionName.isBlank() || downloadUrl.isBlank()) {
                    triggerUpdate()
                    return@launch
                }

                var versionCode = payload.optInt("versionCode")
                if (versionCode <= 0) {
                    versionCode = deriveVersionCode(versionName)
                }

                val updateInfo = UpdateInfo(
                    versionName = versionName,
                    versionCode = versionCode,
                    downloadUrl = downloadUrl,
                    releaseNotes = payload.optString("releaseNotes").takeIf { it.isNotBlank() },
                    mandatory = payload.optBoolean("mandatory", false),
                    fileSize = payload.optLong("fileSize", 0L),
                    checksum = payload.optString("checksum").takeIf { it.isNotBlank() }
                )

                updateManager.startUpdateFromWeb(updateInfo)

                context.runOnUiThread {
                    android.widget.Toast.makeText(
                        context,
                        "Preparing update...",
                        android.widget.Toast.LENGTH_SHORT
                    ).show()
                }
            } catch (e: Exception) {
                context.runOnUiThread {
                    android.widget.Toast.makeText(
                        context,
                        "Failed to start native update",
                        android.widget.Toast.LENGTH_SHORT
                    ).show()
                }

                triggerUpdate()
            }
        }
    }

    private fun deriveVersionCode(versionName: String): Int {
        return runCatching {
            if (versionName == "4.0.0") {
                return@runCatching 4
            }

            val parts = versionName.split('.')
            val major = parts.getOrNull(0)?.toIntOrNull() ?: 0
            val minor = parts.getOrNull(1)?.toIntOrNull() ?: 0

            major * 10 + minor
        }.getOrDefault(0)
    }
    
    @JavascriptInterface
    fun isNativeApp(): Boolean {
        return true
    }
    
    @JavascriptInterface
    fun getDeviceInfo(): String {
        return "${Build.MANUFACTURER} ${Build.MODEL} (Android ${Build.VERSION.RELEASE})"
    }
}