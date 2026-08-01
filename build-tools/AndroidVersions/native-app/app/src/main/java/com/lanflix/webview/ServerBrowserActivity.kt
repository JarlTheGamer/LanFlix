package com.lanflix.webview

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.lanflix.settings.ServerConnectionViewModel
import com.lanflix.ui.compose.ServerConnectionScreen

class ServerBrowserActivity : ComponentActivity() {
    private val viewModel: ServerConnectionViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme {
                val state by viewModel.state.collectAsState()
                ServerConnectionScreen(
                    state = state,
                    onBack = { finish() },
                    onRefresh = viewModel::refresh,
                    onConnect = { url -> viewModel.connect(url, ::openLanflix) },
                    onRemove = viewModel::remove,
                    onContinueOffline = { openLanflix(null) }
                )
            }
        }
    }

    private fun openLanflix(serverUrl: String?) {
        startActivity(Intent(this, MainActivity::class.java).apply {
            serverUrl?.let { putExtra("SERVER_URL", it) }
            putExtra("SERVER_ONLINE", serverUrl != null)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        })
        finish()
    }
}
