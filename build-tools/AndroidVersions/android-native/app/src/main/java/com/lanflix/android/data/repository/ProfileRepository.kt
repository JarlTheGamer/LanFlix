package com.lanflix.android.data.repository

import com.lanflix.android.data.api.CreateProfileRequest
import com.lanflix.android.data.api.LanflixApiService
import com.lanflix.android.data.api.UpdateProfileRequest
import com.lanflix.android.domain.model.Profile
import com.lanflix.android.domain.model.UserPreferences
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ProfileRepository @Inject constructor(
    private val apiService: LanflixApiService
) {
    
    suspend fun getProfiles(): List<Profile> {
        println("ProfileRepository: Getting profiles...")
        val response = apiService.getProfiles()
        if (response.isSuccessful) {
            val profiles = response.body() ?: emptyList()
            println("ProfileRepository: Successfully loaded ${profiles.size} profiles")
            profiles.forEach { profile ->
                println("ProfileRepository: Profile - ID: ${profile.id}, Name: ${profile.name}, IsKids: ${profile.isKidsProfile}")
            }
            return profiles
        } else {
            println("ProfileRepository: Failed to load profiles - ${response.code()}: ${response.message()}")
            throw Exception("Failed to load profiles: ${response.message()}")
        }
    }
    
    suspend fun createProfile(
        name: String,
        avatarPath: String? = null,
        isKidsProfile: Boolean = false,
        preferences: UserPreferences? = null
    ): Profile {
        val request = CreateProfileRequest(name, avatarPath, isKidsProfile, preferences)
        val response = apiService.createProfile(request)
        
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Empty response")
        } else {
            throw Exception("Failed to create profile: ${response.message()}")
        }
    }
    
    suspend fun updateProfile(
        profileId: Int,
        name: String? = null,
        avatarPath: String? = null,
        isKidsProfile: Boolean? = null,
        preferences: UserPreferences? = null
    ): Profile {
        val request = UpdateProfileRequest(name, avatarPath, isKidsProfile, preferences)
        val response = apiService.updateProfile(profileId.toString(), request)
        
        if (response.isSuccessful) {
            return response.body() ?: throw Exception("Empty response")
        } else {
            throw Exception("Failed to update profile: ${response.message()}")
        }
    }
    
    suspend fun deleteProfile(profileId: Int) {
        val response = apiService.deleteProfile(profileId.toString())
        
        if (!response.isSuccessful) {
            throw Exception("Failed to delete profile: ${response.message()}")
        }
    }
}