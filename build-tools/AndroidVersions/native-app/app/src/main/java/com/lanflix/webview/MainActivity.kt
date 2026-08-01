package com.lanflix.webview

import android.os.Build
import android.os.Bundle
import android.view.View
import android.view.WindowManager
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
import com.lanflix.ui.fragments.HomeFragment
import com.lanflix.ui.fragments.LibrariesFragment
import com.lanflix.webview.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val serverUrl = intent.getStringExtra("SERVER_URL") ?: ServerManager.getSavedServer(this)
        if (!serverUrl.isNullOrBlank()) {
            ServerManager.activeServerUrl = serverUrl
        }

        setupImmersiveDisplay()
        setupNativeNavigation()

        if (savedInstanceState == null) {
            supportFragmentManager.beginTransaction()
                .replace(R.id.fragment_container, HomeFragment())
                .commit()
        }
    }

    override fun onResume() {
        super.onResume()
        setupImmersiveDisplay()
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) {
            setupImmersiveDisplay()
        }
    }

    private fun setupImmersiveDisplay() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            window.attributes.layoutInDisplayCutoutMode =
                WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
        }

        androidx.core.view.WindowCompat.setDecorFitsSystemWindows(window, false)
        val controller = androidx.core.view.WindowCompat.getInsetsController(window, window.decorView)
        controller.systemBarsBehavior = androidx.core.view.WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        controller.hide(androidx.core.view.WindowInsetsCompat.Type.systemBars())
    }

    fun setTopHeaderGlassAlpha(alpha: Float) {
        val clamped = alpha.coerceIn(0f, 1f)
        val alphaInt = (clamped * 220).toInt()
        val color = androidx.core.graphics.ColorUtils.setAlphaComponent(
            android.graphics.Color.parseColor("#0A0A0E"),
            alphaInt
        )
        binding.topHeaderBar.setBackgroundColor(color)
    }

    private fun setupNativeNavigation() {
        binding.bottomNavigation.setOnItemSelectedListener { item ->
            val fragment = when (item.itemId) {
                R.id.nav_home -> HomeFragment()
                R.id.nav_libraries -> LibrariesFragment()
                else -> HomeFragment()
            }
            supportFragmentManager.beginTransaction()
                .replace(R.id.fragment_container, fragment)
                .commit()
            true
        }
    }
}