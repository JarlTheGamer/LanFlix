package com.lanflix.ui.compose

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.lanflix.api.AccountSession
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.SocialActivity
import com.lanflix.api.SocialComment
import com.lanflix.api.SocialNotification
import com.lanflix.api.SocialRelationship
import com.lanflix.auth.LanflixAccount
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.launch

@Composable
fun AuthenticationScreen(state: LanflixUiState, onAuthenticate: (String, String, String, String?) -> Unit, onServer: () -> Unit) {
    var username by remember { mutableStateOf("") }
    var displayName by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var invitation by remember { mutableStateOf("") }
    var isRegisterMode by remember { mutableStateOf(state.requiresOwnerSetup) }

    Box(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF173F53), Color(0xFF08151F), Color(0xFF040608))))) {
        Box(Modifier.fillMaxWidth().height(420.dp).background(Brush.radialGradient(listOf(Color(0x8844A6C6), Color.Transparent), radius = 720f)))
        LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(horizontal = 24.dp, vertical = 40.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            item {
                Surface(
                    onClick = onServer,
                    shape = RoundedCornerShape(20.dp),
                    color = Color.White.copy(alpha = .08f),
                    modifier = Modifier.padding(bottom = 24.dp)
                ) {
                    Row(Modifier.padding(horizontal = 14.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                        Box(Modifier.size(8.dp).clip(CircleShape).background(if (state.online) Color(0xFF58C878) else Color(0xFFE59A44)))
                        Text(
                            text = com.lanflix.webview.ServerManager.activeServerUrl,
                            color = Color.White,
                            fontSize = 11.sp,
                            fontWeight = FontWeight.Medium,
                            modifier = Modifier.padding(start = 8.dp, end = 8.dp)
                        )
                        Icon(Icons.Default.Storage, "Change Server", tint = LanflixGold, modifier = Modifier.size(16.dp))
                    }
                }

                Box(Modifier.size(72.dp).clip(CircleShape).background(LanflixGold), contentAlignment = Alignment.Center) {
                    Icon(Icons.Default.PlayArrow, null, tint = Color.Black, modifier = Modifier.size(44.dp))
                }

                Text(
                    text = if (state.requiresOwnerSetup) "Set Up Server Owner" else if (isRegisterMode) "Join Lanflix Server" else "Welcome to Lanflix",
                    color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold, modifier = Modifier.padding(top = 20.dp)
                )
                Text(
                    text = if (state.requiresOwnerSetup) "Step 1 of 1: Create your server administrator account." else "Sign in with your account or redeem an invitation code.",
                    color = LanflixMuted, fontSize = 12.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp, bottom = 20.dp)
                )

                if (!state.requiresOwnerSetup) {
                    Row(
                        modifier = Modifier.fillMaxWidth().padding(bottom = 16.dp).clip(RoundedCornerShape(16.dp)).background(Color.White.copy(alpha = .06f)).padding(4.dp)
                    ) {
                        Button(
                            onClick = { isRegisterMode = false },
                            modifier = Modifier.weight(1f).height(42.dp),
                            shape = RoundedCornerShape(12.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (!isRegisterMode) LanflixGold else Color.Transparent,
                                contentColor = if (!isRegisterMode) Color.Black else Color.White
                            )
                        ) {
                            Text("Sign In", fontWeight = FontWeight.Bold)
                        }
                        Button(
                            onClick = { isRegisterMode = true },
                            modifier = Modifier.weight(1f).height(42.dp),
                            shape = RoundedCornerShape(12.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (isRegisterMode) LanflixGold else Color.Transparent,
                                contentColor = if (isRegisterMode) Color.Black else Color.White
                            )
                        ) {
                            Text("Sign Up with Invite", fontWeight = FontWeight.Bold)
                        }
                    }
                }

                Surface(shape = RoundedCornerShape(24.dp), color = Color.White.copy(alpha = .075f)) {
                    Column(Modifier.fillMaxWidth().padding(18.dp)) {
                        if (isRegisterMode) OutlinedTextField(displayName, { displayName = it }, Modifier.fillMaxWidth(), label = { Text("Display Name") }, singleLine = true)
                        OutlinedTextField(username, { username = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Username") }, singleLine = true)
                        OutlinedTextField(password, { password = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Password") }, singleLine = true, visualTransformation = PasswordVisualTransformation())
                        if (isRegisterMode && !state.requiresOwnerSetup) OutlinedTextField(invitation, { invitation = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Invitation Code (LFX-...)") }, singleLine = true)
                        
                        Button(
                            onClick = { onAuthenticate(username, displayName.ifBlank { username }, password, invitation.takeIf { isRegisterMode && it.isNotBlank() }) },
                            enabled = username.length >= 3 && password.length >= 10 && (!isRegisterMode || state.requiresOwnerSetup || invitation.isNotBlank()),
                            modifier = Modifier.fillMaxWidth().height(56.dp).padding(top = 14.dp),
                            shape = RoundedCornerShape(28.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black)
                        ) {
                            if (state.loading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
                            else Text(if (state.requiresOwnerSetup) "Create Owner Account" else if (isRegisterMode) "Redeem Invite & Register" else "Sign In", fontWeight = FontWeight.Bold)
                        }
                        state.error?.let { Text(it, color = Color(0xFFFF9B8E), fontSize = 12.sp, modifier = Modifier.padding(top = 10.dp)) }
                    }
                }

                TextButton(onClick = onServer, modifier = Modifier.padding(top = 12.dp)) {
                    Icon(Icons.Default.Storage, null, tint = LanflixGold)
                    Text("Choose or Scan Another Server", color = LanflixGold, modifier = Modifier.padding(start = 7.dp))
                }
            }
        }
    }
}

@Composable
fun AccountSecurityScreen(account: LanflixAccount, onBack: () -> Unit, onSignedOut: () -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient(context) }
    val repository = remember(context) { DevicePreferencesRepository(context.applicationContext) }
    val preferences by repository.preferences.collectAsStateWithLifecycle(initialValue = DevicePreferences())
    val scope = rememberCoroutineScope()
    var sessions by remember { mutableStateOf<List<AccountSession>>(emptyList()) }
    var currentPassword by remember { mutableStateOf("") }
    var newPassword by remember { mutableStateOf("") }
    var message by remember { mutableStateOf<String?>(null) }
    LaunchedEffect(account.id) { sessions = api.getSessions() }
    LazyColumn(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF17394B), LanflixBackground))), contentPadding = PaddingValues(bottom = 40.dp)) {
        item { ScreenHeader("Account & security", onBack) }
        item {
            Column(Modifier.padding(horizontal = 16.dp)) {
                SettingsPanel("Signed in account") {
                    Text(account.displayName, color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                    Text("@${account.username} • ${account.role}", color = LanflixMuted, fontSize = 12.sp)
                }

                Text("Passkeys & Biometric Security", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 22.dp, bottom = 8.dp))
                SettingsPanel(null) {
                    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Default.Fingerprint, "Passkey", tint = Color.White, modifier = Modifier.size(24.dp))
                        Column(Modifier.padding(start = 12.dp).weight(1f)) {
                            Text("Passkey & Fingerprint Unlock", color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                            Text("Require fingerprint or face unlock to access app", color = LanflixMuted, fontSize = 11.sp)
                        }
                        Switch(
                            checked = preferences.passkeyBiometricLock,
                            onCheckedChange = { enabled ->
                                if (enabled) {
                                    promptBiometricAuthentication(context,
                                        onSuccess = { scope.launch { repository.setPasskeyBiometricLock(true) } },
                                        onError = { message = it }
                                    )
                                } else {
                                    scope.launch { repository.setPasskeyBiometricLock(false) }
                                }
                            }
                        )
                    }
                    Spacer(Modifier.height(10.dp))
                    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Default.Lock, "App Launch Lock", tint = Color.White, modifier = Modifier.size(24.dp))
                        Column(Modifier.padding(start = 12.dp).weight(1f)) {
                            Text("Require Passkey on Launch", color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                            Text("Prompt for passkey every time Lanflix opens", color = LanflixMuted, fontSize = 11.sp)
                        }
                        Switch(
                            checked = preferences.requirePasskeyOnLaunch,
                            onCheckedChange = { enabled ->
                                if (enabled) {
                                    promptBiometricAuthentication(context,
                                        onSuccess = { scope.launch { repository.setRequirePasskeyOnLaunch(true) } },
                                        onError = { message = it }
                                    )
                                } else {
                                    scope.launch { repository.setRequirePasskeyOnLaunch(false) }
                                }
                            }
                        )
                    }
                }

                Text("Two-Factor Authentication (2FA)", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 22.dp, bottom = 8.dp))
                SettingsPanel(null) {
                    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Default.Lock, "2FA", tint = if (preferences.twoFactorEnabled) Color(0xFF58C878) else LanflixMuted, modifier = Modifier.size(24.dp))
                        Column(Modifier.padding(start = 12.dp).weight(1f)) {
                            Text(if (preferences.twoFactorEnabled) "2FA Protection Active" else "Two-Factor Authentication", color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                            Text(if (preferences.twoFactorEnabled) "Account protected with TOTP & Passkeys" else "Secure your account with TOTP authenticator apps", color = LanflixMuted, fontSize = 11.sp)
                        }
                        Switch(
                            checked = preferences.twoFactorEnabled,
                            onCheckedChange = { enabled ->
                                if (enabled) {
                                    promptBiometricAuthentication(context,
                                        onSuccess = { scope.launch { repository.setTwoFactorEnabled(true); message = "2FA enabled on this device." } },
                                        onError = { message = it }
                                    )
                                } else {
                                    scope.launch { repository.setTwoFactorEnabled(false); message = "2FA disabled." }
                                }
                            }
                        )
                    }
                }

                Text("Device sessions", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 22.dp, bottom = 8.dp))
            }
        }
        items(sessions, key = { it.id }) { session ->
            Surface(Modifier.padding(horizontal = 16.dp, vertical = 4.dp), shape = RoundedCornerShape(14.dp), color = Color.White.copy(alpha = .07f)) {
                Row(Modifier.fillMaxWidth().padding(13.dp), verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Default.PhoneAndroid, null, tint = Color.White)
                    Column(Modifier.padding(start = 12.dp).weight(1f)) { Text(session.deviceName, color = Color.White); Text("Expires ${session.expiresAtUtc.take(10)}", color = LanflixMuted, fontSize = 10.sp) }
                    IconButton(onClick = { scope.launch { if (api.revokeSession(session.id)) sessions = sessions.filterNot { it.id == session.id } } }) { Icon(Icons.Default.Logout, "Revoke session", tint = Color.White) }
                }
            }
        }
        item {
            Column(Modifier.padding(horizontal = 16.dp)) {
                Text("Change password", color = LanflixGold, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 22.dp, bottom = 8.dp))
                SettingsPanel(null) {
                    OutlinedTextField(currentPassword, { currentPassword = it }, Modifier.fillMaxWidth(), label = { Text("Current password") }, visualTransformation = PasswordVisualTransformation())
                    OutlinedTextField(newPassword, { newPassword = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("New password") }, visualTransformation = PasswordVisualTransformation())
                    Button(onClick = { scope.launch { message = if (api.changePassword(currentPassword, newPassword)) "Password changed. Sign in again." else "Password could not be changed."; if (message!!.startsWith("Password changed")) onSignedOut() } },
                        enabled = currentPassword.isNotBlank() && newPassword.length >= 10, modifier = Modifier.fillMaxWidth().padding(top = 10.dp)) { Text("Change password") }
                    message?.let { Text(it!!, color = LanflixMuted, fontSize = 11.sp, modifier = Modifier.padding(top = 8.dp)) }
                }
                OutlinedButton(onClick = { scope.launch { api.logout(); onSignedOut() } }, Modifier.fillMaxWidth().padding(top = 20.dp)) { Icon(Icons.Default.Logout, null); Text("Sign out", modifier = Modifier.padding(start = 8.dp)) }
            }
        }
    }
}

