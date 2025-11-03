package com.lanflix.android.data.api

import com.lanflix.android.domain.model.Profile
import com.lanflix.android.domain.model.Content
import com.lanflix.android.domain.model.StreamInfo
import retrofit2.Response
import retrofit2.http.*

interface LanflixApiService {
    
    // Profile Management
    @GET("api/profiles")
    suspend fun getProfiles(): Response<List<Profile>>
    
    @POST("api/profiles")
    suspend fun createProfile(
        @Body profile: CreateProfileRequest
    ): Response<Profile>
    
    @PUT("api/profiles/{id}")
    suspend fun updateProfile(
        @Path("id") profileId: String,
        @Body updates: UpdateProfileRequest
    ): Response<Profile>
    
    @DELETE("api/profiles/{id}")
    suspend fun deleteProfile(@Path("id") profileId: String): Response<Unit>
    
    // Content
    @GET("api/content/movies")
    suspend fun getMovies(
        @Query("page") page: Int = 1,
        @Query("limit") limit: Int = 20
    ): Response<ContentResponse>
    
    @GET("api/content/series")
    suspend fun getSeries(
        @Query("page") page: Int = 1,
        @Query("limit") limit: Int = 20
    ): Response<ContentResponse>
    
    @GET("api/content/{id}")
    suspend fun getContentDetails(@Path("id") contentId: String): Response<Content>
    
    // Search
    @GET("api/search")
    suspend fun searchContent(
        @Query("q") query: String,
        @Query("type") type: String? = null
    ): Response<List<Content>>
    
    // Streaming
    @GET("api/stream/{id}")
    suspend fun getStreamInfo(@Path("id") contentId: String): Response<StreamInfo>
    
    // Settings
    @GET("api/settings")
    suspend fun getSettings(): Response<Map<String, Any>>
    
    @PUT("api/settings")
    suspend fun updateSettings(@Body settings: Map<String, Any>): Response<Unit>
}

data class CreateProfileRequest(
    val name: String,
    val avatarColorPrimary: String,
    val avatarColorSecondary: String
)

data class UpdateProfileRequest(
    val name: String? = null,
    val avatarColorPrimary: String? = null,
    val avatarColorSecondary: String? = null
)

data class ContentResponse(
    val items: List<Content>,
    val totalCount: Int,
    val page: Int,
    val totalPages: Int
)