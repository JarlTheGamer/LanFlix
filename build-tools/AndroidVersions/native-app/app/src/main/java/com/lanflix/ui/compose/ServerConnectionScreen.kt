package com.lanflix.ui.compose

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.settings.ServerConnectionState

@Composable
fun ServerConnectionScreen(
    state: ServerConnectionState,
    onBack: () -> Unit,
    onRefresh: () -> Unit,
    onConnect: (String) -> Unit,
    onRemove: (String) -> Unit,
    onContinueOffline: () -> Unit
) {
    var manualUrl by remember(state.currentServer) { mutableStateOf(state.currentServer) }
    Box(
        Modifier.fillMaxSize().background(
            Brush.verticalGradient(listOf(Color(0xFF123D4B), Color(0xFF13232C), Color(0xFF05070A)), endY = 1500f)
        )
    ) {
        Box(Modifier.fillMaxWidth().height(360.dp).background(Brush.radialGradient(listOf(Color(0x8840C8B8), Color.Transparent), radius = 650f)))
        LazyColumn(contentPadding = PaddingValues(start = 18.dp, end = 18.dp, bottom = 40.dp)) {
            item {
                Row(Modifier.fillMaxWidth().statusBarsPadding().height(64.dp), verticalAlignment = Alignment.CenterVertically) {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }
                    Text("Server connection", color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
                    IconButton(onClick = onRefresh) { Icon(Icons.Default.Refresh, "Scan again", tint = Color.White) }
                }
                Text("Your Lanflix server", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.ExtraBold, modifier = Modifier.padding(top = 22.dp))
                Text("Choose a server on your network or enter its address. LAN HTTP connections are marked as insecure.", color = Color.White.copy(.68f), fontSize = 13.sp, lineHeight = 19.sp, modifier = Modifier.padding(top = 8.dp, bottom = 24.dp))
            }

            item {
                SectionLabel("DISCOVERED AND SAVED")
                if (state.scanning && state.servers.isEmpty()) {
                    GlassCard { Row(Modifier.padding(18.dp), verticalAlignment = Alignment.CenterVertically) { CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp, color = Color(0xFFFFB21A)); Text("Scanning your network…", color = Color.White.copy(.75f), modifier = Modifier.padding(start = 14.dp)) } }
                }
            }

            items(state.servers.size) { index ->
                val server = state.servers[index]
                GlassCard(Modifier.padding(bottom = 9.dp).clickable(enabled = server.online && state.connectingUrl == null) { onConnect(server.url) }) {
                    Row(Modifier.fillMaxWidth().padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                        Box(Modifier.size(42.dp).clip(CircleShape).background(if (server.online) Color(0x224FD89B) else Color(0x22E69A49)), contentAlignment = Alignment.Center) {
                            Icon(Icons.Default.Storage, null, tint = if (server.online) Color(0xFF69E0A9) else Color(0xFFE8A858))
                        }
                        Column(Modifier.padding(start = 12.dp).weight(1f)) {
                            Text(server.name, color = Color.White, fontWeight = FontWeight.SemiBold, maxLines = 1, overflow = TextOverflow.Ellipsis)
                            Text(server.url, color = Color.White.copy(.56f), fontSize = 11.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Icon(if (server.url.startsWith("https://")) Icons.Default.Lock else Icons.Default.Warning, null, tint = if (server.url.startsWith("https://")) Color(0xFF69E0A9) else Color(0xFFE8A858), modifier = Modifier.size(12.dp))
                                Text(if (server.online) "  Online" else "  Offline", color = Color.White.copy(.56f), fontSize = 10.sp)
                            }
                        }
                        if (state.connectingUrl == server.url) CircularProgressIndicator(Modifier.size(22.dp), strokeWidth = 2.dp, color = Color(0xFFFFB21A))
                        else IconButton(onClick = { onRemove(server.url) }) { Icon(Icons.Default.DeleteOutline, "Remove saved server", tint = Color.White.copy(.54f)) }
                    }
                }
            }

            item {
                SectionLabel("CONNECT MANUALLY", Modifier.padding(top = 18.dp))
                GlassCard {
                    Column(Modifier.padding(15.dp)) {
                        OutlinedTextField(
                            value = manualUrl,
                            onValueChange = { manualUrl = it },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            label = { Text("Server address") },
                            placeholder = { Text("192.168.1.50:5037") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                            colors = OutlinedTextFieldDefaults.colors(focusedTextColor = Color.White, unfocusedTextColor = Color.White, focusedBorderColor = Color(0xFFFFB21A), unfocusedBorderColor = Color.White.copy(.22f), focusedLabelColor = Color(0xFFFFB21A), unfocusedLabelColor = Color.White.copy(.55f))
                        )
                        Button(
                            onClick = { onConnect(manualUrl) },
                            enabled = manualUrl.isNotBlank() && state.connectingUrl == null,
                            modifier = Modifier.fillMaxWidth().height(50.dp).padding(top = 8.dp),
                            shape = RoundedCornerShape(25.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black)
                        ) { Icon(Icons.Default.Link, null); Text("Connect", fontWeight = FontWeight.Bold, modifier = Modifier.padding(start = 8.dp)) }
                    }
                }
                if (state.hasOfflineMedia) TextButton(onClick = onContinueOffline, modifier = Modifier.fillMaxWidth().padding(top = 12.dp)) { Icon(Icons.Default.CloudOff, null); Text("Continue with offline downloads", modifier = Modifier.padding(start = 8.dp)) }
            }
        }
    }
}

@Composable private fun SectionLabel(text: String, modifier: Modifier = Modifier) { Text(text, color = Color(0xFFFFB21A), fontSize = 11.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.sp, modifier = modifier.padding(bottom = 9.dp, start = 3.dp)) }
@Composable private fun GlassCard(modifier: Modifier = Modifier, content: @Composable () -> Unit) { Surface(modifier, shape = RoundedCornerShape(18.dp), color = Color.White.copy(.075f), tonalElevation = 2.dp, content = content) }
