package com.lanflix.webview

import android.annotation.SuppressLint
import android.content.Intent
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.view.KeyEvent
import android.view.View
import android.webkit.*
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.lanflix.webview.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {
    
    private lateinit var binding: ActivityMainBinding
    private lateinit var webView: WebView
    private lateinit var swipeRefresh: SwipeRefreshLayout
    
    // Default server URL - change this to your server address
    private val serverUrl = "http://192.168.178.13:5037" // Change to your server IP
    
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
            
            // Zoom and viewport settings
            setSupportZoom(false)
            builtInZoomControls = false
            displayZoomControls = false
            useWideViewPort = true
            loadWithOverviewMode = true
            
            // Media settings
            mediaPlaybackRequiresUserGesture = false
            
            // Mixed content for HTTPS/HTTP
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
        }
        
        // Enable hardware acceleration on the WebView
        webView.setLayerType(View.LAYER_TYPE_HARDWARE, null)
        
        // Disable default focus highlighting
        webView.isScrollbarFadingEnabled = false
        webView.isVerticalScrollBarEnabled = false
        webView.isHorizontalScrollBarEnabled = false
     
        // Set WebView client for handling page navigation
        webView.webViewClient = object : WebViewClient() {
            override fun onPageStarted(view: WebView?, url: String?, favicon: Bitmap?) {
                super.onPageStarted(view, url, favicon)
                binding.progressBar.visibility = View.VISIBLE
            }
            
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                binding.progressBar.visibility = View.GONE
                swipeRefresh.isRefreshing = false
                
                // Inject CSS to remove all focus outlines
                webView.evaluateJavascript(
                    """
                    var style = document.createElement('style');
                    style.innerHTML = '* { outline: none !important; -webkit-tap-highlight-color: transparent !important; }';
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
                    if (!url.contains(Uri.parse(serverUrl).host ?: "")) {
                        // Open external links in browser
                        val intent = Intent(Intent.ACTION_VIEW, Uri.parse(url))
                        startActivity(intent)
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
        
        // Disable focus to prevent blue outlines
        webView.isFocusable = false
        webView.isFocusableInTouchMode = false
    }
    
    private fun setupSwipeRefresh() {
        swipeRefresh.setOnRefreshListener {
            webView.reload()
        }
        
        // Set refresh colors
        swipeRefresh.setColorSchemeResources(
            android.R.color.holo_blue_bright,
            android.R.color.holo_green_light,
            android.R.color.holo_orange_light,
            android.R.color.holo_red_light
        )
    }
    
    private fun loadWebApp() {
        try {
            webView.loadUrl(serverUrl)
        } catch (e: Exception) {
            Toast.makeText(this, "Error loading web app: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }
    
    // Handle back button only
    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        // Only handle back button for WebView navigation
        if (keyCode == KeyEvent.KEYCODE_BACK && webView.canGoBack()) {
            webView.goBack()
            return true
        }
        
        return super.onKeyDown(keyCode, event)
    }
    

    
    override fun onDestroy() {
        super.onDestroy()
        webView.destroy()
    }
}