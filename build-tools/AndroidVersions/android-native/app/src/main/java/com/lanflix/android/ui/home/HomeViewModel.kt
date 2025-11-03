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
                // Load recently added content for hero carousel
                val recentlyAdded = contentRepository.getMovies(1, 5) + contentRepository.getSeries(1, 5)
                
                // Load discover preview (if available)
                val discoverPreview = try {
                    contentRepository.searchContent("trending", null).take(10)
                } catch (e: Exception) {
                    emptyList()
                }
                
                // Load all recently added for content section
                val allRecentlyAdded = contentRepository.getMovies(1, 20) + contentRepository.getSeries(1, 20)
                
                _uiState.value = _uiState.value.copy(
                    heroContent = recentlyAdded.take(5),
                    recentlyAdded = allRecentlyAdded,
                    discoverPreview = discoverPreview,
                    isLoading = false,
                    error = null
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = e.message ?: "Failed to load content"
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