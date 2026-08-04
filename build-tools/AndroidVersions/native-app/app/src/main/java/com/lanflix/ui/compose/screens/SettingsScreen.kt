package com.lanflix.ui.compose.screens

import android.content.Intent
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AccountCircle
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.DynamicFeed
import androidx.compose.material.icons.filled.LiveTv
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.People
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Storage
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import com.lanflix.ui.compose.AdministrationSection
import com.lanflix.ui.compose.LanflixBackground
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.LanflixUiState
import com.lanflix.ui.compose.components.ProfileMenuRow
import com.lanflix.ui.compose.components.SettingsCard
import com.lanflix.ui.compose.components.SettingsHeading
import com.lanflix.ui.compose.components.SettingsToggleRow
import com.lanflix.ui.compose.AdministrationScreen
import com.lanflix.ui.compose.AppearanceSettingsScreen
import com.lanflix.ui.compose.DiagnosticsScreen
import com.lanflix.ui.compose.DownloadStorageScreen
import com.lanflix.ui.compose.PlaybackSettingsScreen
import com.lanflix.webview.ServerBrowserActivity
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.launch

@Composable
fun SettingsScreen(
    state: LanflixUiState,
    onBack: () -> Unit,
    onRetry: () -> Unit,
    onAccount: () -> Unit,
    onActivity: () -> Unit,
    onNotifications: () -> Unit,
    onFriends: () -> Unit = {},
    onEditProfile: () -> Unit = {}
) {
    val context = LocalContext.current
    val repository = remember(context) { DevicePreferencesRepository(context.applicationContext) }
    val preferences by repository.preferences.collectAsStateWithLifecycle(initialValue = DevicePreferences())
    val scope = rememberCoroutineScope()
    var subpage by remember { mutableStateOf<String?>(null) }
    BackHandler(enabled = subpage != null) { subpage = null }
    when (subpage) {
        "playback" -> { PlaybackSettingsScreen(preferences, repository) { subpage = null }; return }
        "downloads" -> { DownloadStorageScreen { subpage = null }; return }
        "diagnostics" -> { DiagnosticsScreen(state.account?.displayName) { subpage = null }; return }
        "admin-overview" -> state.administration?.let { AdministrationScreen(it, AdministrationSection.Overview) { subpage = null }; return }
        "admin-accounts" -> state.administration?.let { AdministrationScreen(it, AdministrationSection.Accounts) { subpage = null }; return }
        "admin-invitations" -> state.administration?.let { AdministrationScreen(it, AdministrationSection.Invitations) { subpage = null }; return }
        "admin-jobs" -> state.administration?.let { AdministrationScreen(it, AdministrationSection.Jobs) { subpage = null }; return }
        "admin-live-tv" -> state.administration?.let { AdministrationScreen(it, AdministrationSection.LiveTv) { subpage = null }; return }
    }
    LazyColumn(
        Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF1E1738), LanflixBackground))),
        contentPadding = PaddingValues(bottom = 42.dp)
    ) {
        item {
            Row(Modifier.fillMaxWidth().statusBarsPadding().height(58.dp), verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = onBack) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
                Text("Settings", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold)
            }
        }
        item {
            Column(Modifier.padding(horizontal = 16.dp)) {
                SettingsHeading("Account and profile")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.Person, "Edit profile", "Avatar, background banner, display name & status", onEditProfile)
                    ProfileMenuRow(Icons.Filled.AccountCircle, state.account?.displayName ?: "Account security", "Password, email and security preferences", onAccount)
                }
                SettingsHeading("Server connection")
                SettingsCard {
                    Row(Modifier.fillMaxWidth().padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.Storage, null, tint = LanflixGold, modifier = Modifier.size(24.dp))
                        Column(Modifier.padding(start = 12.dp).weight(1f)) {
                            Text(if (state.online) "Connected" else "Server unavailable", color = Color.White, fontWeight = FontWeight.Bold)
                            Text(ServerManager.activeServerUrl, color = LanflixMuted, fontSize = 10.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
                        }
                        Box(Modifier.size(9.dp).clip(CircleShape).background(if (state.online) Color(0xFF58C878) else Color(0xFFE59A44)))
                    }
                    ProfileMenuRow(Icons.Filled.Storage, "Change server", "Discover or enter a server address") {
                        context.startActivity(Intent(context, ServerBrowserActivity::class.java))
                    }
                    ProfileMenuRow(Icons.Filled.CloudOff, "Retry connection", "Refresh server and cached library", onRetry)
                }
                SettingsHeading("Playback and downloads")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.PlayArrow, "Playback", "Quality, subtitles and audio") { subpage = "playback" }
                    SettingsToggleRow(Icons.Filled.Download, "Wi-Fi only downloads", "Prevent mobile-data downloads", preferences.wifiOnlyDownloads) {
                        scope.launch { repository.setWifiOnlyDownloads(it) }
                    }
                    ProfileMenuRow(Icons.Filled.Download, "Download storage", "Manage completed offline movies and episodes") { subpage = "downloads" }
                }
                SettingsHeading("Appearance and accessibility")
                SettingsCard {
                    SettingsToggleRow(Icons.Filled.Settings, "Dynamic artwork colors", "Use the server palette across each title", preferences.dynamicArtworkColors) {
                        scope.launch { repository.setDynamicArtworkColors(it) }
                    }
                    SettingsToggleRow(Icons.Filled.Settings, "Reduced motion", "Limit artwork transitions and animation", preferences.reducedMotion) {
                        scope.launch { repository.setReducedMotion(it) }
                    }
                }
                SettingsHeading("Notifications and social")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.Notifications, "Notifications",
                        if (state.unreadNotificationCount > 0) "${state.unreadNotificationCount} unread" else "No new notifications",
                        onNotifications)
                    ProfileMenuRow(Icons.Filled.People, "Friends & Social",
                        "Friends, following, requests and activity", onFriends)
                    ProfileMenuRow(Icons.Filled.DynamicFeed, "Activity feed",
                        "See what friends are watching and reviewing", onActivity)
                    SettingsToggleRow(Icons.Filled.Notifications, "Push notifications", "Downloads, activity, invites and requests", preferences.notificationsEnabled) {
                        scope.launch { repository.setNotificationsEnabled(it) }
                    }
                }
                SettingsHeading("Devices and diagnostics")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.Cast, "Devices and sessions", "Registered playback clients and account sessions", onAccount)
                    ProfileMenuRow(Icons.Filled.Settings, "Diagnostics", "Server health and local cache") { subpage = "diagnostics" }
                }
                state.administration?.let { admin ->
                    SettingsHeading("Server administration")
                    SettingsCard {
                        ProfileMenuRow(Icons.Filled.Storage, "Server overview", "${admin.movies} movies • ${admin.series} series • ${admin.musicTracks} tracks") { subpage = "admin-overview" }
                        ProfileMenuRow(Icons.Filled.Person, "Accounts", "${admin.accounts} accounts • ${admin.openReports} open reports") { subpage = "admin-accounts" }
                        ProfileMenuRow(Icons.Filled.AccountCircle, "Invitations", "Create single-use account invitations") { subpage = "admin-invitations" }
                        ProfileMenuRow(Icons.Filled.Settings, "Jobs", "${admin.pendingJobs} active background jobs") { subpage = "admin-jobs" }
                        ProfileMenuRow(Icons.Filled.LiveTv, "Live TV sources", "${admin.liveTvChannels} configured channels") { subpage = "admin-live-tv" }
                    }
                }
            }
        }
    }
}
