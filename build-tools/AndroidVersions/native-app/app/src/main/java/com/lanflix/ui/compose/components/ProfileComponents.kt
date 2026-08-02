package com.lanflix.ui.compose.components

import android.content.Intent
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Storage
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.webview.ServerBrowserActivity

@Composable
fun ProfileMenu(
    online: Boolean,
    onProfile: () -> Unit,
    onDownloads: () -> Unit,
    onSettings: () -> Unit
) {
    val context = LocalContext.current
    Surface(
        modifier = Modifier.width(52.dp),
        shape = RoundedCornerShape(26.dp),
        color = Color(0xF7132634),
        shadowElevation = 18.dp,
        tonalElevation = 6.dp
    ) {
        Column(Modifier.padding(vertical = 4.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            ProfilePillAction(Icons.Filled.Person, "Open profile", selected = true, onClick = onProfile)
            ProfilePillAction(Icons.Filled.Download, "Downloads", onClick = onDownloads)
            Box {
                ProfilePillAction(Icons.Filled.Storage, "Connect to server") {
                    context.startActivity(Intent(context, ServerBrowserActivity::class.java))
                }
                Box(
                    Modifier.align(Alignment.TopEnd).padding(top = 7.dp, end = 7.dp).size(7.dp).clip(CircleShape)
                        .background(if (online) Color(0xFF58C878) else Color(0xFFE59A44))
                )
            }
            Box(Modifier.width(28.dp).height(1.dp).background(Color.White.copy(alpha = .12f)))
            ProfilePillAction(Icons.Filled.Settings, "Settings", onClick = onSettings)
        }
    }
}

@Composable
fun ProfilePillAction(icon: ImageVector, description: String, selected: Boolean = false, onClick: () -> Unit) {
    Box(Modifier.size(48.dp).clip(CircleShape).clickable(onClick = onClick), contentAlignment = Alignment.Center) {
        if (selected) Box(Modifier.size(34.dp).clip(CircleShape).background(LanflixGold.copy(alpha = .16f)))
        Icon(icon, description, tint = if (selected) LanflixGold else Color.White.copy(alpha = .88f), modifier = Modifier.size(21.dp))
    }
}

@Composable
fun ProfileMenuRow(icon: ImageVector, title: String, subtitle: String, onClick: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().clip(RoundedCornerShape(12.dp)).clickable(onClick = onClick).padding(horizontal = 11.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, null, tint = Color.White.copy(alpha = .86f), modifier = Modifier.size(21.dp))
        Column(Modifier.padding(start = 12.dp).weight(1f)) {
            Text(title, color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
            Text(subtitle, color = LanflixMuted, fontSize = 9.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
        }
    }
}

@Composable
fun Stat(value: String, label: String) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(value, color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
        Text(label, color = LanflixMuted, fontSize = 11.sp)
    }
}
