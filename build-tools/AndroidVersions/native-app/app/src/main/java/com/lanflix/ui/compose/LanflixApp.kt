@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class)

package com.lanflix.ui.compose

import android.content.Intent
import android.app.Activity
import android.content.pm.ActivityInfo
import android.net.Uri
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.AccountCircle
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.VideoLibrary
import androidx.compose.material.icons.filled.LiveTv
import androidx.compose.material.icons.filled.MusicNote
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.Storage
import androidx.compose.material.icons.filled.SwapHoriz
import androidx.compose.material.icons.filled.TravelExplore
import androidx.compose.material.icons.filled.Tv
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material.icons.outlined.Download
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.VideoLibrary
import androidx.compose.material.icons.outlined.LiveTv
import androidx.compose.material.icons.outlined.TravelExplore
import androidx.compose.material3.AssistChip
import androidx.compose.material3.AssistChipDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ScrollableTabRow
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Tab
import androidx.compose.material3.Text
import androidx.compose.material3.OutlinedTextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.activity.compose.BackHandler
import androidx.core.graphics.ColorUtils
import androidx.core.graphics.drawable.toBitmap
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.palette.graphics.Palette
import androidx.media3.common.MediaItem
import androidx.media3.common.C
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.DefaultLoadControl
import androidx.media3.exoplayer.SeekParameters
import androidx.media3.datasource.DataSource
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import androidx.media3.ui.PlayerView
import androidx.media3.ui.AspectRatioFrameLayout
import coil.compose.AsyncImage
import coil.compose.SubcomposeAsyncImage
import coil.compose.SubcomposeAsyncImageContent
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.models.EpisodeItem
import com.lanflix.models.SeasonSummary
import com.lanflix.webview.ServerBrowserActivity
import com.lanflix.webview.ServerManager
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import java.io.File
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext

private enum class Destination(val label: String, val selected: ImageVector, val unselected: ImageVector) {
    Home("Home", Icons.Filled.Home, Icons.Outlined.Home),
    Libraries("Libraries", Icons.Filled.VideoLibrary, Icons.Outlined.VideoLibrary),
    Live("Live TV", Icons.Filled.LiveTv, Icons.Outlined.LiveTv),
    Demand("On Demand", Icons.Filled.Download, Icons.Outlined.Download),
    Discover("Discover", Icons.Filled.TravelExplore, Icons.Outlined.TravelExplore)
}

private enum class AppOverlay { Search, Profile, Settings, Account, Activity, Notifications }

