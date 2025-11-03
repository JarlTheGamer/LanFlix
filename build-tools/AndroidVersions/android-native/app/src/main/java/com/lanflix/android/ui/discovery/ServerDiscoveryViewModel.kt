package com.lanflix.android.ui.discovery

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.android.data.repository.ServerDiscoveryRepository
import com.lanflix.android.domain.model.ServerInfo
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ServerDiscoveryViewModel @Inject constructor(
    private val serverDiscoveryRepository: ServerDiscoveryRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(ServerDiscoveryUiState())
    val uiState: StateFlow<ServerDiscoveryUiState> = _uiState.asStateFlow()
    
    fun startDiscovery() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isDiscovering = true)
            
            try {
                serverDiscoveryRepository.discoverServers().collect { servers ->
                    _uiState.value = _uiState.value.copy(
                        discoveredServers = servers,
                        isDiscovering = false
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isDiscovering = false,
                    connectionError = "Discovery failed: ${e.message}"
                )
            }
        }
    }
    
    fun refreshDiscovery() {
        _uiState.value = _uiState.value.copy(
            discoveredServers = emptyList(),
            connectionError = null
        )
        startDiscovery()
    }
    
    fun connectToManualServer(url: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(
                isConnecting = true,
                connectionError = null
            )
            
            try {
                val serverInfo = serverDiscoveryRepository.testConnection(url)
                _uiState.value = _uiState.value.copy(
                    isConnecting = false,
                    manualServer = serverInfo
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isConnecting = false,
                    connectionError = "Connection failed: ${e.message}"
                )
            }
        }
    }
    
    fun saveServerConnection(serverInfo: ServerInfo) {
        viewModelScope.launch {
            serverDiscoveryRepository.saveServerConnection(serverInfo)
        }
    }
}

data class ServerDiscoveryUiState(
    val discoveredServers: List<ServerInfo> = emptyList(),
    val manualServer: ServerInfo? = null,
    val isDiscovering: Boolean = false,
    val isConnecting: Boolean = false,
    val connectionError: String? = null
)