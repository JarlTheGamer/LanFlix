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
// Removed mDNS imports - using IP scanning only

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
            // Emit empty list first to show discovery started
            emit(emptyList())
            
            // Use IP scanning to find Lanflix servers
            scanCommonIpRanges(servers)
            emit(servers.toList())
        } catch (e: Exception) {
            // Return empty list on error
            emit(emptyList())
        }
    }
    
    private suspend fun scanCommonIpRanges(servers: MutableList<ServerInfo>) {
        // Get local network range
        val localIp = getLocalIpAddress()?.hostAddress ?: return
        val networkPrefix = localIp.substringBeforeLast(".")
        
        // Lanflix server ports - prioritize 5037
        val lanflixPorts = listOf(5037, 8080, 3000, 5000, 8000, 5001)
        
        // First, try the known server IP from logs
        val knownServerIps = listOf("192.168.178.13")
        for (knownIp in knownServerIps) {
            for (port in lanflixPorts) {
                try {
                    val testUrl = "http://$knownIp:$port"
                    println("Testing known server: $testUrl")
                    val serverInfo = testConnection(testUrl)
                    servers.add(serverInfo)
                    println("Successfully found known server: $testUrl")
                    return // Found the server, no need to scan further
                } catch (e: Exception) {
                    println("Known server test failed for $knownIp:$port - ${e.message}")
                }
            }
        }
        
        // If known server not found, scan broader IP range
        val ipRanges = listOf(
            1..50,   // Common router DHCP range
            100..150, // Extended DHCP range
            200..254  // High range
        )
        
        for (range in ipRanges) {
            for (i in range) {
                val testIp = "$networkPrefix.$i"
                
                // Skip our own IP and already tested known IPs
                if (testIp == localIp || knownServerIps.contains(testIp)) continue
                
                for (port in lanflixPorts) {
                    try {
                        val testUrl = "http://$testIp:$port"
                        val serverInfo = testConnection(testUrl)
                        
                        // Check if this server is already found (different port same IP)
                        val existingServer = servers.find { it.baseUrl.contains(testIp) }
                        if (existingServer == null) {
                            servers.add(serverInfo)
                        }
                        break // Found server on this IP, no need to test other ports
                    } catch (e: Exception) {
                        // Continue to next port/IP
                    }
                }
            }
        }
    }
    
    suspend fun testConnection(url: String): ServerInfo {
        val client = OkHttpClient.Builder()
            .connectTimeout(2, TimeUnit.SECONDS) // Faster timeout for discovery
            .readTimeout(3, TimeUnit.SECONDS)
            .build()
        
        val retrofit = Retrofit.Builder()
            .baseUrl(if (url.endsWith("/")) url else "$url/")
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
        
        val apiService = retrofit.create(LanflixApiService::class.java)
        
        return try {
            // Test multiple endpoints to confirm it's a Lanflix server
            println("Testing connection to: $url")
            val profilesResponse = apiService.getProfiles()
            
            if (profilesResponse.isSuccessful) {
                println("Successfully connected to Lanflix server at: $url")
                // Try to get server info if available
                var serverName = "Lanflix Server"
                var version = "Unknown"
                
                try {
                    // You can add a server info endpoint later
                    // val infoResponse = apiService.getServerInfo()
                    // if (infoResponse.isSuccessful) {
                    //     serverName = infoResponse.body()?.name ?: serverName
                    //     version = infoResponse.body()?.version ?: version
                    // }
                } catch (e: Exception) {
                    // Server info endpoint might not exist, that's ok
                }
                
                ServerInfo(
                    baseUrl = url,
                    name = serverName,
                    version = version,
                    isConnected = true
                )
            } else {
                println("Server at $url responded with error: ${profilesResponse.code()}")
                throw Exception("Server responded with error: ${profilesResponse.code()}")
            }
        } catch (e: Exception) {
            println("Failed to connect to $url: ${e.message}")
            throw Exception("Cannot connect to server at $url: ${e.message}")
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