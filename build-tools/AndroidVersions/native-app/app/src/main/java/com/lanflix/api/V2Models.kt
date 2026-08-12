package com.lanflix.api

import com.lanflix.auth.LanflixAccount
import com.lanflix.models.ContentItem
import com.lanflix.models.EpisodeItem

data class SetupStatus(val requiresOwnerSetup: Boolean = false)
data class AccountSession(val id: String = "", val deviceName: String = "", val createdAtUtc: String = "", val expiresAtUtc: String = "")
data class V2HomeResponse(val continueWatching: List<ContentItem> = emptyList(), val recentlyAdded: List<ContentItem> = emptyList(), val hero: ContentItem? = null)
data class V2Page<T>(val items: List<T> = emptyList(), val total: Int = 0, val offset: Int = 0, val limit: Int = 0)
data class V2Season(val seasonNumber: Int = 0, val episodes: List<EpisodeItem> = emptyList())
data class V2MediaDetail(val media: ContentItem = ContentItem(), val seasons: List<V2Season> = emptyList())
data class PlaybackDownloadManifest(val id: Int = 0, val kind: String = "movie", val title: String = "", val fileSize: Long = 0,
    val mimeType: String = "video/mp4", val sha256: String = "", val downloadUrl: String = "")
data class PlaybackInfo(val id: Int = 0, val kind: String = "movie", val title: String = "", val streamUrl: String = "",
    val introStartSeconds: Double? = null, val introEndSeconds: Double? = null, val creditsStartSeconds: Double? = null,
    val progress: PlaybackProgress? = null, val durationSeconds: Double = 0.0, val playbackMode: String = "Unknown",
    val playbackReason: String = "", val supportsSeeking: Boolean = true,
    val transcodesVideo: Boolean = false, val transcodesAudio: Boolean = false,
    val subtitles: List<PlaybackSubtitle> = emptyList())
data class PlaybackSubtitle(val index: Int = 0, val language: String = "und", val title: String = "",
    val format: String = "", val isForced: Boolean = false, val isDefault: Boolean = false,
    val isEmbedded: Boolean = true, val url: String = "")
data class PlaybackProgress(val mediaKind: String = "", val mediaId: Int = 0, val positionMilliseconds: Long = 0L,
    val durationMilliseconds: Long = 0L, val percentage: Double = 0.0, val completed: Boolean = false)

data class SocialAuthor(val id: String = "", val displayName: String = "", val avatarUrl: String? = null)
data class SocialActivity(val id: String = "", val author: SocialAuthor = SocialAuthor(), val kind: String = "",
    val contentId: Int? = null, val reviewId: String? = null, val body: String? = null, val visibility: String = "friends",
    val commentCount: Int = 0, val reactionCount: Int = 0, val createdAtUtc: String = "",
    val contentTitle: String? = null, val contentPosterUrl: String? = null)
data class SocialNotification(val id: String = "", val actor: SocialAuthor? = null, val kind: String = "",
    val resourceType: String = "", val resourceId: String = "", val isRead: Boolean = false, val createdAtUtc: String = "")
data class SocialReview(val id: String = "", val author: SocialAuthor = SocialAuthor(), val contentId: Int = 0,
    val rating: Int = 0, val body: String? = null, val visibility: String = "friends", val updatedAtUtc: String = "")
data class SocialComment(val id: String = "", val author: SocialAuthor = SocialAuthor(),
    val body: String = "", val createdAtUtc: String = "")
data class SocialAccountInfo(val id: String = "", val displayName: String = "", val role: String = "User")
data class SocialRelationship(val id: String = "", val account: SocialAccountInfo = SocialAccountInfo(),
    val kind: String = "follow", val status: String = "accepted", val incoming: Boolean = false, val createdAtUtc: String = "")
data class SocialPrivacy(val defaultVisibility: String = "friends", val activityEnabled: Boolean = true)
data class UnreadCount(val count: Int = 0)
data class MusicArtist(val id: Long = 0, val name: String = "", val artworkUrl: String? = null)
data class MusicAlbum(val id: Long = 0, val title: String = "", val year: Int? = null, val artist: MusicArtist = MusicArtist(), val artworkUrl: String? = null, val trackCount: Int = 0)
data class MusicHome(val recentlyAdded: List<MusicAlbum> = emptyList(), val artists: List<MusicArtist> = emptyList())
data class MusicListeningStats(val listens: Int = 0, val completedTracks: Int = 0)
data class MusicTrack(val id: Long = 0, val title: String = "", val trackNumber: Int = 0, val discNumber: Int? = null,
    val durationMilliseconds: Long = 0, val genres: List<String> = emptyList(), val codec: String = "",
    val bitrateKbps: Int? = null, val sampleRateHz: Int? = null, val channels: Int? = null,
    val artist: MusicArtist = MusicArtist(), val album: MusicAlbum = MusicAlbum(), val streamUrl: String = "",
    val serverAvailable: Boolean = false)
