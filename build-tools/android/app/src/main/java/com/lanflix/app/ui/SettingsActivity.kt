package com.lanflix.app.ui

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.app.api.ApiClient
import com.lanflix.app.databinding.ActivitySettingsBinding
import com.lanflix.app.utils.PreferenceManager
import kotlinx.coroutines.launch

class SettingsActivity : AppCompatActivity() {
    
    private lateinit var binding: ActivitySettingsBinding
    private lateinit var preferenceManager: PreferenceManager
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        
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
        
        binding.buttonTest.isEnabled = false
        binding.buttonTest.text = "Testing..."
        
        lifecycleScope.launch {
            try {
                ApiClient.initialize(this@SettingsActivity, serverUrl)
                val response = ApiClient.getApi().healthCheck()
                
                if (response.isSuccessful) {
                    response.body()?.let { health ->
                        Toast.makeText(
                            this@SettingsActivity,
                            "Connected to ${health.name} v${health.version}",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                } else {
                    Toast.makeText(
                        this@SettingsActivity,
                        "Connection failed: ${response.code()}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            } catch (e: Exception) {
                Toast.makeText(
                    this@SettingsActivity,
                    "Error: ${e.message}",
                    Toast.LENGTH_SHORT
                ).show()
            } finally {
                binding.buttonTest.isEnabled = true
                binding.buttonTest.text = "Test Connection"
            }
        }
    }
    
    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
