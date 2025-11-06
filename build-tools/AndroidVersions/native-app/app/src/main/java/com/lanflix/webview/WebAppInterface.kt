package com.lanflix.webview

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.webkit.JavascriptInterface
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.ota.UpdateManager
import kotlinx.coroutines.launch

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
    fun isNativeApp(): Boolean {
        return true
    }
    
    @JavascriptInterface
    fun getDeviceInfo(): String {
        return "${Build.MANUFACTURER} ${Build.MODEL} (Android ${Build.VERSION.RELEASE})"
    }
}