package com.lanflix.android.domain.model

import android.os.Parcelable
import kotlinx.parcelize.Parcelize

@Parcelize
data class Profile(
    val id: String,
    val name: String,
    val avatarPath: String? = null,
    val isKidsProfile: Boolean = false,
    val preferences: UserPreferences? = null,
    val createdAt: String? = null
) : Parcelable {
    
    // Computed properties for UI - not serialized
    val avatarColorPrimary: String
        get() = generateAvatarColor(name, true)
    
    val avatarColorSecondary: String
        get() = generateAvatarColor(name, false)
    
    companion object {
        private fun generateAvatarColor(name: String, isPrimary: Boolean): String {
            val colors = listOf(
                "#ff6b6b" to "#ff8e8e", // Red
                "#4ecdc4" to "#6ed5ce", // Teal
                "#45b7d1" to "#6bc5d8", // Blue
                "#96ceb4" to "#a8d4c0", // Green
                "#feca57" to "#fed976", // Yellow
                "#ff9ff3" to "#ffb3f5", // Pink
                "#54a0ff" to "#6bb0ff", // Light Blue
                "#5f27cd" to "#7c4ddb"  // Purple
            )
            val colorPair = colors[name.hashCode().absoluteValue % colors.size]
            return if (isPrimary) colorPair.first else colorPair.second
        }
        
        private val Int.absoluteValue: Int
            get() = if (this < 0) -this else this
    }
}

@Parcelize
data class UserPreferences(
    val preferredAudioLanguage: String? = null,
    val preferredSubtitleLanguage: String? = null,
    val subtitlesEnabled: Boolean = false,
    val preferredBitrate: Long? = null,
    val autoSkipIntro: Boolean = false,
    val autoPlayNextEpisode: Boolean = true,
    val maxResolution: String? = null,
    val allowHardwareAcceleration: Boolean = true,
    val forceTranscode: Boolean = false,
    val theme: String = "dark"
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