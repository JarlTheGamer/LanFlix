package com.lanflix.ui.compose.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.models.EpisodeItem
import com.lanflix.models.SeasonSummary
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted

@Composable
fun SeriesEpisodeBrowser(item: ContentItem, online: Boolean, onPlayEpisode: (EpisodeItem) -> Unit) {
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
fun EpisodeRow(episode: EpisodeItem, onClick: () -> Unit) {
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

@Composable
fun DetailAction(icon: ImageVector, label: String, enabled: Boolean = false, onClick: () -> Unit = {}) {
    Column(
        modifier = Modifier.clip(RoundedCornerShape(10.dp)).clickable(enabled = enabled, onClick = onClick).padding(5.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) { Icon(icon, label, tint = Color.White, modifier = Modifier.size(22.dp)); Text(label, color = LanflixMuted, fontSize = 9.sp, modifier = Modifier.padding(top = 4.dp)) }
}
