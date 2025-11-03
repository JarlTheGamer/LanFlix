package com.lanflix.android.ui.navigation

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import com.lanflix.android.domain.model.Profile

enum class NavigationPage {
    HOME, DISCOVER, SERIES, MOVIES, MY_LIST
}

enum class FocusedElement {
    PROFILE, MENU, HERO, TABS, CARDS
}

@Composable
fun NavigationSystem(
    currentPage: NavigationPage,
    selectedProfile: Profile?,
    onPageChange: (NavigationPage) -> Unit,
    onProfileClick: () -> Unit,
    onSearchClick: () -> Unit,
    onSettingsClick: () -> Unit,
    onNotificationsClick: () -> Unit,
    isScrolled: Boolean = false,
    modifier: Modifier = Modifier
) {
    var focusedElement by remember { mutableStateOf(FocusedElement.MENU) }
    var focusedMenuIndex by remember { mutableIntStateOf(0) }
    val focusManager = LocalFocusManager.current
    
    // Top navigation with exact CSS styling
    Box(
        modifier = modifier
            .fillMaxWidth()
            .background(
                if (isScrolled) {
                    Color(0xF0040404) // CSS: rgba(4, 4, 4, 0.94) when is-solid
                } else {
                    Color.Transparent // CSS: background: transparent with backdrop-filter
                }
            )
            .padding(horizontal = 56.dp, vertical = 22.dp) // CSS: padding: 22px 56px
            .onKeyEvent { keyEvent ->
                if (keyEvent.type == KeyEventType.KeyDown) {
                    handleKeyNavigation(
                        keyEvent.key,
                        focusedElement,
                        focusedMenuIndex,
                        currentPage,
                        onFocusChange = { element, index ->
                            focusedElement = element
                            focusedMenuIndex = index
                        },
                        onPageChange = onPageChange,
                        onProfileClick = onProfileClick,
                        onSettingsClick = onSettingsClick
                    )
                    true
                } else false
            }
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Left section - Profile button
            ProfileButton(
                profile = selectedProfile,
                onClick = onProfileClick,
                isFocused = focusedElement == FocusedElement.PROFILE
            )
            
            // Center - Navigation menu
            NavigationMenu(
                currentPage = currentPage,
                focusedIndex = if (focusedElement == FocusedElement.MENU) focusedMenuIndex else -1,
                onPageChange = onPageChange,
                onSearchClick = onSearchClick
            )
            
            // Right section - Brand and action buttons
            BrandSection(
                onNotificationsClick = onNotificationsClick,
                onSettingsClick = onSettingsClick,
                settingsFocused = focusedElement == FocusedElement.MENU && focusedMenuIndex == NavigationPage.values().size
            )
        }
    }
}

@Composable
private fun ProfileButton(
    profile: Profile?,
    onClick: () -> Unit,
    isFocused: Boolean
) {
    Row(
        modifier = Modifier
            .clickable { onClick() }
            .background(
                if (isFocused) {
                    Color(0xF2FFFFFF) // CSS: rgba(255, 255, 255, 0.95) when focused
                } else {
                    Color(0x59000000) // CSS: rgba(0, 0, 0, 0.35)
                },
                RoundedCornerShape(8.dp)
            )
            .padding(horizontal = 12.dp, vertical = 6.dp)
            .padding(start = 6.dp), // CSS: padding: 6px 12px 6px 6px
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        // Profile avatar with exact CSS styling
        Box(
            modifier = Modifier
                .size(32.dp) // CSS: width: 32px, height: 32px
                .clip(RoundedCornerShape(6.dp)) // CSS: border-radius: 6px
                .background(
                    if (profile != null) {
                        Brush.linearGradient(
                            colors = listOf(
                                Color(android.graphics.Color.parseColor(profile.avatarColorPrimary)),
                                Color(android.graphics.Color.parseColor(profile.avatarColorSecondary))
                            ),
                            start = Offset(0f, 0f),
                            end = Offset(1f, 1f) // 135deg gradient
                        )
                    } else {
                        Brush.linearGradient(
                            colors = listOf(Color(0xFFff6b6b), Color(0xFFff6b6b))
                        )
                    }
                ),
            contentAlignment = Alignment.Center
        ) {
            // Profile icon matching CSS ::before pseudo-element
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                Box(
                    modifier = Modifier
                        .size(12.dp) // CSS: width: 12px, height: 12px
                        .background(
                            if (isFocused) {
                                Color(0xCC000000) // CSS: rgba(0, 0, 0, 0.8) when focused
                            } else {
                                Color(0xE6FFFFFF) // CSS: rgba(255, 255, 255, 0.9)
                            },
                            androidx.compose.foundation.shape.CircleShape
                        )
                )
                Box(
                    modifier = Modifier
                        .size(12.dp)
                        .background(
                            if (isFocused) {
                                Color(0xCC000000)
                            } else {
                                Color(0xE6FFFFFF)
                            },
                            androidx.compose.foundation.shape.CircleShape
                        )
                )
            }
        }
        
        // Arrow icon
        Icon(
            Icons.Default.KeyboardArrowDown,
            contentDescription = null,
            tint = if (isFocused) {
                Color(0xCC000000) // CSS: rgba(0, 0, 0, 0.8) when focused
            } else {
                Color(0xB3FFE2DB) // CSS: rgba(255, 226, 219, 0.7)
            },
            modifier = Modifier.size(14.dp)
        )
    }
}