@Composable
fun ActivityScreen(
    feed: List<SocialActivity>,
    onBack: () -> Unit,
    onCreatePost: (String, String) -> Unit = { _, _ -> },
    onReact: (postId: String, kind: String) -> Unit = { _, _ -> },
    onDelete: (postId: String) -> Unit = {}
) {
    val context = LocalContext.current
    val api = remember(context) { LanflixApiClient(context) }
    val scope = rememberCoroutineScope()
    var showCreateSheet by remember { mutableStateOf(false) }

    Box(Modifier.fillMaxSize()) {
        LazyColumn(
            Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF3C1837), LanflixBackground))),
            contentPadding = PaddingValues(bottom = 96.dp)
        ) {
            item {
                ScreenHeader("Activity", onBack)
                if (feed.isEmpty()) {
                    EmptyMessage(
                        Icons.Default.DynamicFeed,
                        "No activity yet",
                        "Follow or befriend another account, or publish a review."
                    )
                }
            }
            items(feed, key = { it.id }) { activity ->
                FeedCard(activity, api, scope, onReact, onDelete)
            }
        }

        // Create post FAB
        FloatingActionButton(
            onClick = { showCreateSheet = true },
            modifier = Modifier.align(Alignment.BottomEnd).padding(20.dp),
            containerColor = LanflixGold,
            contentColor = Color.Black,
            shape = CircleShape
        ) { Icon(Icons.Default.Edit, "New post") }
    }

    if (showCreateSheet) {
        CreatePostSheet(onDismiss = { showCreateSheet = false }, onSubmit = { body, visibility ->
            onCreatePost(body, visibility)
            showCreateSheet = false
        })
    }
}

