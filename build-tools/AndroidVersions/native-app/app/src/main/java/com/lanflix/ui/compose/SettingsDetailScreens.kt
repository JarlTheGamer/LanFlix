package com.lanflix.ui.compose

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.api.*
import com.lanflix.models.ContentItem
import com.lanflix.offline.OfflineMediaStore
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.launch
import java.io.File

@Composable
fun PlaybackSettingsScreen(preferences: DevicePreferences, repository: DevicePreferencesRepository, onBack: () -> Unit) {
    val scope = rememberCoroutineScope()
    var audio by remember(preferences.preferredAudioLanguage) { mutableStateOf(preferences.preferredAudioLanguage) }
    var subtitles by remember(preferences.preferredSubtitleLanguage) { mutableStateOf(preferences.preferredSubtitleLanguage) }
    SettingsPage("Playback, audio & subtitles", onBack) {
        item {
            SettingsDetailCard("Streaming quality") {
                listOf("Original", "High", "Data saver").forEach { quality ->
                    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                        RadioButton(selected = preferences.playbackQuality == quality, onClick = { scope.launch { repository.setPlaybackQuality(quality) } })
                        Text(quality, color = Color.White)
                    }
                }
            }
            Spacer(Modifier.height(12.dp))
            SettingsDetailCard("Languages") {
                OutlinedTextField(audio, { audio = it; scope.launch { repository.setPreferredAudioLanguage(it) } }, Modifier.fillMaxWidth(), label = { Text("Preferred audio language") }, singleLine = true)
                OutlinedTextField(subtitles, { subtitles = it; scope.launch { repository.setPreferredSubtitleLanguage(it) } }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Preferred subtitle language") }, singleLine = true)
                Row(Modifier.fillMaxWidth().padding(top = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                    Column(Modifier.weight(1f)) { Text("Automatic subtitles", color = Color.White, fontWeight = FontWeight.Bold); Text("Select matching text tracks when available", color = LanflixMuted, fontSize = 10.sp) }
                    Switch(preferences.automaticSubtitles, { scope.launch { repository.setAutomaticSubtitles(it) } })
                }
            }
        }
    }
}

@Composable
fun DownloadStorageScreen(onBack: () -> Unit) {
    val context = LocalContext.current
    val store = remember { OfflineMediaStore(context) }
    val scope = rememberCoroutineScope()
    var downloads by remember { mutableStateOf<List<ContentItem>>(emptyList()) }
    suspend fun reload() { downloads = store.readCatalog().filter { it.localFilePath?.let(::File)?.isFile == true } }
    LaunchedEffect(Unit) { reload() }
    val total = downloads.sumOf { it.localFilePath?.let(::File)?.length() ?: 0L }
    SettingsPage("Download storage", onBack) {
        item { SettingsDetailCard("Storage usage") { Text("${downloads.size} verified downloads", color = Color.White, fontWeight = FontWeight.Bold); Text(formatBytes(total), color = LanflixMuted) } }
        if (downloads.isEmpty()) item { Text("No completed downloads on this device.", color = LanflixMuted, modifier = Modifier.padding(24.dp)) }
        items(downloads, key = { "${it.type}:${it.id}" }) { item ->
            Surface(Modifier.fillMaxWidth().padding(top = 8.dp), shape = RoundedCornerShape(14.dp), color = Color.White.copy(alpha = .07f)) {
                Row(Modifier.fillMaxWidth().padding(13.dp), verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Default.DownloadDone, null, tint = LanflixGold)
                    Column(Modifier.padding(start = 12.dp).weight(1f)) { Text(item.displayTitle, color = Color.White, fontWeight = FontWeight.Bold); Text(formatBytes(item.localFilePath?.let(::File)?.length() ?: 0), color = LanflixMuted, fontSize = 10.sp) }
                    IconButton(onClick = { scope.launch { store.removeDownload(item); reload() } }) { Icon(Icons.Default.DeleteOutline, "Delete download", tint = Color.White) }
                }
            }
        }
    }
}

@Composable
fun DiagnosticsScreen(accountName: String?, onBack: () -> Unit) {
    val context = LocalContext.current
    val store = remember { OfflineMediaStore(context) }
    val scope = rememberCoroutineScope()
    var online by remember { mutableStateOf(ServerManager.isOnline) }
    var cachedItems by remember { mutableIntStateOf(0) }
    LaunchedEffect(Unit) { cachedItems = store.readCatalog().size }
    SettingsPage("Diagnostics", onBack) {
        item {
            SettingsDetailCard("Connection") {
                Text(if (online) "Server reachable" else "Server unavailable", color = if (online) Color(0xFF69E0A9) else Color(0xFFE8A858), fontWeight = FontWeight.Bold)
                Text(ServerManager.activeServerUrl, color = LanflixMuted, fontSize = 11.sp)
                Text("Account: ${accountName ?: "offline"}", color = LanflixMuted, fontSize = 11.sp)
                Button(onClick = { scope.launch { online = ServerManager.pingServer(context, ServerManager.activeServerUrl, 3000) } }, Modifier.padding(top = 10.dp)) { Text("Run connection test") }
            }
            Spacer(Modifier.height(12.dp))
            SettingsDetailCard("Local data") {
                Text("$cachedItems cached media records", color = Color.White)
                OutlinedButton(onClick = { scope.launch { store.clearMetadataCache(); cachedItems = store.readCatalog().size } }, Modifier.padding(top = 10.dp)) { Text("Clear metadata-only cache") }
                Text("Completed downloads are never removed by this action.", color = LanflixMuted, fontSize = 10.sp, modifier = Modifier.padding(top = 7.dp))
            }
        }
    }
}

