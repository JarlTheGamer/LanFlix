@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.ui.compose.screens

import android.net.Uri
import androidx.compose.foundation.background
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.datasource.DataSource
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.MusicAlbum
import com.lanflix.api.MusicLyrics
import com.lanflix.api.MusicTrack
import com.lanflix.auth.LanflixSessionStore
import com.lanflix.music.MusicPlaybackController
import com.lanflix.music.MusicPlaybackState
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.LanflixSurfaceRaised
import com.lanflix.ui.compose.theme.ArtworkPalette
import com.lanflix.ui.compose.theme.DefaultArtworkPalette
import com.lanflix.ui.compose.theme.extractArtworkPalette
import com.lanflix.ui.compose.theme.darkenArtworkColor
import com.lanflix.utils.RefreshingHttpDataSource
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@Composable
fun MusicAlbumScreen(album: MusicAlbum, onBack: () -> Unit, onPlay: (MusicTrack, List<MusicTrack>) -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient.getInstance(context) }
    val session = remember { LanflixSessionStore(context) }
    var tracks by remember(album.id) { mutableStateOf<List<MusicTrack>>(emptyList()) }
    var favoriteIds by remember { mutableStateOf<Set<Long>>(emptySet()) }
    var loading by remember(album.id) { mutableStateOf(true) }
    var palette by remember(album.id) { mutableStateOf(DefaultArtworkPalette) }
    val scope = rememberCoroutineScope()
    LaunchedEffect(album.id) {
        tracks = api.getMusicAlbumTracks(album.id)
        favoriteIds = api.getMusicFavorites().mapTo(mutableSetOf()) { it.id }
        loading = false
    }
    val artwork = authenticatedArtwork(album.artworkUrl, session.accessToken, context)

    Box(Modifier.fillMaxSize().background(Color.Black)) {
        MusicDynamicBackdrop(artwork, palette) { drawable -> scope.launch { palette = extractArtworkPalette(drawable) } }
        LazyColumn(contentPadding = PaddingValues(bottom = 40.dp)) {
            item {
                Column(Modifier.fillMaxWidth().padding(top = 72.dp, start = 20.dp, end = 20.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                    AsyncImage(artwork, album.title, Modifier.size(230.dp).clip(RoundedCornerShape(18.dp)).background(LanflixSurfaceRaised), contentScale = ContentScale.Crop,
                        onSuccess = { state -> scope.launch { palette = extractArtworkPalette(state.result.drawable) } })
                    Text(album.title, color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 18.dp))
                    Text(listOfNotNull(album.artist.name, album.year?.toString(), "${album.trackCount} tracks").joinToString("  •  "), color = LanflixMuted, fontSize = 12.sp)
                    Button(onClick = { tracks.firstOrNull()?.let { onPlay(it, tracks) } }, enabled = tracks.isNotEmpty(), modifier = Modifier.fillMaxWidth().padding(top = 18.dp).height(48.dp), colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black)) {
                        Icon(Icons.Filled.PlayArrow, null, tint = Color.Black); Text("Play album", fontWeight = FontWeight.Bold, color = Color.Black)
                    }
                }
            }
            if (loading) item { Box(Modifier.fillMaxWidth().padding(32.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator(color = LanflixGold) } }
            itemsIndexed(tracks, key = { _, track -> track.id }) { index, track ->
                Row(Modifier.fillMaxWidth().clickable { onPlay(track, tracks) }.padding(horizontal = 20.dp, vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
                    Text((if (track.trackNumber > 0) track.trackNumber else index + 1).toString(), color = LanflixMuted, modifier = Modifier.width(30.dp))
                    Column(Modifier.weight(1f)) {
                        Text(track.title, color = Color.White, maxLines = 1, overflow = TextOverflow.Ellipsis, fontWeight = FontWeight.Medium)
                        Text("${track.codec.uppercase()}  •  ${formatDuration(track.durationMilliseconds)}", color = LanflixMuted, fontSize = 11.sp)
                    }
                    IconButton(onClick = {
                        val favorite = track.id !in favoriteIds
                        favoriteIds = if (favorite) favoriteIds + track.id else favoriteIds - track.id
                        scope.launch { if (!api.setMusicFavorite(track.id, favorite)) favoriteIds = if (favorite) favoriteIds - track.id else favoriteIds + track.id }
                    }) {
                        Icon(if (track.id in favoriteIds) Icons.Filled.Favorite else Icons.Filled.FavoriteBorder, "Favorite ${track.title}", tint = if (track.id in favoriteIds) palette.accent else Color.White)
                    }
                    Icon(Icons.Filled.PlayArrow, "Play ${track.title}", tint = Color.White)
                }
            }
        }
        IconButton(onClick = onBack, Modifier.statusBarsPadding().padding(8.dp).clip(CircleShape).background(Color.Black.copy(alpha = .45f))) {
            Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back", tint = Color.White)
        }
    }
}

