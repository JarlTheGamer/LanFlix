package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Tv
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.api.LiveTvChannel
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.components.EmptyState

@Composable
fun LiveTvScreen(online: Boolean, channels: List<LiveTvChannel>) {
    LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(top = 82.dp, bottom = 90.dp, start = 14.dp, end = 14.dp)) {
        item {
            Text(if (online) "Guide  •  What’s on now" else "Guide unavailable offline", color = LanflixMuted, fontSize = 12.sp)
            Box(Modifier.fillMaxWidth().height(190.dp).padding(top = 16.dp).clip(RoundedCornerShape(16.dp)).background(Brush.linearGradient(listOf(Color(0xFF17485A), Color(0xFF0A1D29))))) {
                Column(Modifier.align(Alignment.BottomStart).padding(18.dp)) {
                    Text("Your channels, one beautiful guide", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold)
                    Text("Add an M3U/XMLTV source or HDHomeRun tuner in server settings.", color = Color.White.copy(alpha = .72f), fontSize = 12.sp)
                }
            }
        }
        item { Text("${channels.size} channels", color = Color.White, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 24.dp, bottom = 10.dp)) }
        if (online && channels.isEmpty()) item { EmptyState("No Live TV sources", "Add M3U/XMLTV or HDHomeRun in server administration.") }
        items(channels, key = { it.id }) { channel ->
            Row(Modifier.fillMaxWidth().padding(vertical = 4.dp).clip(RoundedCornerShape(10.dp)).background(Color.White.copy(alpha = .06f)).padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.size(42.dp).clip(RoundedCornerShape(8.dp)).background(Color.White.copy(alpha = .09f)), contentAlignment = Alignment.Center) { Icon(Icons.Filled.Tv, null, tint = LanflixGold) }
                Column(Modifier.padding(start = 12.dp).weight(1f)) { Text("${channel.number}  ${channel.name}", color = Color.White); Text(channel.now?.title ?: "No guide data", color = LanflixMuted, fontSize = 11.sp) }
                Icon(Icons.Filled.PlayArrow, null, tint = Color.White)
            }
        }
    }
}
