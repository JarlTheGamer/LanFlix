@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class)

package com.lanflix.ui.compose

import android.content.Intent
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import com.lanflix.ui.compose.screens.EditProfileScreen
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.activity.compose.BackHandler
import com.lanflix.models.ContentItem
import com.lanflix.api.MusicAlbum
import com.lanflix.api.MusicTrack
import com.lanflix.api.MusicPlaylist
import com.lanflix.music.MusicPlaybackController
import com.lanflix.webview.ServerBrowserActivity
import com.lanflix.webview.ServerManager
import com.lanflix.ui.compose.navigation.Destination
import com.lanflix.ui.compose.navigation.AppOverlay
import com.lanflix.ui.compose.components.TopChrome
import com.lanflix.ui.compose.components.BottomChrome
import com.lanflix.ui.compose.components.ProfileMenu
import com.lanflix.ui.compose.screens.*
import kotlinx.coroutines.launch

@Composable
fun LanflixApp(viewModel: LanflixViewModel = viewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val appContext = LocalContext.current
    val scope = rememberCoroutineScope()
    val musicController = remember { MusicPlaybackController.get(appContext) }
    val musicPlayback by musicController.state.collectAsStateWithLifecycle()
    var destination by remember { mutableStateOf(Destination.Home) }
    var detail by remember { mutableStateOf<ContentItem?>(null) }
    var profileMenuVisible by remember { mutableStateOf(false) }
    val overlayStack = remember { mutableStateListOf<AppOverlay>() }
    var playerItem by remember { mutableStateOf<ContentItem?>(null) }
    var musicAlbum by remember { mutableStateOf<MusicAlbum?>(null) }
    var musicTrack by remember { mutableStateOf<MusicTrack?>(null) }
    var musicQueue by remember { mutableStateOf<List<MusicTrack>>(emptyList()) }
    var musicPlaylist by remember { mutableStateOf<MusicPlaylist?>(null) }
    var libraryFilter by remember { mutableStateOf("Movies") }
    var musicHomeVisible by remember { mutableStateOf(false) }
    val currentOverlay = overlayStack.lastOrNull()

    // Smart TV DLNA Cast Manager
    val castManager = remember { com.lanflix.cast.DlnaCastManager(appContext) }
    val discoveredDevices by castManager.discoveredDevices.collectAsStateWithLifecycle()
    val activeCastDevice by castManager.activeDevice.collectAsStateWithLifecycle()
    val isCasting by castManager.isCasting.collectAsStateWithLifecycle()
    val isPlayingOnTv by castManager.isPlayingOnTv.collectAsStateWithLifecycle()
    var showCastDialog by remember { mutableStateOf(false) }
    var itemToCast by remember { mutableStateOf<ContentItem?>(null) }

    val startCastFlow = { targetItem: ContentItem? ->
        itemToCast = targetItem ?: detail ?: state.library.firstOrNull()
        showCastDialog = true
        scope.launch { castManager.discoverDevices() }
    }

    fun openOverlay(overlay: AppOverlay) {
        profileMenuVisible = false
        if (overlayStack.lastOrNull() != overlay) overlayStack.add(overlay)
    }

    fun closeOverlay() {
        if (overlayStack.isNotEmpty()) overlayStack.removeAt(overlayStack.lastIndex)
    }

    if (state.authenticationRequired && state.online) {
        LanflixTheme {
            AuthenticationScreen(state, onAuthenticate = { username, displayName, password, invitation ->
                viewModel.authenticate(username, displayName, password, invitation) { }
            }, onServer = {
                appContext.startActivity(Intent(appContext, ServerBrowserActivity::class.java))
            })
        }
        return
    }

    BackHandler(enabled = playerItem != null || musicTrack != null || musicAlbum != null || musicPlaylist != null || detail != null || musicHomeVisible || profileMenuVisible || overlayStack.isNotEmpty()) {
        when {
            playerItem != null -> playerItem = null
            musicTrack != null -> musicTrack = null
            musicAlbum != null -> musicAlbum = null
            musicPlaylist != null -> musicPlaylist = null
            detail != null -> detail = null
            musicHomeVisible -> musicHomeVisible = false
            profileMenuVisible -> profileMenuVisible = false
            overlayStack.isNotEmpty() -> closeOverlay()
        }
    }

    LanflixTheme {
        Surface(modifier = Modifier.fillMaxSize(), color = LanflixBackground) {
            when {
                playerItem != null -> PlayerScreen(playerItem!!, onBack = { playerItem = null })
                musicTrack != null -> MusicPlayerScreen(musicTrack!!, musicQueue, onBack = { musicTrack = null })
                musicAlbum != null -> MusicAlbumScreen(
                    album = musicAlbum!!,
                    onBack = { musicAlbum = null },
                    onPlay = { track, queue -> musicQueue = queue; musicTrack = track }
                )
                musicPlaylist != null -> MusicPlaylistScreen(
                    playlist = musicPlaylist!!,
                    onBack = { musicPlaylist = null },
                    onPlay = { track, queue -> musicQueue = queue; musicTrack = track }
                )
                detail != null -> DetailScreen(
                    item = detail!!,
                    online = state.online,
                    downloading = "${detail!!.type}:${detail!!.id}" in state.downloading,
                    onBack = { detail = null },
                    onPlay = { playerItem = detail },
                    onPlayEpisode = { episode -> playerItem = episode.asContentItem(detail!!) },
                    onDownload = { viewModel.download(detail!!) { saved -> if (saved != null) detail = saved } },
                    onCast = { startCastFlow(it) }
                )
                currentOverlay == AppOverlay.Search -> SearchScreen(state.library, onBack = ::closeOverlay, onSelect = { detail = it })
                currentOverlay == AppOverlay.Account && state.account != null -> AccountSecurityScreen(state.account!!, onBack = ::closeOverlay, onSignedOut = { overlayStack.clear(); viewModel.signOut() })
                currentOverlay == AppOverlay.Activity -> ActivityScreen(
                    feed = state.socialFeed,
                    onBack = ::closeOverlay,
                    onCreatePost = { body, visibility -> viewModel.createPost(body, visibility) },
                    onReact = { postId, kind -> viewModel.react(postId, kind) },
                    onDelete = { postId -> viewModel.deletePost(postId) }
                )
                currentOverlay == AppOverlay.Notifications -> NotificationsScreen(
                    notifications = state.notifications,
                    onBack = ::closeOverlay,
                    onMarkAllRead = { viewModel.markAllNotificationsRead() },
                    onMarkRead = { id -> viewModel.markNotificationRead(id) }
                )
                currentOverlay == AppOverlay.Settings -> SettingsScreen(
                    state = state,
                    onBack = ::closeOverlay,
                    onRetry = viewModel::refresh,
                    onAccount = { openOverlay(AppOverlay.Account) },
                    onActivity = { openOverlay(AppOverlay.Activity) },
                    onNotifications = { openOverlay(AppOverlay.Notifications) },
                    onFriends = { viewModel.loadRelationships(); openOverlay(AppOverlay.Friends) },
                    onEditProfile = { openOverlay(AppOverlay.EditProfile) }
                )
                currentOverlay == AppOverlay.Profile -> ProfileScreen(
                    library = state.library,
                    account = state.account,
                    activity = emptyList(),
                    pendingRequests = state.relationships.count { it.kind == "friend" && it.status == "pending" && it.incoming },
                    onBack = ::closeOverlay,
                    onSelect = { detail = it },
                    onAccount = { openOverlay(AppOverlay.Account) },
                    onActivity = { openOverlay(AppOverlay.Activity) },
                    onFriends = { viewModel.loadRelationships(); openOverlay(AppOverlay.Friends) },
                    onEditProfile = { openOverlay(AppOverlay.EditProfile) }
                )
                currentOverlay == AppOverlay.Friends -> FriendsScreen(
                    relationships = state.relationships,
                    onBack = ::closeOverlay,
                    onAccept = { id -> viewModel.acceptFriendRequest(id) },
                    onRemoveFriend = { id -> viewModel.removeFriend(id) },
                    onUnfollow = { id -> viewModel.unfollow(id) }
                )
                currentOverlay == AppOverlay.EditProfile -> EditProfileScreen(
                    account = state.account,
                    onBack = ::closeOverlay,
                    onProfileUpdated = { viewModel.refresh() }
                )
                else -> Box(Modifier.fillMaxSize()) {
                    if (musicHomeVisible) {
                        MusicLibraryScreen(
                            music = state.music,
                            onAlbum = { musicAlbum = it },
                            onPlay = { track, queue -> musicQueue = queue; musicTrack = track },
                            onPlaylist = { musicPlaylist = it },
                            onBack = { musicHomeVisible = false }
                        )
                    } else AnimatedContent(targetState = destination, label = "main-destination") { target ->
                        when (target) {
                            Destination.Home -> HomeScreen(
                                state = state,
                                onSelect = { detail = it },
                                onRetry = viewModel::refresh,
                                onOpenMusic = {
                                    destination = Destination.Libraries
                                    musicHomeVisible = true
                                }
                            )
                            Destination.Libraries -> LibraryScreen(
                                media = state.library,
                                music = state.music,
                                selectedFilter = libraryFilter,
                                onFilterSelected = { libraryFilter = it },
                                onOpenMusic = { musicHomeVisible = true },
                                onSelect = { detail = it },
                                onMusicAlbum = { musicAlbum = it },
                                onMusicPlay = { track, queue -> musicQueue = queue; musicTrack = track }
                            )
                            Destination.Live -> LiveTvScreen(state.online, state.liveTvChannels)
                            Destination.Demand -> DownloadsScreen(state.library, onSelect = { detail = it })
                            Destination.Discover -> DiscoverScreen(state, onSelect = { detail = it })
                        }
                    }
                    if (!musicHomeVisible) {
                        TopChrome(
                            title = if (destination == Destination.Home) "lanflix" else destination.label,
                            online = state.online,
                            onSearch = { openOverlay(AppOverlay.Search) },
                            onProfile = { profileMenuVisible = !profileMenuVisible },
                            onCast = { startCastFlow(null) }
                        )
                    }
                    AnimatedVisibility(
                        visible = profileMenuVisible,
                        enter = fadeIn(),
                        exit = fadeOut(),
                        modifier = Modifier.align(Alignment.TopEnd).statusBarsPadding().padding(top = 54.dp, end = 9.dp)
                    ) {
                        ProfileMenu(
                            online = state.online,
                            onProfile = { openOverlay(AppOverlay.Profile) },
                            onDownloads = { profileMenuVisible = false; destination = Destination.Demand },
                            onSettings = { openOverlay(AppOverlay.Settings) }
                        )
                    }

                    // Smart TV Active Casting Bar
                    if (isCasting && activeCastDevice != null) {
                        Row(
                            modifier = Modifier
                                .align(Alignment.BottomCenter)
                                .padding(bottom = 68.dp, start = 12.dp, end = 12.dp)
                                .fillMaxWidth()
                                .clip(RoundedCornerShape(16.dp))
                                .background(Color(0xF7101820))
                                .padding(horizontal = 14.dp, vertical = 10.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Filled.Cast, contentDescription = null, tint = Color(0xFFE50914), modifier = Modifier.size(24.dp))
                            Spacer(Modifier.width(10.dp))
                            Column(Modifier.weight(1f)) {
                                Text("Casting to ${activeCastDevice!!.name}", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 13.sp)
                                if (!itemToCast?.title.isNullOrBlank()) {
                                    Text(itemToCast!!.title ?: "", color = Color.Gray, fontSize = 11.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
                                }
                            }
                            IconButton(onClick = {
                                scope.launch {
                                    if (isPlayingOnTv) castManager.pause() else castManager.play()
                                }
                            }) {
                                Icon(if (isPlayingOnTv) Icons.Filled.PlayArrow else Icons.Filled.PlayArrow, contentDescription = "Play/Pause", tint = Color.White)
                            }
                            Button(
                                onClick = { scope.launch { castManager.stopCasting() } },
                                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFE50914), contentColor = Color.White),
                                shape = RoundedCornerShape(12.dp),
                                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 4.dp)
                            ) {
                                Text("Stop", fontSize = 11.sp, fontWeight = FontWeight.Bold)
                            }
                        }
                    }

                    if (!musicHomeVisible) {
                        Box(Modifier.align(Alignment.BottomCenter)) {
                            BottomChrome(destination, onSelect = {
                                musicHomeVisible = false
                                destination = it
                            })
                        }
                    }
                    if (musicHomeVisible && musicPlayback.currentTrack != null) {
                        Box(Modifier.align(Alignment.BottomCenter)) {
                            MusicMiniPlayer(musicPlayback) {
                                musicQueue = musicPlayback.queue
                                musicTrack = musicPlayback.currentTrack
                            }
                        }
                    }
                }
            }
        }
    }

    if (showCastDialog) {
        androidx.compose.material3.AlertDialog(
            onDismissRequest = { showCastDialog = false },
            title = { Text("Cast to Smart TV") },
            text = {
                Column {
                    Text("Select a TV on your Wi-Fi network (Samsung, LG, Sony, Fire TV, DLNA):", fontSize = 14.sp, color = Color.Gray)
                    Spacer(Modifier.height(12.dp))
                    if (discoveredDevices.isEmpty()) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            CircularProgressIndicator(modifier = Modifier.size(20.dp), strokeWidth = 2.dp)
                            Spacer(Modifier.width(12.dp))
                            Text("Searching for Smart TVs...")
                        }
                    } else {
                        discoveredDevices.forEach { device ->
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clickable {
                                        showCastDialog = false
                                        val mediaItem = itemToCast ?: detail ?: state.library.firstOrNull()
                                        if (mediaItem != null) {
                                            val kind = if (mediaItem.type.equals("episode", true)) "episode" else "movie"
                                            val mediaUrl = "${ServerManager.activeServerUrl}/api/v2/playback/$kind/${mediaItem.id}/file?client=tv"
                                            scope.launch {
                                                castManager.castMedia(device, mediaUrl, mediaItem.title ?: "Lanflix Media")
                                            }
                                        }
                                    }
                                    .padding(vertical = 10.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Icon(Icons.Filled.Cast, contentDescription = null, tint = Color(0xFFE50914))
                                Spacer(Modifier.width(12.dp))
                                Column {
                                    Text(device.name, fontWeight = FontWeight.Bold)
                                    if (device.manufacturer.isNotBlank()) {
                                        Text(device.manufacturer, fontSize = 12.sp, color = Color.Gray)
                                    }
                                }
                            }
                        }
                    }
                }
            },
            confirmButton = {
                androidx.compose.material3.TextButton(onClick = { showCastDialog = false }) {
                    Text("Cancel")
                }
            }
        )
    }
}
