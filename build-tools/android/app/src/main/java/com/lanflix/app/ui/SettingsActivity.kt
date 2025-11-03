package com.lanflix.app.ui

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.app.databinding.ActivitySettingsBinding
import com.lanflix.app.utils.PreferenceManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.net.HttpURLConnection
import java.net.URL
import java.net.MalformedURLException

class SettingsActivity : AppCompatActivity() {
    
    private lateinit var binding: ActivitySettingsBinding
    private lateinit var preferenceManager: PreferenceManager
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        
        preferenceManager = PreferenceManager(this)
        
        // Load current settings
        binding.editServerUrl.setText(preferenceManager.getServerUrl())
        
        binding.buttonSave.setOnClickListener {
            saveSettings()
        }
        
        binding.buttonTest.setOnClickListener {
            testConnection()
        }
    }
    
    private fun saveSettings() {
        val serverUrl = binding.editServerUrl.text.toString().trim()
        
        if (serverUrl.isEmpty()) {
            Toast.makeText(this, "Please enter server URL", Toast.LENGTH_SHORT).show()
            return
        }
        
        // Validate URL format
        try {
            URL(serverUrl)
        } catch (e: MalformedURLException) {
            Toast.makeText(this, "Invalid URL format", Toast.LENGTH_SHORT).show()
            return
        }
        
        preferenceManager.setServerUrl(serverUrl)
        Toast.makeText(this, "Settings saved", Toast.LENGTH_SHORT).show()
        
        // Restart app
        val intent = Intent(this, MainActivity::class.java)
        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        startActivity(intent)
        finish()
    }
    
    private fun testConnection() {
        val serverUrl = binding.editServerUrl.text.toString().trim()
        
        if (serverUrl.isEmpty()) {
            Toast.makeText(this, "Please enter server URL", Toast.LENGTH_SHORT).show()
            return
        }
        
        // Validate URL format
        try {
            URL(serverUrl)
        } catch (e: MalformedURLException) {
            Toast.makeText(this, "Invalid URL format", Toast.LENGTH_SHORT).show()
            return
        }
        
        binding.buttonTest.isEnabled = false
        binding.buttonTest.text = "Testing..."
        
        lifecycleScope.launch {
            try {
                val result = withContext(Dispatchers.IO) {
                    val url = URL("$serverUrl/health")
                    val connection = url.openConnection() as HttpURLConnection
                    connection.requestMethod = "GET"
                    connection.connectTimeout = 5000
                    connection.readTimeout = 5000
                    connection.responseCode == 200
                }
                
                if (result) {
                    Toast.makeText(
                        this@SettingsActivity,
                        "Connected successfully!",
                        Toast.LENGTH_LONG
                    ).show()
                } else {
                    Toast.makeText(
                        this@SettingsActivity,
                        "Connection failed - Server returned error",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            } catch (e: Exception) {
                val errorMessage = when (e) {
                    is java.net.ConnectException -> "Cannot connect to server"
                    is java.net.SocketTimeoutException -> "Connection timeout"
                    is java.net.UnknownHostException -> "Server not found"
                    else -> "Error: ${e.message}"
                }
                Toast.makeText(
                    this@SettingsActivity,
                    errorMessage,
                    Toast.LENGTH_LONG
                ).show()
            } finally {
                binding.buttonTest.isEnabled = true
                binding.buttonTest.text = "Test Connection"
            }
        }
    }
}
