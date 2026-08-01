package com.lanflix.models

import com.google.gson.annotations.SerializedName

data class ContentItem(
    @SerializedName("id") val id: Int = 0,
    @SerializedName("collectionId") val collectionId: Int? = null,
    @SerializedName("tmdbId") val tmdbId: Int = 0,
    @SerializedName("title") val title: String? = null,
    @SerializedName("name") val name: String? = null,
    @SerializedName("collectionName") val collectionName: String? = null,
    @SerializedName("overview") val overview: String? = null,
    @SerializedName("posterPath") val posterPath: String? = null,
    @SerializedName("backdropPath") val backdropPath: String? = null,
    @SerializedName("posterUrl") val posterUrl: String? = null,
    @SerializedName("backdropUrl") val backdropUrl: String? = null,
    @SerializedName("rating") val rating: String? = "PG-13",
    @SerializedName("releaseDate") val releaseDate: String? = null,
    @SerializedName("type") val type: String? = "movie",
    @SerializedName("itemCount") val itemCount: Int? = null
) {
    val displayTitle: String
        get() = title ?: collectionName ?: name ?: "Untitled"

    val resolvedPosterUrl: String?
        get() = posterUrl ?: posterPath?.let { if (it.startsWith("http")) it else "https://image.tmdb.org/t/p/w500$it" }

    val resolvedBackdropUrl: String?
        get() = backdropUrl ?: backdropPath?.let { if (it.startsWith("http")) it else "https://image.tmdb.org/t/p/w1280$it" }
}

data class CastMember(
    val name: String,
    val role: String,
    val profileUrl: String?
)
