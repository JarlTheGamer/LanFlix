package com.lanflix.api

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.models.ContentItem
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

class LanflixApiClient(private val baseUrl: String = "http://192.168.0.218:5037") {

    private val client = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()

    private val gson = Gson()

    suspend fun getHomeContent(): List<ContentItem> = withContext(Dispatchers.IO) {
        val endpoints = listOf("/api/collections", "/api/content/popular", "/api/movies")
        for (endpoint in endpoints) {
            try {
                val request = Request.Builder()
                    .url("$baseUrl$endpoint")
                    .get()
                    .build()

                client.newCall(request).execute().use { response ->
                    if (response.isSuccessful) {
                        val body = response.body?.string()
                        if (!body.isNullOrBlank()) {
                            val type = object : TypeToken<List<ContentItem>>() {}.type
                            val items = gson.fromJson<List<ContentItem>>(body, type) ?: emptyList()
                            if (items.isNotEmpty()) return@withContext items
                        }
                    }
                }
            } catch (e: Exception) {
                // Try next endpoint
            }
        }
        emptyList()
    }

    suspend fun getMovies(): List<ContentItem> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url("$baseUrl/api/movies")
                .get()
                .build()

            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val body = response.body?.string() ?: return@withContext emptyList()
                
                val type = object : TypeToken<List<ContentItem>>() {}.type
                return@withContext gson.fromJson<List<ContentItem>>(body, type) ?: emptyList()
            }
        } catch (e: Exception) {
            emptyList()
        }
    }

    suspend fun getCollections(): List<ContentItem> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url("$baseUrl/api/collections")
                .get()
                .build()

            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val body = response.body?.string() ?: return@withContext emptyList()
                
                val type = object : TypeToken<List<ContentItem>>() {}.type
                return@withContext gson.fromJson<List<ContentItem>>(body, type) ?: emptyList()
            }
        } catch (e: Exception) {
            emptyList()
        }
    }
}
