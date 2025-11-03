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
            
            // TV-specific viewport settings
            layoutAlgorithm = WebSettings.LayoutAlgorithm.NORMAL
            
            // Media settings
            mediaPlaybackRequiresUserGesture = false
            
            // Mixed content for HTTPS/HTTP
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
        }
        
        // Enable hardware acceleration on the WebView
        webView.setLayerType(View.LAYER_TYPE_HARDWARE, null)
        
        // Set initial scale for proper TV display
        webView.setInitialScale(100)
        
        // Force WebView to use full screen dimensions
        webView.layoutParams.width = android.view.ViewGroup.LayoutParams.MATCH_PARENT
        webView.layoutParams.height = android.view.ViewGroup.LayoutParams.MATCH_PARENT
        
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
                
                // Inject TV-friendly CSS and JavaScript
                injectTvOptimizations()
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
        
        // Enable focus for TV remote navigation
        webView.isFocusable = true
        webView.isFocusableInTouchMode = true
        webView.requestFocus()
        
        // Disable swipe refresh on TV (no touch)
        if (packageManager.hasSystemFeature("android.software.leanback")) {
            swipeRefresh.isEnabled = false
        }
        
        // Ensure proper scrolling behavior for mobile devices
        if (!packageManager.hasSystemFeature("android.software.leanback")) {
            // This is a mobile device - ensure scrolling works
            webView.isVerticalScrollBarEnabled = true
            webView.isHorizontalScrollBarEnabled = true
            webView.scrollBarStyle = View.SCROLLBARS_INSIDE_OVERLAY
            
            // Enable nested scrolling for better touch behavior
            webView.isNestedScrollingEnabled = true
        }
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
    
    // Handle remote control and keyboard navigation
    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        when (keyCode) {
            // Back button - navigate back in WebView or exit
            KeyEvent.KEYCODE_BACK -> {
                if (webView.canGoBack()) {
                    webView.goBack()
                    return true
                }
            }
            
            // TV remote control support - Enter/OK button
            KeyEvent.KEYCODE_DPAD_CENTER,
            KeyEvent.KEYCODE_ENTER -> {
                // Send both Enter key and click event for better compatibility
                webView.evaluateJavascript("""
                    console.log('🎮 Android: Center/OK button pressed');
                    
                    // Try multiple approaches to trigger profile selection
                    var activeElement = document.activeElement;
                    var focusedProfile = document.querySelector('.profile-item.focused');
                    
                    if (focusedProfile) {
                        console.log('🎮 Clicking focused profile');
                        focusedProfile.click();
                    } else if (activeElement && activeElement.classList.contains('profile-item')) {
                        console.log('🎮 Clicking active element');
                        activeElement.click();
                    } else {
                        // Fallback: dispatch Enter key event
                        console.log('🎮 Dispatching Enter key event');
                        document.dispatchEvent(new KeyboardEvent('keydown', {
                            key: 'Enter', 
                            keyCode: 13, 
                            which: 13, 
                            bubbles: true, 
                            cancelable: true
                        }));
                    }
                """.trimIndent(), null)
                return true
            }
            
            // D-pad navigation - enhanced for Fire TV
            KeyEvent.KEYCODE_DPAD_UP -> {
                webView.evaluateJavascript("document.dispatchEvent(new KeyboardEvent('keydown', {keyCode: 38, which: 38}));", null)
                return true
            }
            
            KeyEvent.KEYCODE_DPAD_DOWN -> {
                webView.evaluateJavascript("document.dispatchEvent(new KeyboardEvent('keydown', {keyCode: 40, which: 40}));", null)
                return true
            }
            
            KeyEvent.KEYCODE_DPAD_LEFT -> {
                webView.evaluateJavascript("document.dispatchEvent(new KeyboardEvent('keydown', {keyCode: 37, which: 37}));", null)
                return true
            }
            
            KeyEvent.KEYCODE_DPAD_RIGHT -> {
                webView.evaluateJavascript("document.dispatchEvent(new KeyboardEvent('keydown', {keyCode: 39, which: 39}));", null)
                return true
            }
            
            // Menu button - refresh page
            KeyEvent.KEYCODE_MENU -> {
                webView.reload()
                return true
            }
        }
        
        return super.onKeyDown(keyCode, event)
    }
    
    private fun injectTvOptimizations() {
        val tvOptimizationScript = """
            (function() {
                // Add TV-friendly focus styles and viewport fixes
                var style = document.createElement('style');
                style.textContent = `
                    * { 
                        -webkit-user-select: none; 
                        -webkit-touch-callout: none; 
                    }
                    
                    /* TV viewport fixes */
                    html, body {
                        width: 100vw !important;
                        height: 100vh !important;
                        overflow-x: hidden !important;
                        margin: 0 !important;
                        padding: 0 !important;
                        box-sizing: border-box !important;
                    }
                    
                    /* Orientation-specific styles */
                    @media (orientation: landscape) {
                        body.landscape-mode {
                            /* Landscape optimizations */
                        }
                    }
                    
                    @media (orientation: portrait) {
                        body.portrait-mode {
                            /* Portrait optimizations */
                        }
                    }
                    
                    /* Container fixes for TV */
                    .container, .main-container, #app, [class*="container"] {
                        width: 100% !important;
                        max-width: none !important;
                        min-height: 100vh !important;
                    }
                    
                    /* Remove ALL blue tap highlights and focus outlines globally */
                    *, *:focus, *:active, *:hover, *:visited {
                        -webkit-tap-highlight-color: transparent !important;
                        -webkit-touch-callout: none !important;
                        -webkit-focus-ring-color: transparent !important;
                        outline: none !important;
                        outline-width: 0 !important;
                        outline-style: none !important;
                        outline-color: transparent !important;
                    }
                    
                    /* Specifically target common elements that show blue outlines */
                    input, button, select, textarea, a, div, span {
                        -webkit-tap-highlight-color: transparent !important;
                        -webkit-focus-ring-color: transparent !important;
                        outline: none !important;
                        box-shadow: none !important;
                    }
                    
                    /* Remove WebView default focus styles */
                    input:focus, button:focus, select:focus, textarea:focus, a:focus {
                        outline: none !important;
                        -webkit-tap-highlight-color: transparent !important;
                        -webkit-focus-ring-color: transparent !important;
                        box-shadow: none !important;
                    }
                    
                    /* Mobile devices - ensure normal scrolling */
                    .mobile-mode {
                        overflow: auto !important;
                        height: auto !important;
                        -webkit-overflow-scrolling: touch !important;
                    }
                    
                    /* Only show subtle focus for actual TV navigation */
                    .tv-mode button:focus, 
                    .tv-mode a:focus, 
                    .tv-mode [tabindex]:focus,
                    .tv-mode .focusable:focus, 
                    .tv-mode [role="button"]:focus {
                        outline: 2px solid rgba(3, 218, 197, 0.6) !important;
                        outline-offset: 2px !important;
                        background-color: rgba(3, 218, 197, 0.1) !important;
                        transition: all 0.2s ease !important;
                    }
                    
                    /* Movie/content card focus for TV only */
                    .tv-mode .movie-card:focus, 
                    .tv-mode .content-item:focus, 
                    .tv-mode [class*="card"]:focus {
                        outline: 2px solid rgba(3, 218, 197, 0.6) !important;
                        outline-offset: 1px !important;
                        transform: scale(1.05) !important;
                    }
                `;
                document.head.appendChild(style);
                
                // Add viewport meta tag for proper TV scaling
                var viewport = document.querySelector('meta[name="viewport"]');
                if (!viewport) {
                    viewport = document.createElement('meta');
                    viewport.name = 'viewport';
                    document.head.appendChild(viewport);
                }
                // Force landscape-friendly viewport for TV
                viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover';
                
                // Detect if this is actually a TV device
                var isActualTV = false;
                var userAgent = navigator.userAgent.toLowerCase();
                
                // Check for Fire TV specifically first
                var isFireTV = userAgent.includes('aftm') || 
                              userAgent.includes('aftb') || 
                              userAgent.includes('afts') ||
                              userAgent.includes('firetv');
                
                // Check for actual TV indicators - be more specific
                if (isFireTV || 
                    (userAgent.includes('tv') && !userAgent.includes('mobile')) || 
                    (window.innerWidth > 1280 && window.innerHeight > 720 && !('ontouchstart' in window))) {
                    isActualTV = true;
                    document.body.classList.add('tv-mode');
                    if (isFireTV) {
                        document.body.classList.add('fire-tv');
                        console.log('🔥 Fire TV device detected - User Agent:', userAgent);
                    } else {
                        console.log('📺 TV device detected - User Agent:', userAgent);
                    }
                } else {
                    document.body.classList.add('mobile-mode');
                    console.log('📱 Mobile device detected - User Agent:', userAgent);
                    console.log('📱 Screen size:', window.innerWidth + 'x' + window.innerHeight);
                    console.log('📱 Touch support:', 'ontouchstart' in window);
                }
                
                // Force body to use full viewport
                document.body.style.width = '100vw';
                document.body.style.height = '100vh';
                document.body.style.margin = '0';
                document.body.style.padding = '0';
                
                // Only disable scrolling on actual TV devices
                if (isActualTV) {
                    document.body.style.overflow = 'hidden';
                    document.body.style.height = '100vh';
                } else {
                    // Mobile devices need proper scrolling
                    document.body.style.overflow = 'auto';
                    document.body.style.height = 'auto';
                    document.body.style.minHeight = '100vh';
                    document.documentElement.style.overflow = 'auto';
                    document.documentElement.style.height = 'auto';
                    
                    // Enable touch scrolling
                    document.body.style.webkitOverflowScrolling = 'touch';
                    document.body.style.overflowScrolling = 'touch';
                }
                
                // Make all clickable elements focusable and add TV navigation
                var clickables = document.querySelectorAll('button, a, [onclick], .clickable, [role="button"], .movie-card, .content-item, [class*="card"]');
                clickables.forEach(function(el, index) {
                    if (!el.hasAttribute('tabindex')) {
                        el.setAttribute('tabindex', '0');
                    }
                    el.setAttribute('data-nav-index', index);
                });
                
                // TV Remote Navigation System
                var currentFocusIndex = 0;
                var focusableElements = Array.from(clickables);
                
                // Only activate TV navigation on actual TV devices
                setTimeout(function() {
                    if (isActualTV) {
                        // Make body focusable for remote navigation
                        document.body.setAttribute('tabindex', '0');
                        document.body.focus();
                        
                        // Initialize TV navigation if available
                        if (window.tvNavigation && typeof window.tvNavigation.initialize === 'function') {
                            console.log('🎮 Initializing TV navigation module...');
                            window.tvNavigation.initialize();
                        }
                        
                        // Wait a bit then activate main navigation
                        setTimeout(function() {
                            if (window.navigation && typeof window.navigation.activateNavigation === 'function') {
                                console.log('🎮 Activating main navigation...');
                                window.navigation.activateNavigation();
                            }
                            
                            // Ensure we start with proper focus
                            var menuItems = document.querySelectorAll('.menu-item');
                            if (menuItems.length > 0) {
                                // Focus on Home button (index 1)
                                if (menuItems[1]) {
                                    menuItems[1].focus();
                                    menuItems[1].classList.add('focused');
                                }
                            }
                            
                            console.log('🎮 Fire TV navigation fully initialized and ready');
                        }, 500);
                        
                    } else {
                        console.log('📱 Mobile device - TV navigation disabled');
                        // Remove any tabindex that might interfere with touch
                        document.body.removeAttribute('tabindex');
                    }
                }, 300);
                
                // Enhanced keyboard/remote navigation
                document.addEventListener('keydown', function(e) {
                    var current = document.activeElement;
                    var currentIndex = focusableElements.indexOf(current);
                    
                    switch(e.keyCode) {
                        case 13: // Enter/OK button
                            e.preventDefault();
                            if (current && (current.click || current.onclick)) {
                                current.click();
                            }
                            break;
                            
                        case 37: // Left arrow / D-pad left
                            e.preventDefault();
                            navigateHorizontal(-1, currentIndex);
                            break;
                            
                        case 39: // Right arrow / D-pad right
                            e.preventDefault();
                            navigateHorizontal(1, currentIndex);
                            break;
                            
                        case 38: // Up arrow / D-pad up
                            e.preventDefault();
                            navigateVertical(-1, currentIndex);
                            break;
                            
                        case 40: // Down arrow / D-pad down
                            e.preventDefault();
                            navigateVertical(1, currentIndex);
                            break;
                            
                        case 8: // Back button
                            e.preventDefault();
                            window.history.back();
                            break;
                    }
                });
                
                function navigateHorizontal(direction, currentIndex) {
                    var newIndex = currentIndex + direction;
                    if (newIndex >= 0 && newIndex < focusableElements.length) {
                        focusableElements[newIndex].focus();
                        scrollIntoViewIfNeeded(focusableElements[newIndex]);
                    }
                }
                
                function navigateVertical(direction, currentIndex) {
                    // Try to find element in same column but different row
                    var current = focusableElements[currentIndex];
                    if (!current) return;
                    
                    var currentRect = current.getBoundingClientRect();
                    var candidates = focusableElements.filter(function(el, index) {
                        if (index === currentIndex) return false;
                        var rect = el.getBoundingClientRect();
                        var isInSameColumn = Math.abs(rect.left - currentRect.left) < 100;
                        var isInCorrectDirection = direction > 0 ? rect.top > currentRect.top : rect.top < currentRect.top;
                        return isInSameColumn && isInCorrectDirection;
                    });
                    
                    if (candidates.length > 0) {
                        // Sort by distance and focus closest
                        candidates.sort(function(a, b) {
                            var aRect = a.getBoundingClientRect();
                            var bRect = b.getBoundingClientRect();
                            var aDist = Math.abs(aRect.top - currentRect.top);
                            var bDist = Math.abs(bRect.top - currentRect.top);
                            return aDist - bDist;
                        });
                        candidates[0].focus();
                        scrollIntoViewIfNeeded(candidates[0]);
                    } else {
                        // Fallback to simple navigation
                        navigateHorizontal(direction * 5, currentIndex);
                    }
                }
                
                function scrollIntoViewIfNeeded(element) {
                    var rect = element.getBoundingClientRect();
                    var isVisible = rect.top >= 0 && rect.bottom <= window.innerHeight;
                    if (!isVisible) {
                        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
                
                // Handle dynamic content loading
                var observer = new MutationObserver(function(mutations) {
                    mutations.forEach(function(mutation) {
                        if (mutation.addedNodes.length > 0) {
                            // Re-scan for new focusable elements
                            setTimeout(function() {
                                var newClickables = document.querySelectorAll('button, a, [onclick], .clickable, [role="button"], .movie-card, .content-item, [class*="card"]');
                                newClickables.forEach(function(el, index) {
                                    if (!el.hasAttribute('tabindex')) {
                                        el.setAttribute('tabindex', '0');
                                    }
                                });
                                focusableElements = Array.from(newClickables);
                            }, 100);
                        }
                    });
                });
                
                observer.observe(document.body, { childList: true, subtree: true });
                
                console.log('TV Navigation initialized with', focusableElements.length, 'focusable elements');
            })();
        """.trimIndent()
        
        webView.evaluateJavascript(tvOptimizationScript, null)
    }
    
    override fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
        super.onConfigurationChanged(newConfig)
        
        // Handle orientation changes
        when (newConfig.orientation) {
            android.content.res.Configuration.ORIENTATION_LANDSCAPE -> {
                // Landscape mode - inject CSS to optimize for wide screens
                webView.evaluateJavascript(
                    """
                    document.body.classList.add('landscape-mode');
                    document.body.classList.remove('portrait-mode');
                    """.trimIndent(),
                    null
                )
            }
            android.content.res.Configuration.ORIENTATION_PORTRAIT -> {
                // Portrait mode
                webView.evaluateJavascript(
                    """
                    document.body.classList.add('portrait-mode');
                    document.body.classList.remove('landscape-mode');
                    """.trimIndent(),
                    null
                )
            }
        }
        
        // Re-inject TV optimizations after orientation change
        webView.post {
            injectTvOptimizations()
        }
    }
    
    override fun onDestroy() {
        super.onDestroy()
        webView.destroy()
    }
}