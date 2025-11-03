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
        val response = apiService.getMovies()
        if (response.isSuccessful) {
            return response.body()?.items ?: emptyList()
        } else {
            throw Exception("Failed to load movies: ${response.message()}")
        }
    }
    
    suspend fun getSeries(): List<Content> {
        val response = apiService.getSeries()
        if (response.isSuccessful) {
            return response.body()?.items ?: emptyList()
        } else {
            throw Exception("Failed to load series: ${response.message()}")
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
        val response = apiService.searchContent(query, type)
        if (response.isSuccessful) {
            return response.body() ?: emptyList()
        } else {
            throw Exception("Search failed: ${response.message()}")
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