enum class AdministrationSection(val title: String) {
    Overview("Server overview"),
    Accounts("Accounts"),
    Invitations("Invitations"),
    Jobs("Background jobs"),
    LiveTv("Live TV sources")
}

@Composable
fun AdministrationScreen(overview: AdministrationOverview, section: AdministrationSection, onBack: () -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient(context) }
    val scope = rememberCoroutineScope()
    var accounts by remember { mutableStateOf<List<AccountSummary>>(emptyList()) }
    var jobs by remember { mutableStateOf<List<AdminJob>>(emptyList()) }
    var sources by remember { mutableStateOf<List<LiveTvSource>>(emptyList()) }
    var invitation by remember { mutableStateOf<InvitationResult?>(null) }
    var message by remember { mutableStateOf<String?>(null) }
    suspend fun reload() {
        when (section) {
            AdministrationSection.Accounts -> accounts = api.getAccounts()
            AdministrationSection.Jobs -> jobs = api.getAdminJobs()
            AdministrationSection.LiveTv -> sources = api.getLiveTvSources()
            else -> Unit
        }
    }
    LaunchedEffect(section) { reload() }
    SettingsPage(section.title, onBack) {
        when (section) {
            AdministrationSection.Overview -> item {
                SettingsDetailCard("Status") {
                    Text("${overview.movies} movies • ${overview.series} series • ${overview.episodes} episodes", color = Color.White)
                    Text("${overview.musicTracks} tracks • ${overview.liveTvChannels} TV channels", color = LanflixMuted)
                    Text("${formatBytes(overview.workingSetBytes)} memory • ${overview.pendingJobs} active jobs", color = LanflixMuted)
                }
            }
            AdministrationSection.Accounts -> {
                if (accounts.isEmpty()) item { Text("No accounts found.", color = LanflixMuted, modifier = Modifier.padding(16.dp)) }
                items(accounts, key = { it.id }) { account ->
                    SettingsDetailCard(account.displayName) {
                        Text("@${account.username} • ${account.role}${if (account.isDisabled) " • disabled" else ""}", color = LanflixMuted)
                        account.lastLoginAtUtc?.let { Text("Last signed in ${it.take(10)}", color = LanflixMuted, fontSize = 10.sp) }
                    }
                    Spacer(Modifier.height(7.dp))
                }
            }
            AdministrationSection.Invitations -> item {
                SettingsDetailCard("Create invitation") {
                    Text("Invitations are single-use and expire automatically.", color = LanflixMuted)
                    Button(onClick = { scope.launch { invitation = api.createInvitation("User") } }, Modifier.padding(top = 12.dp)) {
                        Icon(Icons.Default.PersonAdd, null)
                        Text("Create user invitation", modifier = Modifier.padding(start = 7.dp))
                    }
                    invitation?.let { Text("Code: ${it.code}\nExpires: ${it.expiresAtUtc.take(10)}", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 12.dp)) }
                }
            }
            AdministrationSection.Jobs -> {
                item {
                    SettingsDetailCard("Run job") {
                        Row(horizontalArrangement = Arrangement.spacedBy(7.dp)) {
                            listOf("library-scan", "music-scan", "live-tv-refresh").forEach { job ->
                                AssistChip(onClick = { scope.launch { message = if (api.triggerAdminJob(job)) "$job queued" else "$job could not be queued"; reload() } }, label = { Text(job.substringBefore('-')) })
                            }
                        }
                        message?.let { Text(it, color = LanflixMuted, fontSize = 11.sp) }
                    }
                }
                items(jobs.take(20), key = { it.id }) { job ->
                    SettingsDetailCard(job.name) { Text(job.status, color = Color.White); job.error?.let { Text(it, color = Color(0xFFFF8A80), fontSize = 10.sp) } }
                    Spacer(Modifier.height(7.dp))
                }
            }
            AdministrationSection.LiveTv -> {
                if (sources.isEmpty()) item { Text("No Live TV sources configured.", color = LanflixMuted, modifier = Modifier.padding(16.dp)) }
                items(sources, key = { it.id }) { source ->
                    SettingsDetailCard(source.name) {
                        Text("${source.kind} • ${if (source.enabled) "enabled" else "disabled"}", color = LanflixMuted)
                        source.lastError?.let { Text(it, color = Color(0xFFFF8A80), fontSize = 10.sp) }
                        Row {
                            TextButton(onClick = { scope.launch { api.refreshLiveTvSource(source.id); reload() } }) { Text("Refresh") }
                            TextButton(onClick = { scope.launch { api.deleteLiveTvSource(source.id); reload() } }) { Text("Remove") }
                        }
                    }
                    Spacer(Modifier.height(7.dp))
                }
            }
        }
    }
}