@Composable
private fun NavigationMenu(
    currentPage: NavigationPage,
    focusedIndex: Int,
    onPageChange: (NavigationPage) -> Unit,
    onSearchClick: () -> Unit
) {
    Row(
        horizontalArrangement = Arrangement.spacedBy(26.dp), // CSS: gap: 26px
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Search button with exact CSS styling
        IconButton(
            onClick = onSearchClick,
            modifier = Modifier
                .size(40.dp) // CSS: width: 40px, height: 40px
                .background(
                    if (focusedIndex == -1) { // Special case for search focus
                        Color(0xF2FFFFFF) // CSS: rgba(255, 255, 255, 0.95) when focused
                    } else {
                        Color.Transparent
                    },
                    RoundedCornerShape(12.dp) // CSS: border-radius: 12px
                )
        ) {
            androidx.compose.material.icons.Icons.Default.Search.let { searchIcon ->
                Icon(
                    searchIcon,
                    contentDescription = "Search",
                    tint = if (focusedIndex == -1) {
                        Color(0xCC000000) // CSS: rgba(0, 0, 0, 0.8) when focused
                    } else {
                        Color(0xE6FFE2DB) // CSS: rgba(255, 226, 219, 0.9)
                    },
                    modifier = Modifier.size(20.dp)
                )
            }
        }
        
        // Menu items
        NavigationPage.values().forEachIndexed { index, page ->
            MenuButton(
                text = getPageTitle(page),
                isActive = currentPage == page,
                isFocused = focusedIndex == index,
                onClick = { onPageChange(page) }
            )
        }
    }
}

@Composable
private fun MenuButton(
    text: String,
    isActive: Boolean,
    isFocused: Boolean,
    onClick: () -> Unit
) {
    Button(
        onClick = onClick,
        colors = ButtonDefaults.buttonColors(
            containerColor = when {
                isActive || isFocused -> Color(0xF2FFFFFF) // CSS: rgba(255, 255, 255, 0.95)
                else -> Color.Transparent
            }
        ),
        shape = RoundedCornerShape(20.dp), // CSS: border-radius: 20px
        contentPadding = PaddingValues(horizontal = 18.dp, vertical = 10.dp), // CSS: padding: 10px 18px
        modifier = Modifier.defaultMinSize(minWidth = 1.dp, minHeight = 1.dp)
    ) {
        Text(
            text = text,
            color = when {
                isActive || isFocused -> Color.Black // CSS: color: #000000 when active/focused
                else -> Color(0xD1FFFFFF) // CSS: rgba(255, 255, 255, 0.82)
            },
            fontSize = 16.sp, // CSS: 0.98rem
            fontWeight = FontWeight.Medium, // CSS: font-weight: 500
            letterSpacing = 0.02.em // CSS: letter-spacing: 0.02em
        )
    }
}

