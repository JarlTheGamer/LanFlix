@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class)

package com.lanflix.ui.compose

import android.net.Uri
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
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
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.VideoLibrary
import androidx.compose.material.icons.filled.LiveTv
import androidx.compose.material.icons.filled.MusicNote
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Star
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
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.OutlinedTextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
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
import androidx.media3.common.MediaItem
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.ui.PlayerView
import coil.compose.AsyncImage
import com.lanflix.models.ContentItem
import com.lanflix.webview.ServerManager
import java.io.File

private enum class Destination(val label: String, val selected: ImageVector, val unselected: ImageVector) {
    Home("Home", Icons.Filled.Home, Icons.Outlined.Home),
    Libraries("Libraries", Icons.Filled.VideoLibrary, Icons.Outlined.VideoLibrary),
    Live("Live TV", Icons.Filled.LiveTv, Icons.Outlined.LiveTv),
    Demand("On Demand", Icons.Filled.Download, Icons.Outlined.Download),
    Discover("Discover", Icons.Filled.TravelExplore, Icons.Outlined.TravelExplore)
}

@Composable
fun LanflixApp(viewModel: LanflixViewModel = viewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var destination by remember { mutableStateOf(Destination.Home) }
    var detail by remember { mutableStateOf<ContentItem?>(null) }
    var profileVisible by remember { mutableStateOf(false) }
    var searchVisible by remember { mutableStateOf(false) }
    var playerItem by remember { mutableStateOf<ContentItem?>(null) }

    BackHandler(enabled = playerItem != null || detail != null || profileVisible || searchVisible) {
        when {
            playerItem != null -> playerItem = null
            detail != null -> detail = null
            searchVisible -> searchVisible = false
            profileVisible -> profileVisible = false
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
                    onDownload = { viewModel.download(detail!!) { saved -> if (saved != null) detail = saved } }
                )
                searchVisible -> SearchScreen(state.library, onBack = { searchVisible = false }, onSelect = { searchVisible = false; detail = it })
                profileVisible -> ProfileScreen(
                    library = state.library,
                    onBack = { profileVisible = false },
                    onSelect = { detail = it }
                )
                else -> Box(Modifier.fillMaxSize()) {
                    AnimatedContent(targetState = destination, label = "main-destination") { target ->
                        when (target) {
                            Destination.Home -> HomeScreen(state, onSelect = { detail = it }, onRetry = viewModel::refresh)
                            Destination.Libraries -> LibraryScreen(state.library, onSelect = { detail = it })
                            Destination.Live -> LiveTvScreen(state.online)
                            Destination.Demand -> DownloadsScreen(state.library, onSelect = { detail = it })
                            Destination.Discover -> DiscoverScreen(state, onSelect = { detail = it })
                        }
                    }
                    TopChrome(
                        title = if (destination == Destination.Home) "lanflix" else destination.label,
                        online = state.online,
                        onSearch = { searchVisible = true },
                        onProfile = { profileVisible = true }
                    )
                    Box(Modifier.align(Alignment.BottomCenter)) {
                        BottomChrome(destination, onSelect = { destination = it })
                    }
                }
            }
        }
    }
}

@Composable
private fun TopChrome(title: String, online: Boolean, onSearch: () -> Unit, onProfile: () -> Unit) {
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
        IconButton(onClick = onSearch) { Icon(Icons.Filled.Search, "Search", tint = Color.White, modifier = Modifier.size(19.dp)) }
        IconButton(onClick = {}) { Icon(Icons.Filled.Cast, "Cast", tint = Color.White, modifier = Modifier.size(19.dp)) }
        IconButton(onClick = {}) { Icon(Icons.Outlined.BookmarkBorder, "Watchlist", tint = Color.White, modifier = Modifier.size(19.dp)) }
        Box(
            modifier = Modifier.size(26.dp).clip(CircleShape).background(LanflixGold).clickable(onClick = onProfile),
            contentAlignment = Alignment.Center
        ) { Icon(Icons.Filled.Person, "Profile", tint = Color.Black, modifier = Modifier.size(17.dp)) }
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
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(bottom = 92.dp)
    ) {
        item { Hero(item = hero, loading = state.loading, onSelect = onSelect, onRetry = onRetry) }
        if (!state.online) item { OfflineNotice() }
        if (state.library.isNotEmpty()) {
            item { MediaShelf("Continue Watching", state.library.take(8), onSelect) }
            item { MediaShelf("Recently Added", state.library.drop(1).take(10), onSelect) }
            item { MediaShelf("Because it’s movie night", state.library.shuffled().take(8), onSelect) }
        }
        item { MusicPreview() }
    }
}

