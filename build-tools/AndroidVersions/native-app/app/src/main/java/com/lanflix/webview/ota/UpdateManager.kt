package com.lanflix.webview.ota

import android.app.AlertDialog
import android.content.Context
import android.content.SharedPreferences
import androidx.work.Constraints
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.NetworkType
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.Worker
import androidx.work.WorkerParameters
import java.util.concurrent.TimeUnit

class UpdateManager(private val context: Context) {
    
    private val prefs: SharedPreferences = context.getSharedPreferences("update_prefs", Context.MODE_PRIVATE)
    private val updateChecker = UpdateChecker(context)
    private val updateInstaller = UpdateInstaller(context)
    
    companion object {
        private const val WORK_NAME = "update_check_work"
        private const val PREF_LAST_CHECK = "last_update_check"
        private const val PREF_SKIP_VERSION = "skip_version"
        private const val CHECK_INTERVAL_HOURS = 6L
    }
    
    fun schedulePeriodicUpdateCheck() {
        val constraints = Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()
        
        val updateWorkRequest = PeriodicWorkRequestBuilder<UpdateCheckWorker>(
            CHECK_INTERVAL_HOURS, TimeUnit.HOURS
        )
            .setConstraints(constraints)
            .build()
        
        WorkManager.getInstance(context).enqueueUniquePeriodicWork(
            WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            updateWorkRequest
        )
    }
    
    suspend fun checkForUpdateManually(showNoUpdateDialog: Boolean = true): Boolean {
        return try {
            val response = updateChecker.checkForUpdate()
            
            if (response.hasUpdate && response.updateInfo != null) {
                val updateInfo = response.updateInfo
                
                // Check if user has skipped this version
                val skippedVersion = prefs.getInt(PREF_SKIP_VERSION, 0)
                if (!updateInfo.mandatory && updateInfo.versionCode == skippedVersion) {
                    return false
                }
                
                showUpdateDialog(updateInfo)
                true
            } else {
                if (showNoUpdateDialog) {
                    showNoUpdateDialog()
                }
                false
            }
        } catch (e: Exception) {
            e.printStackTrace()
            false
        }
    }
    
    private fun showUpdateDialog(updateInfo: UpdateInfo) {
        val context = this.context
        
        if (updateInfo.mandatory) {
            // For mandatory updates, go directly to update screen
            launchUpdateScreen(updateInfo)
        } else {
            // For optional updates, show choice dialog first
            val message = buildString {
                append("A new version (${updateInfo.versionName}) is available!\n\n")
                if (!updateInfo.releaseNotes.isNullOrEmpty()) {
                    append("What's new:\n${updateInfo.releaseNotes}\n\n")
                }
                if (updateInfo.fileSize > 0) {
                    val sizeMB = updateInfo.fileSize / (1024 * 1024)
                    append("Download size: ${sizeMB}MB")
                }
            }
            
            val builder = AlertDialog.Builder(context)
                .setTitle("Update Available")
                .setMessage(message)
                .setCancelable(true)
            
            // Update button - launches update screen
            builder.setPositiveButton("Update Now") { _, _ ->
                launchUpdateScreen(updateInfo)
            }
            
            // Skip button
            builder.setNegativeButton("Skip This Version") { _, _ ->
                prefs.edit()
                    .putInt(PREF_SKIP_VERSION, updateInfo.versionCode)
                    .apply()
            }
            
            builder.setNeutralButton("Later") { _, _ ->
                // Do nothing, just dismiss
            }
            
            builder.show()
        }
    }
    
    private fun launchUpdateScreen(updateInfo: UpdateInfo) {
        try {
            val updateActivityClass = Class.forName("com.lanflix.webview.UpdateActivity")
            val startMethod = updateActivityClass.getMethod("start", Context::class.java, UpdateInfo::class.java)
            startMethod.invoke(null, context, updateInfo)
        } catch (e: Exception) {
            e.printStackTrace()
            // Fallback to service-based download if UpdateActivity is not available
            UpdateService.startDownload(context, updateInfo)
        }
    }
    
    private fun showNoUpdateDialog() {
        AlertDialog.Builder(context)
            .setTitle("No Updates")
            .setMessage("You're running the latest version of Lanflix!")
            .setPositiveButton("OK") { _, _ -> }
            .show()
    }
    
    fun getLastCheckTime(): Long {
        return prefs.getLong(PREF_LAST_CHECK, 0)
    }
    
    private fun updateLastCheckTime() {
        prefs.edit()
            .putLong(PREF_LAST_CHECK, System.currentTimeMillis())
            .apply()
    }
    
    // Worker class for background update checks
    class UpdateCheckWorker(
        context: Context,
        params: WorkerParameters
    ) : Worker(context, params) {
        
        override fun doWork(): Result {
            return try {
                val updateManager = UpdateManager(applicationContext)
                
                // This is a simplified version for background checks
                // In a real implementation, you might want to use coroutines properly
                val updateChecker = UpdateChecker(applicationContext)
                
                // Note: This is a blocking call in a worker thread
                // For production, consider using a different approach
                updateManager.updateLastCheckTime()
                
                Result.success()
            } catch (e: Exception) {
                e.printStackTrace()
                Result.retry()
            }
        }
    }
}