@Composable
fun LanflixApp(viewModel: LanflixViewModel = viewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val appContext = LocalContext.current
    val scope = rememberCoroutineScope()
    var destination by remember { mutableStateOf(Destination.Home) }
    var detail by remember { mutableStateOf<ContentItem?>(null) }
    var profileMenuVisible by remember { mutableStateOf(false) }
    val overlayStack = remember { mutableStateListOf<AppOverlay>() }
    var playerItem by remember { mutableStateOf<ContentItem?>(null) }
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

    BackHandler(enabled = playerItem != null || detail != null || profileMenuVisible || overlayStack.isNotEmpty()) {
        when {
            playerItem != null -> playerItem = null
            detail != null -> detail = null
            profileMenuVisible -> profileMenuVisible = false
            overlayStack.isNotEmpty() -> closeOverlay()
        }
    }

    LanflixTheme {
        Surface(modifier = Modifier.fillMaxSize(), color = LanflixBackground) {
            when {
                playerItem != null -> PlayerScreen(playerItem!!, onBack = { playerItem = null })
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
                currentOverlay == AppOverlay.Activity -> ActivityScreen(state.socialFeed, onBack = ::closeOverlay)
                currentOverlay == AppOverlay.Notifications -> NotificationsScreen(state.notifications, onBack = ::closeOverlay)
                currentOverlay == AppOverlay.Settings -> SettingsScreen(
                    state = state,
                    onBack = ::closeOverlay,
                    onRetry = viewModel::refresh,
                    onAccount = { openOverlay(AppOverlay.Account) },
                    onActivity = { openOverlay(AppOverlay.Activity) },
                    onNotifications = { openOverlay(AppOverlay.Notifications) }
                )
                currentOverlay == AppOverlay.Profile -> ProfileScreen(
                    library = state.library,
                    account = state.account,
                    activity = state.socialFeed,
                    onBack = ::closeOverlay,
                    onSelect = { detail = it },
                    onAccount = { openOverlay(AppOverlay.Account) },
                    onActivity = { openOverlay(AppOverlay.Activity) }
                )
                else -> Box(Modifier.fillMaxSize()) {
                    AnimatedContent(targetState = destination, label = "main-destination") { target ->
                        when (target) {
                            Destination.Home -> HomeScreen(state, onSelect = { detail = it }, onRetry = viewModel::refresh)
                            Destination.Libraries -> LibraryScreen(state.library, state.music, onSelect = { detail = it })
                            Destination.Live -> LiveTvScreen(state.online, state.liveTvChannels)
                            Destination.Demand -> DownloadsScreen(state.library, onSelect = { detail = it })
                            Destination.Discover -> DiscoverScreen(state, onSelect = { detail = it })
                        }
                    }
                    TopChrome(
                        title = if (destination == Destination.Home) "lanflix" else destination.label,
                        online = state.online,
                        onSearch = { openOverlay(AppOverlay.Search) },
                        onProfile = { profileMenuVisible = !profileMenuVisible },
                        onCast = { startCastFlow(null) }
                    )
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

                    Box(Modifier.align(Alignment.BottomCenter)) {
                        BottomChrome(destination, onSelect = { destination = it })
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
                                            val mediaUrl = "${ServerManager.activeServerUrl}/api/v2/playback/$kind/${mediaItem.id}/file?client=direct"
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

@Composable
private fun TopChrome(title: String, online: Boolean, onSearch: () -> Unit, onProfile: () -> Unit, onCast: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Brush.verticalGradient(listOf(Color.Black.copy(alpha = .76f), Color.Transparent)))
            .statusBarsPadding()
            .height(52.dp)
            .padding(horizontal = 12.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = title,
            color = if (title == "lanflix") LanflixGold else Color.White,
            fontWeight = FontWeight.ExtraBold,
            fontSize = if (title == "lanflix") 18.sp else 17.sp
        )
        Spacer(Modifier.weight(1f))
        if (!online) Icon(Icons.Filled.CloudOff, "Server offline", tint = LanflixMuted, modifier = Modifier.size(19.dp))
        IconButton(onClick = onSearch, modifier = Modifier.size(44.dp)) { Icon(Icons.Filled.Search, "Search", tint = Color.White, modifier = Modifier.size(19.dp)) }
        Row(modifier = Modifier.padding(end = 4.dp), verticalAlignment = Alignment.CenterVertically) {
            CompactHeaderAction(Icons.Filled.Cast, "Cast", onClick = onCast)
            CompactHeaderAction(Icons.Outlined.BookmarkBorder, "Watchlist")
            Box(
                modifier = Modifier.size(38.dp).clickable(onClick = onProfile),
                contentAlignment = Alignment.Center
            ) {
                Box(Modifier.size(27.dp).clip(CircleShape).background(LanflixGold), contentAlignment = Alignment.Center) {
                    Icon(Icons.Filled.Person, "Profile", tint = Color.Black, modifier = Modifier.size(17.dp))
                }
            }
        }
    }
}

@Composable
private fun CompactHeaderAction(icon: ImageVector, label: String, onClick: () -> Unit = {}) {
    Box(Modifier.size(38.dp).clickable(onClick = onClick), contentAlignment = Alignment.Center) {
        Icon(icon, label, tint = Color.White, modifier = Modifier.size(18.dp))
    }
}

@Composable
private fun SearchScreen(media: List<ContentItem>, onBack: () -> Unit, onSelect: (ContentItem) -> Unit) {
    var query by remember { mutableStateOf("") }
    val results = remember(media, query) {
        if (query.isBlank()) media else media.filter {
            it.displayTitle.contains(query.trim(), ignoreCase = true)
        }
    }
    Column(Modifier.fillMaxSize().statusBarsPadding().padding(top = 6.dp)) {
        Row(Modifier.fillMaxWidth().padding(horizontal = 8.dp), verticalAlignment = Alignment.CenterVertically) {
            IconButton(onClick = onBack) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
            OutlinedTextField(
                value = query,
                onValueChange = { query = it },
                modifier = Modifier.weight(1f),
                singleLine = true,
                placeholder = { Text("Search your library") },
                leadingIcon = { Icon(Icons.Filled.Search, null) },
                shape = RoundedCornerShape(24.dp)
            )
        }
        if (results.isEmpty()) EmptyState("No results", "Try a different title.") else {
            LazyVerticalGrid(
                columns = GridCells.Fixed(3),
                contentPadding = PaddingValues(12.dp, 18.dp, 12.dp, 30.dp),
                horizontalArrangement = Arrangement.spacedBy(9.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) { items(results, key = { "search-${it.type}-${it.id}" }) { PosterCard(it, onSelect, Modifier.fillMaxWidth()) } }
        }
    }
}

@Composable
private fun BottomChrome(selected: Destination, onSelect: (Destination) -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color(0xF207080B))
            .navigationBarsPadding()
            .height(58.dp),
        horizontalArrangement = Arrangement.SpaceAround,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Destination.entries.forEach { destination ->
            Column(
                modifier = Modifier.weight(1f).fillMaxHeight().clickable { onSelect(destination) },
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Icon(
                    imageVector = if (selected == destination) destination.selected else destination.unselected,
                    contentDescription = destination.label,
                    tint = if (selected == destination) LanflixGold else Color.White.copy(alpha = .62f),
                    modifier = Modifier.size(21.dp)
                )
                Text(destination.label, fontSize = 8.sp, color = if (selected == destination) LanflixGold else LanflixMuted)
            }
        }
    }
}

@Composable
private fun HomeScreen(state: LanflixUiState, onSelect: (ContentItem) -> Unit, onRetry: () -> Unit) {
    val hero = state.library.firstOrNull()
    var targetPalette by remember(hero?.id) { mutableStateOf(DefaultArtworkPalette) }
    LaunchedEffect(hero?.id) { targetPalette = DefaultArtworkPalette }
    val artworkPalette = targetPalette
    Box(
        Modifier.fillMaxSize().background(Color(0xFF090A0E))
    ) {
    if (hero != null) {
        AsyncImage(
            model = hero.resolvedBackdropUrl ?: hero.resolvedPosterUrl,
            contentDescription = null,
            modifier = Modifier.fillMaxSize().blur(45.dp).alpha(.75f),
            contentScale = ContentScale.Crop
        )
        Box(
            Modifier.fillMaxSize().background(
                Brush.radialGradient(
                    colors = listOf(artworkPalette.glow.copy(alpha = .55f), Color.Transparent),
                    center = Offset(900f, 650f),
                    radius = 1700f
                )
            )
        )
        Box(
            Modifier.fillMaxSize().background(
                Brush.radialGradient(
                    colors = listOf(artworkPalette.accent.copy(alpha = .42f), Color.Transparent),
                    center = Offset(100f, 1500f),
                    radius = 1500f
                )
            )
        )
        Box(
            Modifier.fillMaxSize().background(
                Brush.verticalGradient(
                    0f to Color.Black.copy(alpha = .35f),
                    .25f to Color.Transparent,
                    .65f to artworkPalette.glow.copy(alpha = .20f),
                    1f to Color.Black.copy(alpha = .55f)
                )
            )
        )
    }
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(bottom = 92.dp)
    ) {
        item { Hero(item = hero, loading = state.loading, onSelect = onSelect, onRetry = onRetry, palette = artworkPalette, onArtworkPalette = { targetPalette = it }) }
        if (!state.online) item { OfflineNotice() }
        if (state.library.isNotEmpty()) {
            item { MediaShelf("Continue Watching", state.library.take(8), onSelect) }
            item { MediaShelf("Recently Added", state.library.drop(1).take(10), onSelect) }
            item { MediaShelf("Because it’s movie night", state.library.shuffled().take(8), onSelect) }
        }
        item { MusicPreview() }
    }
    }
}

@Composable
private fun Hero(item: ContentItem?, loading: Boolean, onSelect: (ContentItem) -> Unit, onRetry: () -> Unit, palette: ArtworkPalette, onArtworkPalette: (ArtworkPalette) -> Unit) {
    val scope = rememberCoroutineScope()
    Box(Modifier.fillMaxWidth().height(500.dp)) {
        if (item != null) {
            AsyncImage(
                model = item.resolvedBackdropUrl ?: item.resolvedPosterUrl,
                contentDescription = item.displayTitle,
                modifier = Modifier.fillMaxSize()
                    .graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }
                    .drawWithContent {
                        drawContent()
                        drawRect(
                            brush = Brush.verticalGradient(
                                0f to Color.White,
                                .78f to Color.White,
                                1f to Color.Transparent
                            ),
                            blendMode = BlendMode.DstIn
                        )
                    },
                contentScale = ContentScale.Crop,
                onSuccess = { state -> scope.launch { onArtworkPalette(extractArtworkPalette(state.result.drawable)) } }
            )
        } else {
            Box(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF14304B), LanflixBackground))))
        }
        Box(
            Modifier.fillMaxSize().background(
                Brush.verticalGradient(
                    0f to Color.Black.copy(alpha = .28f),
                    .36f to Color.Black.copy(alpha = .06f),
                    .62f to Color.Transparent,
                    1f to Color.Transparent
                )
            )
        )
        Column(
            modifier = Modifier.align(Alignment.BottomCenter).fillMaxWidth().padding(horizontal = 22.dp, vertical = 20.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            when {
                loading -> CircularProgressIndicator(color = LanflixGold)
                item == null -> {
                    Text("Your cinema, beautifully organized", color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
                    Text("Connect to your Lanflix server to fill this screen.", color = LanflixMuted, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))
                    Button(onClick = onRetry, colors = ButtonDefaults.buttonColors(containerColor = Color.White), modifier = Modifier.padding(top = 16.dp)) {
                        Text("Try again", color = Color.Black)
                    }
                }
                else -> {
                    TitleArtwork(item, Modifier.fillMaxWidth(.76f).widthIn(max = 258.dp).height(82.dp))
                    Text(
                        listOfNotNull(item.displayYear, item.rating, item.type?.replaceFirstChar { it.uppercase() }).joinToString("  •  "),
                        color = Color.White.copy(alpha = .74f), fontSize = 12.sp, modifier = Modifier.padding(top = 8.dp)
                    )
                    Text(item.overview.orEmpty(), color = Color.White.copy(alpha = .86f), maxLines = 2, overflow = TextOverflow.Ellipsis, textAlign = TextAlign.Center, fontSize = 12.sp, lineHeight = 17.sp, modifier = Modifier.widthIn(max = 320.dp).padding(top = 10.dp))
                    Button(
                        onClick = { onSelect(item) },
                        modifier = Modifier.fillMaxWidth().padding(top = 14.dp).height(48.dp),
                        shape = RoundedCornerShape(24.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = Color.White)
                    ) {
                        Icon(Icons.Filled.PlayArrow, null, tint = Color.Black)
                        Text(if (item.isOfflinePlayable) "Play offline" else "Resume", color = Color.Black, fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

@Composable
private fun OfflineNotice() {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp).clip(RoundedCornerShape(14.dp)).background(Color.White.copy(alpha = .07f)).padding(14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(Icons.Filled.CloudOff, null, tint = LanflixGold)
        Column(Modifier.padding(start = 12.dp)) {
            Text("Offline mode", color = Color.White, fontWeight = FontWeight.SemiBold)
            Text("Only completed device downloads can play.", color = LanflixMuted, fontSize = 12.sp)
        }
    }
}

@Composable
private fun MediaShelf(title: String, media: List<ContentItem>, onSelect: (ContentItem) -> Unit) {
    Column(Modifier.padding(top = 18.dp)) {
        Text(title, color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
        LazyRow(contentPadding = PaddingValues(horizontal = 12.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            items(media, key = { "${it.type}-${it.id}" }) { item -> PosterCard(item, onSelect) }
        }
    }
}

@Composable
private fun PosterCard(item: ContentItem, onSelect: (ContentItem) -> Unit, modifier: Modifier = Modifier.width(126.dp)) {
    Column(modifier.clickable { onSelect(item) }) {
        Box(Modifier.fillMaxWidth().aspectRatio(.68f).clip(RoundedCornerShape(10.dp)).background(LanflixSurfaceRaised)) {
            AsyncImage(model = item.resolvedPosterUrl, contentDescription = item.displayTitle, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
            if (item.isOfflinePlayable) {
                Icon(Icons.Filled.Download, "Downloaded", tint = Color.White, modifier = Modifier.align(Alignment.TopEnd).padding(7.dp).size(18.dp))
            }
        }
        Text(item.displayTitle, color = Color.White, fontSize = 11.sp, fontWeight = FontWeight.Medium, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 6.dp))
        Text(item.displayYear.orEmpty(), color = LanflixMuted, fontSize = 9.sp)
    }
}

@Composable
private fun LibraryScreen(media: List<ContentItem>, music: com.lanflix.api.MusicHome?, onSelect: (ContentItem) -> Unit) {
    var selectedFilter by remember { mutableStateOf("Movies") }
    Column(Modifier.fillMaxSize().padding(top = 68.dp, bottom = 58.dp)) {
        LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            items(listOf("Movies", "Series", "Music", "Collections")) { filter ->
                FilterChip(selected = selectedFilter == filter, onClick = { selectedFilter = filter }, label = { Text(filter) })
            }
        }
        val filtered = when (selectedFilter) {
            "Movies" -> media.filter { it.type.equals("movie", true) }
            "Series" -> media.filter { it.type.equals("series", true) }
            else -> emptyList()
        }
        if (selectedFilter == "Music") MusicLibrary(music) else if (filtered.isEmpty()) EmptyState("No $selectedFilter yet", "When this library is scanned, it will appear here.") else {
            LazyVerticalGrid(
                columns = GridCells.Fixed(3),
                contentPadding = PaddingValues(10.dp),
                horizontalArrangement = Arrangement.spacedBy(9.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) { items(filtered, key = { "${it.type}-${it.id}" }) { PosterCard(it, onSelect, Modifier.fillMaxWidth()) } }
        }
    }
}

@Composable
private fun LiveTvScreen(online: Boolean, channels: List<com.lanflix.api.LiveTvChannel>) {
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(top = 82.dp, bottom = 90.dp, start = 14.dp, end = 14.dp)) {
        item {
            Text(if (online) "Guide  •  What’s on now" else "Guide unavailable offline", color = LanflixMuted, fontSize = 12.sp)
            Box(Modifier.fillMaxWidth().height(190.dp).padding(top = 16.dp).clip(RoundedCornerShape(16.dp)).background(Brush.linearGradient(listOf(Color(0xFF17485A), Color(0xFF0A1D29))))) {
                Column(Modifier.align(Alignment.BottomStart).padding(18.dp)) {
                    Text("Your channels, one beautiful guide", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold)
                    Text("Add an M3U/XMLTV source or HDHomeRun tuner in server settings.", color = Color.White.copy(alpha = .72f), fontSize = 12.sp)
                }
            }
        }
        item { Text("${channels.size} channels", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 24.dp, bottom = 10.dp)) }
        if (online && channels.isEmpty()) item { EmptyState("No Live TV sources", "Add M3U/XMLTV or HDHomeRun in server administration.") }
        items(channels, key = { it.id }) { channel ->
            Row(Modifier.fillMaxWidth().padding(vertical = 4.dp).clip(RoundedCornerShape(10.dp)).background(Color.White.copy(alpha = .06f)).padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.size(42.dp).clip(RoundedCornerShape(8.dp)).background(Color.White.copy(alpha = .09f)), contentAlignment = Alignment.Center) { Icon(Icons.Filled.Tv, null, tint = LanflixGold) }
                Column(Modifier.padding(start = 12.dp).weight(1f)) { Text("${channel.number}  ${channel.name}", color = Color.White); Text(channel.now?.title ?: "No guide data", color = LanflixMuted, fontSize = 11.sp) }
                Icon(Icons.Filled.PlayArrow, null, tint = Color.White)
            }
        }
    }
}

@Composable
private fun DownloadsScreen(media: List<ContentItem>, onSelect: (ContentItem) -> Unit) {
    val downloads = media.filter { it.isOfflinePlayable && it.localFilePath?.let(::File)?.isFile == true }
    Column(Modifier.fillMaxSize().padding(top = 82.dp, bottom = 68.dp, start = 14.dp, end = 14.dp)) {
        Text("On Demand", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
        Text("Device downloads and server requests", color = LanflixMuted, fontSize = 12.sp)
        Row(Modifier.fillMaxWidth().padding(top = 18.dp).clip(RoundedCornerShape(14.dp)).background(Color.White.copy(alpha = .06f)).padding(16.dp)) {
            Icon(Icons.Filled.Download, null, tint = LanflixGold)
            Column(Modifier.padding(start = 12.dp)) { Text("${downloads.size} downloaded", color = Color.White, fontWeight = FontWeight.Bold); Text("Available without the server", color = LanflixMuted, fontSize = 11.sp) }
        }
        if (downloads.isEmpty()) EmptyState("Nothing downloaded", "Download a movie or episode to watch when your server is offline.")
        else LazyRow(contentPadding = PaddingValues(top = 22.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) { items(downloads) { PosterCard(it, onSelect) } }
    }
}

@Composable
private fun DiscoverScreen(state: LanflixUiState, onSelect: (ContentItem) -> Unit) {
    if (!state.online) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { EmptyState("Discover is offline", "Reconnect to your server to search and request new media.") }
        return
    }
    val page = state.discovery
    LazyColumn(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF211238), LanflixBackground))), contentPadding = PaddingValues(top = 82.dp, bottom = 90.dp)) {
        item { Text("Discover", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp)); Text("Trending and popular on IMDb", color = LanflixMuted, modifier = Modifier.padding(horizontal = 16.dp)) }
        if (page == null) item { EmptyState("Discovery unavailable", "Configure TMDb in server settings to browse external titles.") }
        page?.let {
            item { DiscoveryShelf("Trending movies", it.trendingMovies, onSelect) }
            item { DiscoveryShelf("Trending series", it.trendingSeries, onSelect) }
            item { DiscoveryShelf("Popular movies", it.popularMovies, onSelect) }
            item { DiscoveryShelf("Popular series", it.popularSeries, onSelect) }
        }
    }
}

