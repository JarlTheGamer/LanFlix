package com.lanflix.ui.compose.screens

import android.content.Context
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lanflix.api.LanflixApiClient
import com.lanflix.auth.LanflixAccount
import com.lanflix.ui.compose.LanflixBackground
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

@Composable
fun EditProfileScreen(
    account: LanflixAccount?,
    onBack: () -> Unit,
    onProfileUpdated: () -> Unit = {}
) {
    val context = LocalContext.current
    val api = remember(context) { LanflixApiClient.getInstance(context) }
    val scope = rememberCoroutineScope()

    var displayName by remember { mutableStateOf(account?.displayName.orEmpty()) }
    var bio by remember { mutableStateOf("") }
    var defaultVisibility by remember { mutableStateOf("Friends") }
    val visibilities = listOf("Friends", "Server", "Household")

    var avatarVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var backdropVersion by remember { mutableStateOf(System.currentTimeMillis()) }
    var isSaving by remember { mutableStateOf(false) }
    var statusMessage by remember { mutableStateOf<String?>(null) }

    val avatarUrl = account?.id?.let { "${ServerManager.activeServerUrl}/api/v2/accounts/$it/avatar?t=$avatarVersion" }
    val backdropUrl = account?.id?.let { "${ServerManager.activeServerUrl}/api/v2/accounts/$it/backdrop?t=$backdropVersion" }

    val avatarLauncher = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            scope.launch(Dispatchers.IO) {
                val bytes = context.contentResolver.openInputStream(it)?.use { stream -> stream.readBytes() }
                if (bytes != null && api.uploadAvatar(bytes)) {
                    avatarVersion = System.currentTimeMillis()
                    statusMessage = "Avatar updated!"
                }
            }
        }
    }

    val backdropLauncher = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            scope.launch(Dispatchers.IO) {
                val bytes = context.contentResolver.openInputStream(it)?.use { stream -> stream.readBytes() }
                if (bytes != null && api.uploadBackdrop(bytes)) {
                    backdropVersion = System.currentTimeMillis()
                    statusMessage = "Banner updated!"
                }
            }
        }
    }

    Column(
        Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF1E102A), LanflixBackground)))
    ) {
        // Header
        Row(
            Modifier
                .fillMaxWidth()
                .statusBarsPadding()
                .height(60.dp)
                .padding(horizontal = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }
            Text(
                "Edit Profile",
                color = Color.White,
                fontSize = 20.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(1f)
            )
            Button(
                onClick = {
                    scope.launch {
                        isSaving = true
                        api.updatePrivacy(defaultVisibility, activityEnabled = true)
                        isSaving = false
                        onProfileUpdated()
                        onBack()
                    }
                },
                enabled = !isSaving,
                colors = ButtonDefaults.buttonColors(containerColor = LanflixGold, contentColor = Color.Black),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 6.dp)
            ) {
                Text("Save", fontWeight = FontWeight.Bold)
            }
        }

        Column(
            Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(20.dp)
        ) {
            // Banner / Background Card
            Card(
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.06f)),
                shape = RoundedCornerShape(16.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(Modifier.padding(14.dp)) {
                    Text("Profile Banner", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                    Text("Customize your profile background", color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(bottom = 10.dp))

                    Box(
                        Modifier
                            .fillMaxWidth()
                            .height(130.dp)
                            .clip(RoundedCornerShape(12.dp))
                            .background(Color.White.copy(alpha = 0.08f))
                            .clickable { backdropLauncher.launch("image/*") },
                        contentAlignment = Alignment.Center
                    ) {
                        if (!backdropUrl.isNullOrBlank()) {
                            AsyncImage(
                                model = backdropUrl,
                                contentDescription = "Banner",
                                modifier = Modifier.fillMaxSize(),
                                contentScale = ContentScale.Crop
                            )
                        }
                        Box(
                            Modifier
                                .fillMaxSize()
                                .background(Color.Black.copy(alpha = 0.35f)),
                            contentAlignment = Alignment.Center
                        ) {
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Icon(Icons.Default.CameraAlt, null, tint = Color.White, modifier = Modifier.size(18.dp))
                                Text(" Change Banner", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Medium)
                            }
                        }
                    }
                }
            }

            // Avatar Card
            Card(
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.06f)),
                shape = RoundedCornerShape(16.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    Modifier
                        .padding(16.dp)
                        .fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Box(
                        Modifier
                            .size(76.dp)
                            .clip(CircleShape)
                            .background(Color.White.copy(alpha = 0.12f))
                            .clickable { avatarLauncher.launch("image/*") },
                        contentAlignment = Alignment.Center
                    ) {
                        AsyncImage(
                            model = avatarUrl,
                            contentDescription = "Avatar",
                            modifier = Modifier
                                .fillMaxSize()
                                .clip(CircleShape),
                            contentScale = ContentScale.Crop
                        )
                        Box(
                            Modifier
                                .fillMaxSize()
                                .background(Color.Black.copy(alpha = 0.3f)),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(Icons.Default.CameraAlt, null, tint = Color.White, modifier = Modifier.size(20.dp))
                        }
                    }

                    Column(Modifier.padding(start = 16.dp).weight(1f)) {
                        Text("Profile Avatar", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                        Text("Tap to upload new profile photo", color = LanflixMuted, fontSize = 12.sp)
                        OutlinedButton(
                            onClick = { avatarLauncher.launch("image/*") },
                            modifier = Modifier.padding(top = 8.dp),
                            shape = RoundedCornerShape(20.dp),
                            colors = ButtonDefaults.outlinedButtonColors(contentColor = LanflixGold)
                        ) {
                            Text("Upload Photo", fontSize = 12.sp)
                        }
                    }
                }
            }

            // Profile Information Form
            Card(
                colors = CardDefaults.cardColors(containerColor = Color.White.copy(alpha = 0.06f)),
                shape = RoundedCornerShape(16.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
                    Text("Personal Details", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 15.sp)

                    OutlinedTextField(
                        value = displayName,
                        onValueChange = { displayName = it },
                        label = { Text("Display Name") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = LanflixGold,
                            unfocusedBorderColor = Color.White.copy(alpha = 0.2f),
                            focusedTextColor = Color.White,
                            unfocusedTextColor = Color.White
                        )
                    )

                    OutlinedTextField(
                        value = bio,
                        onValueChange = { bio = it },
                        label = { Text("About / Status") },
                        placeholder = { Text("Share what you're currently watching...") },
                        modifier = Modifier.fillMaxWidth().height(80.dp),
                        maxLines = 3,
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = LanflixGold,
                            unfocusedBorderColor = Color.White.copy(alpha = 0.2f),
                            focusedTextColor = Color.White,
                            unfocusedTextColor = Color.White
                        )
                    )

                    Text("Default Activity Visibility", color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(top = 6.dp))
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        visibilities.forEach { vis ->
                            FilterChip(
                                selected = defaultVisibility == vis,
                                onClick = { defaultVisibility = vis },
                                label = { Text(vis, fontSize = 12.sp) },
                                colors = FilterChipDefaults.filterChipColors(
                                    selectedContainerColor = LanflixGold,
                                    selectedLabelColor = Color.Black
                                )
                            )
                        }
                    }
                }
            }

            statusMessage?.let { msg ->
                Text(msg, color = LanflixGold, fontSize = 13.sp, fontWeight = FontWeight.Medium)
            }
        }
    }
}
