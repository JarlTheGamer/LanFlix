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
        return try {
            println("ProfileRepository: Getting profiles...")
            val response = apiService.getProfiles()
            
            if (response.isSuccessful) {
                println("ProfileRepository: Response successful, parsing body...")
                val profiles = response.body()
                
                if (profiles == null) {
                    println("ProfileRepository: Response body is null")
                    return emptyList()
                }
                
                println("ProfileRepository: Successfully loaded ${profiles.size} profiles")
                profiles.forEach { profile ->
                    println("ProfileRepository: Profile - ID: ${profile.id}, Name: '${profile.name}', IsKids: ${profile.isKidsProfile}, AvatarPath: ${profile.avatarPath}")
                    println("ProfileRepository: Profile colors - Primary: ${profile.avatarColorPrimary}, Secondary: ${profile.avatarColorSecondary}")
                }
                
                profiles
            } else {
                val errorBody = response.errorBody()?.string()
                println("ProfileRepository: Failed to load profiles - ${response.code()}: ${response.message()}")
                println("ProfileRepository: Error body: $errorBody")
                throw Exception("Failed to load profiles: ${response.code()} ${response.message()}")
            }
        } catch (e: Exception) {
            println("ProfileRepository: Exception occurred: ${e.javaClass.simpleName}: ${e.message}")
            e.printStackTrace()
            throw e
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
        profileId: String,
        name: String? = null,
        avatarPath: String? = null,
        isKidsProfile: Boolean? = null,
        preferences: UserPreferences? = null
    ): Profile {
        val request = UpdateProfileRequest(name, avatarPath, isKidsProfile, preferences)
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