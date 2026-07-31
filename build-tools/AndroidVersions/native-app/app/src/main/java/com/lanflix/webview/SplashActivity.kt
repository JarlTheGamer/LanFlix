package com.lanflix.webview

import android.animation.AnimatorSet
import android.animation.ObjectAnimator
import android.animation.ValueAnimator
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.view.animation.DecelerateInterpolator
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.lanflix.webview.databinding.ActivitySplashBinding
import kotlinx.coroutines.launch

class SplashActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySplashBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySplashBinding.inflate(layoutInflater)
        setContentView(binding.root)

        runCinematicIntroAnimation()
        proceedToMainApp()
    }

    private fun runCinematicIntroAnimation() {
        binding.logoContainer.scaleX = 0.75f
        binding.logoContainer.scaleY = 0.75f
        binding.logoContainer.alpha = 0f
        binding.ambientGlowView.alpha = 0f

        val scaleX = ObjectAnimator.ofFloat(binding.logoContainer, View.SCALE_X, 0.75f, 1.0f)
        val scaleY = ObjectAnimator.ofFloat(binding.logoContainer, View.SCALE_Y, 0.75f, 1.0f)
        val alpha = ObjectAnimator.ofFloat(binding.logoContainer, View.ALPHA, 0f, 1.0f)
        val glowAlpha = ObjectAnimator.ofFloat(binding.ambientGlowView, View.ALPHA, 0f, 0.75f)

        AnimatorSet().apply {
            playTogether(scaleX, scaleY, alpha, glowAlpha)
            duration = 900
            interpolator = DecelerateInterpolator()
            start()
        }

        ObjectAnimator.ofFloat(binding.ambientGlowView, View.ALPHA, 0.5f, 0.85f, 0.5f).apply {
            duration = 2000
            repeatCount = ValueAnimator.INFINITE
            repeatMode = ValueAnimator.RESTART
            start()
        }
    }

    private fun proceedToMainApp() {
        lifecycleScope.launch {
            kotlinx.coroutines.delay(1200)

            val serverUrl = ServerManager.getSavedServer(this@SplashActivity)
            ServerManager.saveServer(this@SplashActivity, serverUrl)

            val isOnline = ServerManager.pingServer(this@SplashActivity, serverUrl, timeoutMs = 1500)
            if (isOnline) {
                launchMainActivity(serverUrl)
            } else {
                launchServerBrowser()
            }
        }
    }

    private fun launchMainActivity(serverUrl: String) {
        val intent = Intent(this, MainActivity::class.java).apply {
            putExtra("SERVER_URL", serverUrl)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }

    private fun launchServerBrowser() {
        val intent = Intent(this, ServerBrowserActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }
}