package com.lanflix.android

import com.lanflix.android.data.api.LanflixApiService
import kotlinx.coroutines.runBlocking
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class ApiIntegrationTest {

    private lateinit var apiService: LanflixApiService

    @Before
    fun setup() {
        val logging = HttpLoggingInterceptor()
        logging.setLevel(HttpLoggingInterceptor.Level.BODY)
        val client = OkHttpClient.Builder()
            .addInterceptor(logging)
            .build()

        apiService = Retrofit.Builder()
            .baseUrl("http://192.168.178.13:5037/")
            .addConverterFactory(GsonConverterFactory.create())
            .client(client)
            .build()
            .create(LanflixApiService::class.java)
    }

    @Test
    fun testGetProfiles() = runBlocking {
        val response = apiService.getProfiles()
        assertTrue(response.isSuccessful)
    }

    @Test
    fun testGetMovies() = runBlocking {
        val response = apiService.getMovies()
        assertTrue(response.isSuccessful)
    }

    @Test
    fun testGetSeries() = runBlocking {
        val response = apiService.getSeries()
        assertTrue(response.isSuccessful)
    }
}