package com.lanflix.android.ui.discovery

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.lanflix.android.domain.model.ServerInfo
import com.lanflix.android.ui.theme.LanflixTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ServerDiscoveryScreen(
    onServerSelected: (ServerInfo) -> Unit,
    viewModel: ServerDiscoveryViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var showManualInput by remember { mutableStateOf(false) }
    var manualUrl by remember { mutableStateOf("") }
    
    LaunchedEffect(Unit) {
        viewModel.startDiscovery()
    }
    
    LanflixTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0xFF050505)) // Exact body background from CSS
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                // Header - matching your web design style
                Text(
                    text = "Find Your Lanflix Server",
                    color = Color.White,
                    fontSize = 48.sp, // Larger, more prominent
                    fontWeight = FontWeight.ExtraBold, // font-weight: 800 like profile title
                    textAlign = TextAlign.Center,
                    letterSpacing = (-0.02).em,
                    modifier = Modifier.padding(bottom = 16.dp)
                )
                
                Text(
                    text = "Connect to your media server to continue",
                    color = Color(0xC7FFFFFF), // --text-secondary
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Normal,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.padding(bottom = 48.dp)
                )
                
                // Auto-discovery section - matching spotlight styling
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = 24.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = Color(0x99141417) // rgba(15, 15, 16, 0.6) like spotlight
                    ),
                    shape = RoundedCornerShape(32.dp), // border-radius: 32px like spotlight
                    border = androidx.compose.foundation.BorderStroke(
                        1.dp, 
                        Color(0x0AFFFFFF) // rgba(255, 255, 255, 0.04) - inset border
                    )
                ) {
                    Column(
                        modifier = Modifier.padding(horizontal = 36.dp, vertical = 32.dp) // spotlight padding
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = "Discovered Servers",
                                color = Color.White,
                                fontSize = 25.sp, // 1.6rem like spotlight h2
                                fontWeight = FontWeight.SemiBold // font-weight: 600
                            )
                            
                            IconButton(
                                onClick = { viewModel.refreshDiscovery() },
                                modifier = Modifier
                                    .background(
                                        Color(0x12FFFFFF), // rgba(255, 255, 255, 0.06) like tab background
                                        RoundedCornerShape(10.dp)
                                    )
                            ) {
                                Icon(
                                    Icons.Default.Refresh,
                                    contentDescription = "Refresh",
                                    tint = Color(0xC7FFFFFF) // --text-secondary
                                )
                            }
                        }
                        
                        Spacer(modifier = Modifier.height(16.dp))
                        
                        when {
                            uiState.isDiscovering -> {
                                Row(
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    CircularProgressIndicator(
                                        modifier = Modifier.size(20.dp),
                                        color = Color(0xFFe50914), // --accent
                                        strokeWidth = 2.dp
                                    )
                                    Spacer(modifier = Modifier.width(12.dp))
                                    Text(
                                        text = "Searching for servers...",
                                        color = Color(0x99FFFFFF), // --text-muted
                                        fontSize = 14.sp
                                    )
                                }
                            }
                            
                            uiState.discoveredServers.isEmpty() -> {
                                Text(
                                    text = "No servers found on your network.\nMake sure your Lanflix server is running.",
                                    color = Color(0x99FFFFFF), // --text-muted
                                    fontSize = 14.sp,
                                    textAlign = TextAlign.Center,
                                    modifier = Modifier.fillMaxWidth()
                                )
                            }
                            
                            else -> {
                                LazyColumn(
                                    verticalArrangement = Arrangement.spacedBy(8.dp)
                                ) {
                                    items(uiState.discoveredServers) { server ->
                                        ServerItem(
                                            server = server,
                                            onClick = { onServerSelected(server) }
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Manual input section
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = Color(0xFF1A1A1A)
                    ),
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(20.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = "Manual Connection",
                                color = Color.White,
                                fontSize = 18.sp,
                                fontWeight = FontWeight.Medium
                            )
                            
                            IconButton(
                                onClick = { showManualInput = !showManualInput }
                            ) {
                                Icon(
                                    if (showManualInput) Icons.Default.Search else Icons.Default.Add,
                                    contentDescription = if (showManualInput) "Connect" else "Add manually",
                                    tint = Color.White
                                )
                            }
                        }
                        
                        if (showManualInput) {
                            Spacer(modifier = Modifier.height(16.dp))
                            
                            OutlinedTextField(
                                value = manualUrl,
                                onValueChange = { manualUrl = it },
                                label = { Text("Server URL", color = Color.Gray) },
                                placeholder = { Text("http://192.168.1.100:5037", color = Color.Gray) },
                                modifier = Modifier.fillMaxWidth(),
                                colors = OutlinedTextFieldDefaults.colors(
                                    focusedTextColor = Color.White,
                                    unfocusedTextColor = Color.White,
                                    focusedBorderColor = MaterialTheme.colorScheme.primary,
                                    unfocusedBorderColor = Color.Gray
                                ),
                                keyboardOptions = KeyboardOptions(
                                    keyboardType = KeyboardType.Uri
                                ),
                                singleLine = true
                            )
                            
                            Spacer(modifier = Modifier.height(16.dp))
                            
                            Button(
                                onClick = {
                                    if (manualUrl.isNotBlank()) {
                                        viewModel.connectToManualServer(manualUrl)
                                    }
                                },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = manualUrl.isNotBlank() && !uiState.isConnecting
                            ) {
                                if (uiState.isConnecting) {
                                    CircularProgressIndicator(
                                        modifier = Modifier.size(16.dp),
                                        color = Color.White,
                                        strokeWidth = 2.dp
                                    )
                                    Spacer(modifier = Modifier.width(8.dp))
                                }
                                Text("Connect to Server")
                            }
                            
                            if (uiState.connectionError != null) {
                                Spacer(modifier = Modifier.height(8.dp))
                                Text(
                                    text = uiState.connectionError ?: "Connection error",
                                    color = Color.Red,
                                    fontSize = 12.sp
                                )
                            }
                        }
                    }
                }
                
                Spacer(modifier = Modifier.height(24.dp))
                
                // Help text
                Text(
                    text = "Make sure you're connected to the same network as your Lanflix server",
                    color = Color.Gray,
                    fontSize = 12.sp,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }
    }
}

@Composable
private fun ServerItem(
    server: ServerInfo,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable { onClick() },
        colors = CardDefaults.cardColors(
            containerColor = Color(0x0AFFFFFF) // rgba(255, 255, 255, 0.04) like movie cards
        ),
        shape = RoundedCornerShape(16.dp), // More rounded like movie cards
        border = androidx.compose.foundation.BorderStroke(
            1.dp, 
            Color(0x05FFFFFF) // rgba(255, 255, 255, 0.02) - subtle border
        )
    ) {
        Column(
            modifier = Modifier.padding(18.dp) // Slightly more padding
        ) {
            Text(
                text = server.name,
                color = Color.White,
                fontSize = 16.sp,
                fontWeight = FontWeight.SemiBold // font-weight: 600
            )
            
            Spacer(modifier = Modifier.height(8.dp))
            
            Text(
                text = server.baseUrl,
                color = Color(0x99FFFFFF), // --text-muted
                fontSize = 14.sp
            )
            
            if (server.version.isNotEmpty()) {
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Version: ${server.version}",
                    color = Color(0xC7FFFFFF), // --text-secondary
                    fontSize = 12.sp
                )
            }
        }
    }
}