package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Favorite
import androidx.compose.material.icons.filled.PlayArrow
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
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.lanflix.api.*
import com.lanflix.auth.LanflixSessionStore
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.LanflixSurfaceRaised
import com.lanflix.ui.compose.components.EmptyState
import com.lanflix.ui.compose.theme.DefaultArtworkPalette
import com.lanflix.ui.compose.theme.extractArtworkPalette
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.launch
import java.util.Calendar

@Composable
fun MusicLibraryScreen(
    music: MusicHome?,
    onAlbum: (MusicAlbum) -> Unit,
    onPlay: (MusicTrack, List<MusicTrack>) -> Unit
) {
    if (music == null || music.recentlyAdded.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            EmptyState("Music library", "Add music to the server's media/music folder and run a music scan.")
        }
        return
    }

    val context = LocalContext.current
    val api = remember { LanflixApiClient.getInstance(context) }
    val session = remember { LanflixSessionStore(context) }
    val scope = rememberCoroutineScope()
    var playlists by remember { mutableStateOf<List<MusicPlaylist>>(emptyList()) }
    var favorites by remember { mutableStateOf<List<MusicTrack>>(emptyList()) }
    var palette by remember(music.recentlyAdded.first().id) { mutableStateOf(DefaultArtworkPalette) }
    var showCreatePlaylist by remember { mutableStateOf(false) }
    var playlistName by remember { mutableStateOf("") }

    suspend fun refreshPersonalMusic() {
        playlists = api.getMusicPlaylists()
        favorites = api.getMusicFavorites()
    }
    LaunchedEffect(music.recentlyAdded) { refreshPersonalMusic() }

    val heroArtwork = musicArtworkRequest(music.recentlyAdded.first().artworkUrl, session.accessToken, context)
    val currentYear = Calendar.getInstance().get(Calendar.YEAR)
    val anniversaryAlbum = music.recentlyAdded.filter { it.year != null && currentYear - it.year!! >= 5 }
        .maxByOrNull { it.year ?: 0 }

    Box(Modifier.fillMaxSize().background(palette.depth)) {
        AsyncImage(heroArtwork, null, Modifier.fillMaxSize().blur(60.dp).alpha(.44f), contentScale = ContentScale.Crop,
            onSuccess = { result -> scope.launch { palette = extractArtworkPalette(result.result.drawable) } })
        Box(Modifier.fillMaxSize().background(Brush.radialGradient(
            0f to palette.glow.copy(alpha = .82f), .55f to palette.base.copy(alpha = .34f), 1f to Color.Transparent,
            center = Offset(760f, 180f), radius = 1300f
        )))
        Box(Modifier.fillMaxSize().background(Brush.radialGradient(
            0f to palette.accent.copy(alpha = .52f), .64f to Color.Transparent,
            center = Offset(60f, 1450f), radius = 1250f
        )))
        Box(Modifier.fillMaxSize().background(Brush.verticalGradient(0f to Color.Black.copy(alpha = .10f), 1f to palette.depth.copy(alpha = .90f))))

        LazyColumn(Modifier.fillMaxSize().statusBarsPadding(), contentPadding = PaddingValues(bottom = 100.dp)) {
            item {
                Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 14.dp), verticalAlignment = Alignment.CenterVertically) {
                    Column(Modifier.weight(1f)) {
                        Text("Music", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
                        Text("Your Lanflix library", color = Color.White.copy(alpha = .62f), fontSize = 11.sp)
                    }
                    FilledIconButton(onClick = { showCreatePlaylist = true }, colors = IconButtonDefaults.filledIconButtonColors(containerColor = Color.White.copy(alpha = .12f))) {
                        Icon(Icons.Filled.Add, "New playlist", tint = Color.White)
                    }
                }
            }

            if (playlists.isNotEmpty()) {
                item { MusicSectionTitle("Recent playlists") }
                item {
                    LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                        items(playlists, key = { it.id }) { playlist ->
                            PlaylistCard(playlist, palette.accent) { playlist.tracks.firstOrNull()?.let { onPlay(it, playlist.tracks) } }
                        }
                    }
                }
            }

            if (anniversaryAlbum != null) {
                item { MusicSectionTitle("On this day") }
                item {
                    val years = currentYear - (anniversaryAlbum.year ?: currentYear)
                    Row(
                        Modifier.fillMaxWidth().padding(horizontal = 14.dp).clip(RoundedCornerShape(16.dp))
                            .background(Color.White.copy(alpha = .13f)).clickable { onAlbum(anniversaryAlbum) }.padding(12.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        AlbumArtwork(anniversaryAlbum, session.accessToken, Modifier.size(82.dp))
                        Column(Modifier.padding(start = 14.dp).weight(1f)) {
                            Text("$years YEARS AGO", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 12.sp)
                            Text(anniversaryAlbum.title, color = Color.White, fontWeight = FontWeight.Bold, maxLines = 1)
                            Text("${anniversaryAlbum.artist.name}  •  ${anniversaryAlbum.year}", color = Color.White.copy(alpha = .65f), fontSize = 11.sp)
                        }
                        Icon(Icons.Filled.PlayArrow, "Open album", tint = palette.accent)
                    }
                }
            }

            if (favorites.isNotEmpty()) {
                item { MusicSectionTitle("Favorites") }
                item {
                    LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                        items(favorites.take(12), key = { it.id }) { track -> TrackCard(track, session.accessToken, palette.accent) { onPlay(track, favorites) } }
                    }
                }
            }

            item { MusicSectionTitle("Recently added") }
            item {
                LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    items(music.recentlyAdded, key = { it.id }) { album -> AlbumCard(album, session.accessToken) { onAlbum(album) } }
                }
            }

            if (music.artists.isNotEmpty()) {
                item { MusicSectionTitle("Artists") }
                item {
                    LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(14.dp)) {
                        items(music.artists, key = { it.id }) { artist ->
                            Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.width(76.dp)) {
                                Box(Modifier.size(64.dp).clip(CircleShape).background(Brush.radialGradient(listOf(palette.accent, palette.base))), contentAlignment = Alignment.Center) {
                                    Text(artist.name.take(1).uppercase(), color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
                                }
                                Text(artist.name, color = Color.White, fontSize = 10.sp, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 6.dp))
                            }
                        }
                    }
                }
            }

            item { MusicSectionTitle("Albums") }
            items(music.recentlyAdded.chunked(2), key = { row -> row.joinToString("-") { it.id.toString() } }) { row ->
                Row(Modifier.fillMaxWidth().padding(horizontal = 14.dp, vertical = 6.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    row.forEach { album -> Box(Modifier.weight(1f)) { AlbumCard(album, session.accessToken, Modifier.fillMaxWidth()) { onAlbum(album) } } }
                    if (row.size == 1) Spacer(Modifier.weight(1f))
                }
            }
        }
    }

    if (showCreatePlaylist) {
        AlertDialog(
            onDismissRequest = { showCreatePlaylist = false },
            title = { Text("New playlist") },
            text = { OutlinedTextField(playlistName, { playlistName = it }, label = { Text("Name") }, singleLine = true) },
            confirmButton = {
                TextButton(enabled = playlistName.isNotBlank(), onClick = {
                    val name = playlistName
                    playlistName = ""; showCreatePlaylist = false
                    scope.launch { if (api.createMusicPlaylist(name)) refreshPersonalMusic() }
                }) { Text("Create") }
            },
            dismissButton = { TextButton(onClick = { showCreatePlaylist = false }) { Text("Cancel") } }
        )
    }
}

