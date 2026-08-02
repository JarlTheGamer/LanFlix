package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Download
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.ui.compose.components.EmptyState
import com.lanflix.ui.compose.components.PosterCard
import java.io.File

@Composable
fun DownloadsScreen(media: List<ContentItem>, onSelect: (ContentItem) -> Unit) {
    val downloads = media.filter { it.isOfflinePlayable && it.localFilePath?.let(::File)?.isFile == true }
    Column(Modifier.fillMaxSize().padding(top = 82.dp, bottom = 68.dp, start = 14.dp, end = 14.dp)) {
        Text("On Demand", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
        Text("Device downloads and server requests", color = LanflixMuted, fontSize = 12.sp)
        Row(Modifier.fillMaxWidth().padding(top = 18.dp).clip(RoundedCornerShape(14.dp)).background(Color.White.copy(alpha = .06f)).padding(16.dp)) {
            Icon(Icons.Filled.Download, null, tint = LanflixGold)
            Column(Modifier.padding(start = 12.dp)) { Text("${downloads.size} downloaded", color = Color.White, fontWeight = FontWeight.Bold); Text("Available without the server", color = LanflixMuted, fontSize = 11.sp) }
        }
        if (downloads.isEmpty()) EmptyState("Nothing downloaded", "Download a movie or episode to watch when your server is offline.")
        else LazyRow(contentPadding = PaddingValues(top = 22.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) { items(downloads) { PosterCard(it, onSelect) } }
    }
}
