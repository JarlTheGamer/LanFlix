package com.lanflix.android.ui.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.android.data.repository.ContentRepository
import com.lanflix.android.domain.model.Content
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val contentRepository: ContentRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(HomeUiState())
    val uiState: StateFlow<HomeUiState> = _uiState.asStateFlow()
    
    fun loadContent() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            
            try {
                println("HomeViewModel: Starting to load content...")

                // Load movies and series once
                val movies = try {
                    println("HomeViewModel: Loading movies...")
                    contentRepository.getMovies()
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load movies: ${e.message}")
                    throw e
                }

                val series = try {
                    println("HomeViewModel: Loading series...")
                    contentRepository.getSeries()
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load series: ${e.message}")
                    throw e
                }

                val allContent = movies + series
                
                // Load discover preview (if available)
                val discoverPreview = try {
                    println("HomeViewModel: Loading discover preview...")
                    contentRepository.searchContent("trending", null).take(10)
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load discover preview: ${e.message}")
                    emptyList()
                }
                
                println("HomeViewModel: Successfully loaded content")
                _uiState.value = _uiState.value.copy(
                    heroContent = allContent.take(5),
                    recentlyAdded = allContent,
                    discoverPreview = discoverPreview,
                    isLoading = false,
                    error = null
                )
            } catch (e: Exception) {
                val errorMessage = when {
                    e.message?.contains("ConnectException") == true -> "Cannot connect to server. Please check if the server is running and accessible."
                    e.message?.contains("timeout") == true -> "Connection timeout. Server may be slow or unreachable."
                    e.message?.contains("UnknownHostException") == true -> "Server not found. Please check the server address."
                    else -> e.message ?: "Failed to load content"
                }
                
                println("HomeViewModel: Error loading content: $errorMessage")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = errorMessage
                )
            }
        }
    }
    
    fun refreshContent() {
        loadContent()
    }
}

data class HomeUiState(
    val heroContent: List<Content> = emptyList(),
    val recentlyAdded: List<Content> = emptyList(),
    val discoverPreview: List<Content> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)