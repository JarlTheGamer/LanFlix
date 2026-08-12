@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.ui.compose.screens

import android.app.Activity
import android.net.Uri
import androidx.compose.foundation.background
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
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
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.foundation.basicMarquee
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
    MusicImmersiveSystemBars()
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
@OptIn(ExperimentalFoundationApi::class)
fun MusicPlayerScreen(initialTrack: MusicTrack, queue: List<MusicTrack>, onBack: () -> Unit) {
    MusicImmersiveSystemBars()
    val context = LocalContext.current
    val api = remember { LanflixApiClient.getInstance(context) }
    val session = remember { LanflixSessionStore(context) }
    val controller = remember { MusicPlaybackController.get(context) }
    val playback by controller.state.collectAsStateWithLifecycle()
    val track = playback.currentTrack ?: initialTrack
    var lyrics by remember { mutableStateOf<MusicLyrics?>(null) }
    var waveform by remember { mutableStateOf<List<Float>>(emptyList()) }
    var showLyrics by remember { mutableStateOf(false) }
    // Palette identity follows the artwork, not the track. Tracks on one
    // album must not flash through the default palette when the queue advances.
    val artworkKey = track.album.artworkUrl ?: "album:${track.album.id}"
    var palette by remember(artworkKey) { mutableStateOf(DefaultArtworkPalette) }
    val animatedBase by animateColorAsState(palette.base, tween(650), label = "music-base")
    val animatedDepth by animateColorAsState(palette.depth, tween(650), label = "music-depth")
    val animatedGlow by animateColorAsState(palette.glow, tween(650), label = "music-glow")
    val animatedAccent by animateColorAsState(palette.accent, tween(650), label = "music-accent")
    val scope = rememberCoroutineScope()

    LaunchedEffect(initialTrack.id, queue.map { it.id }) { controller.play(queue.ifEmpty { listOf(initialTrack) }, initialTrack) }
    LaunchedEffect(track.id) {
        lyrics = api.getMusicLyrics(track.id)
        waveform = api.getMusicWaveform(track.id)?.amplitudes.orEmpty()
    }
    val artwork = authenticatedArtwork(track.album.artworkUrl, session.accessToken, context)

    Box(Modifier.fillMaxSize().background(Color.Black)) {
        MusicDynamicBackdrop(artwork, palette.copy(base = animatedBase, depth = animatedDepth, glow = animatedGlow, accent = animatedAccent)) { drawable -> scope.launch { palette = extractArtworkPalette(drawable) } }
        Column(Modifier.fillMaxSize().statusBarsPadding().padding(horizontal = 24.dp, vertical = 10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back", tint = Color.White) }
                Text("Now playing", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f), textAlign = androidx.compose.ui.text.style.TextAlign.Center)
                IconButton(onClick = { showLyrics = !showLyrics }, enabled = lyrics != null) { Icon(Icons.Filled.Lyrics, "Lyrics", tint = if (showLyrics) animatedAccent else Color.White) }
            }
            if (showLyrics && lyrics != null) {
                LazyColumn(Modifier.weight(1f).fillMaxWidth().padding(vertical = 18.dp)) { item { Text(lyrics!!.text, color = Color.White, fontSize = 17.sp, lineHeight = 27.sp) } }
            } else {
                AsyncImage(artwork, track.album.title, Modifier.padding(top = 26.dp).fillMaxWidth().aspectRatio(1f).clip(RoundedCornerShape(24.dp)).background(LanflixSurfaceRaised), contentScale = ContentScale.Crop,
                    onSuccess = { state -> scope.launch { palette = extractArtworkPalette(state.result.drawable) } })
                Spacer(Modifier.height(10.dp))
            }
            Text(track.title, color = Color.White, fontSize = 23.sp, fontWeight = FontWeight.Bold, maxLines = 1, overflow = TextOverflow.Ellipsis)
            Text(
                "${track.artist.name}  •  ${track.album.title}",
                color = Color.White.copy(alpha = .68f),
                maxLines = 1,
                softWrap = false,
                modifier = Modifier.fillMaxWidth().basicMarquee(iterations = Int.MAX_VALUE)
            )
            MusicWaveform(
                position = playback.positionMilliseconds,
                duration = playback.durationMilliseconds,
                accent = animatedAccent,
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
    val artworkKey = track.album.artworkUrl ?: "album:${track.album.id}"
    var palette by remember(artworkKey) { mutableStateOf(DefaultArtworkPalette) }
    val animatedAccent by animateColorAsState(palette.accent, tween(500), label = "mini-accent")
    val solidColor = darkenArtworkColor(animatedAccent, .18f)
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
    val targetProgress = if (duration > 0) (position.toFloat() / duration).coerceIn(0f, 1f) else 0f
    val progress by animateFloatAsState(targetProgress, tween(220), label = "wave-progress")
    val barAmplitudes = amplitudes.mapIndexed { index, value ->
        animateFloatAsState(
            targetValue = value.coerceIn(.08f, 1f),
            animationSpec = tween(600),
            label = "wave-height-$index"
        ).value
    }
    val barProgress = amplitudes.mapIndexed { index, _ ->
        val target = (progress * amplitudes.size - index).coerceIn(0f, 1f)
        animateFloatAsState(target, tween(180), label = "wave-bar-$index").value
    }
    var waveformWidth by remember { mutableIntStateOf(0) }
    Box(
        modifier = modifier
            .height(52.dp)
            .onSizeChanged { waveformWidth = it.width }
            .pointerInput(duration, waveformWidth) {
                detectTapGestures { offset ->
                    if (duration > 0 && waveformWidth > 0) {
                        onSeek((offset.x / waveformWidth.toFloat() * duration).toLong().coerceIn(0L, duration))
                    }
                }
            },
        contentAlignment = Alignment.Center
    ) {
        Canvas(Modifier.fillMaxSize().padding(horizontal = 6.dp, vertical = 8.dp)) {
            val bars = amplitudes.size
            if (bars == 0) return@Canvas
            val gap = size.width / bars
            repeat(bars) { index ->
                val normalized = barAmplitudes.getOrElse(index) { .08f }
                val barHeight = size.height * normalized
                val barPlayed = barProgress.getOrElse(index) { 0f }
                drawRoundRect(
                    color = lerp(accent.copy(alpha = .55f), Color.White, barPlayed),
                    topLeft = Offset(index * gap, (size.height - barHeight) / 2f),
                    size = androidx.compose.ui.geometry.Size((gap * .48f).coerceAtLeast(1.5f), barHeight),
                    cornerRadius = androidx.compose.ui.geometry.CornerRadius(gap * .25f)
                )
            }
        }
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

@Composable
fun MusicImmersiveSystemBars() {
    val activity = LocalContext.current as? Activity ?: return
    DisposableEffect(activity) {
        val controller = androidx.core.view.WindowCompat.getInsetsController(
            activity.window, activity.window.decorView
        )
        val previousBehavior = controller.systemBarsBehavior
        controller.systemBarsBehavior = androidx.core.view.WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        controller.hide(androidx.core.view.WindowInsetsCompat.Type.systemBars())
        onDispose {
            controller.systemBarsBehavior = previousBehavior
            controller.show(androidx.core.view.WindowInsetsCompat.Type.systemBars())
        }
    }
}

private fun formatDuration(milliseconds: Long): String {
    val seconds = (milliseconds.coerceAtLeast(0) / 1000)
    return "%d:%02d".format(seconds / 60, seconds % 60)
}
