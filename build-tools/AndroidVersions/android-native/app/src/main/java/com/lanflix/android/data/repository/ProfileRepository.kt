package com.lanflix.android.data.repository

import com.lanflix.android.data.api.CreateProfileRequest
import com.lanflix.android.data.api.LanflixApiService
import com.lanflix.android.data.api.UpdateProfileRequest
import com.lanflix.android.domain.model.Profile
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ProfileRepository @Inject constructor(
    private val apiService: LanflixApiService
) {
    
    suspend fun getProfiles(): List<Profile> {
        val response = apiService.getProfiles()
        if (response.isSuccessful) {
            return response.body() ?: emptyList()
        } else {
            throw Exception("Failed to load profiles: ${response.message()}")
        }
    }
    
    suspend fun createProfile(
        name: String,
        primaryColor: String,
        secondaryColor: String
    ): Profile {
        val request = CreateProfileRequest(name, primaryColor, secondaryColor)
        val response = apiService.createProfile(request)
        
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Empty response")
        } else {
            throw Exception("Failed to create profile: ${response.message()}")
        }
    }
    
    suspend fun updateProfile(
        profileId: String,
        name: String? = null,
        primaryColor: String? = null,
        secondaryColor: String? = null
    ): Profile {
        val request = UpdateProfileRequest(name, primaryColor, secondaryColor)
        val response = apiService.updateProfile(profileId, request)
        
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Empty response")
        } else {
            throw Exception("Failed to update profile: ${response.message()}")
        }
    }
    
    suspend fun deleteProfile(profileId: String) {
        val response = apiService.deleteProfile(profileId)
        
        if (!response.isSuccessful) {
            throw Exception("Failed to delete profile: ${response.message()}")
        }
    }
}