@Composable
private fun DiscoveryShelf(title: String, media: List<com.lanflix.api.DiscoveryItem>, onSelect: (ContentItem) -> Unit) {
    val context = LocalContext.current
    val api = remember(context) { LanflixApiClient.getInstance(context) }
    val scope = rememberCoroutineScope()
    var requestedId by remember { mutableStateOf<Int?>(null) }
    Column(Modifier.padding(top = 20.dp)) {
        Text(title, color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
        LazyRow(contentPadding = PaddingValues(horizontal = 12.dp), horizontalArrangement = Arrangement.spacedBy(11.dp)) {
            items(media, key = { "${it.type}-${it.tmdbId}" }) { item ->
                Column(Modifier.width(132.dp).clickable { onSelect(item.asContentItem()) }) {
                    AsyncImage(item.posterUrl, item.title, Modifier.fillMaxWidth().aspectRatio(2f / 3f).clip(RoundedCornerShape(12.dp)).background(Color.White.copy(alpha = .06f)), contentScale = ContentScale.Crop)
                    Text(item.title, color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 11.sp, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 7.dp))
                    Row(Modifier.fillMaxWidth().padding(top = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                        Box(Modifier.clip(RoundedCornerShape(3.dp)).background(Color(0xFFF5C518)).padding(horizontal = 4.dp, vertical = 2.dp)) { Text("IMDb", color = Color.Black, fontSize = 8.sp, fontWeight = FontWeight.Black) }
                        Text(" %.1f".format(item.rating), color = Color.White.copy(alpha = .8f), fontSize = 10.sp)
                        Spacer(Modifier.weight(1f))
                        IconButton(onClick = { scope.launch { if (api.acquire(item)) requestedId = item.tmdbId } }, modifier = Modifier.size(28.dp)) {
                            Icon(if (requestedId == item.tmdbId) Icons.Filled.Download else Icons.Outlined.Download, if (requestedId == item.tmdbId) "Requested" else "Request", tint = if (requestedId == item.tmdbId) LanflixGold else Color.White, modifier = Modifier.size(17.dp))
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DetailScreen(
    item: ContentItem,
    online: Boolean,
    downloading: Boolean,
    onBack: () -> Unit,
    onPlay: () -> Unit,
    onPlayEpisode: (EpisodeItem) -> Unit,
    onDownload: () -> Unit,
    onCast: (ContentItem) -> Unit = {}
) {
    val isPlayableType = item.type.equals("movie", true) || item.type.equals("episode", true)
    val isDiscovery = item.id < 0 && item.tmdbId > 0
    val canPlay = isPlayableType && (item.isOfflinePlayable || (online && item.serverAvailable))
    val context = LocalContext.current
    val discoveryApi = remember { LanflixApiClient(context) }
    var acquisitionRequested by remember(item.id) { mutableStateOf(false) }
    var targetPalette by remember(item.id) { mutableStateOf(DefaultArtworkPalette) }
    LaunchedEffect(item.id) { targetPalette = DefaultArtworkPalette }
    val artworkPalette = targetPalette
    val scope = rememberCoroutineScope()
    Box(
        Modifier.fillMaxSize().background(Color(0xFF090A0E))
    ) {
    AsyncImage(
        model = item.resolvedBackdropUrl ?: item.resolvedPosterUrl,
        contentDescription = null,
        modifier = Modifier.fillMaxSize().blur(45.dp).alpha(.75f),
        contentScale = ContentScale.Crop
    )
    Box(
        Modifier.fillMaxSize().background(
            Brush.radialGradient(
                colors = listOf(artworkPalette.glow.copy(alpha = .55f), Color.Transparent),
                center = Offset(850f, 700f),
                radius = 1700f
            )
        )
    )
    Box(
        Modifier.fillMaxSize().background(
            Brush.radialGradient(
                colors = listOf(artworkPalette.accent.copy(alpha = .42f), Color.Transparent),
                center = Offset(120f, 1600f),
                radius = 1500f
            )
        )
    )
    Box(
        Modifier.fillMaxSize().background(
            Brush.verticalGradient(
                0f to Color.Black.copy(alpha = .35f),
                .25f to Color.Transparent,
                .65f to artworkPalette.glow.copy(alpha = .20f),
                1f to Color.Black.copy(alpha = .55f)
            )
        )
    )
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 36.dp)) {
        item {
            Box(Modifier.fillMaxWidth().height(500.dp)) {
                AsyncImage(
                    model = item.resolvedBackdropUrl ?: item.resolvedPosterUrl,
                    contentDescription = item.displayTitle,
                    modifier = Modifier.fillMaxSize()
                        .graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }
                        .drawWithContent {
                            drawContent()
                            drawRect(
                                brush = Brush.verticalGradient(
                                    0f to Color.White,
                                    .84f to Color.White,
                                    1f to Color.Transparent
                                ),
                                blendMode = BlendMode.DstIn
                            )
                        },
                    contentScale = ContentScale.Crop,
                    onSuccess = { state -> scope.launch { targetPalette = extractArtworkPalette(state.result.drawable) } }
                )
                Box(
                    Modifier.fillMaxSize().background(
                        Brush.verticalGradient(
                            0f to Color.Black.copy(alpha = .24f),
                            .28f to Color.Transparent,
                            1f to Color.Transparent
                        )
                    )
                )
                IconButton(onClick = onBack, modifier = Modifier.statusBarsPadding().padding(8.dp).clip(CircleShape).background(Color.Black.copy(alpha = .38f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
                IconButton(
                    onClick = { },
                    modifier = Modifier.align(Alignment.TopEnd).statusBarsPadding().padding(8.dp).clip(CircleShape).background(Color.Black.copy(alpha = .38f))
                ) { Icon(Icons.Filled.MoreVert, "More options", tint = Color.White) }
                Column(
                    Modifier.align(Alignment.BottomCenter).fillMaxWidth().padding(horizontal = 18.dp, vertical = 18.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    TitleArtwork(item, Modifier.fillMaxWidth(.72f).widthIn(max = 250.dp).height(80.dp))
                    Text(listOfNotNull(item.displayYear, item.rating, item.type).joinToString("  •  "), color = Color.White.copy(alpha = .72f), fontSize = 12.sp, modifier = Modifier.padding(top = 6.dp))
                }
            }
        }
        item {
            Column(Modifier.padding(horizontal = 16.dp)) {
                Button(
                    onClick = {
                        if (isDiscovery) scope.launch { acquisitionRequested = discoveryApi.acquire(com.lanflix.api.DiscoveryItem(item.tmdbId, item.type ?: "movie", item.displayTitle, item.overview, item.year, item.rating?.toDoubleOrNull() ?: 0.0, item.posterUrl, item.backdropUrl)) }
                        else if (canPlay) onPlay()
                    },
                    enabled = canPlay || (isDiscovery && online) || (!isPlayableType),
                    modifier = Modifier.fillMaxWidth().height(50.dp),
                    shape = RoundedCornerShape(25.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color.White,
                        contentColor = Color.Black,
                        disabledContainerColor = Color.White.copy(alpha = .18f),
                        disabledContentColor = Color.White.copy(alpha = .5f)
                    )
                ) {
                    Icon(
                        if (isDiscovery) Icons.Filled.Download else if (isPlayableType) Icons.Filled.PlayArrow else Icons.Filled.Tv,
                        null,
                        tint = Color.Black
                    )
                    Text(
                        if (isDiscovery) { if (acquisitionRequested) "Requested" else "Request" }
                        else if (!isPlayableType) "Choose an episode below"
                        else if (item.isOfflinePlayable) "Play offline"
                        else if (online) "Play"
                        else "Unavailable offline",
                        color = Color.Black,
                        fontWeight = FontWeight.Bold
                    )
                }
                Row(Modifier.fillMaxWidth().padding(vertical = 12.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                    DetailAction(Icons.Outlined.BookmarkBorder, "Watchlist")
                    DetailAction(
                        Icons.Outlined.Download,
                        if (downloading) "Downloading" else if (item.isOfflinePlayable) "Downloaded" else "Download",
                        enabled = isPlayableType && online && !item.isOfflinePlayable && !downloading,
                        onClick = onDownload
                    )
                    DetailAction(Icons.Filled.Star, "Rate")
                    DetailAction(Icons.Filled.Cast, "Cast", enabled = online, onClick = { onCast(item) })
                }
                if (item.type.equals("series", true)) {
                    SeriesEpisodeBrowser(item = item, online = online, onPlayEpisode = onPlayEpisode)
                }
                Text(item.overview ?: "No overview available.", color = Color.White.copy(alpha = .88f), fontSize = 14.sp, lineHeight = 20.sp)
                Text(if (isDiscovery) "Available to request through your Lanflix server" else "Available from your Lanflix server", color = LanflixMuted, fontSize = 11.sp, modifier = Modifier.padding(top = 8.dp))
                Text("Activity", color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 24.dp))
                Row(Modifier.fillMaxWidth().padding(top = 10.dp).clip(RoundedCornerShape(14.dp)).background(Color.White.copy(alpha = .06f)).padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                    Box(Modifier.size(34.dp).clip(CircleShape).background(LanflixGold), contentAlignment = Alignment.Center) { Icon(Icons.Filled.Person, null, tint = Color.Black) }
                    Text("Be the first to rate or review this title.", color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(start = 10.dp))
                }
                Text("Cast & Crew", color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 24.dp))
                Text("Cast metadata will appear here when available from the server.", color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(top = 8.dp))
            }
        }
    }
    }
}

@Composable
private fun TitleArtwork(item: ContentItem, modifier: Modifier) {
    SubcomposeAsyncImage(
        model = item.resolvedLogoUrl,
        contentDescription = item.displayTitle,
        modifier = modifier,
        contentScale = ContentScale.Fit,
        loading = { Spacer(Modifier.fillMaxSize()) },
        error = { Spacer(Modifier.fillMaxSize()) },
        success = { SubcomposeAsyncImageContent() }
    )
}

@Composable
private fun SeriesEpisodeBrowser(item: ContentItem, online: Boolean, onPlayEpisode: (EpisodeItem) -> Unit) {
    val context = LocalContext.current
    val api = remember(item.id) { LanflixApiClient(context) }
    var seasons by remember(item.id) { mutableStateOf<List<SeasonSummary>>(emptyList()) }
    var seasonPayloads by remember(item.id) { mutableStateOf<List<com.lanflix.api.V2Season>>(emptyList()) }
    var selectedSeason by remember(item.id) { mutableStateOf<Int?>(null) }
    var episodes by remember(item.id) { mutableStateOf<List<EpisodeItem>>(emptyList()) }
    var loading by remember(item.id) { mutableStateOf(false) }

    LaunchedEffect(item.id, online) {
        if (!online) return@LaunchedEffect
        loading = true
        seasonPayloads = api.getContentDetail(item.id)?.seasons.orEmpty()
        seasons = seasonPayloads.map { season ->
            SeasonSummary(
                seasonNumber = season.seasonNumber,
                episodeCount = season.episodes.size,
                availableEpisodes = season.episodes.count { it.hasFile }
            )
        }
        selectedSeason = seasons.firstOrNull()?.seasonNumber
        loading = false
    }
    LaunchedEffect(item.id, selectedSeason, seasonPayloads) {
        val season = selectedSeason ?: return@LaunchedEffect
        episodes = seasonPayloads.firstOrNull { it.seasonNumber == season }?.episodes.orEmpty()
    }

    Column(Modifier.fillMaxWidth().padding(bottom = 20.dp)) {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            Text("Episodes", color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
            if (loading) CircularProgressIndicator(color = LanflixGold, strokeWidth = 2.dp, modifier = Modifier.size(20.dp))
        }
        if (!online) {
            Text("Reconnect to load seasons. Completed episode downloads remain available in On Demand.", color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(top = 8.dp))
            return@Column
        }
        LazyRow(
            contentPadding = PaddingValues(vertical = 10.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(seasons, key = { it.seasonNumber }) { season ->
                FilterChip(
                    selected = selectedSeason == season.seasonNumber,
                    onClick = { selectedSeason = season.seasonNumber },
                    label = { Text("Season ${season.seasonNumber}") }
                )
            }
        }
        if (!loading && seasons.isEmpty()) {
            Text("No seasons were found on this server.", color = LanflixMuted, fontSize = 12.sp)
        }
        episodes.forEach { episode ->
            EpisodeRow(episode = episode, onClick = { if (episode.hasFile) onPlayEpisode(episode) })
        }
    }
}

@Composable
private fun EpisodeRow(episode: EpisodeItem, onClick: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().padding(vertical = 5.dp).clip(RoundedCornerShape(13.dp))
            .background(Color.White.copy(alpha = .065f)).clickable(enabled = episode.hasFile, onClick = onClick).padding(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(Modifier.width(116.dp).height(68.dp).clip(RoundedCornerShape(9.dp)).background(Color.Black.copy(alpha = .22f))) {
            AsyncImage(
                model = episode.resolvedStillUrl,
                contentDescription = episode.title,
                modifier = Modifier.fillMaxSize(),
                contentScale = ContentScale.Crop
            )
            if (episode.hasFile) {
                Box(Modifier.align(Alignment.Center).size(32.dp).clip(CircleShape).background(Color.Black.copy(alpha = .58f)), contentAlignment = Alignment.Center) {
                    Icon(Icons.Filled.PlayArrow, "Play episode", tint = Color.White, modifier = Modifier.size(22.dp))
                }
            }
        }
        Column(Modifier.padding(start = 11.dp).weight(1f)) {
            Text("${episode.episodeNumber}. ${episode.title ?: "Episode ${episode.episodeNumber}"}", color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 13.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
            Text(episode.overview.orEmpty(), color = LanflixMuted, fontSize = 10.sp, maxLines = 2, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 3.dp))
            if (!episode.hasFile) Text("Unavailable", color = Color(0xFFE6A35B), fontSize = 9.sp, modifier = Modifier.padding(top = 2.dp))
        }
    }
}

private data class ArtworkPalette(val base: Color, val depth: Color, val glow: Color, val accent: Color)

private val DefaultArtworkPalette = ArtworkPalette(
    base = Color(0xFF0F1720),
    depth = Color(0xFF070B10),
    glow = Color(0xFF1B5375),
    accent = LanflixGold
)

@Composable
private fun animatedArtworkPalette(target: ArtworkPalette): ArtworkPalette = target

private fun com.lanflix.models.ServerArtworkPalette.toComposePalette() = ArtworkPalette(
    base = runCatching { Color(android.graphics.Color.parseColor(base)) }.getOrDefault(DefaultArtworkPalette.base),
    depth = runCatching { Color(android.graphics.Color.parseColor(depth)) }.getOrDefault(DefaultArtworkPalette.depth),
    glow = runCatching { Color(android.graphics.Color.parseColor(glow)) }.getOrDefault(DefaultArtworkPalette.glow),
    accent = runCatching { Color(android.graphics.Color.parseColor(accent)) }.getOrDefault(DefaultArtworkPalette.accent)
)

private suspend fun extractArtworkPalette(drawable: android.graphics.drawable.Drawable): ArtworkPalette = withContext(Dispatchers.Default) {
    runCatching {
        val sourceBitmap = drawable.toBitmap(width = 192, height = 192)
        val readableBitmap = if (sourceBitmap.config == android.graphics.Bitmap.Config.HARDWARE) {
            sourceBitmap.copy(android.graphics.Bitmap.Config.ARGB_8888, false)
        } else sourceBitmap
        val palette = Palette.from(readableBitmap).maximumColorCount(24).generate()

        val swatches = listOfNotNull(
            palette.vibrantSwatch,
            palette.lightVibrantSwatch,
            palette.darkVibrantSwatch,
            palette.dominantSwatch,
            palette.mutedSwatch
        )
        val signatureSwatch = swatches.maxByOrNull { swatch ->
            val hsv = FloatArray(3)
            android.graphics.Color.colorToHSV(swatch.rgb, hsv)
            val sat = hsv[1]
            val lightness = hsv[2]
            val vividness = if (sat > 0.30f && lightness in 0.18f..0.88f) 2.5f else 0.4f
            swatch.population * sat * vividness
        }

        val signatureRgb = signatureSwatch?.rgb ?: 0xFF143D5A.toInt()
        val accentRgb = swatches.firstOrNull { it.rgb != signatureRgb }?.rgb ?: signatureRgb

        ArtworkPalette(
            base = artworkTone(signatureRgb, .22f, .28f, minSat = .65f, maxSat = .85f),
            depth = artworkTone(signatureRgb, .12f, .16f, minSat = .55f, maxSat = .75f),
            glow = artworkTone(signatureRgb, .42f, .65f, minSat = .78f, maxSat = 1.0f),
            accent = artworkTone(accentRgb, .52f, .78f, minSat = .75f, maxSat = 1.0f)
        )
    }.getOrDefault(DefaultArtworkPalette)
}

private fun artworkTone(rgb: Int, minValue: Float, maxValue: Float, minSat: Float = .30f, maxSat: Float = .96f): Color {
    val hsv = FloatArray(3)
    android.graphics.Color.colorToHSV(rgb, hsv)
    hsv[1] = hsv[1].coerceIn(minSat, maxSat)
    hsv[2] = hsv[2].coerceIn(minValue, maxValue)
    return Color(android.graphics.Color.HSVToColor(hsv))
}

private fun darkenArtworkColor(color: Color, blackAmount: Float): Color = Color(
    ColorUtils.blendARGB(color.toArgb(), android.graphics.Color.BLACK, blackAmount)
)

@Composable
private fun DetailAction(icon: ImageVector, label: String, enabled: Boolean = false, onClick: () -> Unit = {}) {
    Column(
        modifier = Modifier.clip(RoundedCornerShape(10.dp)).clickable(enabled = enabled, onClick = onClick).padding(5.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) { Icon(icon, label, tint = Color.White, modifier = Modifier.size(22.dp)); Text(label, color = LanflixMuted, fontSize = 9.sp, modifier = Modifier.padding(top = 4.dp)) }
}

@Composable
private fun ProfileMenu(
    online: Boolean,
    onProfile: () -> Unit,
    onDownloads: () -> Unit,
    onSettings: () -> Unit
) {
    val context = LocalContext.current
    Surface(
        modifier = Modifier.width(52.dp),
        shape = RoundedCornerShape(26.dp),
        color = Color(0xF7132634),
        shadowElevation = 18.dp,
        tonalElevation = 6.dp
    ) {
        Column(Modifier.padding(vertical = 4.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            ProfilePillAction(Icons.Filled.Person, "Open profile", selected = true, onClick = onProfile)
            ProfilePillAction(Icons.Filled.Download, "Downloads", onClick = onDownloads)
            Box {
                ProfilePillAction(Icons.Filled.Storage, "Connect to server") {
                context.startActivity(Intent(context, ServerBrowserActivity::class.java))
                }
                Box(
                    Modifier.align(Alignment.TopEnd).padding(top = 7.dp, end = 7.dp).size(7.dp).clip(CircleShape)
                        .background(if (online) Color(0xFF58C878) else Color(0xFFE59A44))
                )
            }
            Box(Modifier.width(28.dp).height(1.dp).background(Color.White.copy(alpha = .12f)))
            ProfilePillAction(Icons.Filled.Settings, "Settings", onClick = onSettings)
        }
    }
}

@Composable
private fun ProfilePillAction(icon: ImageVector, description: String, selected: Boolean = false, onClick: () -> Unit) {
    Box(Modifier.size(48.dp).clip(CircleShape).clickable(onClick = onClick), contentAlignment = Alignment.Center) {
        if (selected) Box(Modifier.size(34.dp).clip(CircleShape).background(LanflixGold.copy(alpha = .16f)))
        Icon(icon, description, tint = if (selected) LanflixGold else Color.White.copy(alpha = .88f), modifier = Modifier.size(21.dp))
    }
}

@Composable
private fun ProfileMenuRow(icon: ImageVector, title: String, subtitle: String, onClick: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().clip(RoundedCornerShape(12.dp)).clickable(onClick = onClick).padding(horizontal = 11.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, null, tint = Color.White.copy(alpha = .86f), modifier = Modifier.size(21.dp))
        Column(Modifier.padding(start = 12.dp).weight(1f)) {
            Text(title, color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
            Text(subtitle, color = LanflixMuted, fontSize = 9.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
        }
    }
}

@Composable
private fun SettingsScreen(
    state: LanflixUiState,
    onBack: () -> Unit,
    onRetry: () -> Unit,
    onAccount: () -> Unit,
    onActivity: () -> Unit,
    onNotifications: () -> Unit
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
        Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF14374A), LanflixBackground), endY = 1000f)),
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
                SettingsHeading("Account and security")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.AccountCircle, state.account?.displayName ?: "Lanflix account", "${state.account?.role ?: "Offline"} account and security", onAccount)
                    ProfileMenuRow(Icons.Filled.Storage, "Device sessions", "Review, revoke and sign out connected devices", onAccount)
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
                SettingsHeading("Notifications and privacy")
                SettingsCard {
                    ProfileMenuRow(Icons.Filled.Notifications, "Notifications", "${state.notifications.count { !it.isRead }} unread server notifications", onNotifications)
                    ProfileMenuRow(Icons.Filled.Person, "Activity and social", "Friends, reviews, reactions and privacy", onActivity)
                    SettingsToggleRow(Icons.Filled.Notifications, "Notifications", "Downloads, activity, invites and requests", preferences.notificationsEnabled) {
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

@Composable
private fun SettingsToggleRow(icon: ImageVector, title: String, subtitle: String, checked: Boolean, onCheckedChange: (Boolean) -> Unit) {
    Row(Modifier.fillMaxWidth().padding(horizontal = 11.dp, vertical = 9.dp), verticalAlignment = Alignment.CenterVertically) {
        Icon(icon, null, tint = Color.White.copy(alpha = .86f), modifier = Modifier.size(21.dp))
        Column(Modifier.padding(start = 12.dp).weight(1f)) {
            Text(title, color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
            Text(subtitle, color = LanflixMuted, fontSize = 9.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
        }
        Switch(checked = checked, onCheckedChange = onCheckedChange)
    }
}

@Composable
private fun SettingsHeading(title: String) {
    Text(title, color = LanflixGold, fontSize = 12.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 20.dp, bottom = 8.dp, start = 4.dp))
}

@Composable
private fun SettingsCard(content: @Composable () -> Unit) {
    Surface(shape = RoundedCornerShape(16.dp), color = Color.White.copy(alpha = .065f)) { Column(content = { content() }) }
}

@Composable
private fun ProfileScreen(
    library: List<ContentItem>,
    account: com.lanflix.auth.LanflixAccount?,
    activity: List<com.lanflix.api.SocialActivity>,
    onBack: () -> Unit,
    onSelect: (ContentItem) -> Unit,
    onAccount: () -> Unit,
    onActivity: () -> Unit
) {
    val context = LocalContext.current
    val api = remember(context) { LanflixApiClient.getInstance(context) }
    val scope = rememberCoroutineScope()
    var avatarVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var backdropVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var watchHistory by remember { mutableStateOf<List<com.lanflix.api.WatchHistoryItem>>(emptyList()) }

    LaunchedEffect(account?.id) {
        if (account != null) {
            watchHistory = api.getWatchHistory()
        }
    }

    val avatarLauncher = androidx.activity.compose.rememberLauncherForActivityResult(
        contract = androidx.activity.result.contract.ActivityResultContracts.GetContent()
    ) { uri ->
        uri?.let {
            scope.launch(Dispatchers.IO) {
                val bytes = context.contentResolver.openInputStream(it)?.use { stream -> stream.readBytes() }
                if (bytes != null && api.uploadAvatar(bytes)) {
                    avatarVersion = System.currentTimeMillis()
                }
            }
        }
    }

    val backdropLauncher = androidx.activity.compose.rememberLauncherForActivityResult(
        contract = androidx.activity.result.contract.ActivityResultContracts.GetContent()
    ) { uri ->
        uri?.let {
            scope.launch(Dispatchers.IO) {
                val bytes = context.contentResolver.openInputStream(it)?.use { stream -> stream.readBytes() }
                if (bytes != null && api.uploadBackdrop(bytes)) {
                    backdropVersion = System.currentTimeMillis()
                }
            }
        }
    }

    val defaultBackdrop = library.firstOrNull { !it.resolvedBackdropUrl.isNullOrBlank() }
    val customBackdropUrl = account?.id?.let { "${ServerManager.activeServerUrl}/api/v2/accounts/$it/backdrop?t=$backdropVersion" }
    val customAvatarUrl = account?.id?.let { "${ServerManager.activeServerUrl}/api/v2/accounts/$it/avatar?t=$avatarVersion" }

    val activeBackdropUrl = customBackdropUrl ?: defaultBackdrop?.resolvedBackdropUrl

    Box(Modifier.fillMaxSize().background(Color(0xFF090A0E))) {
        if (!activeBackdropUrl.isNullOrBlank()) {
            AsyncImage(
                model = activeBackdropUrl,
                contentDescription = null,
                modifier = Modifier.fillMaxSize().blur(45.dp).alpha(.75f),
                contentScale = ContentScale.Crop
            )
        }
        Box(
            Modifier.fillMaxSize().background(
                Brush.verticalGradient(
                    0f to Color.Black.copy(alpha = .35f),
                    .25f to Color.Transparent,
                    .65f to Color.Black.copy(alpha = .20f),
                    1f to Color.Black.copy(alpha = .55f)
                )
            )
        )

        LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 40.dp)) {
            item {
                Box(Modifier.fillMaxWidth().height(420.dp)) {
                    AsyncImage(
                        model = activeBackdropUrl,
                        contentDescription = null,
                        modifier = Modifier.fillMaxSize()
                            .clickable { backdropLauncher.launch("image/*") }
                            .graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }
                            .drawWithContent {
                                drawContent()
                                drawRect(
                                    brush = Brush.verticalGradient(
                                        0f to Color.White,
                                        .84f to Color.White,
                                        1f to Color.Transparent
                                    ),
                                    blendMode = BlendMode.DstIn
                                )
                            },
                        contentScale = ContentScale.Crop
                    )
                    Box(
                        Modifier.fillMaxSize().background(
                            Brush.verticalGradient(
                                0f to Color.Black.copy(alpha = .24f),
                                .28f to Color.Transparent,
                                1f to Color.Transparent
                            )
                        )
                    )
                    IconButton(onClick = onBack, modifier = Modifier.statusBarsPadding().padding(8.dp).clip(CircleShape).background(Color.Black.copy(alpha = .28f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
                    Column(Modifier.align(Alignment.BottomCenter).padding(horizontal = 20.dp, vertical = 18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                        Box(
                            Modifier.size(94.dp).clip(CircleShape).background(Color.White.copy(alpha = .12f)).clickable { avatarLauncher.launch("image/*") },
                            contentAlignment = Alignment.Center
                        ) {
                            AsyncImage(
                                model = customAvatarUrl,
                                contentDescription = "Avatar",
                                modifier = Modifier.fillMaxSize().clip(CircleShape),
                                contentScale = ContentScale.Crop
                            )
                        }
                        Text(account?.displayName ?: "Offline account", color = Color.White, fontSize = 25.sp, fontWeight = FontWeight.ExtraBold, modifier = Modifier.padding(top = 10.dp))
                        Text(account?.let { "@${it.username}  •  ${it.role}" } ?: "Cached downloads", color = Color.White.copy(alpha = .7f), fontSize = 11.sp)
                        Text("Tap avatar or backdrop banner to customize artwork.", color = Color.White.copy(alpha = .75f), fontSize = 11.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))

                        Row(Modifier.padding(top = 12.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            AssistChip(onClick = onAccount, label = { Text("Account") }, leadingIcon = { Icon(Icons.Filled.Person, null, Modifier.size(16.dp)) })
                            AssistChip(onClick = onActivity, label = { Text("Activity") }, leadingIcon = { Icon(Icons.Filled.Star, null, Modifier.size(16.dp)) })
                        }
                    }
                }
            }
        item {
            Surface(Modifier.fillMaxWidth().padding(horizontal = 16.dp).offset(y = (-8).dp), shape = RoundedCornerShape(18.dp), color = Color.Black.copy(alpha = .28f)) {
                Row(Modifier.fillMaxWidth().padding(vertical = 17.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                    Stat(watchHistory.size.toString(), "Watched")
                    Stat(library.count { it.type == "movie" }.toString(), "Movies")
                    Stat(library.count { it.type == "series" }.toString(), "Shows")
                }
            }
            if (watchHistory.isNotEmpty()) {
                Text("Real Watch History", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 18.sp, modifier = Modifier.padding(horizontal = 16.dp, vertical = 10.dp))
                LazyRow(contentPadding = PaddingValues(horizontal = 16.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    items(watchHistory, key = { it.id }) { history ->
                        val matchingContent = library.firstOrNull { it.id == history.mediaId }
                        Column(Modifier.width(130.dp).clickable { matchingContent?.let(onSelect) }) {
                            Box(Modifier.fillMaxWidth().aspectRatio(16f / 9f).clip(RoundedCornerShape(10.dp)).background(Color.White.copy(alpha = .08f))) {
                                AsyncImage(
                                    model = history.backdropUrl?.let { if (it.startsWith("http")) it else "${ServerManager.activeServerUrl}$it" } ?: matchingContent?.resolvedPosterUrl,
                                    contentDescription = history.title,
                                    modifier = Modifier.fillMaxSize(),
                                    contentScale = ContentScale.Crop
                                )
                                if (history.completed) {
                                    Box(Modifier.align(Alignment.TopEnd).padding(6.dp).clip(RoundedCornerShape(4.dp)).background(Color(0xFF58C878)).padding(horizontal = 4.dp, vertical = 2.dp)) {
                                        Text("DONE", color = Color.Black, fontSize = 8.sp, fontWeight = FontWeight.Bold)
                                    }
                                }
                            }
                            Text(history.title, color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.SemiBold, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 5.dp))
                            if (!history.episodeTitle.isNullOrBlank()) {
                                Text(history.episodeTitle, color = LanflixMuted, fontSize = 10.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
                            }
                        }
                    }
                }
            } else {
                Text("Watch History", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 18.sp, modifier = Modifier.padding(horizontal = 16.dp, vertical = 10.dp))
                MediaShelf("Continue and recently watched", library.sortedByDescending { it.progressPercentage ?: 0.0 }.take(8), onSelect)
            }
            if (activity.isNotEmpty()) {
                Text("Recent Activity", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 18.sp, modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp))
                activity.take(3).forEach { entry ->
                    Surface(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 4.dp), shape = RoundedCornerShape(14.dp), color = Color.White.copy(alpha = .07f)) {
                        Column(Modifier.padding(13.dp)) { Text(entry.kind.replaceFirstChar { it.uppercase() }, color = LanflixGold, fontWeight = FontWeight.Bold, fontSize = 11.sp); Text(entry.body ?: "Media activity", color = Color.White.copy(alpha = .84f), maxLines = 2, overflow = TextOverflow.Ellipsis) }
                    }
                }
            }
        }
    }
}
}


@Composable private fun Stat(value: String, label: String) { Column(horizontalAlignment = Alignment.CenterHorizontally) { Text(value, color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold); Text(label, color = LanflixMuted, fontSize = 11.sp) } }

@Composable
private fun MusicPreview() {
    Column(Modifier.padding(16.dp)) {
        Text("Recently Added in Music", color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold)
        Box(Modifier.fillMaxWidth().height(130.dp).padding(top = 10.dp).clip(RoundedCornerShape(16.dp)).background(Brush.horizontalGradient(listOf(Color(0xFF721449), Color(0xFF3C123C))))) {
            Icon(Icons.Filled.MusicNote, null, tint = Color.White.copy(alpha = .18f), modifier = Modifier.align(Alignment.CenterEnd).padding(18.dp).size(82.dp))
            Column(Modifier.align(Alignment.CenterStart).padding(18.dp)) { Text("Your music, reimagined", color = Color.White, fontSize = 19.sp, fontWeight = FontWeight.Bold); Text("Albums, mixes, radio and offline listening", color = Color.White.copy(alpha = .7f), fontSize = 11.sp) }
        }
    }
}

@Composable
private fun MusicLibrary(music: com.lanflix.api.MusicHome?) {
    if (music == null || music.recentlyAdded.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { EmptyState("Music library", "Add a music folder on the server and run a music scan.") }
        return
    }
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 90.dp)) {
        item { Text("Recently added albums", color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(16.dp)) }
        items(music.recentlyAdded, key = { it.id }) { album ->
            Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 5.dp).clip(RoundedCornerShape(14.dp)).background(Color.White.copy(alpha = .07f)).padding(12.dp), verticalAlignment = Alignment.CenterVertically) {
                AsyncImage(album.artworkUrl?.let { if (it.startsWith("http")) it else "${ServerManager.activeServerUrl}$it" }, album.title, Modifier.size(58.dp).clip(RoundedCornerShape(9.dp)), contentScale = ContentScale.Crop)
                Column(Modifier.padding(start = 12.dp).weight(1f)) { Text(album.title, color = Color.White, fontWeight = FontWeight.Bold); Text("${album.artist.name} • ${album.trackCount} tracks", color = LanflixMuted, fontSize = 11.sp) }
                Icon(Icons.Filled.PlayArrow, "Play album", tint = Color.White)
            }
        }
    }
}

@Composable
private fun EmptyState(title: String, message: String) {
    Column(Modifier.fillMaxWidth().padding(32.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(title, color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
        Text(message, color = LanflixMuted, textAlign = TextAlign.Center, fontSize = 12.sp, modifier = Modifier.padding(top = 7.dp))
    }
}

@Composable
private fun PlayerScreen(item: ContentItem, onBack: () -> Unit) {
    val context = LocalContext.current
    val activity = context as? Activity
    val sessionStore = remember { com.lanflix.auth.LanflixSessionStore(context) }
    val playbackPreferencesRepository = remember { DevicePreferencesRepository(context.applicationContext) }
    val api = remember { LanflixApiClient(context) }

    val playbackPreferences by playbackPreferencesRepository.preferences.collectAsStateWithLifecycle(initialValue = DevicePreferences())
    var playbackInfo by remember(item.id) { mutableStateOf<com.lanflix.api.PlaybackInfo?>(null) }
    LaunchedEffect(item.id) { if (item.localFilePath == null) playbackInfo = api.getPlaybackInfo(item) }
    androidx.compose.runtime.DisposableEffect(activity) {
        if (activity == null) return@DisposableEffect onDispose { }
        val previousOrientation = activity.requestedOrientation
        val controller = WindowCompat.getInsetsController(activity.window, activity.window.decorView)
        controller.systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        controller.hide(WindowInsetsCompat.Type.systemBars())
        activity.window.addFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        activity.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE
        onDispose {
            controller.show(WindowInsetsCompat.Type.systemBars())
            activity.window.clearFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            activity.requestedOrientation = previousOrientation
        }
    }
    val uri = remember(item, playbackPreferences.playbackQuality) {
        item.localFilePath?.let { Uri.fromFile(File(it)) } ?: run {
            val kind = if (item.type.equals("episode", true)) "episode" else "movie"
            val client = if (playbackPreferences.playbackQuality == "Data saver") "mobile-low" else "direct"
            Uri.parse("${ServerManager.activeServerUrl}/api/v2/playback/$kind/${item.id}/file?client=$client")
        }
    }
    val player = remember(uri) {
        val dataSourceFactory = DataSource.Factory {
            val token = sessionStore.accessToken
            val headers = token?.let { mapOf("Authorization" to "Bearer $it") }.orEmpty()
            DefaultHttpDataSource.Factory()
                .setConnectTimeoutMs(8_000)
                .setReadTimeoutMs(8_000)
                .setAllowCrossProtocolRedirects(true)
                .setDefaultRequestProperties(headers)
                .createDataSource()
        }
        val loadControl = DefaultLoadControl.Builder()
            .setBufferDurationsMs(
                /* minBufferMs = */ 15_000,
                /* maxBufferMs = */ 30_000,
                /* bufferForPlaybackMs = */ 1_000,
                /* bufferForPlaybackAfterRebufferMs = */ 2_000
            )
            .setTargetBufferBytes(64 * 1024 * 1024)
            .setPrioritizeTimeOverSizeThresholds(false)
            .build()
        ExoPlayer.Builder(context)
            .setLoadControl(loadControl)
            .setSeekBackIncrementMs(10_000)
            .setSeekForwardIncrementMs(10_000)
            .setMediaSourceFactory(DefaultMediaSourceFactory(dataSourceFactory)).build().apply {
            setSeekParameters(SeekParameters.CLOSEST_SYNC)
            trackSelectionParameters = trackSelectionParameters.buildUpon()
                .setPreferredAudioLanguage(playbackPreferences.preferredAudioLanguage.ifBlank { null })
                .setPreferredTextLanguage(playbackPreferences.preferredSubtitleLanguage.ifBlank { null })
                .setSelectUndeterminedTextLanguage(playbackPreferences.automaticSubtitles)
                .setTrackTypeDisabled(C.TRACK_TYPE_TEXT, !playbackPreferences.automaticSubtitles)
                .build()
            setMediaItem(MediaItem.fromUri(uri)); prepare(); playWhenReady = true
        }
    }
    androidx.compose.runtime.DisposableEffect(player) { onDispose { player.release() } }
    var positionMs by remember { mutableStateOf(0L) }
    LaunchedEffect(player) {
        while (true) {
            positionMs = player.currentPosition.coerceAtLeast(0L)
            delay(300)
        }
    }
    val introEndMs = playbackInfo?.introEndSeconds?.times(1000)?.toLong()
    val introStartMs = playbackInfo?.introStartSeconds?.times(1000)?.toLong() ?: 0L
    val showSkipIntro = introEndMs != null && positionMs in introStartMs until introEndMs
    Box(Modifier.fillMaxSize().background(Color.Black)) {
        AndroidView(factory = {
            PlayerView(it).apply {
                this.player = player
                useController = true
                controllerShowTimeoutMs = 4_000
                resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIT
                setShowRewindButton(true)
                setShowFastForwardButton(true)
                setShowPreviousButton(false)
                setShowNextButton(false)
                setShowSubtitleButton(true)
            }
        }, modifier = Modifier.fillMaxSize())
        IconButton(onClick = onBack, modifier = Modifier.padding(10.dp).clip(CircleShape).background(Color.Black.copy(alpha = .48f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
        if (showSkipIntro) {
            Button(
                onClick = { player.seekTo(introEndMs!!) },
                modifier = Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = 58.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                shape = RoundedCornerShape(20.dp)
            ) { Text("Skip intro", fontWeight = FontWeight.Bold) }
        }
    }
}

private fun shiftArtworkHue(color: Color, degrees: Float): Color {
    val hsv = FloatArray(3)
    android.graphics.Color.colorToHSV(color.toArgb(), hsv)
    hsv[0] = (hsv[0] + degrees) % 360f
    hsv[1] = hsv[1].coerceAtLeast(.68f)
    hsv[2] = hsv[2].coerceAtLeast(.58f)
    return Color(android.graphics.Color.HSVToColor(hsv))
}
