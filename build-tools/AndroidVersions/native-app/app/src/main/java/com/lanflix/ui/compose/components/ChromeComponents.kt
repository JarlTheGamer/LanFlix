package com.lanflix.ui.compose.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Cast
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import coil.request.ImageRequest
import coil.request.CachePolicy
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.layout.ContentScale
import com.lanflix.auth.LanflixAccount
import com.lanflix.webview.ServerManager
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.navigation.Destination

@Composable
fun TopChrome(title: String, online: Boolean, account: LanflixAccount?, onSearch: () -> Unit, onProfile: () -> Unit, onCast: () -> Unit) {
    val context = LocalContext.current
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
                AsyncImage(
                    model = account?.id?.let {
                        ImageRequest.Builder(context)
                            .data("${ServerManager.activeServerUrl}/api/v2/accounts/$it/avatar")
                            .memoryCachePolicy(CachePolicy.DISABLED)
                            .build()
                    },
                    contentDescription = "Profile", contentScale = ContentScale.Crop,
                    modifier = Modifier.size(27.dp).clip(CircleShape).background(LanflixGold)
                )
            }
        }
    }
}

@Composable
fun CompactHeaderAction(icon: ImageVector, label: String, onClick: () -> Unit = {}) {
    Box(Modifier.size(38.dp).clickable(onClick = onClick), contentAlignment = Alignment.Center) {
        Icon(icon, label, tint = Color.White, modifier = Modifier.size(18.dp))
    }
}

@Composable
fun BottomChrome(selected: Destination, onSelect: (Destination) -> Unit) {
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