@Composable
private fun Hero(item: ContentItem?, loading: Boolean, onSelect: (ContentItem) -> Unit, onRetry: () -> Unit) {
    Box(Modifier.fillMaxWidth().height(520.dp)) {
        if (item != null) {
            AsyncImage(
                model = item.resolvedBackdropUrl ?: item.resolvedPosterUrl,
                contentDescription = item.displayTitle,
                modifier = Modifier.fillMaxSize(),
                contentScale = ContentScale.Crop
            )
        } else {
            Box(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF14304B), LanflixBackground))))
        }
        Box(
            Modifier.fillMaxSize().background(
                Brush.verticalGradient(
                    0f to Color.Black.copy(alpha = .18f),
                    .5f to Color.Transparent,
                    1f to LanflixBackground
                )
            )
        )
        Column(
            modifier = Modifier.align(Alignment.BottomCenter).fillMaxWidth().padding(horizontal = 22.dp, vertical = 26.dp),
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
                    Text(item.displayTitle, color = Color.White, fontSize = 34.sp, lineHeight = 35.sp, fontWeight = FontWeight.ExtraBold, textAlign = TextAlign.Center)
                    Text(
                        listOfNotNull(item.releaseDate?.take(4), item.rating, item.type?.replaceFirstChar { it.uppercase() }).joinToString("  •  "),
                        color = Color.White.copy(alpha = .74f), fontSize = 12.sp, modifier = Modifier.padding(top = 8.dp)
                    )
                    Text(item.overview.orEmpty(), color = Color.White.copy(alpha = .82f), maxLines = 3, overflow = TextOverflow.Ellipsis, textAlign = TextAlign.Center, fontSize = 12.sp, modifier = Modifier.padding(top = 12.dp))
                    Button(
                        onClick = { onSelect(item) },
                        modifier = Modifier.fillMaxWidth().padding(top = 16.dp).height(48.dp),
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
        Text(item.releaseDate?.take(4).orEmpty(), color = LanflixMuted, fontSize = 9.sp)
    }
}

