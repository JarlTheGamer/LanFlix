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
                
                // Load recently added content for hero carousel
                val recentlyAdded = try {
                    println("HomeViewModel: Loading movies and series...")
                    val movies = contentRepository.getMovies(1, 5)
                    val series = contentRepository.getSeries(1, 5)
                    movies + series
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load movies/series: ${e.message}")
                    throw e
                }
                
                // Load discover preview (if available)
                val discoverPreview = try {
                    println("HomeViewModel: Loading discover preview...")
                    contentRepository.searchContent("trending", null).take(10)
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load discover preview: ${e.message}")
                    emptyList()
                }
                
                // Load all recently added for content section
                val allRecentlyAdded = try {
                    println("HomeViewModel: Loading all recently added content...")
                    contentRepository.getMovies(1, 20) + contentRepository.getSeries(1, 20)
                } catch (e: Exception) {
                    println("HomeViewModel: Failed to load all recently added: ${e.message}")
                    recentlyAdded // Use the smaller set if this fails
                }
                
                println("HomeViewModel: Successfully loaded content")
                _uiState.value = _uiState.value.copy(
                    heroContent = recentlyAdded.take(5),
                    recentlyAdded = allRecentlyAdded,
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