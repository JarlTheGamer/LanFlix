package com.lanflix.webview.ota

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat
import com.lanflix.webview.MainActivity
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch

class UpdateService : Service() {
    
    private val serviceScope = CoroutineScope(Dispatchers.Main + Job())
    private lateinit var notificationManager: NotificationManager
    private lateinit var updateDownloader: UpdateDownloader
    private lateinit var updateInstaller: UpdateInstaller
    
    companion object {
        private const val NOTIFICATION_ID = 1001
        private const val CHANNEL_ID = "update_channel"
        const val ACTION_DOWNLOAD_UPDATE = "download_update"
        const val EXTRA_UPDATE_INFO = "update_info"
        
        fun startDownload(context: Context, updateInfo: UpdateInfo) {
            val intent = Intent(context, UpdateService::class.java).apply {
                action = ACTION_DOWNLOAD_UPDATE
                putExtra(EXTRA_UPDATE_INFO, updateInfo)
            }
            context.startForegroundService(intent)
        }
    }
    
    override fun onCreate() {
        super.onCreate()
        notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        updateDownloader = UpdateDownloader(this)
        updateInstaller = UpdateInstaller(this)
        
        createNotificationChannel()
    }
    
    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_DOWNLOAD_UPDATE -> {
                val updateInfo = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    intent.getSerializableExtra(EXTRA_UPDATE_INFO, UpdateInfo::class.java)
                } else {
                    @Suppress("DEPRECATION")
                    intent.getSerializableExtra(EXTRA_UPDATE_INFO) as? UpdateInfo
                }
                
                if (updateInfo != null) {
                    startForeground(NOTIFICATION_ID, createDownloadNotification(0))
                    downloadUpdate(updateInfo)
                } else {
                    stopSelf()
                }
            }
        }
        
        return START_NOT_STICKY
    }
    
    private fun downloadUpdate(updateInfo: UpdateInfo) {
        serviceScope.launch {
            updateDownloader.downloadUpdate(updateInfo).collect { state ->
                when (state) {
                    is DownloadState.Starting -> {
                        updateNotification(createDownloadNotification(0, "Starting download..."))
                    }
                    
                    is DownloadState.Progress -> {
                        val message = "Downloading update... ${state.percentage}%"
                        updateNotification(createDownloadNotification(state.percentage, message))
                    }
                    
                    is DownloadState.Success -> {
                        updateNotification(createInstallNotification(state.file))
                        
                        // Auto-install if possible
                        if (updateInstaller.canInstallPackages()) {
                            updateInstaller.installUpdate(state.file)
                            stopSelf()
                        }
                    }
                    
                    is DownloadState.Error -> {
                        updateNotification(createErrorNotification(state.message))
                        stopSelf()
                    }
                    
                    else -> {}
                }
            }
        }
    }
    
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "App Updates",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "Notifications for app updates"
                setSound(null, null)
            }
            notificationManager.createNotificationChannel(channel)
        }
    }
    
    private fun createDownloadNotification(progress: Int, message: String = "Downloading update..."): Notification {
        val intent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Lanflix Update")
            .setContentText(message)
            .setSmallIcon(android.R.drawable.stat_sys_download)
            .setProgress(100, progress, progress == 0)
            .setOngoing(true)
            .setContentIntent(pendingIntent)
            .build()
    }
    
    private fun createInstallNotification(apkFile: java.io.File): Notification {
        val installIntent = Intent(Intent.ACTION_VIEW).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_GRANT_READ_URI_PERMISSION
            
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                val apkUri = androidx.core.content.FileProvider.getUriForFile(
                    this@UpdateService,
                    "${packageName}.fileprovider",
                    apkFile
                )
                setDataAndType(apkUri, "application/vnd.android.package-archive")
            } else {
                setDataAndType(android.net.Uri.fromFile(apkFile), "application/vnd.android.package-archive")
            }
        }
        
        val pendingIntent = PendingIntent.getActivity(
            this, 0, installIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Lanflix Update Ready")
            .setContentText("Tap to install the update")
            .setSmallIcon(android.R.drawable.stat_sys_download_done)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .addAction(
                android.R.drawable.ic_input_add,
                "Install",
                pendingIntent
            )
            .build()
    }
    
    private fun createErrorNotification(error: String): Notification {
        val intent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Update Failed")
            .setContentText("Failed to download update: $error")
            .setSmallIcon(android.R.drawable.stat_notify_error)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .build()
    }
    
    private fun updateNotification(notification: Notification) {
        notificationManager.notify(NOTIFICATION_ID, notification)
    }
    
    override fun onDestroy() {
        super.onDestroy()
        serviceScope.cancel()
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
}
