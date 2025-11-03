package com.lanflix.android.di

import com.lanflix.android.data.api.LanflixApiService
import com.lanflix.android.data.preferences.ServerPreferences
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.coroutines.runBlocking
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import javax.inject.Qualifier
import javax.inject.Singleton

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class ServerUrl

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {
    
    @Provides
    @Singleton
    fun provideOkHttpClient(): OkHttpClient {
        val loggingInterceptor = HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BODY
        }
        
        return OkHttpClient.Builder()
            .addInterceptor(loggingInterceptor)
            .connectTimeout(10, java.util.concurrent.TimeUnit.SECONDS)
            .readTimeout(30, java.util.concurrent.TimeUnit.SECONDS)
            .build()
    }
    
    @Provides
    @Singleton
    fun provideRetrofit(
        okHttpClient: OkHttpClient,
        serverPreferences: ServerPreferences
    ): Retrofit {
        // Get the saved server URL or use default
        val serverUrl = runBlocking { 
            try {
                val url = serverPreferences.getServerUrl()
                if (url.isBlank()) {
                    "http://localhost:5037/" // Temporary fallback for development
                } else {
                    url
                }
            } catch (e: Exception) {
                "http://localhost:5037/" // Temporary fallback for development
            }
        }
        
        return Retrofit.Builder()
            .baseUrl(serverUrl)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }
    
    @Provides
    @Singleton
    fun provideLanflixApiService(retrofit: Retrofit): LanflixApiService {
        return retrofit.create(LanflixApiService::class.java)
    }
}