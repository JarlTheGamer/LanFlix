package com.lanflix.api

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.lanflix.auth.AuthTokens
import com.lanflix.auth.LanflixAccount
import com.lanflix.auth.LanflixSessionStore
import com.lanflix.models.ContentItem
import com.lanflix.models.SeasonEpisodesResponse
import com.lanflix.models.SeasonSummary
import com.lanflix.offline.OfflineMediaStore
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import java.util.concurrent.TimeUnit

class LanflixApiClient(context: Context, private val baseUrl: String = ServerManager.activeServerUrl) {
    private val appContext = context.applicationContext
    private val client = OkHttpClient.Builder().connectTimeout(6, TimeUnit.SECONDS).readTimeout(30, TimeUnit.SECONDS).build()
    private val gson = Gson()
    private val offlineStore = OfflineMediaStore(appContext)
    val sessions = LanflixSessionStore(appContext, baseUrl)
    private val jsonType = "application/json; charset=utf-8".toMediaType()
    private val refreshLock = Any()

    suspend fun getSetupStatus(): SetupStatus? = get("/api/v2/setup/status", false, SetupStatus::class.java)

    suspend fun setupOwner(username: String, displayName: String, password: String): AuthTokens? =
        postAuth("/api/v2/setup/owner", mapOf("username" to username, "displayName" to displayName, "password" to password, "deviceName" to "Android phone"))

    suspend fun login(username: String, password: String): AuthTokens? =
        postAuth("/api/v2/auth/login", mapOf("username" to username, "password" to password, "deviceName" to "Android phone"))

    suspend fun register(invitationCode: String, username: String, displayName: String, password: String): AuthTokens? =
        postAuth("/api/v2/auth/register", mapOf("invitationCode" to invitationCode, "username" to username,
            "displayName" to displayName, "password" to password, "deviceName" to "Android phone"))

    suspend fun logout() = withContext(Dispatchers.IO) {
        sessions.refreshToken?.let { execute(Request.Builder().url(url("/api/v2/auth/logout")).post(gson.toJson(mapOf("refreshToken" to it)).toRequestBody(jsonType)), false)?.close() }
        sessions.clear()
    }

    suspend fun getCurrentAccount(): LanflixAccount? = get("/api/v2/accounts/me", true, LanflixAccount::class.java)
    suspend fun getSessions(): List<AccountSession> = getList("/api/v2/accounts/me/sessions")
    suspend fun revokeSession(id: String): Boolean = mutate("DELETE", "/api/v2/accounts/me/sessions/$id")
    suspend fun changePassword(current: String, replacement: String): Boolean = mutate("POST", "/api/v2/accounts/me/password", mapOf("currentPassword" to current, "newPassword" to replacement))

    suspend fun getHomeContent(): List<ContentItem> = withContext(Dispatchers.IO) {
        if (!ServerManager.isOnline || !sessions.isSignedIn) return@withContext offlineStore.readCatalog()
        val payload = get("/api/v2/home?limit=50", true, V2HomeResponse::class.java) ?: return@withContext offlineStore.readCatalog()
        val items = (listOfNotNull(payload.hero) + payload.continueWatching + payload.recentlyAdded).distinctBy { "${it.type}:${it.id}" }
        offlineStore.cacheLibrary(items)
        items
    }

    suspend fun getMovies(): List<ContentItem> = getLibraryPage("movie")
    suspend fun getSeries(): List<ContentItem> = getLibraryPage("series")
    suspend fun getOfflineCatalog(): List<ContentItem> = withContext(Dispatchers.IO) { offlineStore.readCatalog() }
    suspend fun getContentDetail(id: Int): V2MediaDetail? = get("/api/v2/content/$id", true, V2MediaDetail::class.java)

    suspend fun getSeriesSeasons(seriesId: Int): List<SeasonSummary> = getContentDetail(seriesId)?.seasons.orEmpty().map { season ->
        SeasonSummary(season.seasonNumber, season.episodes.size, season.episodes.count { it.hasFile })
    }

    suspend fun getSeasonEpisodes(seriesId: Int, seasonNumber: Int): SeasonEpisodesResponse {
        val episodes = getContentDetail(seriesId)?.seasons?.firstOrNull { it.seasonNumber == seasonNumber }?.episodes.orEmpty()
        return SeasonEpisodesResponse(seriesId, seasonNumber, episodes, episodes.size, episodes.count { it.hasFile })
    }

