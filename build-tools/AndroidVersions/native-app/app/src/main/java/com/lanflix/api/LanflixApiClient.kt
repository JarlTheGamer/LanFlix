package com.lanflix.api

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.models.ContentItem
import com.lanflix.models.LibraryResponse
import com.lanflix.models.SeasonEpisodesResponse
import com.lanflix.models.SeasonSummary
import com.lanflix.models.SeriesSeasonsResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

import com.lanflix.webview.ServerManager
import android.content.Context
import com.lanflix.offline.OfflineMediaStore

class LanflixApiClient(context: Context, private val baseUrl: String = ServerManager.activeServerUrl) {

    private val client = OkHttpClient.Builder()
        .connectTimeout(6, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()

    private val gson = Gson()
    private val offlineStore = OfflineMediaStore(context)

    suspend fun getHomeContent(): List<ContentItem> = withContext(Dispatchers.IO) {
        val movies = getMovies()
        val series = if (ServerManager.isOnline) getSeries() else emptyList()
        val library = (movies + series).distinctBy { "${it.type}:${it.id}" }
        if (library.isNotEmpty()) return@withContext library

        val collections = getCollections()
        if (collections.isNotEmpty()) return@withContext collections

        emptyList()
    }

    suspend fun getSeries(): List<ContentItem> = getLibraryPage("series")

    suspend fun getSeriesSeasons(seriesId: Int): List<SeasonSummary> = withContext(Dispatchers.IO) {
        runCatching {
            val request = Request.Builder().url("$baseUrl/api/series/$seriesId/seasons").get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@use emptyList()
                gson.fromJson(response.body?.string(), SeriesSeasonsResponse::class.java)?.seasons.orEmpty()
            }
        }.getOrDefault(emptyList())
    }

    suspend fun getSeasonEpisodes(seriesId: Int, seasonNumber: Int) = withContext(Dispatchers.IO) {
        runCatching {
            val request = Request.Builder().url("$baseUrl/api/series/$seriesId/seasons/$seasonNumber/episodes").get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@use SeasonEpisodesResponse()
                gson.fromJson(response.body?.string(), SeasonEpisodesResponse::class.java) ?: SeasonEpisodesResponse()
            }
        }.getOrDefault(SeasonEpisodesResponse())
    }

    private suspend fun getLibraryPage(kind: String): List<ContentItem> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder().url("$baseUrl/api/library/$kind?limit=50").get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val body = response.body?.string() ?: return@withContext emptyList()
                val items = gson.fromJson(body, LibraryResponse::class.java)?.items ?: emptyList()
                offlineStore.cacheLibrary(items)
                items
            }
        } catch (_: Exception) {
            emptyList()
        }
    }

    suspend fun getMovies(): List<ContentItem> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url("$baseUrl/api/library/movies?limit=50")
                .get()
                .build()

            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext offlineStore.readCatalog()
                val body = response.body?.string() ?: return@withContext offlineStore.readCatalog()
                
                val libraryResponse = gson.fromJson(body, LibraryResponse::class.java)
                val items = libraryResponse?.items ?: emptyList()
                offlineStore.cacheLibrary(items)
                return@withContext items
            }
        } catch (e: Exception) {
            offlineStore.readCatalog()
        }
    }

    suspend fun getOfflineCatalog(): List<ContentItem> = withContext(Dispatchers.IO) {
        offlineStore.readCatalog()
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
