package com.lanflix.webview

import android.annotation.SuppressLint
import android.content.Intent
import android.graphics.Bitmap
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.view.KeyEvent
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.webkit.*
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.lanflix.webview.databinding.ActivityMainBinding
import com.lanflix.webview.ota.UpdateManager
import kotlinx.coroutines.launch
import java.util.Locale

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var webView: WebView
    private lateinit var swipeRefresh: SwipeRefreshLayout
    private lateinit var updateManager: UpdateManager

    private var currentServerUrl: String = ServerManager.DEFAULT_MDNS_HOST

    private val isAmazonFireTv: Boolean by lazy {
        val manufacturer = Build.MANUFACTURER.orEmpty()
        val model = Build.MODEL.orEmpty()
        val product = Build.PRODUCT.orEmpty()
        val device = Build.DEVICE.orEmpty()

        manufacturer.equals("Amazon", ignoreCase = true) &&
            (model.startsWith("AFT", ignoreCase = true) ||
                product.startsWith("AFT", ignoreCase = true) ||
                device.startsWith("AFT", ignoreCase = true))
    }

    private fun getServerHosts(): Set<String> {
        val hosts = mutableSetOf<String>()
        runCatching { Uri.parse(currentServerUrl).host?.lowercase(Locale.US) }
            .getOrNull()?.let { hosts.add(it) }
        hosts.add("lanflix.local")
        return hosts
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        resolveServerUrl()
        setupFailoverUI()
        setupUpdateManager()
        setupWebView()
        setupSwipeRefresh()
        loadWebApp()

        lifecycleScope.launch {
            kotlinx.coroutines.delay(3000)
            updateManager.checkForUpdateManually(showNoUpdateDialog = false)
        }
    }

    private fun resolveServerUrl() {
        val intentUrl = intent.getStringExtra("SERVER_URL")
        currentServerUrl = if (!intentUrl.isNullOrBlank()) {
            ServerManager.formatServerUrl(intentUrl)
        } else {
            ServerManager.getSavedServer(this)
        }
        ServerManager.saveServer(this, currentServerUrl)
    }

    private fun setupFailoverUI() {
        binding.btnRetryConnection.setOnClickListener {
            binding.serverUnreachableLayout.visibility = View.GONE
            binding.webView.visibility = View.VISIBLE
            loadWebApp()
        }

        binding.btnOpenServerBrowser.setOnClickListener {
            openServerBrowser()
        }
    }

    private fun openServerBrowser() {
        val intent = Intent(this, ServerBrowserActivity::class.java)
        startActivity(intent)
    }

    private fun setupUpdateManager() {
        updateManager = UpdateManager(this)
        updateManager.schedulePeriodicUpdateCheck()
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView() {
        webView = binding.webView
        swipeRefresh = binding.swipeRefresh

        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            databaseEnabled = true
            cacheMode = WebSettings.LOAD_DEFAULT

            val defaultUserAgent = userAgentString.orEmpty()
            userAgentString = if (!defaultUserAgent.contains("Lanflix-AndroidNativeApp")) {
                "$defaultUserAgent Lanflix-AndroidNativeApp Mobile"
            } else {
                defaultUserAgent
            }

            setRenderPriority(WebSettings.RenderPriority.HIGH)
            setEnableSmoothTransition(true)

            allowFileAccess = true
            allowContentAccess = true
            allowFileAccessFromFileURLs = true
            allowUniversalAccessFromFileURLs = true

            setSupportZoom(false)
            builtInZoomControls = false
            displayZoomControls = false
            useWideViewPort = true
            loadWithOverviewMode = true

            textZoom = 100
            minimumFontSize = 1
            minimumLogicalFontSize = 1
            defaultFontSize = 16
            defaultFixedFontSize = 13

            layoutAlgorithm = WebSettings.LayoutAlgorithm.NORMAL
            mediaPlaybackRequiresUserGesture = false
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
        }

        webView.addJavascriptInterface(WebAppInterface(this, updateManager), "Android")

        if (isAmazonFireTv) {
            webView.setLayerType(View.LAYER_TYPE_NONE, null)
        } else {
            webView.setLayerType(View.LAYER_TYPE_HARDWARE, null)
        }

        webView.setInitialScale(0)
        webView.isScrollbarFadingEnabled = true
        webView.isVerticalScrollBarEnabled = true
        webView.isHorizontalScrollBarEnabled = false
        webView.setBackgroundColor(android.graphics.Color.parseColor("#0D0D11"))
        webView.isHapticFeedbackEnabled = true
        webView.isScrollContainer = true

        webView.webViewClient = object : WebViewClient() {
            override fun onPageStarted(view: WebView?, url: String?, favicon: Bitmap?) {
                super.onPageStarted(view, url, favicon)
                applyLayerTypeForUrl(url)
                binding.progressBar.visibility = View.VISIBLE
                binding.serverUnreachableLayout.visibility = View.GONE
                webView.visibility = View.VISIBLE
            }

            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                binding.progressBar.visibility = View.GONE
                swipeRefresh.isRefreshing = false

                webView.evaluateJavascript(
                    """
                    var viewport = document.querySelector('meta[name="viewport"]');
                    if (!viewport) {
                        viewport = document.createElement('meta');
                        viewport.name = 'viewport';
                        document.head.appendChild(viewport);
                    }
                    viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover, shrink-to-fit=no';
                    
                    var style = document.createElement('style');
                    style.innerHTML = `
                        * {
                            outline: none !important;
                            -webkit-tap-highlight-color: transparent !important;
                            -webkit-focus-ring-color: transparent !important;
                            -webkit-text-size-adjust: 100% !important;
                            text-size-adjust: 100% !important;
                        }
                        body {
                            background-color: #0d0d11 !important;
                            overflow-y: auto !important;
                            -webkit-overflow-scrolling: touch !important;
                        }
                    `;
                    document.head.appendChild(style);
                    """.trimIndent(),
                    null
                )
            }

            override fun onReceivedError(
                view: WebView?,
                request: WebResourceRequest?,
                error: WebResourceError?
            ) {
                super.onReceivedError(view, request, error)
                if (request == null || request.isForMainFrame) {
                    showUnreachableOverlay("Could not connect to $currentServerUrl\n${error?.description}")
                }
            }

            override fun onReceivedHttpError(
                view: WebView?,
                request: WebResourceRequest?,
                errorResponse: WebResourceResponse?
            ) {
                super.onReceivedHttpError(view, request, errorResponse)
                if (request != null && request.isForMainFrame && (errorResponse?.statusCode ?: 200) >= 500) {
                    showUnreachableOverlay("Server returned HTTP error ${errorResponse?.statusCode} at $currentServerUrl")
                }
            }

            override fun shouldOverrideUrlLoading(
                view: WebView?,
                request: WebResourceRequest?
            ): Boolean {
                val url = request?.url.toString()
                if (url.startsWith("http://") || url.startsWith("https://")) {
                    val parsed = runCatching { Uri.parse(url) }.getOrNull()
                    val targetHost = parsed?.host?.lowercase(Locale.US)

                    val validHosts = getServerHosts()
                    val isServerHost = targetHost != null && (validHosts.contains(targetHost) || validHosts.isEmpty() || targetHost.endsWith(".local"))

                    if (!isServerHost && parsed != null) {
                        startActivity(Intent(Intent.ACTION_VIEW, parsed))
                        return true
                    }
                }
                return false
            }
        }

        webView.webChromeClient = object : WebChromeClient() {
            override fun onProgressChanged(view: WebView?, newProgress: Int) {
                super.onProgressChanged(view, newProgress)
                binding.progressBar.progress = newProgress
            }

            override fun onJsAlert(
                view: WebView?,
                url: String?,
                message: String?,
                result: JsResult?
            ): Boolean {
                Toast.makeText(this@MainActivity, message, Toast.LENGTH_SHORT).show()
                result?.confirm()
                return true
            }
        }

        webView.isFocusable = true
        webView.isFocusableInTouchMode = true
    }

    private fun showUnreachableOverlay(message: String) {
        binding.progressBar.visibility = View.GONE
        swipeRefresh.isRefreshing = false
        webView.visibility = View.GONE
        binding.txtUnreachableDetail.text = message
        binding.serverUnreachableLayout.visibility = View.VISIBLE
    }

    private fun setupSwipeRefresh() {
        swipeRefresh.isEnabled = false
    }

    private fun applyLayerTypeForUrl(url: String?) {
        val targetLayerType = when {
            isAmazonFireTv && isVideoPlaybackUrl(url) -> View.LAYER_TYPE_NONE
            else -> View.LAYER_TYPE_HARDWARE
        }

        if (webView.layerType != targetLayerType) {
            webView.setLayerType(targetLayerType, null)
        }
    }

    private fun isVideoPlaybackUrl(url: String?): Boolean {
        if (url.isNullOrBlank()) return false
        val parsedUrl = runCatching { Uri.parse(url) }.getOrNull() ?: return false
        val validHosts = getServerHosts()
        val hostMatches = validHosts.isEmpty() || validHosts.contains(parsedUrl.host?.lowercase(Locale.US))
        if (!hostMatches) return false

        val path = parsedUrl.path?.lowercase(Locale.US).orEmpty()
        val pathSegments = parsedUrl.pathSegments.map { it.lowercase(Locale.US) }
        val fragment = parsedUrl.fragment?.lowercase(Locale.US).orEmpty()
        val pageQuery = parsedUrl.getQueryParameter("page")?.lowercase(Locale.US).orEmpty()

        if (pathSegments.any { it.contains("player") }) return true
        if (path.contains("player")) return true
        if (fragment.contains("player")) return true
        if (pageQuery.contains("player")) return true

        return false
    }

    private fun loadWebApp() {
        lifecycleScope.launch {
            try {
                val connectionUrl = ServerManager.resolveUrlForConnection(this@MainActivity, currentServerUrl)
                webView.loadUrl(connectionUrl)
            } catch (e: Exception) {
                showUnreachableOverlay("Error loading server: ${e.message}")
            }
        }
    }

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        val jsCommand = when (keyCode) {
            KeyEvent.KEYCODE_DPAD_UP -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowUp', bubbles: true}));"
            KeyEvent.KEYCODE_DPAD_DOWN -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowDown', bubbles: true}));"
            KeyEvent.KEYCODE_DPAD_LEFT -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowLeft', bubbles: true}));"
            KeyEvent.KEYCODE_DPAD_RIGHT -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowRight', bubbles: true}));"
            KeyEvent.KEYCODE_DPAD_CENTER, KeyEvent.KEYCODE_ENTER -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', bubbles: true}));"
            KeyEvent.KEYCODE_BACK -> {
                if (webView.canGoBack()) {
                    webView.goBack()
                    return true
                } else {
                    "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'Escape', bubbles: true}));"
                }
            }
            KeyEvent.KEYCODE_MENU -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'm', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_PLAY_PAUSE, KeyEvent.KEYCODE_MEDIA_PLAY, KeyEvent.KEYCODE_MEDIA_PAUSE -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: ' ', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_STOP -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'Escape', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_FAST_FORWARD -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowRight', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_REWIND -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowLeft', bubbles: true}));"
            else -> null
        }

        if (jsCommand != null && jsCommand != "handled") {
            webView.evaluateJavascript(jsCommand, null)
            return true
        }

        return super.onKeyDown(keyCode, event)
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.main_menu, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_change_server -> {
                openServerBrowser()
                true
            }
            R.id.action_check_update -> {
                lifecycleScope.launch {
                    val hasUpdate = updateManager.checkForUpdateManually(showNoUpdateDialog = true)
                    if (!hasUpdate) {
                        runOnUiThread {
                            Toast.makeText(this@MainActivity, "You're running the latest version!", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
                true
            }
            R.id.action_refresh -> {
                webView.reload()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        webView.destroy()
    }
}