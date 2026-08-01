package com.lanflix.webview

import android.content.Context
import android.content.SharedPreferences
import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.HttpURLConnection
import java.net.InetAddress
import java.net.URL

object ServerManager {
    private const val PREFS_NAME = "lanflix_server_prefs"
    private const val KEY_SAVED_SERVER = "saved_server_url"
    private const val KEY_SAVED_SERVERS_LIST = "saved_servers_list"
    private const val KEY_RESOLVED_IP_PREFIX = "resolved_ip_"

    const val DEFAULT_PORT = 5037
    const val DEFAULT_MDNS_HOST = "http://lanflix.local:5037"

    @Volatile
    var activeServerUrl: String = DEFAULT_MDNS_HOST

    @Volatile
    var isOnline: Boolean = false

    @Volatile
    private var cachedResolvedIp: String? = null

    private fun getPrefs(context: Context): SharedPreferences {
        return context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    }

    fun getSavedServer(context: Context): String {
        return getPrefs(context).getString(KEY_SAVED_SERVER, null) ?: DEFAULT_MDNS_HOST
    }

    fun saveServer(context: Context, serverUrl: String) {
        val formattedUrl = formatServerUrl(serverUrl)
        val prefs = getPrefs(context)
        val currentServers = getSavedServers(context).toMutableSet()
        currentServers.add(formattedUrl)

        prefs.edit()
            .putString(KEY_SAVED_SERVER, formattedUrl)
            .putStringSet(KEY_SAVED_SERVERS_LIST, currentServers)
            .apply()
    }

    fun saveResolvedIp(context: Context, domainOrUrl: String, ipAddress: String) {
        val host = runCatching { Uri.parse(formatServerUrl(domainOrUrl)).host }.getOrNull() ?: domainOrUrl
        cachedResolvedIp = ipAddress
        getPrefs(context).edit()
            .putString(KEY_RESOLVED_IP_PREFIX + host.lowercase(), ipAddress)
            .apply()
    }

    fun getSavedServers(context: Context): Set<String> {
        return getPrefs(context).getStringSet(KEY_SAVED_SERVERS_LIST, emptySet()) ?: setOf(DEFAULT_MDNS_HOST)
    }

    fun formatServerUrl(input: String): String {
        var url = input.trim()
        if (url.isEmpty()) return DEFAULT_MDNS_HOST

        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            url = "http://$url"
        }

        url = url.trimEnd('/')

        val uri = runCatching { Uri.parse(url) }.getOrNull()
        if (uri != null && uri.port == -1) {
            val host = uri.host
            if (host != null && !host.contains(":")) {
                url = "${uri.scheme}://$host:$DEFAULT_PORT"
            }
        }

        return url
    }

    suspend fun resolveUrlForConnection(context: Context, serverUrl: String): String {
        return withContext(Dispatchers.IO) {
            val formatted = formatServerUrl(serverUrl)
            val uri = runCatching { Uri.parse(formatted) }.getOrNull() ?: return@withContext formatted
            val host = uri.host ?: return@withContext formatted
            val port = if (uri.port != -1) uri.port else DEFAULT_PORT
            val scheme = uri.scheme ?: "http"

            // If host is already an IPv4 address, return directly
            if (host.matches(Regex("\\d+\\.\\d+\\.\\d+\\.\\d+"))) {
                return@withContext formatted
            }

            // If host is a .local domain, attempt resolution
            if (host.endsWith(".local", ignoreCase = true)) {
                // 1. Try Java native resolution first
                try {
                    val address = InetAddress.getByName(host)
                    val ip = address.hostAddress
                    if (!ip.isNullOrBlank() && ip != "127.0.0.1") {
                        saveResolvedIp(context, host, ip)
                        return@withContext "$scheme://$ip:$port"
                    }
                } catch (e: Exception) {
                    // Standard Java DNS resolution for .local failed on Android OS
                }

                // 2. Check cached in-memory IP
                cachedResolvedIp?.let { ip ->
                    return@withContext "$scheme://$ip:$port"
                }

                // 3. Check saved IP from SharedPreferences
                val savedIp = getPrefs(context).getString(KEY_RESOLVED_IP_PREFIX + host.lowercase(), null)
                if (!savedIp.isNullOrBlank()) {
                    return@withContext "$scheme://$savedIp:$port"
                }
            }

            formatted
        }
    }

    suspend fun pingServer(context: Context, serverUrl: String, timeoutMs: Int = 2000): Boolean {
        return withContext(Dispatchers.IO) {
            val targetUrl = resolveUrlForConnection(context, serverUrl)

            // Use the versioned Host health endpoint; it does not require an account token.
            try {
                val testUrl = "$targetUrl/health"
                val connection = (URL(testUrl).openConnection() as HttpURLConnection).apply {
                    connectTimeout = timeoutMs
                    readTimeout = timeoutMs
                    requestMethod = "GET"
                    instanceFollowRedirects = true
                    setRequestProperty("User-Agent", "Lanflix-AndroidNativeApp")
                }

                val responseCode = connection.responseCode
                connection.disconnect()
                if (responseCode in 200..299) {
                    return@withContext true
                }
            } catch (e: Exception) { }

            // 2. Fallback test root URL
            try {
                val connection = (URL(targetUrl).openConnection() as HttpURLConnection).apply {
                    connectTimeout = timeoutMs
                    readTimeout = timeoutMs
                    requestMethod = "HEAD"
                    instanceFollowRedirects = true
                    setRequestProperty("User-Agent", "Lanflix-AndroidNativeApp")
                }

                val code = connection.responseCode
                connection.disconnect()
                if (code in 200..499) {
                    return@withContext true
                }
            } catch (ex: Exception) { }

            false
        }
    }
}