@Composable
private fun LibraryScreen(media: List<ContentItem>, onSelect: (ContentItem) -> Unit) {
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
        if (selectedFilter == "Music") MusicLibrary() else if (filtered.isEmpty()) EmptyState("No $selectedFilter yet", "When this library is scanned, it will appear here.") else {
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
private fun LiveTvScreen(online: Boolean) {
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(top = 82.dp, bottom = 90.dp, start = 14.dp, end = 14.dp)) {
        item {
            Text("Live TV", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
            Text(if (online) "Guide  •  What’s on now" else "Guide unavailable offline", color = LanflixMuted, fontSize = 12.sp)
            Box(Modifier.fillMaxWidth().height(190.dp).padding(top = 16.dp).clip(RoundedCornerShape(16.dp)).background(Brush.linearGradient(listOf(Color(0xFF17485A), Color(0xFF0A1D29))))) {
                Column(Modifier.align(Alignment.BottomStart).padding(18.dp)) {
                    Text("Your channels, one beautiful guide", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold)
                    Text("Add an M3U/XMLTV source or HDHomeRun tuner in server settings.", color = Color.White.copy(alpha = .72f), fontSize = 12.sp)
                }
            }
        }
        item { Text("Featured", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 24.dp, bottom = 10.dp)) }
        items(listOf("Recently watched", "News & documentary", "Movies", "Family")) { label ->
            Row(Modifier.fillMaxWidth().padding(vertical = 4.dp).clip(RoundedCornerShape(10.dp)).background(Color.White.copy(alpha = .06f)).padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.size(42.dp).clip(RoundedCornerShape(8.dp)).background(Color.White.copy(alpha = .09f)), contentAlignment = Alignment.Center) { Icon(Icons.Filled.Tv, null, tint = LanflixGold) }
                Column(Modifier.padding(start = 12.dp).weight(1f)) { Text(label, color = Color.White); Text("No guide data", color = LanflixMuted, fontSize = 11.sp) }
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
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(top = 82.dp, bottom = 90.dp)) {
        item { Text("Discover", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp)); Text("Find something worth watching", color = LanflixMuted, modifier = Modifier.padding(horizontal = 16.dp)) }
        item { MediaShelf("Available on your server", state.library.take(10), onSelect) }
        item { EmptyState("Discovery service ready", "Trending, people, and requests will appear when the v2 server module is enabled.") }
    }
}

@Composable
private fun DetailScreen(
    item: ContentItem,
    online: Boolean,
    downloading: Boolean,
    onBack: () -> Unit,
    onPlay: () -> Unit,
    onDownload: () -> Unit
) {
    val isPlayableType = item.type.equals("movie", true) || item.type.equals("episode", true)
    val canPlay = isPlayableType && (item.isOfflinePlayable || online)
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 36.dp)) {
        item {
            Box(Modifier.fillMaxWidth().height(390.dp)) {
                AsyncImage(model = item.resolvedBackdropUrl ?: item.resolvedPosterUrl, contentDescription = item.displayTitle, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
                Box(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color.Black.copy(alpha = .2f), Color.Transparent, LanflixBackground))))
                IconButton(onClick = onBack, modifier = Modifier.statusBarsPadding().padding(8.dp).clip(CircleShape).background(Color.Black.copy(alpha = .38f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
                Column(Modifier.align(Alignment.BottomStart).padding(18.dp)) {
                    Text(item.displayTitle, color = Color.White, fontSize = 31.sp, lineHeight = 32.sp, fontWeight = FontWeight.ExtraBold)
                    Text(listOfNotNull(item.releaseDate?.take(4), item.rating, item.type).joinToString("  •  "), color = Color.White.copy(alpha = .72f), fontSize = 12.sp, modifier = Modifier.padding(top = 6.dp))
                }
            }
        }
        item {
            Column(Modifier.padding(horizontal = 16.dp)) {
                Button(
                    onClick = onPlay,
                    enabled = canPlay,
                    modifier = Modifier.fillMaxWidth().height(50.dp),
                    shape = RoundedCornerShape(25.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color.White, disabledContainerColor = Color.White.copy(alpha = .15f))
                ) { Icon(Icons.Filled.PlayArrow, null, tint = Color.Black); Text(if (!isPlayableType) "Choose an episode" else if (item.isOfflinePlayable) "Play offline" else if (online) "Play" else "Unavailable offline", color = if (canPlay) Color.Black else LanflixMuted, fontWeight = FontWeight.Bold) }
                Row(Modifier.fillMaxWidth().padding(vertical = 12.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                    DetailAction(Icons.Outlined.BookmarkBorder, "Watchlist")
                    DetailAction(
                        Icons.Outlined.Download,
                        if (downloading) "Downloading" else if (item.isOfflinePlayable) "Downloaded" else "Download",
                        enabled = isPlayableType && online && !item.isOfflinePlayable && !downloading,
                        onClick = onDownload
                    )
                    DetailAction(Icons.Filled.Star, "Rate")
                    DetailAction(Icons.Filled.Cast, "Cast")
                }
                Text(item.overview ?: "No overview available.", color = Color.White.copy(alpha = .82f), fontSize = 14.sp, lineHeight = 20.sp)
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

@Composable
private fun DetailAction(icon: ImageVector, label: String, enabled: Boolean = false, onClick: () -> Unit = {}) {
    Column(
        modifier = Modifier.clip(RoundedCornerShape(10.dp)).clickable(enabled = enabled, onClick = onClick).padding(5.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) { Icon(icon, label, tint = Color.White, modifier = Modifier.size(22.dp)); Text(label, color = LanflixMuted, fontSize = 9.sp, modifier = Modifier.padding(top = 4.dp)) }
}

@Composable
private fun ProfileScreen(library: List<ContentItem>, onBack: () -> Unit, onSelect: (ContentItem) -> Unit) {
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 40.dp)) {
        item {
            Box(Modifier.fillMaxWidth().height(330.dp).background(Brush.verticalGradient(listOf(Color(0xFF45321C), LanflixBackground)))) {
                IconButton(onClick = onBack, modifier = Modifier.statusBarsPadding().padding(8.dp)) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
                Column(Modifier.align(Alignment.BottomCenter).padding(24.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                    Box(Modifier.size(92.dp).clip(CircleShape).background(LanflixGold), contentAlignment = Alignment.Center) { Icon(Icons.Filled.Person, null, tint = Color.Black, modifier = Modifier.size(56.dp)) }
                    Text("Lanflix profile", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 12.dp))
                    Text("Your household media identity", color = LanflixMuted)
                }
            }
        }
        item {
            Row(Modifier.fillMaxWidth().padding(18.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                Stat(library.count { it.type == "movie" }.toString(), "Movies")
                Stat(library.count { it.type == "series" }.toString(), "Shows")
                Stat(library.count { it.isOfflinePlayable }.toString(), "Offline")
            }
            Text("Watch History", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
            MediaShelf("Recently watched", library.take(8), onSelect)
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

@Composable private fun MusicLibrary() { Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { EmptyState("Music library", "Add a music folder on the server to see albums, artists, mixes and radios.") } }

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
    val uri = remember(item) {
        item.localFilePath?.let { Uri.fromFile(File(it)) } ?: run {
            val kind = if (item.type.equals("episode", true)) "episode" else "movie"
            Uri.parse("${ServerManager.activeServerUrl}/api/stream/$kind/${item.id}/file")
        }
    }
    val player = remember(uri) { ExoPlayer.Builder(context).build().apply { setMediaItem(MediaItem.fromUri(uri)); prepare(); playWhenReady = true } }
    androidx.compose.runtime.DisposableEffect(player) { onDispose { player.release() } }
    Box(Modifier.fillMaxSize().background(Color.Black)) {
        AndroidView(factory = { PlayerView(it).apply { this.player = player; useController = true } }, modifier = Modifier.fillMaxSize())
        IconButton(onClick = onBack, modifier = Modifier.statusBarsPadding().padding(10.dp).clip(CircleShape).background(Color.Black.copy(alpha = .45f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
    }
}
