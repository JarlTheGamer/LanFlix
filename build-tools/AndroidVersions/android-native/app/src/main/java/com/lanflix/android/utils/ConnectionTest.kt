package com.lanflix.android.utils

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

object ConnectionTest {
    
    private val client = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()
    
    suspend fun testServerConnection(serverUrl: String): ConnectionResult {
        return withContext(Dispatchers.IO) {
            try {
                val url = if (serverUrl.endsWith("/")) serverUrl else "$serverUrl/"
                val testUrl = "${url}api/profiles"
                
                println("ConnectionTest: Testing connection to $testUrl")
                
                val request = Request.Builder()
                    .url(testUrl)
                    .build()
                
                val response = client.newCall(request).execute()
                
                if (response.isSuccessful) {
                    println("ConnectionTest: Successfully connected to server")
                    ConnectionResult.Success("Connected successfully to $serverUrl")
                } else {
                    println("ConnectionTest: Server responded with error: ${response.code}")
                    ConnectionResult.Error("Server error: ${response.code} ${response.message}")
                }
            } catch (e: Exception) {
                println("ConnectionTest: Connection failed: ${e.message}")
                val errorMessage = when {
                    e.message?.contains("ConnectException") == true -> "Cannot reach server at $serverUrl"
                    e.message?.contains("timeout") == true -> "Connection timeout - server may be slow"
                    e.message?.contains("UnknownHostException") == true -> "Server address not found"
                    else -> "Connection failed: ${e.message}"
                }
                ConnectionResult.Error(errorMessage)
            }
        }
    }
    
    suspend fun testCommonServerAddresses(): List<Pair<String, ConnectionResult>> {
        val commonAddresses = listOf(
            "http://192.168.178.13:5037",
            "http://localhost:5037",
            "http://127.0.0.1:5037",
            "http://192.168.1.100:5037",
            "http://192.168.0.100:5037"
        )
        
        return commonAddresses.map { address ->
            address to testServerConnection(address)
        }
    }
}

sealed class ConnectionResult {
    data class Success(val message: String) : ConnectionResult()
    data class Error(val message: String) : ConnectionResult()
}