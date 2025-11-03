package com.lanflix.android.domain.model

import android.os.Parcelable
import kotlinx.parcelize.Parcelize

@Parcelize
data class Profile(
    val id: String,
    val name: String,
    val avatarColorPrimary: String,
    val avatarColorSecondary: String,
    val createdAt: String? = null
) : Parcelable

@Parcelize
data class Content(
    val id: String,
    val title: String,
    val type: ContentType,
    val year: Int? = null,
    val rating: String? = null,
    val duration: Int? = null, // in minutes
    val description: String? = null,
    val posterUrl: String? = null,
    val backdropUrl: String? = null,
    val genres: List<String> = emptyList(),
    val cast: List<String> = emptyList(),
    val director: String? = null,
    val seasons: List<Season> = emptyList(), // For series
    val streamUrl: String? = null,
    val isInMyList: Boolean = false
) : Parcelable

@Parcelize
data class Season(
    val id: String,
    val number: Int,
    val title: String,
    val episodes: List<Episode> = emptyList()
) : Parcelable

@Parcelize
data class Episode(
    val id: String,
    val number: Int,
    val title: String,
    val description: String? = null,
    val duration: Int? = null, // in minutes
    val thumbnailUrl: String? = null,
    val streamUrl: String? = null
) : Parcelable

enum class ContentType {
    MOVIE, SERIES
}

@Parcelize
data class StreamInfo(
    val contentId: String,
    val streamUrl: String,
    val subtitleTracks: List<SubtitleTrack> = emptyList(),
    val audioTracks: List<AudioTrack> = emptyList(),
    val qualityOptions: List<QualityOption> = emptyList()
) : Parcelable

@Parcelize
data class SubtitleTrack(
    val id: String,
    val language: String,
    val label: String,
    val url: String
) : Parcelable

@Parcelize
data class AudioTrack(
    val id: String,
    val language: String,
    val label: String,
    val codec: String
) : Parcelable

@Parcelize
data class QualityOption(
    val id: String,
    val label: String, // "1080p", "720p", etc.
    val width: Int,
    val height: Int,
    val bitrate: Int
) : Parcelable

data class ServerInfo(
    val baseUrl: String,
    val name: String,
    val version: String,
    val isConnected: Boolean = false
)