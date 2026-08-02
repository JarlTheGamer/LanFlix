package com.lanflix.ui.compose.screens

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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.outlined.Download
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
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lanflix.api.DiscoveryItem
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.LanflixBackground
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.LanflixUiState
import com.lanflix.ui.compose.components.EmptyState
import kotlinx.coroutines.launch

@Composable
fun DiscoverScreen(state: LanflixUiState, onSelect: (ContentItem) -> Unit) {
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
private fun DiscoveryShelf(title: String, media: List<DiscoveryItem>, onSelect: (ContentItem) -> Unit) {
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