@Composable
private fun BrandSection(
    onNotificationsClick: () -> Unit,
    onSettingsClick: () -> Unit,
    settingsFocused: Boolean
) {
    Row(
        horizontalArrangement = Arrangement.spacedBy(18.dp), // CSS: gap: 18px
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Notifications button
        IconButton(
            onClick = onNotificationsClick,
            modifier = Modifier
                .size(36.dp) // CSS: width: 36px, height: 36px
                .background(
                    Color.Transparent,
                    RoundedCornerShape(10.dp) // CSS: border-radius: 10px
                )
        ) {
            androidx.compose.material.icons.Icons.Default.Notifications.let { notificationIcon ->
                Icon(
                    notificationIcon,
                    contentDescription = "Notifications",
                    tint = Color(0xDBFFECE1), // CSS: rgba(255, 236, 225, 0.86)
                    modifier = Modifier.size(18.dp)
                )
            }
        }
        
        // Settings button
        IconButton(
            onClick = onSettingsClick,
            modifier = Modifier
                .size(36.dp)
                .background(
                    if (settingsFocused) {
                        Color(0xF2FFFFFF) // CSS: rgba(255, 255, 255, 0.95) when focused
                    } else {
                        Color.Transparent
                    },
                    RoundedCornerShape(10.dp)
                )
        ) {
            androidx.compose.material.icons.Icons.Default.Settings.let { settingsIcon ->
                Icon(
                    settingsIcon,
                    contentDescription = "Settings",
                    tint = if (settingsFocused) {
                        Color(0xCC000000) // CSS: rgba(0, 0, 0, 0.8) when focused
                    } else {
                        Color(0xDBFFECE1) // CSS: rgba(255, 236, 225, 0.86)
                    },
                    modifier = Modifier.size(18.dp)
                )
            }
        }
        
        // Brand name
        Text(
            text = "LANFLIX",
            color = Color(0xDBFFECE1), // CSS: rgba(255, 236, 225, 0.86)
            fontSize = 13.sp, // CSS: 0.82rem
            fontWeight = FontWeight.SemiBold, // CSS: font-weight: 600
            letterSpacing = 0.12.em, // CSS: letter-spacing: 0.12em
            modifier = Modifier.padding(start = 12.dp)
        )
    }
}

private fun handleKeyNavigation(
    key: Key,
    focusedElement: FocusedElement,
    focusedMenuIndex: Int,
    currentPage: NavigationPage,
    onFocusChange: (FocusedElement, Int) -> Unit,
    onPageChange: (NavigationPage) -> Unit,
    onProfileClick: () -> Unit,
    onSettingsClick: () -> Unit
): Boolean {
    val menuItems = NavigationPage.values()
    
    when (focusedElement) {
        FocusedElement.PROFILE -> {
            when (key) {
                Key.DirectionRight -> onFocusChange(FocusedElement.MENU, 0)
                Key.DirectionDown -> onFocusChange(FocusedElement.HERO, focusedMenuIndex)
                Key.Enter, Key.NumPadEnter -> onProfileClick()
                else -> return false
            }
        }
        
        FocusedElement.MENU -> {
            when (key) {
                Key.DirectionLeft -> {
                    if (focusedMenuIndex == 0) {
                        onFocusChange(FocusedElement.PROFILE, 0)
                    } else {
                        val newIndex = focusedMenuIndex - 1
                        onFocusChange(FocusedElement.MENU, newIndex)
                        if (newIndex < menuItems.size) {
                            onPageChange(menuItems[newIndex])
                        }
                    }
                }
                Key.DirectionRight -> {
                    val maxIndex = menuItems.size // Include settings button
                    if (focusedMenuIndex == maxIndex) {
                        // Already at settings, stay there
                    } else {
                        val newIndex = focusedMenuIndex + 1
                        onFocusChange(FocusedElement.MENU, newIndex)
                        if (newIndex < menuItems.size) {
                            onPageChange(menuItems[newIndex])
                        }
                    }
                }
                Key.DirectionDown -> onFocusChange(FocusedElement.HERO, focusedMenuIndex)
                Key.Enter, Key.NumPadEnter -> {
                    if (focusedMenuIndex < menuItems.size) {
                        onPageChange(menuItems[focusedMenuIndex])
                        onFocusChange(FocusedElement.HERO, focusedMenuIndex)
                    } else {
                        onSettingsClick()
                    }
                }
                else -> return false
            }
        }
        
        else -> return false
    }
    
    return true
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