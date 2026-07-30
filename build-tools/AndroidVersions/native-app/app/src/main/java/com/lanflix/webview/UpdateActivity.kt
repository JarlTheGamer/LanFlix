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
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import java.io.File
import java.util.Locale

class UpdateActivity : AppCompatActivity() {

    private lateinit var binding: ActivityUpdateBinding
    private var updateInfo: UpdateInfo? = null
    private var isDownloading = false

    companion object {
        private const val EXTRA_UPDATE_INFO = "extra_update_info"

        @JvmStatic
        fun start(context: Context, updateInfo: UpdateInfo) {
            val intent = Intent(context, UpdateActivity::class.java).apply {
                putExtra(EXTRA_UPDATE_INFO, updateInfo)
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
            context.startActivity(intent)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityUpdateBinding.inflate(layoutInflater)
        setContentView(binding.root)

        @Suppress("DEPRECATION")
        updateInfo = intent.getSerializableExtra(EXTRA_UPDATE_INFO) as? UpdateInfo

        if (updateInfo == null) {
            Toast.makeText(this, "No update information available", Toast.LENGTH_SHORT).show()
            finish()
            return
        }

        bindUpdateInfo(updateInfo!!)
        setupListeners()
    }

    private fun bindUpdateInfo(info: UpdateInfo) {
        binding.txtVersionName.text = "Version ${info.versionName} is available"
        binding.txtVersionBadge.text = info.versionName

        if (!info.releaseNotes.isNullOrBlank()) {
            binding.txtReleaseNotes.text = info.releaseNotes
        }

        if (info.mandatory) {
            binding.btnCancel.visibility = View.GONE
        }
    }

    private fun setupListeners() {
        binding.btnCancel.setOnClickListener {
            if (!isDownloading) {
                finish()
            }
        }

        binding.btnStartUpdate.setOnClickListener {
            updateInfo?.let { info ->
                startDownload(info)
            }
        }
    }

    private fun startDownload(info: UpdateInfo) {
        isDownloading = true
        binding.btnStartUpdate.isEnabled = false
        binding.btnCancel.isEnabled = !info.mandatory
        binding.progressContainer.visibility = View.VISIBLE
        binding.txtErrorDetail.visibility = View.GONE

        val downloader = UpdateDownloader(this)
        val startTime = System.currentTimeMillis()

        lifecycleScope.launch {
            downloader.downloadUpdate(info).collectLatest { state ->
                when (state) {
                    is DownloadState.Starting -> {
                        binding.txtStatus.text = "Connecting to server..."
                        binding.progressBarUpdate.progress = 0
                        binding.txtPercentage.text = "0%"
                    }

                    is DownloadState.Progress -> {
                        binding.txtStatus.text = "Downloading update..."
                        binding.progressBarUpdate.progress = state.percentage
                        binding.txtPercentage.text = "${state.percentage}%"

                        val downloadedMB = state.bytesDownloaded / (1024.0 * 1024.0)
                        val totalMB = state.totalBytes / (1024.0 * 1024.0)
                        
                        if (state.totalBytes > 0) {
                            binding.txtDownloadSize.text = String.format(Locale.US, "%.1f MB / %.1f MB", downloadedMB, totalMB)
                        } else {
                            binding.txtDownloadSize.text = String.format(Locale.US, "%.1f MB downloaded", downloadedMB)
                        }

                        val elapsedTime = (System.currentTimeMillis() - startTime) / 1000.0
                        if (elapsedTime > 0 && state.bytesDownloaded > 0) {
                            val speedMBs = (state.bytesDownloaded / (1024.0 * 1024.0)) / elapsedTime
                            binding.txtDownloadSpeed.text = String.format(Locale.US, "%.1f MB/s", speedMBs)
                        }
                    }

                    is DownloadState.Success -> {
                        binding.txtStatus.text = "Download complete! Opening installer..."
                        binding.progressBarUpdate.progress = 100
                        binding.txtPercentage.text = "100%"
                        installUpdate(state.file)
                    }

                    is DownloadState.Error -> {
                        isDownloading = false
                        binding.btnStartUpdate.isEnabled = true
                        binding.btnStartUpdate.text = "Retry Download"
                        binding.txtStatus.text = "Download Failed"
                        binding.txtErrorDetail.text = state.message
                        binding.txtErrorDetail.visibility = View.VISIBLE
                    }

                    else -> {}
                }
            }
        }
    }

    private fun installUpdate(apkFile: File) {
        val installer = UpdateInstaller(this)
        val success = installer.installUpdate(apkFile)
        if (!success) {
            Toast.makeText(this, "Failed to launch APK installer. Please grant install permission.", Toast.LENGTH_LONG).show()
        }
        finish()
    }

    override fun onBackPressed() {
        if (!isDownloading && updateInfo?.mandatory != true) {
            super.onBackPressed()
        }
    }
}