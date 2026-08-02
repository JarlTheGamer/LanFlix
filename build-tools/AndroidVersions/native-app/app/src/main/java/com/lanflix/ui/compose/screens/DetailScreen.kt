package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.Tv
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material.icons.outlined.Download
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lanflix.api.DiscoveryItem
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.models.EpisodeItem
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.components.DetailAction
import com.lanflix.ui.compose.components.SeriesEpisodeBrowser
import com.lanflix.ui.compose.components.TitleArtwork
import com.lanflix.ui.compose.theme.DefaultArtworkPalette
import com.lanflix.ui.compose.theme.extractArtworkPalette
import kotlinx.coroutines.launch

@Composable
fun DetailScreen(
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
                            if (isDiscovery) scope.launch { acquisitionRequested = discoveryApi.acquire(DiscoveryItem(item.tmdbId, item.type ?: "movie", item.displayTitle, item.overview, item.year, item.rating?.toDoubleOrNull() ?: 0.0, item.posterUrl, item.backdropUrl)) }
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
