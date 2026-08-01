package com.lanflix.auth

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import com.google.gson.Gson
import com.lanflix.webview.ServerManager
import java.security.MessageDigest

data class LanflixAccount(
    val id: String = "",
    val username: String = "",
    val displayName: String = "",
    val role: String = "User",
    val isAdministrator: Boolean = false
)

data class AuthTokens(
    val accessToken: String = "",
    val refreshToken: String = "",
    val accessTokenExpiresAtUtc: String = "",
    val account: LanflixAccount = LanflixAccount()
)

class LanflixSessionStore(context: Context, serverUrl: String = ServerManager.activeServerUrl) {
    private val gson = Gson()
    private val preferences = EncryptedSharedPreferences.create(
        context.applicationContext,
        "lanflix_secure_session_${serverKey(serverUrl)}",
        MasterKey.Builder(context.applicationContext).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
        EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
        EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
    )

    val accessToken: String? get() = preferences.getString("access_token", null)
    val refreshToken: String? get() = preferences.getString("refresh_token", null)
    val account: LanflixAccount? get() = preferences.getString("account", null)?.let {
        runCatching { gson.fromJson(it, LanflixAccount::class.java) }.getOrNull()
    }
    val isSignedIn: Boolean get() = !accessToken.isNullOrBlank() && account != null

    fun save(tokens: AuthTokens) {
        preferences.edit()
            .putString("access_token", tokens.accessToken)
            .putString("refresh_token", tokens.refreshToken)
            .putString("account", gson.toJson(tokens.account))
            .apply()
    }

    fun clear() = preferences.edit().clear().apply()

    private companion object {
        fun serverKey(value: String): String = MessageDigest.getInstance("SHA-256")
            .digest(ServerManager.formatServerUrl(value).lowercase().toByteArray())
            .take(8).joinToString("") { "%02x".format(it) }
    }
}
