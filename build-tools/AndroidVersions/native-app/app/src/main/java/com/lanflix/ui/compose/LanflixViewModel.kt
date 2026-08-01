package com.lanflix.ui.compose

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.offline.OfflineDownloadManager
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LanflixUiState(
    val loading: Boolean = true,
    val online: Boolean = ServerManager.isOnline,
    val library: List<ContentItem> = emptyList(),
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
            val online = ServerManager.pingServer(getApplication(), ServerManager.activeServerUrl, timeoutMs = 1500)
            ServerManager.isOnline = online
            _state.update { it.copy(loading = true, error = null, online = online) }
            val content = api.getHomeContent()
            _state.update {
                it.copy(
                    loading = false,
                    library = content,
                    error = if (content.isEmpty()) "Your library is empty" else null
                )
            }
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