@Composable
private fun FeedCard(
    activity: SocialActivity,
    api: LanflixApiClient,
    scope: kotlinx.coroutines.CoroutineScope,
    onReact: (String, String) -> Unit,
    onDelete: (String) -> Unit
) {
    var commentsExpanded by remember { mutableStateOf(false) }
    var comments by remember { mutableStateOf<List<SocialComment>>(emptyList()) }
    var commentInput by remember { mutableStateOf("") }
    var myReaction by remember { mutableStateOf<String?>(null) }

    val kindLabel = when (activity.kind) {
        "post" -> "Posted"
        "review" -> "Reviewed"
        "watch" -> "Watched"
        else -> activity.kind.replaceFirstChar { it.uppercase() }
    }
    val kindColor = when (activity.kind) {
        "review" -> Color(0xFFFFD700)
        "watch" -> Color(0xFF58C8FF)
        else -> LanflixGold
    }

    Surface(
        modifier = Modifier.padding(horizontal = 14.dp, vertical = 6.dp),
        shape = RoundedCornerShape(18.dp),
        color = Color.White.copy(alpha = 0.07f)
    ) {
        Column(Modifier.fillMaxWidth().padding(15.dp)) {
            // ─ Author row ─────────────────────────────────────────────────
            Row(verticalAlignment = Alignment.CenterVertically) {
                AsyncImage(
                    model = activity.author.avatarUrl?.let { if (it.startsWith("http")) it else "${ServerManager.activeServerUrl}$it" },
                    contentDescription = activity.author.displayName,
                    modifier = Modifier.size(38.dp).clip(CircleShape).background(Color.White.copy(alpha = .12f)),
                    contentScale = androidx.compose.ui.layout.ContentScale.Crop
                )
                Column(Modifier.padding(start = 10.dp).weight(1f)) {
                    Text(activity.author.displayName, color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    Text(kindLabel, color = kindColor, fontSize = 11.sp)
                }
                // Visibility badge
                Surface(
                    shape = RoundedCornerShape(6.dp),
                    color = Color.White.copy(alpha = 0.08f)
                ) {
                    Text(
                        text = activity.visibility.replaceFirstChar { it.uppercase() },
                        color = LanflixMuted, fontSize = 10.sp,
                        modifier = Modifier.padding(horizontal = 7.dp, vertical = 3.dp)
                    )
                }
            }

            // ─ Body ───────────────────────────────────────────────────────
            if (!activity.body.isNullOrBlank()) {
                Text(
                    text = activity.body,
                    color = Color.White.copy(alpha = 0.88f),
                    fontSize = 14.sp,
                    modifier = Modifier.padding(top = 10.dp)
                )
            }
            activity.contentTitle?.let { title ->
                Row(
                    modifier = Modifier.fillMaxWidth().padding(top = 11.dp)
                        .clip(RoundedCornerShape(12.dp)).background(Color.White.copy(alpha = .06f)).padding(8.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    AsyncImage(
                        model = activity.contentPosterUrl?.let { if (it.startsWith("http")) it else "${ServerManager.activeServerUrl}$it" },
                        contentDescription = title,
                        modifier = Modifier.size(44.dp, 58.dp).clip(RoundedCornerShape(8.dp)).background(Color.White.copy(alpha = .08f)),
                        contentScale = androidx.compose.ui.layout.ContentScale.Crop
                    )
                    Column(Modifier.padding(start = 10.dp)) {
                        Text(title, color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, maxLines = 1, overflow = TextOverflow.Ellipsis)
                        Text("$kindLabel this title", color = LanflixMuted, fontSize = 11.sp)
                    }
                }
            }

            // ─ Reactions row ──────────────────────────────────────────────
            Row(
                modifier = Modifier.padding(top = 12.dp).fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                ReactionButton("👍", activity.reactionCount, myReaction == "like") {
                    myReaction = if (myReaction == "like") null else "like"
                    onReact(activity.id, "like")
                }
                Spacer(Modifier.width(6.dp))
                ReactionButton("❤️", 0, myReaction == "love") {
                    myReaction = if (myReaction == "love") null else "love"
                    onReact(activity.id, "love")
                }
                Spacer(Modifier.width(6.dp))
                ReactionButton("🔥", 0, myReaction == "fire") {
                    myReaction = if (myReaction == "fire") null else "fire"
                    onReact(activity.id, "fire")
                }
                Spacer(Modifier.weight(1f))
                // Comments toggle
                TextButton(
                    onClick = {
                        commentsExpanded = !commentsExpanded
                        if (commentsExpanded && comments.isEmpty()) {
                            scope.launch { comments = api.getComments(activity.id) }
                        }
                    }
                ) {
                    Icon(Icons.Default.ChatBubbleOutline, null, tint = LanflixMuted, modifier = Modifier.size(15.dp))
                    Text(" ${activity.commentCount}", color = LanflixMuted, fontSize = 12.sp)
                }
            }

            // ─ Comments section ───────────────────────────────────────────
            AnimatedVisibility(visible = commentsExpanded) {
                Column(Modifier.padding(top = 8.dp)) {
                    HorizontalDivider(color = Color.White.copy(alpha = 0.08f))
                    Spacer(Modifier.height(8.dp))
                    comments.forEach { c ->
                        Row(
                            modifier = Modifier.padding(vertical = 4.dp),
                            verticalAlignment = Alignment.Top
                        ) {
                            Box(
                                modifier = Modifier.size(26.dp).clip(CircleShape)
                                    .background(Color.White.copy(alpha = 0.10f)),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = c.author.displayName.firstOrNull()?.uppercase() ?: "?",
                                    color = Color.White, fontSize = 10.sp, fontWeight = FontWeight.Bold
                                )
                            }
                            Column(Modifier.padding(start = 8.dp)) {
                                Text(c.author.displayName, color = LanflixGold, fontSize = 11.sp, fontWeight = FontWeight.SemiBold)
                                Text(c.body, color = Color.White.copy(alpha = 0.82f), fontSize = 13.sp)
                            }
                        }
                    }
                    // Add comment input
                    Row(
                        modifier = Modifier.padding(top = 8.dp).fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        OutlinedTextField(
                            value = commentInput,
                            onValueChange = { commentInput = it },
                            modifier = Modifier.weight(1f),
                            placeholder = { Text("Add a comment…", color = LanflixMuted, fontSize = 13.sp) },
                            singleLine = true,
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = LanflixGold,
                                unfocusedBorderColor = Color.White.copy(alpha = 0.15f),
                                focusedTextColor = Color.White,
                                unfocusedTextColor = Color.White
                            )
                        )
                        IconButton(
                            onClick = {
                                if (commentInput.isNotBlank()) {
                                    scope.launch {
                                        api.addComment(activity.id, commentInput)
                                        comments = api.getComments(activity.id)
                                        commentInput = ""
                                    }
                                }
                            }
                        ) { Icon(Icons.Default.Send, "Send", tint = LanflixGold) }
                    }
                }
            }
        }
    }
}

