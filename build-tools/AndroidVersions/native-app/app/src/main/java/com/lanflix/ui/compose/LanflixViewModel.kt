package com.lanflix.ui.compose

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.offline.OfflineDownloadManager
import com.lanflix.webview.ServerManager
import com.lanflix.auth.LanflixAccount
import com.lanflix.api.SocialActivity
import com.lanflix.api.SocialNotification
import com.lanflix.api.MusicHome
import com.lanflix.api.LiveTvChannel
import com.lanflix.api.AdministrationOverview
import com.lanflix.api.DiscoveryPage
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import coil.imageLoader
import coil.request.ImageRequest

data class LanflixUiState(
    val loading: Boolean = true,
    val online: Boolean = ServerManager.isOnline,
    val library: List<ContentItem> = emptyList(),
    val account: LanflixAccount? = null,
    val requiresOwnerSetup: Boolean = false,
    val authenticationRequired: Boolean = false,
    val socialFeed: List<SocialActivity> = emptyList(),
    val notifications: List<SocialNotification> = emptyList(),
    val music: MusicHome? = null,
    val liveTvChannels: List<LiveTvChannel> = emptyList(),
    val administration: AdministrationOverview? = null,
    val discovery: DiscoveryPage? = null,
    val downloading: Set<String> = emptySet(),
    val error: String? = null
)

class LanflixViewModel(application: Application) : AndroidViewModel(application) {
    private val api = LanflixApiClient(application)
    private val downloads = OfflineDownloadManager(application)
    private val _state = MutableStateFlow(LanflixUiState())
    val state: StateFlow<LanflixUiState> = _state.asStateFlow()

    init { refresh() }

    fun refresh() {
        viewModelScope.launch {
            val cachedCatalog = api.getOfflineCatalog()
            val cachedDiscovery = api.readDiscoveryCache()
            _state.update {
                it.copy(
                    loading = cachedCatalog.isEmpty(),
                    library = if (it.library.isEmpty()) cachedCatalog else it.library,
                    discovery = if (it.discovery == null) cachedDiscovery else it.discovery
                )
            }
            cachedDiscovery?.let { page -> launch { preloadDiscovery(page) } }

            val online = ServerManager.pingServer(getApplication(), ServerManager.activeServerUrl, timeoutMs = 1500)
            ServerManager.isOnline = online
            _state.update { it.copy(loading = it.library.isEmpty(), error = null, online = online) }
            if (!online) return@launch

            coroutineScope {
                val setupDeferred = async { api.getSetupStatus() }
                val accountDeferred = async { if (api.sessions.isSignedIn) api.getCurrentAccount() ?: api.sessions.account else api.sessions.account }
                val setup = setupDeferred.await()
                val account = accountDeferred.await()
                val needsAuthentication = account == null

                val contentDeferred = async { if (needsAuthentication) api.getOfflineCatalog() else api.getHomeContent() }
                val socialDeferred = async { if (account != null) api.getSocialFeed() else emptyList() }
                val notificationsDeferred = async { if (account != null) api.getNotifications() else emptyList() }
                val musicDeferred = async { if (account != null) api.getMusicHome() else null }
                val liveTvDeferred = async { if (account != null) api.getLiveTvChannels() else emptyList() }
                val adminDeferred = async { if (account?.isAdministrator == true) api.getAdministrationOverview() else null }
                val discoveryDeferred = async { if (account != null) api.getDiscoveryPage() else null }

                val content = contentDeferred.await()
                val social = socialDeferred.await()
                val notifications = notificationsDeferred.await()
                val music = musicDeferred.await()
                val liveTv = liveTvDeferred.await()
                val admin = adminDeferred.await()
                val discovery = discoveryDeferred.await()

                _state.update {
                    it.copy(
                        loading = false,
                        library = content,
                        account = account,
                        requiresOwnerSetup = setup?.requiresOwnerSetup == true,
                        authenticationRequired = needsAuthentication,
                        socialFeed = social,
                        notifications = notifications,
                        music = music,
                        liveTvChannels = liveTv,
                        administration = admin,
                        discovery = discovery ?: it.discovery,
                        error = if (content.isEmpty() && !needsAuthentication) "Your library is empty" else null
                    )
                }
                discovery?.let { page -> launch { preloadDiscovery(page) } }
            }
        }
    }

    private suspend fun preloadDiscovery(page: DiscoveryPage) {
        val context = getApplication<Application>()
        val loader = context.imageLoader
        val items = page.trendingMovies + page.trendingSeries + page.popularMovies + page.popularSeries
        val urls = items.flatMap { item -> listOfNotNull(item.posterUrl, item.backdropUrl) }.distinct()
        urls.chunked(8).forEach { batch ->
            coroutineScope {
                batch.map { url -> async { loader.execute(ImageRequest.Builder(context).data(url).build()) } }.awaitAll()
            }
        }
    }

    fun authenticate(username: String, displayName: String, password: String, invitation: String?, onResult: (Boolean) -> Unit) {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            val tokens = when {
                _state.value.requiresOwnerSetup -> api.setupOwner(username, displayName, password)
                !invitation.isNullOrBlank() -> api.register(invitation, username, displayName, password)
                else -> api.login(username, password)
            }
            if (tokens == null) {
                _state.update { it.copy(loading = false, error = "Sign-in failed. Check your details or invitation code.") }
                onResult(false)
            } else {
                _state.update { it.copy(account = tokens.account, authenticationRequired = false, requiresOwnerSetup = false) }
                refresh()
                onResult(true)
            }
        }
    }

    fun signOut() {
        viewModelScope.launch {
            api.logout()
            _state.update { it.copy(account = null, authenticationRequired = it.online, socialFeed = emptyList(), notifications = emptyList(), administration = null) }
        }
    }

    fun download(item: ContentItem, onComplete: (ContentItem?) -> Unit) {
        val key = "${item.type}:${item.id}"
        if (key in _state.value.downloading) return
        viewModelScope.launch {
            _state.update { it.copy(downloading = it.downloading + key) }
            val saved = downloads.download(item)
            _state.update { current ->
                current.copy(
                    downloading = current.downloading - key,
                    library = if (saved == null) current.library else current.library.map {
                        if (it.id == saved.id && it.type == saved.type) saved else it
                    },
                    error = if (saved == null) "Download failed" else current.error
                )
            }
            onComplete(saved)
        }
    }
}