@Composable
fun AdministrationScreen(overview: AdministrationOverview, onBack: () -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient(context) }
    val scope = rememberCoroutineScope()
    var accounts by remember { mutableStateOf<List<AccountSummary>>(emptyList()) }
    var jobs by remember { mutableStateOf<List<AdminJob>>(emptyList()) }
    var invitation by remember { mutableStateOf<InvitationResult?>(null) }
    var message by remember { mutableStateOf<String?>(null) }
    suspend fun reload() { accounts = api.getAccounts(); jobs = api.getAdminJobs() }
    LaunchedEffect(Unit) { reload() }
    SettingsPage("Server administration", onBack) {
        item {
            SettingsDetailCard("Server overview") {
                Text("${overview.movies} movies • ${overview.series} series • ${overview.episodes} episodes", color = Color.White)
                Text("${overview.musicTracks} tracks • ${overview.liveTvChannels} TV channels", color = LanflixMuted)
                Text("${formatBytes(overview.workingSetBytes)} memory • ${overview.pendingJobs} active jobs", color = LanflixMuted)
            }
            Text("Run server jobs", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 20.dp, bottom = 8.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(7.dp)) {
                listOf("library-scan", "music-scan", "live-tv-refresh").forEach { job ->
                    AssistChip(onClick = { scope.launch { message = if (api.triggerAdminJob(job)) "$job queued" else "$job could not be queued"; reload() } }, label = { Text(job.substringBefore('-')) })
                }
            }
            message?.let { Text(it, color = LanflixMuted, fontSize = 11.sp) }
            Text("Accounts & invitations", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 20.dp, bottom = 8.dp))
            Button(onClick = { scope.launch { invitation = api.createInvitation("User") } }) { Icon(Icons.Default.PersonAdd, null); Text("Create user invitation", modifier = Modifier.padding(start = 7.dp)) }
            invitation?.let { Text("Code: ${it.code}\nExpires: ${it.expiresAtUtc.take(10)}", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp)) }
        }
        items(accounts, key = { it.id }) { account ->
            Surface(Modifier.fillMaxWidth().padding(top = 6.dp), shape = RoundedCornerShape(13.dp), color = Color.White.copy(alpha = .06f)) {
                Row(Modifier.fillMaxWidth().padding(12.dp)) { Icon(Icons.Default.Person, null, tint = Color.White); Column(Modifier.padding(start = 10.dp)) { Text(account.displayName, color = Color.White); Text("@${account.username} • ${account.role}${if (account.isDisabled) " • disabled" else ""}", color = LanflixMuted, fontSize = 10.sp) } }
            }
        }
        if (jobs.isNotEmpty()) item { Text("Recent jobs", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 20.dp, bottom = 8.dp)); jobs.take(10).forEach { Text("${it.name}: ${it.status}${it.error?.let { error -> " • $error" } ?: ""}", color = LanflixMuted, fontSize = 11.sp, modifier = Modifier.padding(vertical = 3.dp)) } }
    }
}

@Composable private fun SettingsPage(title: String, onBack: () -> Unit, content: LazyListScope.() -> Unit) { LazyColumn(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF17394B), LanflixBackground))), contentPadding = PaddingValues(start = 16.dp, end = 16.dp, bottom = 40.dp)) { item { Row(Modifier.fillMaxWidth().statusBarsPadding().height(60.dp), verticalAlignment = Alignment.CenterVertically) { IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }; Text(title, color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold) } }; content() } }
@Composable private fun SettingsDetailCard(title: String, content: @Composable ColumnScope.() -> Unit) { Surface(Modifier.fillMaxWidth(), shape = RoundedCornerShape(17.dp), color = Color.White.copy(alpha = .07f)) { Column(Modifier.padding(15.dp)) { Text(title, color = LanflixGold, fontWeight = FontWeight.Bold, fontSize = 11.sp, modifier = Modifier.padding(bottom = 8.dp)); content() } } }
private fun formatBytes(value: Long): String = when { value >= 1L shl 30 -> "%.2f GB".format(value.toDouble() / (1L shl 30)); value >= 1L shl 20 -> "%.1f MB".format(value.toDouble() / (1L shl 20)); else -> "%.1f KB".format(value.toDouble() / 1024) }
