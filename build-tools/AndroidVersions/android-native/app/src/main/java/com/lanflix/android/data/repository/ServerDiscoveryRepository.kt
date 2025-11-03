package com.lanflix.android.data.repository

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.lanflix.android.data.api.LanflixApiService
import com.lanflix.android.domain.model.ServerInfo
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.net.InetAddress
import java.net.NetworkInterface
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton
import javax.jmdns.JmDNS
import javax.jmdns.ServiceEvent
import javax.jmdns.ServiceListener

val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "server_settings")

@Singleton
class ServerDiscoveryRepository @Inject constructor(
    @ApplicationContext private val context: Context
) {
    
    private val SERVER_URL_KEY = stringPreferencesKey("server_url")
    private val SERVER_NAME_KEY = stringPreferencesKey("server_name")
    
    fun discoverServers(): Flow<List<ServerInfo>> = flow {
        val servers = mutableListOf<ServerInfo>()
        
        try {
            // Get local IP address
            val localAddress = getLocalIpAddress()
            if (localAddress != null) {
                val jmdns = JmDNS.create(localAddress)
                
                val serviceListener = object : ServiceListener {
                    override fun serviceAdded(event: ServiceEvent) {
                        // Service discovered, request more info
                        jmdns.requestServiceInfo(event.type, event.name)
                    }
                    
                    override fun serviceRemoved(event: ServiceEvent) {
                        // Handle service removal if needed
                    }
                    
                    override fun serviceResolved(event: ServiceEvent) {
                        val info = event.info
                        if (info != null) {
                            val serverInfo = ServerInfo(
                                baseUrl = "http://${info.hostAddresses[0]}:${info.port}",
                                name = info.name,
                                version = info.getPropertyString("version") ?: "Unknown",
                                isConnected = false
                            )
                            servers.add(serverInfo)
                            // Emit updated list
                        }
                    }
                }
                
                // Listen for Lanflix services (you'll need to add mDNS to your server)
                jmdns.addServiceListener("_lanflix._tcp.local.", serviceListener)
                
                // Also try common IP ranges for fallback
                scanCommonIpRanges(servers)
                
                emit(servers.toList())
                
                // Clean up
                jmdns.removeServiceListener("_lanflix._tcp.local.", serviceListener)
                jmdns.close()
            } else {
                // Fallback to IP scanning
                scanCommonIpRanges(servers)
                emit(servers.toList())
            }
        } catch (e: Exception) {
            // Fallback to IP scanning
            scanCommonIpRanges(servers)
            emit(servers.toList())
        }
    }
    
    private suspend fun scanCommonIpRanges(servers: MutableList<ServerInfo>) {
        // Get local network range
        val localIp = getLocalIpAddress()?.hostAddress ?: return
        val networkPrefix = localIp.substringBeforeLast(".")
        
        // Common Lanflix ports
        val commonPorts = listOf(5037, 8080, 3000, 5000)
        
        // Scan local network range (last 20 IPs for performance)
        for (i in 1..20) {
            val testIp = "$networkPrefix.$i"
            for (port in commonPorts) {
                try {
                    val testUrl = "http://$testIp:$port"
                    val serverInfo = testConnection(testUrl)
                    servers.add(serverInfo)
                    break // Found server on this IP, no need to test other ports
                } catch (e: Exception) {
                    // Continue to next port/IP
                }
            }
        }
    }
    
    suspend fun testConnection(url: String): ServerInfo {
        val client = OkHttpClient.Builder()
            .connectTimeout(3, TimeUnit.SECONDS)
            .readTimeout(5, TimeUnit.SECONDS)
            .build()
        
        val retrofit = Retrofit.Builder()
            .baseUrl(if (url.endsWith("/")) url else "$url/")
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
        
        val apiService = retrofit.create(LanflixApiService::class.java)
        
        // Try to get server info (you might need to add this endpoint)
        return try {
            // For now, just test if we can reach the profiles endpoint
            val response = apiService.getProfiles()
            if (response.isSuccessful) {
                ServerInfo(
                    baseUrl = url,
                    name = "Lanflix Server",
                    version = "Unknown",
                    isConnected = true
                )
            } else {
                throw Exception("Server responded with error: ${response.code()}")
            }
        } catch (e: Exception) {
            throw Exception("Cannot connect to server: ${e.message}")
        }
    }
    
    suspend fun saveServerConnection(serverInfo: ServerInfo) {
        context.dataStore.edit { preferences ->
            preferences[SERVER_URL_KEY] = serverInfo.baseUrl
            preferences[SERVER_NAME_KEY] = serverInfo.name
        }
        
        // Also save to ServerPreferences for NetworkModule
        val serverPreferences = com.lanflix.android.data.preferences.ServerPreferences(context)
        serverPreferences.saveServerInfo(serverInfo.baseUrl, serverInfo.name)
    }
    
    fun getSavedServerUrl(): Flow<String?> {
        return context.dataStore.data.map { preferences ->
            preferences[SERVER_URL_KEY]
        }
    }
    
    private fun getLocalIpAddress(): InetAddress? {
        try {
            val interfaces = NetworkInterface.getNetworkInterfaces()
            while (interfaces.hasMoreElements()) {
                val networkInterface = interfaces.nextElement()
                if (!networkInterface.isLoopback && networkInterface.isUp) {
                    val addresses = networkInterface.inetAddresses
                    while (addresses.hasMoreElements()) {
                        val address = addresses.nextElement()
                        if (!address.isLoopbackAddress && address.isSiteLocalAddress) {
                            return address
                        }
                    }
                }
            }
        } catch (e: Exception) {
            // Handle exception
        }
        return null
    }
}