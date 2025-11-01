package com.lanflix.app.ui

import android.annotation.SuppressLint
import android.content.Intent
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import android.webkit.WebChromeClient
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.app.R
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
        
        setSupportActionBar(binding.toolbar)
        
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
            
            webViewClient = WebViewClient()
            webChromeClient = WebChromeClient()
            
            // Load the server's web UI
            loadUrl(serverUrl)
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
    
    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.menu_main, menu)
        return true
    }
    
    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_settings -> {
                startActivity(Intent(this, SettingsActivity::class.java))
                true
            }
            R.id.action_check_updates -> {
                checkForUpdatesManually()
                true
            }
            R.id.action_refresh -> {
                binding.webView.reload()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }
    
    private fun checkForUpdatesManually() {
        lifecycleScope.launch {
            try {
                Toast.makeText(this@MainActivity, "Checking for updates...", Toast.LENGTH_SHORT).show()
                val updateInfo = updateChecker.checkForUpdates(force = true)
                if (updateInfo != null) {
                    updateChecker.showUpdateDialog(updateInfo)
                } else {
                    Toast.makeText(
                        this@MainActivity,
                        "You're running the latest version!",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            } catch (e: Exception) {
                Toast.makeText(
                    this@MainActivity,
                    "Failed to check for updates",
                    Toast.LENGTH_SHORT
                ).show()
            }
        }
    }
    
    override fun onBackPressed() {
        if (binding.webView.canGoBack()) {
            binding.webView.goBack()
        } else {
            super.onBackPressed()
        }
    }
}
