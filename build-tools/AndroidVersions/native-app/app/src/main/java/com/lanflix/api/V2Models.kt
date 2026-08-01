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

data class SocialAuthor(val id: String = "", val displayName: String = "")
data class SocialActivity(val id: String = "", val author: SocialAuthor = SocialAuthor(), val kind: String = "",
    val contentId: Int? = null, val reviewId: String? = null, val body: String? = null, val visibility: String = "friends",
    val commentCount: Int = 0, val reactionCount: Int = 0, val createdAtUtc: String = "")
data class SocialNotification(val id: String = "", val actor: SocialAuthor? = null, val kind: String = "",
    val resourceType: String = "", val resourceId: String = "", val isRead: Boolean = false, val createdAtUtc: String = "")
data class SocialReview(val id: String = "", val author: SocialAuthor = SocialAuthor(), val contentId: Int = 0,
    val rating: Int = 0, val body: String? = null, val visibility: String = "friends", val updatedAtUtc: String = "")
data class MusicArtist(val id: Long = 0, val name: String = "", val artworkUrl: String? = null)
data class MusicAlbum(val id: Long = 0, val title: String = "", val year: Int? = null, val artist: MusicArtist = MusicArtist(), val artworkUrl: String? = null, val trackCount: Int = 0)
data class MusicHome(val recentlyAdded: List<MusicAlbum> = emptyList(), val artists: List<MusicArtist> = emptyList())
data class LiveTvProgram(val id: Long = 0, val title: String = "", val startsAtUtc: String = "", val endsAtUtc: String = "")
data class LiveTvChannel(val id: Long = 0, val number: String = "", val name: String = "", val logoUrl: String? = null,
    val groupName: String? = null, val favorite: Boolean = false, val now: LiveTvProgram? = null, val next: LiveTvProgram? = null)
data class AdministrationOverview(val accounts: Int = 0, val movies: Int = 0, val series: Int = 0, val episodes: Int = 0,
    val musicTracks: Int = 0, val liveTvChannels: Int = 0, val pendingJobs: Int = 0, val openReports: Int = 0,
    val workingSetBytes: Long = 0, val uptimeSeconds: Long = 0)
data class AccountSummary(val id: String = "", val username: String = "", val displayName: String = "", val role: String = "User",
    val isDisabled: Boolean = false, val lastLoginAtUtc: String? = null)
