package com.lanflix.webview

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.util.Log
import kotlinx.coroutines.*
import java.net.InetAddress

class ServerDiscoveryManager(
    private val context: Context,
    private val onDiscovered: (serverName: String, url: String) -> Unit
) {
    private val TAG = "ServerDiscoveryManager"
    private var nsdManager: NsdManager? = null
    private var discoveryListener: NsdManager.DiscoveryListener? = null
    private val discoveredUrls = mutableSetOf<String>()
    private val scope = CoroutineScope(Dispatchers.Main + Job())

    fun startDiscovery() {
        stopDiscovery()
        discoveredUrls.clear()

        // 1. Proactively test lanflix.local default host
        scope.launch {
            checkAndReport("http://lanflix.local:5037", "Lanflix Server (lanflix.local)")
        }

        // 2. Start Android Network Service Discovery (mDNS)
        nsdManager = context.getSystemService(Context.NSD_SERVICE) as? NsdManager
        if (nsdManager == null) {
            Log.w(TAG, "NSD Service unavailable on this device")
            return
        }

        discoveryListener = object : NsdManager.DiscoveryListener {
            override fun onDiscoveryStarted(regType: String) {
                Log.d(TAG, "mDNS Service Discovery started for $regType")
            }

            override fun onServiceFound(serviceInfo: NsdServiceInfo) {
                Log.d(TAG, "Service found: ${serviceInfo.serviceName}, type: ${serviceInfo.serviceType}")
                val serviceName = serviceInfo.serviceName.lowercase()
                val serviceType = serviceInfo.serviceType.lowercase()

                if (serviceName.contains("lanflix") || serviceType.contains("lanflix") || serviceType.contains("http")) {
                    resolveService(serviceInfo)
                }
            }

            override fun onServiceLost(serviceInfo: NsdServiceInfo) {
                Log.d(TAG, "Service lost: ${serviceInfo.serviceName}")
            }

            override fun onDiscoveryStopped(serviceType: String) {
                Log.d(TAG, "Discovery stopped: $serviceType")
            }

            override fun onStartDiscoveryFailed(serviceType: String, errorCode: Int) {
                Log.e(TAG, "Discovery failed to start: Error code $errorCode")
                nsdManager?.stopServiceDiscovery(this)
            }

            override fun onStopDiscoveryFailed(serviceType: String, errorCode: Int) {
                Log.e(TAG, "Discovery failed to stop: Error code $errorCode")
                nsdManager?.stopServiceDiscovery(this)
            }
        }

        try {
            nsdManager?.discoverServices("_lanflix._tcp.", NsdManager.PROTOCOL_DNS_SD, discoveryListener)
        } catch (e: Exception) {
            Log.e(TAG, "Failed to start _lanflix._tcp discovery", e)
            try {
                nsdManager?.discoverServices("_http._tcp.", NsdManager.PROTOCOL_DNS_SD, discoveryListener)
            } catch (ex: Exception) {
                Log.e(TAG, "Failed to start fallback _http._tcp discovery", ex)
            }
        }
    }

    private fun resolveService(serviceInfo: NsdServiceInfo) {
        val resolveListener = object : NsdManager.ResolveListener {
            override fun onResolveFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {
                Log.e(TAG, "Resolve failed for ${serviceInfo.serviceName}: $errorCode")
            }

            override fun onServiceResolved(serviceInfo: NsdServiceInfo) {
                val host: InetAddress = serviceInfo.host ?: return
                val port = if (serviceInfo.port > 0) serviceInfo.port else ServerManager.DEFAULT_PORT
                val hostAddress = host.hostAddress ?: host.hostName ?: return
                val url = "http://$hostAddress:$port"
                val name = "Lanflix Server ($hostAddress)"

                // Save resolved mDNS IP mapping for lanflix.local
                ServerManager.saveResolvedIp(context, "lanflix.local", hostAddress)

                scope.launch {
                    checkAndReport(url, name)
                    checkAndReport("http://lanflix.local:$port", "Lanflix Server (lanflix.local)")
                }
            }
        }

        try {
            nsdManager?.resolveService(serviceInfo, resolveListener)
        } catch (e: Exception) {
            Log.e(TAG, "Failed to resolve service ${serviceInfo.serviceName}", e)
        }
    }

    private suspend fun checkAndReport(url: String, name: String) {
        val formatted = ServerManager.formatServerUrl(url)
        if (discoveredUrls.contains(formatted)) return

        val isOnline = ServerManager.pingServer(context, formatted, timeoutMs = 2500)
        if (isOnline) {
            discoveredUrls.add(formatted)
            withContext(Dispatchers.Main) {
                onDiscovered(name, formatted)
            }
        }
    }

    fun stopDiscovery() {
        discoveryListener?.let { listener ->
            try {
                nsdManager?.stopServiceDiscovery(listener)
            } catch (e: Exception) {
                Log.e(TAG, "Error stopping discovery", e)
            }
        }
        discoveryListener = null
        scope.coroutineContext.cancelChildren()
    }
}