data class MusicLyrics(val trackId: Long = 0, val text: String = "", val isSynchronized: Boolean = false, val source: String = "")
data class MusicWaveform(val trackId: Long = 0, val amplitudes: List<Float> = emptyList())
data class MusicPlaylist(val id: Long = 0, val name: String = "", val tracks: List<MusicTrack> = emptyList(), val updatedAtUtc: String = "")
data class LiveTvProgram(val id: Long = 0, val title: String = "", val startsAtUtc: String = "", val endsAtUtc: String = "")
data class LiveTvChannel(val id: Long = 0, val number: String = "", val name: String = "", val logoUrl: String? = null,
    val groupName: String? = null, val favorite: Boolean = false, val now: LiveTvProgram? = null, val next: LiveTvProgram? = null)
data class LiveTvSource(val id: Long = 0, val name: String = "", val kind: String = "", val maxTuners: Int = 1,
    val enabled: Boolean = true, val lastRefreshedUtc: String? = null, val lastError: String? = null)
data class DiscoveryItem(val tmdbId: Int = 0, val type: String = "movie", val title: String = "", val overview: String? = null,
    val year: Int? = null, val rating: Double = 0.0, val posterUrl: String? = null, val backdropUrl: String? = null) {
    fun asContentItem() = ContentItem(
        id = if (type == "series") -(1_000_000 + tmdbId) else -tmdbId,
        tmdbId = tmdbId,
        title = title,
        overview = overview,
        posterUrl = posterUrl,
        backdropUrl = backdropUrl,
        logoUrl = "${com.lanflix.webview.ServerManager.activeServerUrl}/api/v2/discovery/$type/$tmdbId/logo",
        rating = if (rating > 0) "%.1f".format(rating) else null,
        year = year,
        type = type,
        serverAvailable = false
    )
}
data class DiscoveryPage(val trendingMovies: List<DiscoveryItem> = emptyList(), val trendingSeries: List<DiscoveryItem> = emptyList(),
    val popularMovies: List<DiscoveryItem> = emptyList(), val popularSeries: List<DiscoveryItem> = emptyList())
data class AdministrationOverview(val accounts: Int = 0, val movies: Int = 0, val series: Int = 0, val episodes: Int = 0,
    val musicTracks: Int = 0, val liveTvChannels: Int = 0, val pendingJobs: Int = 0, val openReports: Int = 0,
    val workingSetBytes: Long = 0, val uptimeSeconds: Long = 0)
data class AccountSummary(val id: String = "", val username: String = "", val displayName: String = "", val role: String = "User",
    val isDisabled: Boolean = false, val lastLoginAtUtc: String? = null)
data class AdminJob(val id: String = "", val name: String = "", val status: String = "", val createdAtUtc: String = "", val result: String? = null, val error: String? = null)
data class InvitationResult(val id: String = "", val code: String = "", val role: String = "User", val expiresAtUtc: String = "")
data class AdminLibraryPaths(val movies: String = "", val series: String = "", val music: String = "")
data class AdminPlaybackSettings(val directPlay: Boolean = true, val directStream: Boolean = true, val hardwareAcceleration: Boolean = true,
    val hardwareAccelerator: String = "auto", val maxConcurrentTranscodes: Int = 2, val transcodePath: String = "")
data class AdminIntegrationSettings(val url: String = "", val configured: Boolean = false, val apiKey: String? = null)
data class AdministrationSettings(val libraries: AdminLibraryPaths = AdminLibraryPaths(), val playback: AdminPlaybackSettings = AdminPlaybackSettings(),
    val tmdb: AdminIntegrationSettings = AdminIntegrationSettings(), val radarr: AdminIntegrationSettings = AdminIntegrationSettings(),
    val sonarr: AdminIntegrationSettings = AdminIntegrationSettings(), val prowlarr: AdminIntegrationSettings = AdminIntegrationSettings(),
    val bazarr: AdminIntegrationSettings = AdminIntegrationSettings())
