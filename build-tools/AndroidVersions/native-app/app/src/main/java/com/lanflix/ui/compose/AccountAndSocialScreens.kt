package com.lanflix.ui.compose

import androidx.compose.foundation.background
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.api.AccountSession
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.SocialActivity
import com.lanflix.api.SocialNotification
import com.lanflix.auth.LanflixAccount
import kotlinx.coroutines.launch

@Composable
fun AuthenticationScreen(state: LanflixUiState, onAuthenticate: (String, String, String, String?) -> Unit, onServer: () -> Unit) {
    var username by remember { mutableStateOf("") }
    var displayName by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var invitation by remember { mutableStateOf("") }
    var registration by remember { mutableStateOf(state.requiresOwnerSetup) }
    Box(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF173F53), Color(0xFF08151F), Color(0xFF040608))))) {
        Box(Modifier.fillMaxWidth().height(420.dp).background(Brush.radialGradient(listOf(Color(0x8844A6C6), Color.Transparent), radius = 720f)))
        LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(horizontal = 24.dp, vertical = 50.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            item {
                Box(Modifier.size(72.dp).clip(CircleShape).background(LanflixGold), contentAlignment = Alignment.Center) {
                    Icon(Icons.Default.PlayArrow, null, tint = Color.Black, modifier = Modifier.size(44.dp))
                }
                Text(if (state.requiresOwnerSetup) "Create the server owner" else if (registration) "Join this Lanflix server" else "Welcome back",
                    color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold, modifier = Modifier.padding(top = 24.dp))
                Text(if (state.requiresOwnerSetup) "This first account controls the server." else "Accounts and downloads stay on your self-hosted server.",
                    color = LanflixMuted, fontSize = 12.sp, modifier = Modifier.padding(top = 7.dp, bottom = 22.dp))
                Surface(shape = RoundedCornerShape(24.dp), color = Color.White.copy(alpha = .075f)) {
                    Column(Modifier.fillMaxWidth().padding(18.dp)) {
                        if (registration) OutlinedTextField(displayName, { displayName = it }, Modifier.fillMaxWidth(), label = { Text("Display name") }, singleLine = true)
                        OutlinedTextField(username, { username = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Username") }, singleLine = true)
                        OutlinedTextField(password, { password = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Password") }, singleLine = true, visualTransformation = PasswordVisualTransformation())
                        if (registration && !state.requiresOwnerSetup) OutlinedTextField(invitation, { invitation = it }, Modifier.fillMaxWidth().padding(top = 8.dp), label = { Text("Invitation code") }, singleLine = true)
                        Button(onClick = { onAuthenticate(username, displayName.ifBlank { username }, password, invitation.takeIf { registration && it.isNotBlank() }) },
                            enabled = username.length >= 3 && password.length >= 10 && (!registration || state.requiresOwnerSetup || invitation.isNotBlank()),
                            modifier = Modifier.fillMaxWidth().height(56.dp).padding(top = 10.dp), shape = RoundedCornerShape(28.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black)) {
                            if (state.loading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp) else Text(if (registration) "Create account" else "Sign in", fontWeight = FontWeight.Bold)
                        }
                        state.error?.let { Text(it, color = Color(0xFFFF9B8E), fontSize = 12.sp, modifier = Modifier.padding(top = 10.dp)) }
                    }
                }
                if (!state.requiresOwnerSetup) TextButton(onClick = { registration = !registration }) { Text(if (registration) "I already have an account" else "Register with an invitation") }
                TextButton(onClick = onServer) { Icon(Icons.Default.Storage, null); Text("Change server", modifier = Modifier.padding(start = 7.dp)) }
            }
        }
    }
}

@Composable
fun AccountSecurityScreen(account: LanflixAccount, onBack: () -> Unit, onSignedOut: () -> Unit) {
    val context = LocalContext.current
    val api = remember { LanflixApiClient(context) }
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
fun ActivityScreen(feed: List<SocialActivity>, onBack: () -> Unit) {
    LazyColumn(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF3C1837), LanflixBackground))), contentPadding = PaddingValues(bottom = 40.dp)) {
        item { ScreenHeader("Activity", onBack) }
        if (feed.isEmpty()) item { EmptyMessage("No activity yet", "Follow or befriend another account, or publish a review.") }
        items(feed, key = { it.id }) { activity -> SocialCard(activity.author.displayName, activity.kind, activity.body, "${activity.reactionCount} reactions • ${activity.commentCount} comments") }
    }
}

@Composable
fun NotificationsScreen(notifications: List<SocialNotification>, onBack: () -> Unit) {
    LazyColumn(Modifier.fillMaxSize().background(Brush.verticalGradient(listOf(Color(0xFF26384E), LanflixBackground))), contentPadding = PaddingValues(bottom = 40.dp)) {
        item { ScreenHeader("Notifications", onBack) }
        if (notifications.isEmpty()) item { EmptyMessage("You are all caught up", "New friend requests, reactions, comments and server activity appear here.") }
        items(notifications, key = { it.id }) { value -> SocialCard(value.actor?.displayName ?: "Lanflix", value.kind.replace('-', ' '), value.resourceType, if (value.isRead) "Read" else "New") }
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
@Composable private fun EmptyMessage(title: String, body: String) { Column(Modifier.fillMaxWidth().padding(40.dp), horizontalAlignment = Alignment.CenterHorizontally) { Text(title, color = Color.White, fontSize = 18.sp, fontWeight = FontWeight.Bold); Text(body, color = LanflixMuted, modifier = Modifier.padding(top = 7.dp)) } }
