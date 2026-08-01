package com.lanflix.webview

import android.os.Build
import android.os.Bundle
import android.view.WindowManager
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.core.view.WindowCompat
import com.lanflix.ui.compose.LanflixApp

/** Phone-first Compose host. Legacy XML fragments remain available during migration. */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)

        intent.getStringExtra("SERVER_URL")
            ?.takeIf { it.isNotBlank() }
            ?.let { ServerManager.activeServerUrl = it }
        ServerManager.isOnline = intent.getBooleanExtra("SERVER_ONLINE", ServerManager.isOnline)

        configureDisplay()
        setContent { LanflixApp() }
    }

    private fun configureDisplay() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            window.attributes.layoutInDisplayCutoutMode =
                WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
        }
        WindowCompat.setDecorFitsSystemWindows(window, false)
    }

    /** Kept temporarily so the legacy fragment source still compiles during migration. */
    fun setTopHeaderGlassAlpha(alpha: Float) = Unit
}
