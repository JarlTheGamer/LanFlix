package com.lanflix.android.data.repository

import com.lanflix.android.data.api.LanflixApiService
import com.lanflix.android.domain.model.Content
import com.lanflix.android.domain.model.StreamInfo
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ContentRepository @Inject constructor(
    private val apiService: LanflixApiService
) {
    
    suspend fun getMovies(): List<Content> {
        return try {
            val response = apiService.getMovies()
            if (response.isSuccessful) {
                val body = response.body()
                println("ContentRepository: Movies response successful, items count: ${body?.items?.size ?: 0}")
                body?.items ?: emptyList()
            } else {
                val errorBody = response.errorBody()?.string()
                println("ContentRepository: Movies request failed with code ${response.code()}: ${response.message()}")
                println("ContentRepository: Error body: $errorBody")
                throw Exception("Failed to load movies: HTTP ${response.code()} - ${response.message()}")
            }
        } catch (e: Exception) {
            println("ContentRepository: Exception loading movies: ${e.message}")
            throw Exception("Failed to load movies: ${e.message}")
        }
    }
    
    suspend fun getSeries(): List<Content> {
        return try {
            val response = apiService.getSeries()
            if (response.isSuccessful) {
                val body = response.body()
                println("ContentRepository: Series response successful, items count: ${body?.items?.size ?: 0}")
                body?.items ?: emptyList()
            } else {
                val errorBody = response.errorBody()?.string()
                println("ContentRepository: Series request failed with code ${response.code()}: ${response.message()}")
                println("ContentRepository: Error body: $errorBody")
                throw Exception("Failed to load series: HTTP ${response.code()} - ${response.message()}")
            }
        } catch (e: Exception) {
            println("ContentRepository: Exception loading series: ${e.message}")
            throw Exception("Failed to load series: ${e.message}")
        }
    }
    
    suspend fun getContentDetails(contentId: String): Content {
        val response = apiService.getContentDetails(contentId)
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Content not found")
        } else {
            throw Exception("Failed to load content: ${response.message()}")
        }
    }
    
    suspend fun searchContent(query: String, type: String? = null): List<Content> {
        return try {
            val response = apiService.searchContent(query, type)
            if (response.isSuccessful) {
                val body = response.body()
                println("ContentRepository: Search response successful, items count: ${body?.size ?: 0}")
                body ?: emptyList()
            } else {
                val errorBody = response.errorBody()?.string()
                println("ContentRepository: Search request failed with code ${response.code()}: ${response.message()}")
                println("ContentRepository: Error body: $errorBody")
                // Don't throw for search failures, just return empty list
                emptyList()
            }
        } catch (e: Exception) {
            println("ContentRepository: Exception during search: ${e.message}")
            // Don't throw for search failures, just return empty list
            emptyList()
        }
    }
    
    suspend fun getStreamInfo(contentId: String): StreamInfo {
        val response = apiService.getStreamInfo(contentId)
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Stream info not found")
        } else {
            throw Exception("Failed to get stream info: ${response.message()}")
        }
    }
}