@Composable private fun MusicSectionTitle(title: String) {
    Text(title.uppercase(), color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(start = 16.dp, top = 22.dp, bottom = 9.dp))
}

@Composable private fun AlbumCard(album: MusicAlbum, token: String?, modifier: Modifier = Modifier.width(142.dp), onClick: () -> Unit) {
    Column(modifier.clickable(onClick = onClick)) {
        AlbumArtwork(album, token, Modifier.fillMaxWidth().aspectRatio(1f))
        Text(album.title, color = Color.White, fontWeight = FontWeight.Bold, fontSize = 11.sp, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 6.dp))
        Text(album.artist.name, color = Color.White.copy(alpha = .60f), fontSize = 9.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
    }
}

@Composable private fun AlbumArtwork(album: MusicAlbum, token: String?, modifier: Modifier) {
    val context = LocalContext.current
    AsyncImage(musicArtworkRequest(album.artworkUrl, token, context), album.title, modifier.clip(RoundedCornerShape(13.dp)).background(LanflixSurfaceRaised), contentScale = ContentScale.Crop)
}

@Composable private fun PlaylistCard(playlist: MusicPlaylist, accent: Color, onClick: () -> Unit) {
    Column(Modifier.width(166.dp).clip(RoundedCornerShape(16.dp)).background(Color.Black.copy(alpha = .22f)).clickable(onClick = onClick).padding(10.dp)) {
        Box(Modifier.fillMaxWidth().aspectRatio(1.45f).clip(RoundedCornerShape(12.dp)).background(Brush.linearGradient(listOf(accent, Color.Black.copy(alpha = .35f)))), contentAlignment = Alignment.Center) {
            Icon(Icons.Filled.PlayArrow, null, tint = Color.White, modifier = Modifier.size(34.dp))
        }
        Text(playlist.name, color = Color.White, fontWeight = FontWeight.Bold, maxLines = 1, modifier = Modifier.padding(top = 8.dp))
        Text("${playlist.tracks.size} tracks", color = Color.White.copy(alpha = .62f), fontSize = 10.sp)
    }
}

@Composable private fun TrackCard(track: MusicTrack, token: String?, accent: Color, onClick: () -> Unit) {
    Column(Modifier.width(126.dp).clickable(onClick = onClick)) {
        Box {
            AlbumArtwork(track.album, token, Modifier.fillMaxWidth().aspectRatio(1f))
            Icon(Icons.Filled.Favorite, null, tint = accent, modifier = Modifier.align(Alignment.TopEnd).padding(7.dp))
        }
        Text(track.title, color = Color.White, fontSize = 10.sp, fontWeight = FontWeight.Bold, maxLines = 1, overflow = TextOverflow.Ellipsis, modifier = Modifier.padding(top = 5.dp))
    }
}

private fun musicArtworkRequest(path: String?, token: String?, context: android.content.Context): Any? {
    if (path.isNullOrBlank()) return null
    val url = if (path.startsWith("http")) path else "${ServerManager.activeServerUrl}$path"
    return ImageRequest.Builder(context).data(url).apply { if (!token.isNullOrBlank()) addHeader("Authorization", "Bearer $token") }.build()
}
