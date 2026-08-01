package com.lanflix.models

import com.google.gson.annotations.SerializedName
import com.lanflix.webview.ServerManager

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
    @SerializedName("logoUrl") val logoUrl: String? = null,
    @SerializedName("rating") val rating: String? = "PG-13",
    @SerializedName("releaseDate") val releaseDate: String? = null,
    @SerializedName("year") val year: Int? = null,
    @SerializedName("type") val type: String? = "movie",
    @SerializedName("itemCount") val itemCount: Int? = null,
    @SerializedName("localFilePath") val localFilePath: String? = null,
    @SerializedName("serverAvailable") val serverAvailable: Boolean = true,
    @SerializedName("progressPercentage") val progressPercentage: Double? = null,
    @SerializedName("palette") val palette: ServerArtworkPalette? = null
) {
    val displayTitle: String
        get() = title ?: collectionName ?: name ?: "Untitled"

    val isOfflinePlayable: Boolean
        get() = !localFilePath.isNullOrBlank()

    val displayYear: String?
        get() = year?.toString() ?: releaseDate?.take(4)

    val resolvedPosterUrl: String?
        get() {
            val raw = posterUrl ?: posterPath ?: return null
            if (raw.startsWith("http")) return raw
            if (raw.startsWith("/api/")) return "${ServerManager.activeServerUrl}$raw"
            val clean = if (raw.startsWith("/")) raw else "/$raw"
            return if (clean.startsWith("/t/p/")) "https://image.tmdb.org$clean" else "https://image.tmdb.org/t/p/w500$clean"
        }

    val resolvedBackdropUrl: String?
        get() {
            val raw = backdropUrl ?: backdropPath ?: return null
            if (raw.startsWith("http")) return raw
            if (raw.startsWith("/api/")) return "${ServerManager.activeServerUrl}$raw"
            val clean = if (raw.startsWith("/")) raw else "/$raw"
            return if (clean.startsWith("/t/p/")) "https://image.tmdb.org$clean" else "https://image.tmdb.org/t/p/w1280$clean"
        }

    val resolvedLogoUrl: String?
        get() {
            val raw = logoUrl
            if (!raw.isNullOrBlank()) {
                if (raw.startsWith("http")) return raw
                return "${ServerManager.activeServerUrl}${if (raw.startsWith('/')) raw else "/$raw"}"
            }
            return if (id > 0 && tmdbId > 0) "${ServerManager.activeServerUrl}/api/v2/artwork/$id/logo" else null
        }
}

data class ServerArtworkPalette(
    @SerializedName("base") val base: String = "#111827",
    @SerializedName("depth") val depth: String = "#030712",
    @SerializedName("glow") val glow: String = "#1F3A5F",
    @SerializedName("accent") val accent: String = "#F59E0B",
    @SerializedName("onBackground") val onBackground: String = "#FFFFFF",
    @SerializedName("algorithmVersion") val algorithmVersion: Int = 1
)

data class CastMember(
    val name: String,
    val role: String,
    val profileUrl: String?
)

data class SeriesSeasonsResponse(
    val seriesId: Int = 0,
    val seriesTitle: String? = null,
    val seasons: List<SeasonSummary> = emptyList(),
    val totalSeasons: Int = 0
)

data class SeasonSummary(
    val seasonNumber: Int = 0,
    val episodeCount: Int = 0,
    val availableEpisodes: Int = 0,
    val firstEpisode: SeasonFirstEpisode? = null
)

data class SeasonFirstEpisode(
    val title: String? = null,
    val airDate: String? = null,
    val stillUrl: String? = null
)

data class SeasonEpisodesResponse(
    val seriesId: Int = 0,
    val seasonNumber: Int = 0,
    val episodes: List<EpisodeItem> = emptyList(),
    val totalEpisodes: Int = 0,
    val availableEpisodes: Int = 0
)

data class EpisodeItem(
    val id: Int = 0,
    val tmdbId: Int = 0,
    val seasonNumber: Int = 0,
    val episodeNumber: Int = 0,
    val title: String? = null,
    val overview: String? = null,
    val airDate: String? = null,
    val stillUrl: String? = null,
    val filePath: String? = null,
    @SerializedName("serverAvailable") val hasFile: Boolean = false,
    @SerializedName("progressPercentage") val progressPercentage: Double? = null
) {
    val resolvedStillUrl: String?
        get() = stillUrl?.let { raw ->
            when {
                raw.startsWith("http") -> raw
                raw.startsWith("/") -> "${ServerManager.activeServerUrl}$raw"
                else -> "${ServerManager.activeServerUrl}/$raw"
            }
        }

    fun asContentItem(series: ContentItem) = ContentItem(
        id = id,
        tmdbId = tmdbId,
        title = title ?: "Episode $episodeNumber",
        overview = overview,
        posterUrl = resolvedStillUrl,
        backdropUrl = series.resolvedBackdropUrl,
        releaseDate = airDate,
        type = "episode"
    )
}
