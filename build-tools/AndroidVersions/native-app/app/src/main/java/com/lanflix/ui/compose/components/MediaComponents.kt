package com.lanflix.ui.compose.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.MusicNote
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import coil.compose.SubcomposeAsyncImage
import coil.compose.SubcomposeAsyncImageContent
import com.lanflix.api.MusicHome
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.LanflixBackground
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.LanflixSurfaceRaised
import com.lanflix.ui.compose.theme.ArtworkPalette
import com.lanflix.ui.compose.theme.extractArtworkPalette
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.launch

@Composable
fun OfflineNotice() {
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
fun MusicLibrary(music: MusicHome?) {
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
fun Hero(item: ContentItem?, loading: Boolean, onSelect: (ContentItem) -> Unit, onRetry: () -> Unit, palette: ArtworkPalette, onArtworkPalette: (ArtworkPalette) -> Unit) {
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
fun MediaShelf(title: String, media: List<ContentItem>, onSelect: (ContentItem) -> Unit) {
    Column(Modifier.padding(top = 18.dp)) {
        Text(title, color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
        LazyRow(contentPadding = PaddingValues(horizontal = 12.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            items(media, key = { "${it.type}-${it.id}" }) { item -> PosterCard(item, onSelect) }
        }
    }
}

@Composable
fun PosterCard(item: ContentItem, onSelect: (ContentItem) -> Unit, modifier: Modifier = Modifier.width(126.dp)) {
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
fun TitleArtwork(item: ContentItem, modifier: Modifier) {
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
fun EmptyState(title: String, message: String) {
    Column(Modifier.fillMaxWidth().padding(32.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(title, color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
        Text(message, color = LanflixMuted, textAlign = TextAlign.Center, fontSize = 12.sp, modifier = Modifier.padding(top = 7.dp))
    }
}

@Composable
fun MusicPreview() {
    Column(Modifier.padding(16.dp)) {
        Text("Recently Added in Music", color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold)
        Box(Modifier.fillMaxWidth().height(130.dp).padding(top = 10.dp).clip(RoundedCornerShape(16.dp)).background(Brush.horizontalGradient(listOf(Color(0xFF721449), Color(0xFF3C123C))))) {
            Icon(Icons.Filled.MusicNote, null, tint = Color.White.copy(alpha = .18f), modifier = Modifier.align(Alignment.CenterEnd).padding(18.dp).size(82.dp))
            Column(Modifier.align(Alignment.CenterStart).padding(18.dp)) { Text("Your music, reimagined", color = Color.White, fontSize = 19.sp, fontWeight = FontWeight.Bold); Text("Albums, mixes, radio and offline listening", color = Color.White.copy(alpha = .7f), fontSize = 11.sp) }
        }
    }
}