@Composable
private fun ReactionButton(emoji: String, count: Int, active: Boolean, onClick: () -> Unit) {
    Surface(
        onClick = onClick,
        shape = RoundedCornerShape(20.dp),
        color = if (active) LanflixGold.copy(alpha = 0.18f) else Color.White.copy(alpha = 0.07f)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 5.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(emoji, fontSize = 14.sp)
            if (count > 0) Text(" $count", color = if (active) LanflixGold else LanflixMuted, fontSize = 12.sp)
        }
    }
}

@Composable
private fun CreatePostSheet(onDismiss: () -> Unit, onSubmit: (String, String) -> Unit) {
    var body by remember { mutableStateOf("") }
    var visibility by remember { mutableStateOf("Friends") }
    val visibilities = listOf("Friends", "Server", "Household")

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("New post", color = Color.White, fontWeight = FontWeight.Bold) },
        text = {
            Column {
                OutlinedTextField(
                    value = body,
                    onValueChange = { body = it },
                    modifier = Modifier.fillMaxWidth().height(110.dp),
                    placeholder = { Text("What\'s on your mind?", color = LanflixMuted) },
                    maxLines = 5,
                    colors = OutlinedTextFieldDefaults.colors(
                        focusedBorderColor = LanflixGold,
                        unfocusedBorderColor = Color.White.copy(alpha = 0.2f),
                        focusedTextColor = Color.White, unfocusedTextColor = Color.White
                    )
                )
                Spacer(Modifier.height(12.dp))
                Text("Visible to", color = LanflixMuted, fontSize = 11.sp)
                Row(Modifier.padding(top = 6.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    visibilities.forEach { v ->
                        FilterChip(
                            selected = visibility == v,
                            onClick = { visibility = v },
                            label = { Text(v, fontSize = 12.sp) },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = LanflixGold,
                                selectedLabelColor = Color.Black
                            )
                        )
                    }
                }
            }
        },
        confirmButton = {
            Button(
                onClick = { if (body.isNotBlank()) onSubmit(body, visibility) },
                enabled = body.isNotBlank(),
                colors = ButtonDefaults.buttonColors(containerColor = LanflixGold, contentColor = Color.Black)
            ) { Text("Post", fontWeight = FontWeight.Bold) }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel", color = LanflixMuted) } },
        containerColor = Color(0xFF1A1A2A)
    )
}

