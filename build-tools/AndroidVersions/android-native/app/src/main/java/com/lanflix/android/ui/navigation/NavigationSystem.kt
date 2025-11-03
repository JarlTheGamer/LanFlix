package com.lanflix.android.ui.navigation

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lanflix.android.domain.model.Profile

enum class NavigationPage {
    HOME, DISCOVER, SERIES, MOVIES, MY_LIST
}

enum class FocusedElement {
    PROFILE, MENU, HERO, SEARCH, NOTIFICATIONS, SETTINGS
}

@Composable
fun NavigationSystem(
    currentPage: NavigationPage,
    selectedProfile: Profile?,
    onPageChange: (NavigationPage) -> Unit,
    onProfileClick: () -> Unit,
    onSearchClick: () -> Unit,
    onNotificationsClick: () -> Unit,
    onSettingsClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .background(Color(0x0A000000))
            .padding(horizontal = 16.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Left side - Profile and Menu
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(24.dp)
        ) {
            // Profile button
            ProfileButton(
                profile = selectedProfile,
                onClick = onProfileClick
            )
            
            // Menu items
            Row(
                horizontalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                NavigationPage.values().forEach { page ->
                    MenuButton(
                        text = getPageTitle(page),
                        isActive = currentPage == page,
                        onClick = { onPageChange(page) }
                    )
                }
            }
        }
        
        // Right side - Actions
        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onSearchClick) {
                Icon(
                    imageVector = Icons.Default.Search,
                    contentDescription = "Search",
                    tint = Color.White
                )
            }
            
            IconButton(onClick = onNotificationsClick) {
                Icon(
                    imageVector = Icons.Default.Notifications,
                    contentDescription = "Notifications",
                    tint = Color.White
                )
            }
            
            IconButton(onClick = onSettingsClick) {
                Icon(
                    imageVector = Icons.Default.Settings,
                    contentDescription = "Settings",
                    tint = Color.White
                )
            }
        }
    }
}

@Composable
private fun ProfileButton(
    profile: Profile?,
    onClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .clickable { onClick() }
            .padding(8.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        // Profile avatar placeholder
        Box(
            modifier = Modifier
                .size(32.dp)
                .background(Color(0xFFE50914), RoundedCornerShape(4.dp))
        )
        
        // Profile name
        Text(
            text = profile?.name ?: "Profile",
            color = Color.White,
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium
        )
        
        Icon(
            imageVector = Icons.Default.KeyboardArrowDown,
            contentDescription = null,
            tint = Color.White,
            modifier = Modifier.size(16.dp)
        )
    }
}

@Composable
private fun MenuButton(
    text: String,
    isActive: Boolean,
    onClick: () -> Unit
) {
    Text(
        text = text,
        color = if (isActive) Color.White else Color(0x99FFFFFF),
        fontSize = 16.sp,
        fontWeight = if (isActive) FontWeight.SemiBold else FontWeight.Medium,
        modifier = Modifier
            .clickable { onClick() }
            .padding(horizontal = 8.dp, vertical = 4.dp)
    )
}

private fun getPageTitle(page: NavigationPage): String {
    return when (page) {
        NavigationPage.HOME -> "Home"
        NavigationPage.DISCOVER -> "Discover"
        NavigationPage.SERIES -> "Series"
        NavigationPage.MOVIES -> "Films"
        NavigationPage.MY_LIST -> "My List"
    }
}