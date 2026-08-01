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
            val cached = api.getOfflineCatalog()
            if (cached.isNotEmpty() && _state.value.library.isEmpty()) {
                _state.update { it.copy(loading = false, library = cached) }
            }
            val online = ServerManager.pingServer(getApplication(), ServerManager.activeServerUrl, timeoutMs = 1500)
            ServerManager.isOnline = online
            _state.update { it.copy(loading = it.library.isEmpty(), error = null, online = online) }
            val setup = if (online) api.getSetupStatus() else null
            val account = if (online && api.sessions.isSignedIn) api.getCurrentAccount() ?: api.sessions.account else api.sessions.account
            val needsAuthentication = online && account == null
            val content = if (needsAuthentication) api.getOfflineCatalog() else api.getHomeContent()
            val social = if (online && account != null) api.getSocialFeed() else emptyList()
            val notifications = if (online && account != null) api.getNotifications() else emptyList()
            val music = if (online && account != null) api.getMusicHome() else null
            val liveTv = if (online && account != null) api.getLiveTvChannels() else emptyList()
            val admin = if (online && account?.isAdministrator == true) api.getAdministrationOverview() else null
            val discovery = if (online && account != null) api.getDiscoveryPage() else null
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
                    discovery = discovery,
                    error = if (content.isEmpty() && !needsAuthentication) "Your library is empty" else null
                )
            }
            discovery?.let { page -> launch { preloadDiscovery(page) } }
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