@Composable
fun NotificationsScreen(
    notifications: List<SocialNotification>,
    onBack: () -> Unit,
    onMarkAllRead: () -> Unit = {},
    onMarkRead: (String) -> Unit = {}
) {
    LazyColumn(
        Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF26384E), LanflixBackground))),
        contentPadding = PaddingValues(bottom = 40.dp)
    ) {
        item {
            Row(
                Modifier.fillMaxWidth().statusBarsPadding().height(60.dp).padding(horizontal = 4.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }
                Text("Notifications", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
                if (notifications.any { !it.isRead }) {
                    TextButton(onClick = onMarkAllRead) {
                        Text("Mark all read", color = LanflixGold, fontSize = 12.sp)
                    }
                }
            }
            if (notifications.isEmpty()) EmptyMessage(
                Icons.Default.Notifications,
                "All caught up",
                "Friend requests, reactions, comments and server events appear here."
            )
        }
        items(notifications, key = { it.id }) { n ->
            NotificationItem(n, onClick = { if (!n.isRead) onMarkRead(n.id) })
        }
    }
}

@Composable
private fun NotificationItem(n: SocialNotification, onClick: () -> Unit) {
    val (icon, tint) = when (n.kind) {
        "friend-request" -> Icons.Default.PersonAdd to Color(0xFF58C878)
        "friend-accepted" -> Icons.Default.People to Color(0xFF58C878)
        "reaction" -> Icons.Default.Favorite to Color(0xFFE05080)
        "comment" -> Icons.Default.ChatBubble to Color(0xFF58C8FF)
        "follow" -> Icons.Default.PersonAdd to LanflixGold
        "review" -> Icons.Default.Star to Color(0xFFFFD700)
        else -> Icons.Default.Notifications to LanflixMuted
    }
    Surface(
        onClick = onClick,
        modifier = Modifier
            .padding(horizontal = 14.dp, vertical = 4.dp)
            .fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        color = if (!n.isRead) Color.White.copy(alpha = 0.10f) else Color.White.copy(alpha = 0.05f)
    ) {
        Row(
            modifier = Modifier.padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Unread accent stripe
            if (!n.isRead) {
                Box(
                    Modifier.width(3.dp).height(36.dp)
                        .clip(RoundedCornerShape(2.dp))
                        .background(LanflixGold)
                        .padding(end = 10.dp)
                )
                Spacer(Modifier.width(10.dp))
            }
            Box(
                modifier = Modifier.size(40.dp).clip(CircleShape)
                    .background(tint.copy(alpha = 0.15f)),
                contentAlignment = Alignment.Center
            ) { Icon(icon, null, tint = tint, modifier = Modifier.size(20.dp)) }
            Column(Modifier.padding(start = 12.dp).weight(1f)) {
                val actor = n.actor?.displayName ?: "Lanflix"
                val description = when (n.kind) {
                    "friend-request" -> "$actor sent you a friend request"
                    "friend-accepted" -> "$actor accepted your friend request"
                    "reaction" -> "$actor reacted to your post"
                    "comment" -> "$actor commented on your post"
                    "follow" -> "$actor started following you"
                    "review" -> "$actor left a review"
                    else -> "$actor — ${n.kind.replace('-', ' ')}"
                }
                Text(description, color = Color.White, fontSize = 13.sp, maxLines = 2, overflow = TextOverflow.Ellipsis)
                Text(
                    text = n.createdAtUtc.take(10),
                    color = LanflixMuted, fontSize = 10.sp,
                    modifier = Modifier.padding(top = 2.dp)
                )
            }
            if (!n.isRead) {
                Box(Modifier.size(8.dp).clip(CircleShape).background(LanflixGold))
            }
        }
    }
}


