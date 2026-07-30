package com.lanflix.webview

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.databinding.ActivityServerBrowserBinding
import com.lanflix.webview.databinding.ItemServerBinding
import kotlinx.coroutines.launch

class ServerBrowserActivity : AppCompatActivity() {

    private lateinit var binding: ActivityServerBrowserBinding
    private lateinit var discoveryManager: ServerDiscoveryManager
    private val verifiedOnlineServers = mutableSetOf<String>()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityServerBrowserBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupUI()
        setupDiscovery()
        scanKnownServers()
    }

    private fun setupUI() {
        val savedServer = ServerManager.getSavedServer(this)
        binding.editServerUrl.setText(savedServer)

        binding.btnManualConnect.setOnClickListener {
            val input = binding.editServerUrl.text.toString().trim()
            if (input.isNotEmpty()) {
                connectToServer(input)
            } else {
                Toast.makeText(this, "Please enter a valid server URL", Toast.LENGTH_SHORT).show()
            }
        }

        binding.btnRefreshScan.setOnClickListener {
            refreshScan()
        }
    }

    private fun refreshScan() {
        binding.serverListContainer.removeAllViews()
        verifiedOnlineServers.clear()
        binding.emptyDiscoveredTextView.visibility = View.VISIBLE
        binding.emptyDiscoveredTextView.text = "Scanning local network for online servers..."
        binding.scanProgressBar.visibility = View.VISIBLE

        discoveryManager.startDiscovery()
        scanKnownServers()
    }

    private fun setupDiscovery() {
        discoveryManager = ServerDiscoveryManager(this) { name, url ->
            runOnUiThread {
                verifyAndAddServer(name, url)
            }
        }
        discoveryManager.startDiscovery()
    }

    private fun scanKnownServers() {
        val savedServers = ServerManager.getSavedServers(this).toMutableSet()
        savedServers.add(ServerManager.DEFAULT_MDNS_HOST)

        lifecycleScope.launch {
            for (url in savedServers) {
                val name = if (url.contains("lanflix.local")) "Lanflix Server (lanflix.local)" else "Lanflix Server"
                verifyAndAddServer(name, url)
            }
        }
    }

    private fun verifyAndAddServer(name: String, url: String) {
        lifecycleScope.launch {
            val formattedUrl = ServerManager.formatServerUrl(url)
            if (verifiedOnlineServers.contains(formattedUrl)) return@launch

            val isOnline = ServerManager.pingServer(this@ServerBrowserActivity, formattedUrl, timeoutMs = 2000)
            if (!isOnline) return@launch

            verifiedOnlineServers.add(formattedUrl)
            binding.emptyDiscoveredTextView.visibility = View.GONE
            binding.scanProgressBar.visibility = View.GONE

            val itemBinding = ItemServerBinding.inflate(layoutInflater, binding.serverListContainer, false)
            itemBinding.txtServerName.text = name
            itemBinding.txtServerUrl.text = formattedUrl
            itemBinding.statusDot.setBackgroundResource(R.drawable.bg_status_online)

            itemBinding.serverItemContainer.setOnClickListener {
                connectToServer(formattedUrl)
            }

            binding.serverListContainer.addView(itemBinding.root)
        }
    }

    private fun connectToServer(serverUrl: String) {
        val formatted = ServerManager.formatServerUrl(serverUrl)
        binding.btnManualConnect.isEnabled = false
        binding.btnManualConnect.text = "Connecting..."

        lifecycleScope.launch {
            val isReachable = ServerManager.pingServer(this@ServerBrowserActivity, formatted, timeoutMs = 2500)
            if (isReachable) {
                ServerManager.saveServer(this@ServerBrowserActivity, formatted)

                val intent = Intent(this@ServerBrowserActivity, MainActivity::class.java).apply {
                    putExtra("SERVER_URL", formatted)
                    flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                }
                startActivity(intent)
                finish()
            } else {
                binding.btnManualConnect.isEnabled = true
                binding.btnManualConnect.text = "Connect to Server"
                Toast.makeText(
                    this@ServerBrowserActivity,
                    "Could not reach server at $formatted. Please check server IP.",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        discoveryManager.stopDiscovery()
    }
}
