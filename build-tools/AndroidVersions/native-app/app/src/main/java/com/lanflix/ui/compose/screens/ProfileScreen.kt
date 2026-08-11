package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.People
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Surface
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
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.SocialActivity
import com.lanflix.api.WatchHistoryItem
import com.lanflix.auth.LanflixAccount
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.components.MediaShelf
import com.lanflix.ui.compose.components.Stat
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

@Composable
fun ProfileScreen(
    library: List<ContentItem>,
    account: LanflixAccount?,
    activity: List<SocialActivity>,
    pendingRequests: Int = 0,
    onBack: () -> Unit,
    onSelect: (ContentItem) -> Unit,
    onAccount: () -> Unit,
    onActivity: () -> Unit,
    onFriends: () -> Unit = {},
    onEditProfile: () -> Unit = onAccount
) {
    val context = LocalContext.current
    val api = remember(context) { LanflixApiClient.getInstance(context) }
    val scope = rememberCoroutineScope()
    var avatarVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var backdropVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var watchHistory by remember { mutableStateOf<List<WatchHistoryItem>>(emptyList()) }

    LaunchedEffect(Unit) {
        scope.launch(Dispatchers.IO) {
            val history = api.getWatchHistory()
            watchHistory = history
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
                            Modifier.size(94.dp).clip(CircleShape).background(Color.White.copy(alpha = .12f)),
                            contentAlignment = Alignment.Center
                        ) {
                            AsyncImage(
                                model = customAvatarUrl,
                                contentDescription = "Avatar",
                                modifier = Modifier.fillMaxSize().clip(CircleShape),
                                contentScale = ContentScale.Crop
                            )
                        }
                        Text(
                            account?.displayName ?: "Offline account",
                            color = Color.White, fontSize = 25.sp, fontWeight = FontWeight.ExtraBold,
                            modifier = Modifier.padding(top = 10.dp)
                        )
                        Text(
                            account?.let { "@${it.username}" } ?: "Cached downloads",
                            color = Color.White.copy(alpha = .6f), fontSize = 12.sp
                        )

                        Row(Modifier.padding(top = 14.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                            // Edit Profile button
                            Surface(
                                onClick = onEditProfile,
                                shape = RoundedCornerShape(20.dp),
                                color = Color.White.copy(alpha = 0.15f)
                            ) {
                                Row(
                                    Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Icon(Icons.Filled.Person, null, tint = Color.White, modifier = Modifier.size(14.dp))
                                    Text(" Edit Profile", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Medium)
                                }
                            }
                            // Friends / Requests button
                            Surface(
                                onClick = onFriends,
                                shape = RoundedCornerShape(20.dp),
                                color = Color.White.copy(alpha = 0.15f)
                            ) {
                                Row(
                                    Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Icon(androidx.compose.material.icons.Icons.Filled.People, null, tint = Color.White, modifier = Modifier.size(14.dp))
                                    Text(
                                        if (pendingRequests > 0) " Requests ($pendingRequests)" else " Friends",
                                        color = if (pendingRequests > 0) LanflixGold else Color.White,
                                        fontSize = 13.sp, fontWeight = FontWeight.Medium
                                    )
                                }
                            }
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
                    Text("Watch History", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 18.sp, modifier = Modifier.padding(horizontal = 16.dp, vertical = 10.dp))
                    LazyRow(contentPadding = PaddingValues(horizontal = 16.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                        items(watchHistory, key = { it.id }) { history ->
                            val matchingContent = library.firstOrNull { it.id == history.mediaId } 
                                ?: ContentItem(id = history.mediaId, title = history.title, backdropUrl = history.backdropUrl, type = history.kind)
                            Column(Modifier.width(130.dp).clickable { onSelect(matchingContent) }) {
                                Box(Modifier.fillMaxWidth().aspectRatio(16f / 9f).clip(RoundedCornerShape(10.dp)).background(Color.White.copy(alpha = .08f))) {
                                    AsyncImage(
                                        model = history.backdropUrl?.let { if (it.startsWith("http")) it else "${ServerManager.activeServerUrl}$it" } ?: matchingContent.resolvedPosterUrl,
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
                    val fallbackItems = library.filter { (it.progressPercentage ?: 0.0) > 0.0 }.ifEmpty { library.take(8) }
                    if (fallbackItems.isNotEmpty()) {
                        Text("Watch History", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 18.sp, modifier = Modifier.padding(horizontal = 16.dp, vertical = 10.dp))
                        MediaShelf("Continue Watching", fallbackItems, onSelect)
                    }
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
