package com.lanflix.android.utils

import com.google.gson.Gson
import com.lanflix.android.domain.model.Profile
import com.lanflix.android.domain.model.UserPreferences

object ProfileTestUtil {
    
    fun testProfileParsing() {
        try {
            println("ProfileTestUtil: Testing profile JSON parsing...")
            
            val sampleJson = """
                [{
                  "id": 1,
                  "name": "Default",
                  "avatarPath": null,
                  "isKidsProfile": false,
                  "preferences": {
                    "preferredAudioLanguage": "en",
                    "preferredSubtitleLanguage": "en",
                    "subtitlesEnabled": false,
                    "preferredBitrate": null,
                    "autoSkipIntro": false,
                    "autoPlayNextEpisode": true,
                    "maxResolution": "1080p",
                    "allowHardwareAcceleration": true,
                    "forceTranscode": false,
                    "theme": "dark"
                  },
                  "createdAt": "2025-11-03T08:44:51.0446278"
                }]
            """.trimIndent()
            
            val gson = Gson()
            val profiles = gson.fromJson(sampleJson, Array<Profile>::class.java).toList()
            
            println("ProfileTestUtil: Successfully parsed ${profiles.size} profiles")
            profiles.forEach { profile ->
                println("ProfileTestUtil: Profile - ID: ${profile.id}, Name: '${profile.name}'")
                println("ProfileTestUtil: Avatar colors - Primary: ${profile.avatarColorPrimary}, Secondary: ${profile.avatarColorSecondary}")
                println("ProfileTestUtil: Preferences: ${profile.preferences}")
            }
            
        } catch (e: Exception) {
            println("ProfileTestUtil: JSON parsing failed: ${e.message}")
            e.printStackTrace()
        }
    }
}