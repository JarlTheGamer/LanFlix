package com.lanflix.android.data.preferences

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

val Context.serverDataStore: DataStore<Preferences> by preferencesDataStore(name = "server_preferences")

@Singleton
class ServerPreferences @Inject constructor(
    @ApplicationContext private val context: Context
) {
    private val SERVER_URL_KEY = stringPreferencesKey("server_url")
    private val SERVER_NAME_KEY = stringPreferencesKey("server_name")
    
    val serverUrl: Flow<String> = context.serverDataStore.data.map { preferences ->
        // Always return empty to force server discovery
        ""
    }
    
    val serverName: Flow<String> = context.serverDataStore.data.map { preferences ->
        preferences[SERVER_NAME_KEY] ?: "Lanflix Server"
    }
    
    suspend fun saveServerInfo(url: String, name: String) {
        context.serverDataStore.edit { preferences ->
            preferences[SERVER_URL_KEY] = url
            preferences[SERVER_NAME_KEY] = name
        }
    }
    
    suspend fun getServerUrl(): String {
        return serverUrl.first()
    }
    
    suspend fun clearServerInfo() {
        context.serverDataStore.edit { preferences ->
            preferences.remove(SERVER_URL_KEY)
            preferences.remove(SERVER_NAME_KEY)
        }
    }
}