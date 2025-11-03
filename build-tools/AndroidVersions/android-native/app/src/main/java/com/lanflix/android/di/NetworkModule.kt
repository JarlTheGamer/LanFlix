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
            .addInterceptor { chain ->
                val request = chain.request()
                println("Making request to: ${request.url}")
                try {
                    val response = chain.proceed(request)
                    println("Response: ${response.code} for ${request.url}")
                    response
                } catch (e: Exception) {
                    println("Request failed for ${request.url}: ${e.message}")
                    throw e
                }
            }
            .connectTimeout(15, java.util.concurrent.TimeUnit.SECONDS) // Increased timeout
            .readTimeout(30, java.util.concurrent.TimeUnit.SECONDS)
            .writeTimeout(30, java.util.concurrent.TimeUnit.SECONDS)
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
                    // Try common server addresses first
                    "http://192.168.178.13:5037/" // Use the actual server IP from logs
                } else {
                    url
                }
            } catch (e: Exception) {
                "http://192.168.178.13:5037/" // Use the actual server IP from logs
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