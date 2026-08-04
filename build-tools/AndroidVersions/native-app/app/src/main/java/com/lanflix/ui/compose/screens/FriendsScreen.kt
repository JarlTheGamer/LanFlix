package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.material3.TabRowDefaults.tabIndicatorOffset
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.api.SocialRelationship
import com.lanflix.ui.compose.LanflixBackground
import com.lanflix.ui.compose.LanflixGold
import com.lanflix.ui.compose.LanflixMuted

@Composable
fun FriendsScreen(
    relationships: List<SocialRelationship>,
    onBack: () -> Unit,
    onAccept: (relationshipId: String) -> Unit = {},
    onRemoveFriend: (targetId: String) -> Unit = {},
    onUnfollow: (targetId: String) -> Unit = {}
) {
    var selectedTab by remember { mutableStateOf(0) }
    val tabs = listOf("Friends", "Following", "Requests")

    val friends = relationships.filter { it.kind == "friend" && it.status == "accepted" }
    val following = relationships.filter { it.kind == "follow" }
    val incomingRequests = relationships.filter { it.kind == "friend" && it.status == "pending" && it.incoming }

    Column(
        Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF183828), LanflixBackground)))
    ) {
        Row(
            Modifier.fillMaxWidth().statusBarsPadding().height(60.dp).padding(horizontal = 4.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back", tint = Color.White) }
            Text("Social", color = Color.White, fontSize = 21.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
        }

        TabRow(
            selectedTabIndex = selectedTab,
            containerColor = Color.Transparent,
            contentColor = LanflixGold,
            indicator = { tabPositions ->
                TabRowDefaults.SecondaryIndicator(
                    modifier = Modifier.tabIndicatorOffset(tabPositions[selectedTab]),
                    color = LanflixGold
                )
            }
        ) {
            tabs.forEachIndexed { index, title ->
                val badge = if (index == 2) incomingRequests.size else 0
                Tab(
                    selected = selectedTab == index,
                    onClick = { selectedTab = index },
                    text = {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(title, color = if (selectedTab == index) LanflixGold else LanflixMuted)
                            if (badge > 0) {
                                Spacer(Modifier.width(4.dp))
                                Box(
                                    modifier = Modifier.size(16.dp).clip(CircleShape).background(LanflixGold),
                                    contentAlignment = Alignment.Center
                                ) { Text("$badge", color = Color.Black, fontSize = 9.sp, fontWeight = FontWeight.Bold) }
                            }
                        }
                    }
                )
            }
        }

        LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(vertical = 12.dp, horizontal = 14.dp)) {
            when (selectedTab) {
                0 -> {
                    if (friends.isEmpty()) item { EmptyRelationshipMessage("No friends yet", "Send friend requests from the activity feed.") }
                    items(friends, key = { it.id }) { r ->
                        RelationshipRow(r.account.displayName, r.account.role, "Remove", Color(0xFFE05050)) { onRemoveFriend(r.account.id) }
                    }
                }
                1 -> {
                    if (following.isEmpty()) item { EmptyRelationshipMessage("Not following anyone", "Follow accounts from the activity feed.") }
                    items(following, key = { it.id }) { r ->
                        RelationshipRow(r.account.displayName, r.account.role, "Unfollow", LanflixMuted) { onUnfollow(r.account.id) }
                    }
                }
                2 -> {
                    if (incomingRequests.isEmpty()) item { EmptyRelationshipMessage("No pending requests", "Friend requests you receive will appear here.") }
                    items(incomingRequests, key = { it.id }) { r ->
                        RequestRow(r.account.displayName, onAccept = { onAccept(r.id) }, onDecline = { onRemoveFriend(r.account.id) })
                    }
                }
            }
        }
    }
}

@Composable
private fun RelationshipRow(displayName: String, role: String, actionLabel: String, actionColor: Color, onAction: () -> Unit) {
    Surface(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), shape = RoundedCornerShape(14.dp), color = Color.White.copy(alpha = 0.07f)) {
        Row(Modifier.padding(horizontal = 14.dp, vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(modifier = Modifier.size(42.dp).clip(CircleShape).background(LanflixGold.copy(alpha = 0.15f)), contentAlignment = Alignment.Center) {
                Text(displayName.firstOrNull()?.uppercase() ?: "?", color = LanflixGold, fontSize = 18.sp, fontWeight = FontWeight.Bold)
            }
            Column(Modifier.padding(start = 12.dp).weight(1f)) {
                Text(displayName, color = Color.White, fontWeight = FontWeight.SemiBold)
                Text(role, color = LanflixMuted, fontSize = 11.sp)
            }
            OutlinedButton(onClick = onAction, shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.outlinedButtonColors(contentColor = actionColor),
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 6.dp)
            ) { Text(actionLabel, fontSize = 12.sp) }
        }
    }
}

@Composable
private fun RequestRow(displayName: String, onAccept: () -> Unit, onDecline: () -> Unit) {
    Surface(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), shape = RoundedCornerShape(14.dp), color = Color(0xFF1A2A1A)) {
        Row(Modifier.padding(horizontal = 14.dp, vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(modifier = Modifier.size(42.dp).clip(CircleShape).background(Color(0xFF58C878).copy(alpha = 0.15f)), contentAlignment = Alignment.Center) {
                Text(displayName.firstOrNull()?.uppercase() ?: "?", color = Color(0xFF58C878), fontSize = 18.sp, fontWeight = FontWeight.Bold)
            }
            Column(Modifier.padding(start = 12.dp).weight(1f)) {
                Text(displayName, color = Color.White, fontWeight = FontWeight.SemiBold)
                Text("Wants to be friends", color = LanflixMuted, fontSize = 11.sp)
            }
            IconButton(onClick = onDecline) { Icon(Icons.Default.Close, "Decline", tint = Color(0xFFE05050)) }
            Button(onClick = onAccept, shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF58C878), contentColor = Color.Black),
                contentPadding = PaddingValues(horizontal = 14.dp, vertical = 6.dp)
            ) { Text("Accept", fontWeight = FontWeight.Bold, fontSize = 12.sp) }
        }
    }
}

@Composable
private fun EmptyRelationshipMessage(title: String, subtitle: String) {
    Column(Modifier.fillMaxWidth().padding(40.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Icon(Icons.Default.People, null, tint = LanflixMuted, modifier = Modifier.size(48.dp))
        Text(title, color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 12.dp))
        Text(subtitle, color = LanflixMuted, modifier = Modifier.padding(top = 6.dp))
    }
}