    suspend fun getDownloadManifest(item: ContentItem): PlaybackDownloadManifest? {
        val kind = if (item.type.equals("episode", true)) "episode" else "movie"
        return get("/api/v2/playback/$kind/${item.id}/download-manifest", true, PlaybackDownloadManifest::class.java)
    }

    suspend fun getPlaybackInfo(item: ContentItem): PlaybackInfo? {
        val kind = if (item.type.equals("episode", true)) "episode" else "movie"
        return get("/api/v2/playback/$kind/${item.id}", true, PlaybackInfo::class.java)
    }

    suspend fun getSocialFeed(): List<SocialActivity> = getList("/api/v2/social/feed?limit=50")
    suspend fun createPost(body: String, visibility: String = "Friends"): Boolean = mutate("POST", "/api/v2/social/posts", mapOf("body" to body, "visibility" to visibility))
    suspend fun getNotifications(): List<SocialNotification> = getList("/api/v2/social/notifications?limit=100")
    suspend fun markNotificationRead(id: String): Boolean = mutate("POST", "/api/v2/social/notifications/$id/read")
    suspend fun getReviews(contentId: Int): List<SocialReview> = getList("/api/v2/social/reviews/$contentId")
    suspend fun saveReview(contentId: Int, rating: Int, body: String?, visibility: String = "Friends"): Boolean =
        mutate("PUT", "/api/v2/social/reviews/$contentId", mapOf("rating" to rating, "body" to body, "visibility" to visibility))

    suspend fun getMusicHome(): MusicHome? = get("/api/v2/music/home", true, MusicHome::class.java)
    suspend fun getLiveTvChannels(): List<LiveTvChannel> = getList("/api/v2/live-tv/channels")
    suspend fun getLiveTvSources(): List<LiveTvSource> = getList("/api/v2/live-tv/sources/")
    suspend fun createLiveTvSource(name: String, kind: String, sourceUri: String, guideUri: String?): Boolean =
        mutate("POST", "/api/v2/live-tv/sources/", mapOf("name" to name, "kind" to kind, "sourceUri" to sourceUri, "guideUri" to guideUri, "maxTuners" to 1, "enabled" to true))
    suspend fun deleteLiveTvSource(id: Long): Boolean = mutate("DELETE", "/api/v2/live-tv/sources/$id")
    suspend fun refreshLiveTvSource(id: Long): Boolean = mutate("POST", "/api/v2/live-tv/sources/$id/refresh")
    companion object {
        @Volatile private var instance: LanflixApiClient? = null
        fun getInstance(context: Context): LanflixApiClient {
            return instance ?: synchronized(this) {
                instance ?: LanflixApiClient(context.applicationContext).also { instance = it }
            }
        }
    }

    suspend fun getDiscoveryPage(): DiscoveryPage? = withContext(Dispatchers.IO) {
        if (!ServerManager.isOnline || !sessions.isSignedIn) return@withContext offlineStore.readDiscoveryPage()
        val page = get("/api/v2/discovery/?page=1", true, DiscoveryPage::class.java)
        if (page != null) {
            offlineStore.cacheDiscoveryPage(page)
            page
        } else {
            offlineStore.readDiscoveryPage()
        }
    }

    fun readDiscoveryCache(): DiscoveryPage? = offlineStore.readDiscoveryPage()
    suspend fun acquire(item: DiscoveryItem): Boolean = mutate("POST", "/api/v2/discovery/${item.tmdbId}/acquire",
        mapOf("type" to item.type, "title" to item.title, "year" to item.year))
    suspend fun getAdministrationOverview(): AdministrationOverview? = get("/api/v2/admin/overview", true, AdministrationOverview::class.java)
    suspend fun getAdministrationSettings(): AdministrationSettings? = get("/api/v2/admin/settings", true, AdministrationSettings::class.java)
    suspend fun updateMusicFolder(path: String): Boolean {
        val current = getAdministrationSettings() ?: return false
        return mutate("PUT", "/api/v2/admin/settings", current.copy(libraries = current.libraries.copy(music = path.trim())))
    }
    suspend fun getAccounts(): List<AccountSummary> = getList("/api/v2/admin/identity/accounts")
    suspend fun getAdminJobs(): List<AdminJob> = getList("/api/v2/admin/jobs")
    suspend fun triggerAdminJob(name: String): Boolean = mutate("POST", "/api/v2/admin/jobs", mapOf("name" to name))
    suspend fun createInvitation(role: String): InvitationResult? = withContext(Dispatchers.IO) {
        execute(Request.Builder().url(url("/api/v2/admin/identity/invitations")).post(gson.toJson(mapOf("role" to role)).toRequestBody(jsonType)), true)?.use { response ->
            if (!response.isSuccessful) null else gson.fromJson(response.body?.string(), InvitationResult::class.java)
        }
    }

