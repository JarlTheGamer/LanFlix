package com.lanflix.android.ui.profile

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lanflix.android.data.repository.ProfileRepository
import com.lanflix.android.domain.model.Profile
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ProfileViewModel @Inject constructor(
    private val profileRepository: ProfileRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(ProfileUiState())
    val uiState: StateFlow<ProfileUiState> = _uiState.asStateFlow()
    
    fun loadProfiles() {
        viewModelScope.launch {
            println("ProfileViewModel: Loading profiles...")
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            
            try {
                val profiles = profileRepository.getProfiles()
                println("ProfileViewModel: Loaded ${profiles.size} profiles successfully")
                _uiState.value = _uiState.value.copy(
                    profiles = profiles,
                    isLoading = false,
                    error = null
                )
            } catch (e: Exception) {
                println("ProfileViewModel: Error loading profiles: ${e.message}")
                e.printStackTrace()
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = e.message ?: "Unknown error occurred"
                )
            }
        }
    }
    
    fun createProfile(name: String, isKidsProfile: Boolean = false) {
        viewModelScope.launch {
            try {
                val newProfile = profileRepository.createProfile(name, null, isKidsProfile, null)
                val updatedProfiles = _uiState.value.profiles + newProfile
                _uiState.value = _uiState.value.copy(profiles = updatedProfiles)
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = e.message ?: "Failed to create profile"
                )
            }
        }
    }
    
    fun updateProfile(profileId: Int, name: String?, isKidsProfile: Boolean? = null) {
        viewModelScope.launch {
            try {
                val updatedProfile = profileRepository.updateProfile(profileId, name, null, isKidsProfile, null)
                val updatedProfiles = _uiState.value.profiles.map { profile ->
                    if (profile.id == profileId) updatedProfile else profile
                }
                _uiState.value = _uiState.value.copy(profiles = updatedProfiles)
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = e.message ?: "Failed to update profile"
                )
            }
        }
    }
    
    fun deleteProfile(profileId: Int) {
        viewModelScope.launch {
            try {
                profileRepository.deleteProfile(profileId)
                val updatedProfiles = _uiState.value.profiles.filter { it.id != profileId }
                _uiState.value = _uiState.value.copy(profiles = updatedProfiles)
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = e.message ?: "Failed to delete profile"
                )
            }
        }
    }
}

data class ProfileUiState(
    val profiles: List<Profile> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)