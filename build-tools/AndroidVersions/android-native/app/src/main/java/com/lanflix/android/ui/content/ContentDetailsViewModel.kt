package com.lanflix.android.ui.content

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
class ContentDetailsViewModel @Inject constructor(
    private val contentRepository: ContentRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(ContentDetailsUiState())
    val uiState: StateFlow<ContentDetailsUiState> = _uiState.asStateFlow()
    
    fun loadContent(contentId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            
            try {
                val content = contentRepository.getContentDetails(contentId)
                _uiState.value = _uiState.value.copy(
                    content = content,
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
}

data class ContentDetailsUiState(
    val content: Content? = null,
    val isLoading: Boolean = false,
    val error: String? = null
)