@Composable
fun MusicPlayerScreen(initialTrack: MusicTrack, queue: List<MusicTrack>, onBack: () -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient.getInstance(context) }
    val session = remember { LanflixSessionStore(context) }
    val controller = remember { MusicPlaybackController.get(context) }
    val playback by controller.state.collectAsStateWithLifecycle()
    val track = playback.currentTrack ?: initialTrack
    var lyrics by remember { mutableStateOf<MusicLyrics?>(null) }
    var waveform by remember { mutableStateOf<List<Float>>(emptyList()) }
    var showLyrics by remember { mutableStateOf(false) }
    var palette by remember(track.id) { mutableStateOf(DefaultArtworkPalette) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(initialTrack.id, queue.map { it.id }) { controller.play(queue.ifEmpty { listOf(initialTrack) }, initialTrack) }
    LaunchedEffect(track.id) {
        palette = DefaultArtworkPalette
        lyrics = api.getMusicLyrics(track.id)
        waveform = api.getMusicWaveform(track.id)?.amplitudes.orEmpty()
    }
    val artwork = authenticatedArtwork(track.album.artworkUrl, session.accessToken, context)

    Box(Modifier.fillMaxSize().background(Color.Black)) {
        MusicDynamicBackdrop(artwork, palette) { drawable -> scope.launch { palette = extractArtworkPalette(drawable) } }
        Column(Modifier.fillMaxSize().statusBarsPadding().padding(horizontal = 24.dp, vertical = 10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back", tint = Color.White) }
                Text("Now playing", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f), textAlign = androidx.compose.ui.text.style.TextAlign.Center)
                IconButton(onClick = { showLyrics = !showLyrics }, enabled = lyrics != null) { Icon(Icons.Filled.Lyrics, "Lyrics", tint = if (showLyrics) palette.accent else Color.White) }
            }
            if (showLyrics && lyrics != null) {
                LazyColumn(Modifier.weight(1f).fillMaxWidth().padding(vertical = 18.dp)) { item { Text(lyrics!!.text, color = Color.White, fontSize = 17.sp, lineHeight = 27.sp) } }
            } else {
                AsyncImage(artwork, track.album.title, Modifier.padding(top = 26.dp).fillMaxWidth().aspectRatio(1f).clip(RoundedCornerShape(24.dp)).background(LanflixSurfaceRaised), contentScale = ContentScale.Crop,
                    onSuccess = { state -> scope.launch { palette = extractArtworkPalette(state.result.drawable) } })
                Spacer(Modifier.weight(1f))
            }
            Text(track.title, color = Color.White, fontSize = 23.sp, fontWeight = FontWeight.Bold, maxLines = 1, overflow = TextOverflow.Ellipsis)
            Text("${track.artist.name}  •  ${track.album.title}", color = Color.White.copy(alpha = .68f), maxLines = 1, overflow = TextOverflow.Ellipsis)
            MusicWaveform(
                position = playback.positionMilliseconds,
                duration = playback.durationMilliseconds,
                accent = palette.accent,
                amplitudes = waveform,
                onSeek = controller::seekTo,
                modifier = Modifier.fillMaxWidth().padding(top = 12.dp)
            )
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) { Text(formatDuration(playback.positionMilliseconds), color = LanflixMuted, fontSize = 11.sp); Text(formatDuration(playback.durationMilliseconds), color = LanflixMuted, fontSize = 11.sp) }
            Row(Modifier.fillMaxWidth().padding(vertical = 18.dp), horizontalArrangement = Arrangement.SpaceEvenly, verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = controller::previous, enabled = playback.currentIndex > 0) { Icon(Icons.Filled.SkipPrevious, "Previous", tint = Color.White, modifier = Modifier.size(38.dp)) }
                IconButton(onClick = controller::seekBack) { Icon(Icons.Filled.Replay10, "Back 10 seconds", tint = Color.White, modifier = Modifier.size(34.dp)) }
                FilledIconButton(onClick = controller::toggle, modifier = Modifier.size(64.dp), colors = IconButtonDefaults.filledIconButtonColors(containerColor = Color.White)) { Icon(if (playback.playing) Icons.Filled.Pause else Icons.Filled.PlayArrow, "Play or pause", tint = Color.Black, modifier = Modifier.size(34.dp)) }
                IconButton(onClick = controller::seekForward) { Icon(Icons.Filled.Forward10, "Forward 10 seconds", tint = Color.White, modifier = Modifier.size(34.dp)) }
                IconButton(onClick = controller::next, enabled = playback.currentIndex in 0 until playback.queue.lastIndex) { Icon(Icons.Filled.SkipNext, "Next", tint = Color.White, modifier = Modifier.size(38.dp)) }
            }
        }
    }
}