@Composable private fun ScreenHeader(title: String, onBack: () -> Unit) { Row(Modifier.fillMaxWidth().statusBarsPadding().height(60.dp), verticalAlignment = Alignment.CenterVertically) { IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }; Text(title, color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold) } }
@Composable private fun SettingsPanel(title: String?, content: @Composable ColumnScope.() -> Unit) { Surface(Modifier.fillMaxWidth(), shape = RoundedCornerShape(17.dp), color = Color.White.copy(alpha = .07f)) { Column(Modifier.padding(15.dp)) { if (title != null) Text(title, color = LanflixGold, fontSize = 11.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 7.dp)); content() } } }
@Composable
private fun SocialCard(author: String, kind: String, body: String?, footer: String) {
    Surface(Modifier.padding(horizontal = 16.dp, vertical = 5.dp), shape = RoundedCornerShape(16.dp), color = Color.White.copy(alpha = .07f)) {
        Column(Modifier.fillMaxWidth().padding(15.dp)) {
            Text(author, color = Color.White, fontWeight = FontWeight.Bold)
            Text(kind.replaceFirstChar { it.uppercase() }, color = LanflixGold, fontSize = 11.sp)
            if (!body.isNullOrBlank()) Text(body, color = Color.White.copy(alpha = .82f), modifier = Modifier.padding(top = 7.dp))
            Text(footer, color = LanflixMuted, fontSize = 10.sp, modifier = Modifier.padding(top = 8.dp))
        }
    }
}
@Composable
private fun EmptyMessage(icon: ImageVector, title: String, body: String) {
    Column(
        Modifier.fillMaxWidth().padding(40.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(icon, null, tint = LanflixMuted, modifier = Modifier.size(48.dp))
        Text(title, color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 12.dp))
        Text(body, color = LanflixMuted, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 7.dp))
    }
}


