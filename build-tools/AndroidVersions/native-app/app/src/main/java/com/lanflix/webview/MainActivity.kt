package com.lanflix.webview

import android.annotation.SuppressLint
import android.content.Intent
import android.graphics.Bitmap
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.view.KeyEvent
import android.view.View
import android.webkit.*
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.lanflix.webview.databinding.ActivityMainBinding
import java.util.Locale

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var webView: WebView
    private lateinit var swipeRefresh: SwipeRefreshLayout

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

    // Default server URL - change this to your server address
    private val serverUrl = "http://192.168.178.13:5037" // Change to your server IP
    private val serverHost: String? by lazy {
        runCatching { Uri.parse(serverUrl).host?.lowercase(Locale.US) }.getOrNull()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        
        setupWebView()
        setupSwipeRefresh()
        loadWebApp()
    }
    
    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView() {
        webView = binding.webView
        swipeRefresh = binding.swipeRefresh
        
        // Enable JavaScript and other WebView settings for performance
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            databaseEnabled = true
            cacheMode = WebSettings.LOAD_DEFAULT
            
            // Performance optimizations
            setRenderPriority(WebSettings.RenderPriority.HIGH)
            setEnableSmoothTransition(true)
            
            // Allow file access for local content
            allowFileAccess = true
            allowContentAccess = true
            allowFileAccessFromFileURLs = true
            allowUniversalAccessFromFileURLs = true
            
            // Zoom and viewport settings for proper scaling
            setSupportZoom(false)
            builtInZoomControls = false
            displayZoomControls = false
            useWideViewPort = true
            loadWithOverviewMode = true
            
            // Better text scaling - prevent automatic text scaling
            textZoom = 100
            minimumFontSize = 1
            minimumLogicalFontSize = 1
            defaultFontSize = 16
            defaultFixedFontSize = 13
            
            // Prevent automatic scaling
            layoutAlgorithm = WebSettings.LayoutAlgorithm.NORMAL
            
            // Media settings
            mediaPlaybackRequiresUserGesture = false
            
            // Mixed content for HTTPS/HTTP
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
        }
        
        // Force hardware acceleration only on devices that need the hint. Fire TV
        // devices (model identifiers start with "AFT") already run the WebView with
        // hardware acceleration by default, but explicitly forcing the layer causes
        // the video element to render as a blank surface while audio continues to
        // play. Leaving the layer type untouched lets the platform pick the
        // appropriate pipeline without giving up hardware acceleration performance.
        val manufacturer = Build.MANUFACTURER.orEmpty()
        val model = Build.MODEL.orEmpty()
        val product = Build.PRODUCT.orEmpty()
        val device = Build.DEVICE.orEmpty()
        val isAmazonFireTv = manufacturer.equals("Amazon", ignoreCase = true) &&
            (model.startsWith("AFT", ignoreCase = true) ||
                product.startsWith("AFT", ignoreCase = true) ||
                device.startsWith("AFT", ignoreCase = true))

        if (isAmazonFireTv) {
            // LAYER_TYPE_NONE keeps hardware acceleration enabled when the hosting
            // window is hardware accelerated, but avoids the blank video surface seen
            // on Fire TV when forcing LAYER_TYPE_HARDWARE.
            webView.setLayerType(View.LAYER_TYPE_NONE, null)
        } else {
            webView.setLayerType(View.LAYER_TYPE_HARDWARE, null)
        }
        
        // Set initial scale for better display - use 0 for automatic scaling
        webView.setInitialScale(0)
        
        // Enable scrollbars for better user experience
        webView.isScrollbarFadingEnabled = true
        webView.isVerticalScrollBarEnabled = true
        webView.isHorizontalScrollBarEnabled = false
        
        // Set background and enable smooth scrolling
        webView.setBackgroundColor(android.graphics.Color.BLACK)
        webView.isHapticFeedbackEnabled = true
        webView.isScrollContainer = true
     
        // Set WebView client for handling page navigation
        webView.webViewClient = object : WebViewClient() {
            override fun onPageStarted(view: WebView?, url: String?, favicon: Bitmap?) {
                super.onPageStarted(view, url, favicon)
                applyLayerTypeForUrl(url)
                binding.progressBar.visibility = View.VISIBLE
            }
            
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                binding.progressBar.visibility = View.GONE
                swipeRefresh.isRefreshing = false
                
                // Inject viewport meta tag and CSS for proper scaling and mobile experience
                webView.evaluateJavascript(
                    """
                    // Add or update viewport meta tag for proper scaling
                    var viewport = document.querySelector('meta[name="viewport"]');
                    if (!viewport) {
                        viewport = document.createElement('meta');
                        viewport.name = 'viewport';
                        document.head.appendChild(viewport);
                    }
                    viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover, shrink-to-fit=no';
                    
                    // Add CSS for better mobile experience and Android WebView optimizations
                    var style = document.createElement('style');
                    style.innerHTML = `
                        * {
                            outline: none !important;
                            -webkit-tap-highlight-color: transparent !important;
                            -webkit-focus-ring-color: transparent !important;
                            -webkit-text-size-adjust: 100% !important;
                            text-size-adjust: 100% !important;
                        }
                        *:focus {
                            outline: none !important;
                            box-shadow: none !important;
                        }
                        html {
                            margin: 0 !important;
                            padding: 0 !important;
                            -webkit-text-size-adjust: 100% !important;
                            text-size-adjust: 100% !important;
                            font-size: 16px !important;
                        }
                        body {
                            overflow-y: auto !important;
                            -webkit-overflow-scrolling: touch !important;
                            margin: 0 !important;
                            padding: 0 !important;
                            -webkit-text-size-adjust: 100% !important;
                            text-size-adjust: 100% !important;
                            min-height: 100vh !important;
                            position: relative !important;
                        }
                        input, textarea, select, button {
                            outline: none !important;
                            -webkit-tap-highlight-color: transparent !important;
                            -webkit-text-size-adjust: 100% !important;
                            text-size-adjust: 100% !important;
                        }
                        input:focus, textarea:focus, select:focus, button:focus {
                            outline: none !important;
                            box-shadow: none !important;
                        }
                        /* Ensure proper scaling on Android */
                        .top-nav {
                            -webkit-transform: translateZ(0) !important;
                            transform: translateZ(0) !important;
                            will-change: transform !important;
                        }
                        /* Prevent zoom on input focus */
                        input[type="text"], input[type="email"], input[type="password"], 
                        input[type="search"], textarea, select {
                            font-size: 16px !important;
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
                binding.progressBar.visibility = View.GONE
                swipeRefresh.isRefreshing = false
                
                // Show error message
                Toast.makeText(
                    this@MainActivity,
                    "Failed to load page: ${error?.description}",
                    Toast.LENGTH_LONG
                ).show()
            }
            
            override fun shouldOverrideUrlLoading(
                view: WebView?,
                request: WebResourceRequest?
            ): Boolean {
                val url = request?.url.toString()
                
                // Handle external links
                if (url.startsWith("http://") || url.startsWith("https://")) {
                    val parsed = runCatching { Uri.parse(url) }.getOrNull()
                    val host = parsed?.host?.lowercase(Locale.US)
                    val isExternalHost = serverHost != null && host != null && host != serverHost

                    if (isExternalHost && parsed != null) {
                        // Open external links in browser
                        startActivity(Intent(Intent.ACTION_VIEW, parsed))
                        return true
                    }
                }

                return false
            }
        }
        
        // Set WebChrome client for better JavaScript support
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
        
        // Enable focus for proper touch interactions
        webView.isFocusable = true
        webView.isFocusableInTouchMode = true
    }
    
    private fun setupSwipeRefresh() {
        // Disable pull-to-refresh to prevent accidental refreshes
        swipeRefresh.isEnabled = false
    }

    // Fire TV renders the HTML player surface as a blank rectangle if the WebView
    // is forced onto the hardware layer. Keep the performance hint enabled for
    // navigation screens, but fall back to the default layer when the dedicated
    // player page is loading so the video element can composite correctly.
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

        val hostMatches = serverHost == null || parsedUrl.host?.lowercase(Locale.US) == serverHost
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
        try {
            webView.loadUrl(serverUrl)
        } catch (e: Exception) {
            Toast.makeText(this, "Error loading web app: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }
    
    // Handle remote control and keyboard input
    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        // Map remote control keys to web navigation
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
            KeyEvent.KEYCODE_MEDIA_PLAY_PAUSE -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: ' ', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_PLAY -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: ' ', bubbles: true}));"
            KeyEvent.KEYCODE_MEDIA_PAUSE -> "window.dispatchEvent(new KeyboardEvent('keydown', {key: ' ', bubbles: true}));"
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
    

    
    override fun onDestroy() {
        super.onDestroy()
        webView.destroy()
    }
}