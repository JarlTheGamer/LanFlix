package com.lanflix.app.ui

import android.annotation.SuppressLint
import android.content.Intent
import android.os.Bundle
import android.webkit.WebChromeClient
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.app.databinding.ActivityMainBinding
import com.lanflix.app.utils.PreferenceManager
import com.lanflix.app.utils.UpdateChecker
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {
    
    private lateinit var binding: ActivityMainBinding
    private lateinit var preferenceManager: PreferenceManager
    private lateinit var updateChecker: UpdateChecker
    
    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        
        preferenceManager = PreferenceManager(this)
        updateChecker = UpdateChecker(this)
        
        // Check if server is configured
        val serverUrl = preferenceManager.getServerUrl()
        if (serverUrl.isEmpty()) {
            startActivity(Intent(this, SettingsActivity::class.java))
            finish()
            return
        }
        
        // Setup WebView to load server frontend
        setupWebView(serverUrl)
        checkForUpdates()
    }
    
    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView(serverUrl: String) {
        binding.webView.apply {
            settings.javaScriptEnabled = true
            settings.domStorageEnabled = true
            settings.databaseEnabled = true
            settings.mediaPlaybackRequiresUserGesture = false
            settings.allowFileAccess = true
            settings.allowContentAccess = true
            settings.mixedContentMode = android.webkit.WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
            
            // TV-specific settings - enable proper scaling for TV screens
            settings.useWideViewPort = true
            settings.loadWithOverviewMode = true
            settings.builtInZoomControls = false
            settings.displayZoomControls = false
            settings.setSupportZoom(false)
            
            // Set initial scale for better TV display
            settings.setInitialScale(100)
            
            // Enable hardware acceleration for better performance
            settings.setRenderPriority(android.webkit.WebSettings.RenderPriority.HIGH)
            settings.setEnableSmoothTransition(true)
            
            // Ensure proper TV user agent
            settings.userAgentString = settings.userAgentString + " AndroidTV"
            
            webViewClient = object : WebViewClient() {
                override fun onReceivedError(
                    view: WebView?,
                    errorCode: Int,
                    description: String?,
                    failingUrl: String?
                ) {
                    super.onReceivedError(view, errorCode, description, failingUrl)
                    Toast.makeText(
                        this@MainActivity,
                        "Failed to load server: $description",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
            webChromeClient = WebChromeClient()
            
            // Enable hardware acceleration for better performance
            setLayerType(android.view.View.LAYER_TYPE_HARDWARE, null)
            
            // Load the server's web UI
            try {
                loadUrl(serverUrl)
            } catch (e: Exception) {
                Toast.makeText(
                    this@MainActivity,
                    "Error loading URL: ${e.message}",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }
    
    private fun checkForUpdates() {
        lifecycleScope.launch {
            try {
                val updateInfo = updateChecker.checkForUpdates()
                updateInfo?.let {
                    updateChecker.showUpdateDialog(it)
                }
            } catch (e: Exception) {
                // Silently fail - don't bother user with update check errors
            }
        }
    }
    
    @Deprecated("Deprecated in Java")
    override fun onBackPressed() {
        if (binding.webView.canGoBack()) {
            binding.webView.goBack()
        } else {
            super.onBackPressed()
        }
    }
}
