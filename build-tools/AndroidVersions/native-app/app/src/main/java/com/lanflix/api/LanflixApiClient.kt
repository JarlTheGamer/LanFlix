package com.lanflix.api

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.models.ContentItem
import com.lanflix.models.LibraryResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

import com.lanflix.webview.ServerManager

class LanflixApiClient(private val baseUrl: String = ServerManager.activeServerUrl) {

    private val client = OkHttpClient.Builder()
        .connectTimeout(6, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()

    private val gson = Gson()

    suspend fun getHomeContent(): List<ContentItem> = withContext(Dispatchers.IO) {
        val movies = getMovies()
        if (movies.isNotEmpty()) return@withContext movies

        val collections = getCollections()
        if (collections.isNotEmpty()) return@withContext collections

        emptyList()
    }

    suspend fun getMovies(): List<ContentItem> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url("$baseUrl/api/library/movies?limit=50")
                .get()
                .build()

            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val body = response.body?.string() ?: return@withContext emptyList()
                
                val libraryResponse = gson.fromJson(body, LibraryResponse::class.java)
                return@withContext libraryResponse?.items ?: emptyList()
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
