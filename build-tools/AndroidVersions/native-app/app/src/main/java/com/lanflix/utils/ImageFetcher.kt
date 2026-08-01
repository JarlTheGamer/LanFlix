package com.lanflix.utils

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import android.widget.ImageView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

object ImageFetcher {
    private val client = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .followRedirects(true)
        .followSslRedirects(true)
        .build()

    private val memoryCache = LruCache<String, Bitmap>(100)

    fun loadImage(imageView: ImageView, url: String?) {
        if (url.isNullOrBlank()) return

        val cachedBitmap = memoryCache.get(url)
        if (cachedBitmap != null) {
            imageView.setImageBitmap(cachedBitmap)
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val request = Request.Builder()
                    .url(url)
                    .header("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)")
                    .build()

                client.newCall(request).execute().use { response ->
                    if (response.isSuccessful) {
                        val bytes = response.body?.bytes()
                        if (bytes != null && bytes.isNotEmpty()) {
                            val bitmap = BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
                            if (bitmap != null) {
                                memoryCache.put(url, bitmap)
                                withContext(Dispatchers.Main) {
                                    imageView.setImageBitmap(bitmap)
                                }
                            }
                        }
                    }
                }
            } catch (e: Exception) {
                // Network fetch error ignored
            }
        }
    }
}
