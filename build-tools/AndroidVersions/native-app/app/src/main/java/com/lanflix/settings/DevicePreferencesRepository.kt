package com.lanflix.settings

import android.content.Context
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.core.stringSetPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.lanflixDataStore by preferencesDataStore(name = "lanflix_device_preferences")

data class DevicePreferences(
    val activeServerUrl: String = ServerManager.DEFAULT_MDNS_HOST,
    val savedServers: Set<String> = setOf(ServerManager.DEFAULT_MDNS_HOST),
    val wifiOnlyDownloads: Boolean = true,
    val reducedMotion: Boolean = false,
    val dynamicArtworkColors: Boolean = true,
    val notificationsEnabled: Boolean = true,
    val playbackQuality: String = "High",
    val preferredAudioLanguage: String = "en",
    val preferredSubtitleLanguage: String = "en",
    val automaticSubtitles: Boolean = true
)

class DevicePreferencesRepository(private val context: Context) {
    private object Keys {
        val activeServer = stringPreferencesKey("active_server")
        val savedServers = stringSetPreferencesKey("saved_servers")
        val wifiOnlyDownloads = booleanPreferencesKey("wifi_only_downloads")
        val reducedMotion = booleanPreferencesKey("reduced_motion")
        val dynamicArtworkColors = booleanPreferencesKey("dynamic_artwork_colors")
        val notificationsEnabled = booleanPreferencesKey("notifications_enabled")
        val playbackQuality = stringPreferencesKey("playback_quality")
        val preferredAudioLanguage = stringPreferencesKey("preferred_audio_language")
        val preferredSubtitleLanguage = stringPreferencesKey("preferred_subtitle_language")
        val automaticSubtitles = booleanPreferencesKey("automatic_subtitles")
    }

    val preferences: Flow<DevicePreferences> = context.lanflixDataStore.data.map { values ->
        val legacyActive = ServerManager.getSavedServer(context)
        val legacySaved = ServerManager.getSavedServers(context)
        DevicePreferences(
            activeServerUrl = values[Keys.activeServer] ?: legacyActive,
            savedServers = values[Keys.savedServers] ?: legacySaved.ifEmpty { setOf(ServerManager.DEFAULT_MDNS_HOST) },
            wifiOnlyDownloads = values[Keys.wifiOnlyDownloads] ?: true,
            reducedMotion = values[Keys.reducedMotion] ?: false,
            dynamicArtworkColors = values[Keys.dynamicArtworkColors] ?: true,
            notificationsEnabled = values[Keys.notificationsEnabled] ?: true,
            playbackQuality = values[Keys.playbackQuality] ?: "High",
            preferredAudioLanguage = values[Keys.preferredAudioLanguage] ?: "en",
            preferredSubtitleLanguage = values[Keys.preferredSubtitleLanguage] ?: "en",
            automaticSubtitles = values[Keys.automaticSubtitles] ?: true
        )
    }

    suspend fun selectServer(url: String) {
        val formatted = ServerManager.formatServerUrl(url)
        context.lanflixDataStore.edit { values ->
            values[Keys.activeServer] = formatted
            values[Keys.savedServers] = (values[Keys.savedServers] ?: ServerManager.getSavedServers(context)) + formatted
        }
        ServerManager.saveServer(context, formatted)
        ServerManager.activeServerUrl = formatted
    }

    suspend fun removeServer(url: String) {
        val formatted = ServerManager.formatServerUrl(url)
        context.lanflixDataStore.edit { values ->
            values[Keys.savedServers] = (values[Keys.savedServers] ?: emptySet()) - formatted
        }
    }

    suspend fun setWifiOnlyDownloads(enabled: Boolean) = context.lanflixDataStore.edit { it[Keys.wifiOnlyDownloads] = enabled }
    suspend fun setReducedMotion(enabled: Boolean) = context.lanflixDataStore.edit { it[Keys.reducedMotion] = enabled }
    suspend fun setDynamicArtworkColors(enabled: Boolean) = context.lanflixDataStore.edit { it[Keys.dynamicArtworkColors] = enabled }
    suspend fun setNotificationsEnabled(enabled: Boolean) = context.lanflixDataStore.edit { it[Keys.notificationsEnabled] = enabled }
    suspend fun setPlaybackQuality(value: String) = context.lanflixDataStore.edit { it[Keys.playbackQuality] = value }
    suspend fun setPreferredAudioLanguage(value: String) = context.lanflixDataStore.edit { it[Keys.preferredAudioLanguage] = value.trim().lowercase().take(8) }
    suspend fun setPreferredSubtitleLanguage(value: String) = context.lanflixDataStore.edit { it[Keys.preferredSubtitleLanguage] = value.trim().lowercase().take(8) }
    suspend fun setAutomaticSubtitles(enabled: Boolean) = context.lanflixDataStore.edit { it[Keys.automaticSubtitles] = enabled }
}
