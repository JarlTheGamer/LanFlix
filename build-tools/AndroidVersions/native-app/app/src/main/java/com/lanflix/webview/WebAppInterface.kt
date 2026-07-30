package com.lanflix.webview

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.webkit.JavascriptInterface
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.ota.UpdateInfo
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
            context.evaluateJavascriptInWebView("window.appUpdater && window.appUpdater.checkForUpdates(true);")
        }
    }

    @JavascriptInterface
    fun triggerUpdateWithInfo(updateJson: String?) {
        triggerUpdate()
    }

    private fun deriveVersionCode(versionName: String): Int {
        return runCatching {
            val parts = versionName.split('.')
            val major = parts.getOrNull(0)?.toIntOrNull() ?: 1
            val minor = parts.getOrNull(1)?.toIntOrNull() ?: 2
            val patch = parts.getOrNull(2)?.toIntOrNull() ?: 8

            major * 100 + minor * 10 + patch
        }.getOrDefault(28)
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