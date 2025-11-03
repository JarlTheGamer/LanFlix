package com.lanflix.android.ui.search

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.android.data.repository.ContentRepository
import com.lanflix.android.domain.model.Content
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@OptIn(FlowPreview::class)
@HiltViewModel
class SearchViewModel @Inject constructor(
    private val contentRepository: ContentRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(SearchUiState())
    val uiState: StateFlow<SearchUiState> = _uiState.asStateFlow()
    
    private val _query = MutableStateFlow("")
    
    init {
        // Auto-search with debounce (like web version)
        _query
            .debounce(300) // Wait 300ms after user stops typing
            .distinctUntilChanged()
            .onEach { query ->
                _uiState.value = _uiState.value.copy(query = query)
                if (query.isNotBlank()) {
                    search(query)
                } else {
                    _uiState.value = _uiState.value.copy(
                        results = emptyList(),
                        isLoading = false,
                        error = null
                    )
                }
            }
            .launchIn(viewModelScope)
    }
    
    fun updateQuery(query: String) {
        _query.value = query
    }
    
    fun clearQuery() {
        _query.value = ""
    }
    
    fun search(query: String) {
        if (query.isBlank()) return
        
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            
            try {
                val results = contentRepository.searchContent(query)
                _uiState.value = _uiState.value.copy(
                    results = results,
                    isLoading = false,
                    error = null
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = e.message ?: "Search failed"
                )
            }
        }
    }
}

data class SearchUiState(
    val query: String = "",
    val results: List<Content> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)