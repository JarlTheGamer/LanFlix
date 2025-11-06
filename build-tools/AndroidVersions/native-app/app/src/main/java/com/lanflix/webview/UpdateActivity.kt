package com.lanflix.webview

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.databinding.ActivityUpdateBinding
import com.lanflix.webview.ota.DownloadState
import com.lanflix.webview.ota.UpdateDownloader
import com.lanflix.webview.ota.UpdateInfo
import com.lanflix.webview.ota.UpdateInstaller
import kotlinx.coroutines.launch
import java.text.DecimalFormat

class UpdateActivity : AppCompatActivity() {

    private lateinit var binding: ActivityUpdateBinding
    private lateinit var updateDownloader: UpdateDownloader
    private lateinit var updateInstaller: UpdateInstaller
    private var updateInfo: UpdateInfo? = null
    private var downloadedApkFile: java.io.File? = null

    companion object {
        private const val EXTRA_UPDATE_INFO = "update_info"

        fun start(context: Context, updateInfo: UpdateInfo) {
            val intent = Intent(context, UpdateActivity::class.java).apply {
                putExtra(EXTRA_UPDATE_INFO, updateInfo)
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
            }
            context.startActivity(intent)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityUpdateBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Hide system UI for immersive experience
        hideSystemUI()

        updateDownloader = UpdateDownloader(this)
        updateInstaller = UpdateInstaller(this)

        // Get update info from intent
        updateInfo = if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU) {
            intent.getSerializableExtra(EXTRA_UPDATE_INFO, UpdateInfo::class.java)
        } else {
            @Suppress("DEPRECATION")
            intent.getSerializableExtra(EXTRA_UPDATE_INFO) as? UpdateInfo
        }

        if (updateInfo == null) {
            finish()
            return
        }

        setupUI()
        startDownload()
    }

    private fun hideSystemUI() {
        window.decorView.systemUiVisibility = (
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
            or View.SYSTEM_UI_FLAG_LAYOUT_STABLE
            or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
            or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
            or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
            or View.SYSTEM_UI_FLAG_FULLSCREEN
        )
    }

    private fun setupUI() {
        val info = updateInfo ?: return

        binding.apply {
            versionTextView.text = "Version ${info.versionName}"
            
            // Show release notes if available
            if (!info.releaseNotes.isNullOrEmpty()) {
                releaseNotesTextView.text = info.releaseNotes
                releaseNotesScrollView.visibility = View.VISIBLE
            }

            actionButton.setOnClickListener {
                when (actionButton.text.toString()) {
                    "Install Update" -> installUpdate()
                    "Restart App" -> restartApp()
                    "Try Again" -> startDownload()
                }
            }
        }
    }

    private fun startDownload() {
        val info = updateInfo ?: return

        binding.apply {
            statusTextView.text = "Downloading update..."
            progressBar.progress = 0
            progressTextView.text = "0%"
            downloadInfoTextView.text = "Preparing download..."
            actionButton.visibility = View.GONE
            loadingProgressBar.visibility = View.VISIBLE
        }

        lifecycleScope.launch {
            updateDownloader.downloadUpdate(info).collect { state ->
                handleDownloadState(state)
            }
        }
    }

    private fun handleDownloadState(state: DownloadState) {
        when (state) {
            is DownloadState.Starting -> {
                binding.apply {
                    statusTextView.text = "Starting download..."
                    loadingProgressBar.visibility = View.VISIBLE
                }
            }

            is DownloadState.Progress -> {
                binding.apply {
                    statusTextView.text = "Downloading update..."
                    progressBar.progress = state.percentage
                    progressTextView.text = "${state.percentage}%"
                    
                    val downloadedMB = state.bytesDownloaded / (1024 * 1024f)
                    val totalMB = state.totalBytes / (1024 * 1024f)
                    val format = DecimalFormat("#.#")
                    
                    downloadInfoTextView.text = if (state.totalBytes > 0) {
                        "${format.format(downloadedMB)} MB / ${format.format(totalMB)} MB"
                    } else {
                        "${format.format(downloadedMB)} MB downloaded"
                    }
                    
                    loadingProgressBar.visibility = View.GONE
                }
            }

            is DownloadState.Success -> {
                downloadedApkFile = state.file
                binding.apply {
                    statusTextView.text = "Download complete!"
                    progressBar.progress = 100
                    progressTextView.text = "100%"
                    loadingProgressBar.visibility = View.GONE
                    
                    // Check if we can install automatically
                    if (updateInstaller.canInstallPackages()) {
                        statusTextView.text = "Ready to install"
                        actionButton.text = "Install Update"
                        actionButton.visibility = View.VISIBLE
                    } else {
                        statusTextView.text = "Installation permission required"
                        actionButton.text = "Grant Permission"
                        actionButton.visibility = View.VISIBLE
                    }
                }
            }

            is DownloadState.Error -> {
                binding.apply {
                    statusTextView.text = "Download failed"
                    downloadInfoTextView.text = state.message
                    loadingProgressBar.visibility = View.GONE
                    actionButton.text = "Try Again"
                    actionButton.visibility = View.VISIBLE
                }
                
                Toast.makeText(this, "Download failed: ${state.message}", Toast.LENGTH_LONG).show()
            }

            else -> {}
        }
    }

    private fun installUpdate() {
        val apkFile = downloadedApkFile
        if (apkFile == null) {
            Toast.makeText(this, "No APK file to install", Toast.LENGTH_SHORT).show()
            return
        }

        if (!updateInstaller.canInstallPackages()) {
            // Request permission
            val permissionIntent = updateInstaller.getInstallPermissionIntent()
            if (permissionIntent != null) {
                startActivity(permissionIntent)
                Toast.makeText(this, "Please enable 'Install from Unknown Sources' and try again", Toast.LENGTH_LONG).show()
            }
            return
        }

        binding.apply {
            statusTextView.text = "Installing update..."
            actionButton.visibility = View.GONE
            loadingProgressBar.visibility = View.VISIBLE
        }

        // Install the APK
        val success = updateInstaller.installUpdate(apkFile)
        
        if (success) {
            binding.apply {
                statusTextView.text = "Installation started"
                loadingProgressBar.visibility = View.GONE
            }
            
            // The installation will take over from here
            // The app will be closed and the new version will start
        } else {
            binding.apply {
                statusTextView.text = "Installation failed"
                actionButton.text = "Try Again"
                actionButton.visibility = View.VISIBLE
                loadingProgressBar.visibility = View.GONE
            }
            
            Toast.makeText(this, "Failed to start installation", Toast.LENGTH_SHORT).show()
        }
    }

    private fun restartApp() {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }

    override fun onBackPressed() {
        // Prevent back button during update process
        // Only allow if there's an error or installation is complete
        if (binding.actionButton.visibility == View.VISIBLE && 
            (binding.actionButton.text == "Try Again" || binding.actionButton.text == "Restart App")) {
            super.onBackPressed()
        }
    }

    override fun onResume() {
        super.onResume()
        hideSystemUI()
        
        // Check if we returned from permission screen
        if (downloadedApkFile != null && updateInstaller.canInstallPackages()) {
            binding.apply {
                statusTextView.text = "Ready to install"
                actionButton.text = "Install Update"
                actionButton.visibility = View.VISIBLE
            }
        }
    }
}