@Composable
fun MusicMiniPlayer(state: MusicPlaybackState, onOpen: () -> Unit) {
    val context = LocalContext.current
    val controller = remember { MusicPlaybackController.get(context) }
    val track = state.currentTrack ?: return
    val session = remember { LanflixSessionStore(context) }
    val scope = rememberCoroutineScope()
    var palette by remember(track.id) { mutableStateOf(DefaultArtworkPalette) }
    val solidColor = darkenArtworkColor(palette.accent, .18f)
    Row(
        Modifier.fillMaxWidth().height(62.dp).background(solidColor).clickable(onClick = onOpen).padding(horizontal = 12.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        AsyncImage(authenticatedArtwork(track.album.artworkUrl, session.accessToken, context), track.album.title, Modifier.size(46.dp).clip(RoundedCornerShape(8.dp)), contentScale = ContentScale.Crop,
            onSuccess = { result -> scope.launch { palette = extractArtworkPalette(result.result.drawable) } })
        Column(Modifier.padding(horizontal = 10.dp).weight(1f)) {
            Text(track.title, color = Color.White, fontWeight = FontWeight.Bold, maxLines = 1, overflow = TextOverflow.Ellipsis)
            Text(track.artist.name, color = Color.White.copy(alpha = .62f), fontSize = 11.sp, maxLines = 1)
        }
        IconButton(onClick = controller::toggle) { Icon(if (state.playing) Icons.Filled.Pause else Icons.Filled.PlayArrow, "Play or pause", tint = Color.White) }
        IconButton(onClick = controller::next, enabled = state.currentIndex in 0 until state.queue.lastIndex) { Icon(Icons.Filled.SkipNext, "Next", tint = Color.White) }
    }
}

@Composable
private fun MusicWaveform(position: Long, duration: Long, accent: Color, amplitudes: List<Float>, onSeek: (Long) -> Unit, modifier: Modifier = Modifier) {
    val progress = if (duration > 0) (position.toFloat() / duration).coerceIn(0f, 1f) else 0f
    Box(modifier.height(52.dp), contentAlignment = Alignment.Center) {
        Canvas(Modifier.fillMaxSize().padding(horizontal = 6.dp, vertical = 8.dp)) {
            val bars = amplitudes.size
            if (bars == 0) return@Canvas
            val gap = size.width / bars
            repeat(bars) { index ->
                val normalized = amplitudes[index].coerceIn(.08f, 1f)
                val barHeight = size.height * normalized
                val played = index.toFloat() / (bars - 1) <= progress
                drawRoundRect(
                    color = if (played) Color.White else accent.copy(alpha = .55f),
                    topLeft = Offset(index * gap, (size.height - barHeight) / 2f),
                    size = androidx.compose.ui.geometry.Size((gap * .48f).coerceAtLeast(1.5f), barHeight),
                    cornerRadius = androidx.compose.ui.geometry.CornerRadius(gap * .25f)
                )
            }
        }
        Slider(
            value = position.toFloat().coerceIn(0f, duration.coerceAtLeast(1L).toFloat()),
            onValueChange = { onSeek(it.toLong()) },
            valueRange = 0f..duration.coerceAtLeast(1L).toFloat(),
            colors = SliderDefaults.colors(
                thumbColor = Color.White,
                activeTrackColor = Color.Transparent,
                inactiveTrackColor = Color.Transparent
            )
        )
    }
}

private fun authenticatedArtwork(path: String?, token: String?, context: android.content.Context): Any? {
    if (path.isNullOrBlank()) return null
    val url = if (path.startsWith("http")) path else "${ServerManager.activeServerUrl}$path"
    return ImageRequest.Builder(context).data(url).apply { if (!token.isNullOrBlank()) addHeader("Authorization", "Bearer $token") }.build()
}

@Composable
private fun MusicDynamicBackdrop(artwork: Any?, palette: ArtworkPalette, onArtwork: (android.graphics.drawable.Drawable) -> Unit) {
    Box(Modifier.fillMaxSize().background(palette.depth)) {
        AsyncImage(
            model = artwork,
            contentDescription = null,
            modifier = Modifier.fillMaxSize().blur(58.dp).alpha(.50f),
            contentScale = ContentScale.Crop,
            onSuccess = { onArtwork(it.result.drawable) }
        )
        Box(Modifier.fillMaxSize().background(Brush.radialGradient(
            0f to palette.glow.copy(alpha = .82f),
            .52f to palette.base.copy(alpha = .44f),
            1f to Color.Transparent,
            center = Offset(760f, 300f), radius = 1300f
        )))
        Box(Modifier.fillMaxSize().background(Brush.radialGradient(
            0f to palette.accent.copy(alpha = .65f),
            .58f to Color.Transparent,
            center = Offset(80f, 1500f), radius = 1250f
        )))
        Box(Modifier.fillMaxSize().background(Brush.verticalGradient(
            0f to Color.Black.copy(alpha = .12f),
            .42f to palette.base.copy(alpha = .24f),
            1f to palette.depth.copy(alpha = .86f)
        )))
    }
}

private fun formatDuration(milliseconds: Long): String {
    val seconds = (milliseconds.coerceAtLeast(0) / 1000)
    return "%d:%02d".format(seconds / 60, seconds % 60)
}