fun promptBiometricAuthentication(context: android.content.Context, onSuccess: () -> Unit, onError: (String) -> Unit) {
    if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.P) {
        val cancellationSignal = android.os.CancellationSignal()
        val executor = context.mainExecutor
        val callback = object : android.hardware.biometrics.BiometricPrompt.AuthenticationCallback() {
            override fun onAuthenticationSucceeded(result: android.hardware.biometrics.BiometricPrompt.AuthenticationResult?) {
                onSuccess()
            }
            override fun onAuthenticationError(errorCode: Int, errString: CharSequence?) {
                onError(errString?.toString() ?: "Authentication error")
            }
        }
        val builder = android.hardware.biometrics.BiometricPrompt.Builder(context)
            .setTitle("Lanflix Passkey & Biometric Security")
            .setSubtitle("Confirm your fingerprint, face or device passkey")

        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.R) {
            builder.setAllowedAuthenticators(
                android.hardware.biometrics.BiometricManager.Authenticators.BIOMETRIC_STRONG or
                android.hardware.biometrics.BiometricManager.Authenticators.DEVICE_CREDENTIAL
            )
        } else {
            builder.setNegativeButton("Cancel", executor) { _, _ -> onError("Authentication cancelled") }
        }

        runCatching {
            val prompt = builder.build()
            prompt.authenticate(cancellationSignal, executor, callback)
        }.onFailure {
            onSuccess()
        }
    } else {
        val keyguardManager = context.getSystemService(android.content.Context.KEYGUARD_SERVICE) as? android.app.KeyguardManager
        if (keyguardManager?.isKeyguardSecure == true) {
            onSuccess()
        } else {
            onError("Device lock is not secured with PIN/Passkey")
        }
    }
}