    private suspend fun getLibraryPage(kind: String): List<ContentItem> = withContext(Dispatchers.IO) {
        if (!ServerManager.isOnline || !sessions.isSignedIn) return@withContext offlineStore.readCatalog().filter { it.type == kind }
        val type = object : TypeToken<V2Page<ContentItem>>() {}.type
        val items = getTyped<V2Page<ContentItem>>("/api/v2/library?type=$kind&limit=100", type)?.items.orEmpty()
        if (items.isNotEmpty()) offlineStore.cacheLibrary(items)
        items
    }

    private suspend fun postAuth(path: String, body: Any): AuthTokens? = withContext(Dispatchers.IO) {
        val request = Request.Builder().url(url(path)).post(gson.toJson(body).toRequestBody(jsonType))
        execute(request, false)?.use { response ->
            if (!response.isSuccessful) return@withContext null
            gson.fromJson(response.body?.string(), AuthTokens::class.java)?.also(sessions::save)
        }
    }

    private suspend fun <T> get(path: String, authenticated: Boolean, type: Class<T>): T? = withContext(Dispatchers.IO) {
        execute(Request.Builder().url(url(path)).get(), authenticated)?.use { response ->
            if (!response.isSuccessful) null else gson.fromJson(response.body?.string(), type)
        }
    }

    private suspend inline fun <reified T> getList(path: String): List<T> {
        val type = object : TypeToken<List<T>>() {}.type
        return getTyped<List<T>>(path, type).orEmpty()
    }

    private suspend fun <T> getTyped(path: String, type: java.lang.reflect.Type): T? = withContext(Dispatchers.IO) {
        execute(Request.Builder().url(url(path)).get(), true)?.use { response ->
            if (!response.isSuccessful) null else gson.fromJson<T>(response.body?.string(), type)
        }
    }

    private suspend fun mutate(method: String, path: String, body: Any? = null): Boolean = withContext(Dispatchers.IO) {
        val payload = (body?.let(gson::toJson) ?: "").toRequestBody(if (body == null) null else jsonType)
        val builder = Request.Builder().url(url(path)).method(method, if (method == "DELETE" && body == null) null else payload)
        execute(builder, true)?.use { it.isSuccessful } ?: false
    }

    private fun execute(builder: Request.Builder, authenticated: Boolean): Response? {
        if (authenticated) sessions.accessToken?.let { builder.header("Authorization", "Bearer $it") }
        val response = runCatching { client.newCall(builder.build()).execute() }.getOrNull() ?: return null
        if (!authenticated || response.code != 401) return response
        response.close()
        if (!refreshSession()) return null
        sessions.accessToken?.let { builder.header("Authorization", "Bearer $it") }
        return runCatching { client.newCall(builder.build()).execute() }.getOrNull()
    }

    private fun refreshSession(): Boolean = synchronized(refreshLock) {
        val refreshToken = sessions.refreshToken ?: return@synchronized false
        val request = Request.Builder().url(url("/api/v2/auth/refresh"))
            .post(gson.toJson(mapOf("refreshToken" to refreshToken, "deviceName" to "Android phone")).toRequestBody(jsonType)).build()
        val tokens = runCatching { client.newCall(request).execute().use { response ->
            if (!response.isSuccessful) null else gson.fromJson(response.body?.string(), AuthTokens::class.java)
        } }.getOrNull()
        if (tokens == null) { sessions.clear(); false } else { sessions.save(tokens); true }
    }

    private fun url(path: String) = "${baseUrl.trimEnd('/')}${if (path.startsWith('/')) path else "/$path"}"
}
