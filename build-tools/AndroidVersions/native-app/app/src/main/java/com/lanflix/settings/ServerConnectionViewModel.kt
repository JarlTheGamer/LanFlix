package com.lanflix.settings

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.offline.OfflineMediaStore
import com.lanflix.webview.ServerDiscoveryManager
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ServerOption(val name: String, val url: String, val online: Boolean, val discovered: Boolean)

data class ServerConnectionState(
    val currentServer: String = ServerManager.DEFAULT_MDNS_HOST,
    val servers: List<ServerOption> = emptyList(),
    val scanning: Boolean = true,
    val connectingUrl: String? = null,
    val error: String? = null,
    val hasOfflineMedia: Boolean = false
)

class ServerConnectionViewModel(application: Application) : AndroidViewModel(application) {
    private val preferences = DevicePreferencesRepository(application)
    private val offlineStore = OfflineMediaStore(application)
    private val _state = MutableStateFlow(ServerConnectionState())
    val state: StateFlow<ServerConnectionState> = _state.asStateFlow()
    private val discovery = ServerDiscoveryManager(application) { name, url -> verify(name, url, discovered = true) }

    init {
        viewModelScope.launch {
            preferences.preferences.collect { settings ->
                _state.update { it.copy(currentServer = settings.activeServerUrl) }
                settings.savedServers.forEach { verify("Saved Lanflix server", it, discovered = false) }
            }
        }
        viewModelScope.launch {
            val hasOffline = offlineStore.readCatalog().any { offlineStore.localFile(it) != null }
            _state.update { it.copy(hasOfflineMedia = hasOffline) }
        }
        refresh()
    }

    fun refresh() {
        _state.update { it.copy(scanning = true, error = null) }
        discovery.startDiscovery()
        viewModelScope.launch {
            verify("Lanflix server", ServerManager.DEFAULT_MDNS_HOST, discovered = true)
            _state.update { it.copy(scanning = false) }
        }
    }

    fun connect(url: String, onConnected: (String) -> Unit) {
        val formatted = ServerManager.formatServerUrl(url)
        _state.update { it.copy(connectingUrl = formatted, error = null) }
        viewModelScope.launch {
            if (ServerManager.pingServer(getApplication(), formatted, timeoutMs = 3000)) {
                preferences.selectServer(formatted)
                ServerManager.isOnline = true
                _state.update { it.copy(currentServer = formatted, connectingUrl = null) }
                onConnected(formatted)
            } else {
                _state.update { it.copy(connectingUrl = null, error = "Lanflix could not reach this server. Check the address and that the server is running.") }
            }
        }
    }

    fun remove(url: String) {
        viewModelScope.launch {
            preferences.removeServer(url)
            _state.update { state -> state.copy(servers = state.servers.filterNot { it.url == url }) }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }

    private fun verify(name: String, url: String, discovered: Boolean) {
        val formatted = ServerManager.formatServerUrl(url)
        if (_state.value.servers.any { it.url == formatted && it.online }) return
        viewModelScope.launch {
            val online = ServerManager.pingServer(getApplication(), formatted, timeoutMs = 2200)
            _state.update { state ->
                val existing = state.servers.filterNot { it.url == formatted }
                state.copy(servers = (existing + ServerOption(name, formatted, online, discovered)).sortedByDescending { it.online })
            }
        }
    }

    override fun onCleared() {
        discovery.stopDiscovery()
        super.onCleared()
    }
}
