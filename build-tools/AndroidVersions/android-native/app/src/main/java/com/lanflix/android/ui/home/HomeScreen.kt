package com.lanflix.android.ui.home

import androidx.compose.animation.animateContentSize
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalConfiguration
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import androidx.hilt.navigation.compose.hiltViewModel
import coil.compose.AsyncImage
import com.lanflix.android.domain.model.Content
import com.lanflix.android.domain.model.Profile
import com.lanflix.android.ui.theme.LanflixTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    selectedProfile: Profile?,
    onProfileClick: () -> Unit,
    onContentClick: (String, String) -> Unit,
    onSearchClick: () -> Unit,
    onSettingsClick: () -> Unit,
    onNotificationsClick: () -> Unit,
    viewModel: HomeViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var currentHeroIndex by remember { mutableIntStateOf(0) }
    var activeAmbilightLayer by remember { mutableIntStateOf(1) }
    var isScrolled by remember { mutableStateOf(false) }
    
    LaunchedEffect(Unit) {
        viewModel.loadContent()
    }
    
    // Auto-advance hero carousel
    LaunchedEffect(uiState.heroContent) {
        if (uiState.heroContent.isNotEmpty()) {
            while (true) {
                kotlinx.coroutines.delay(8000) // 8 seconds like web
                currentHeroIndex = (currentHeroIndex + 1) % uiState.heroContent.size
            }
        }
    }
    
    LanflixTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0xFF050505)) // Exact body background
        ) {
            // Hero ambilight background - EXACT replica
            HeroAmbilight(
                heroContent = uiState.heroContent,
                currentIndex = currentHeroIndex,
                activeLayer = activeAmbilightLayer,
                onLayerChange = { activeAmbilightLayer = it }
            )
            
            // Main content - EXACT CSS structure
            LazyColumn(
                modifier = Modifier.fillMaxSize()
            ) {
                item {
                    // Hero stage - EXACT replica
                    HeroStage(
                        heroContent = uiState.heroContent,
                        currentIndex = currentHeroIndex,
                        onIndexChange = { currentHeroIndex = it },
                        onContentClick = onContentClick,
                        modifier = Modifier.padding(top = 96.dp) // main padding-top from CSS
                    )
                }
                
                item {
                    // Content sections - EXACT replica
                    ContentSections(
                        recentlyAdded = uiState.recentlyAdded,
                        discoverPreview = uiState.discoverPreview,
                        onContentClick = onContentClick,
                        modifier = Modifier.padding(horizontal = 72.dp, vertical = 80.dp) // content-shell padding
                    )
                }
            }
            
            // Fixed top navigation - EXACT CSS positioning
            TopNavigation(
                selectedProfile = selectedProfile,
                onProfileClick = onProfileClick,
                onSearchClick = onSearchClick,
                onSettingsClick = onSettingsClick,
                onNotificationsClick = onNotificationsClick,
                isScrolled = isScrolled,
                heroBackgroundImage = uiState.heroContent.getOrNull(currentHeroIndex)?.backdropUrl,
                modifier = Modifier
                    .fillMaxWidth()
                    .zIndex(20f) // CSS: z-index: 20
            )
        }
    }
}

@Composable
private fun HeroAmbilight(
    heroContent: List<Content>,
    currentIndex: Int,
    activeLayer: Int,
    onLayerChange: (Int) -> Unit
) {
    val currentHero = heroContent.getOrNull(currentIndex)
    val backgroundImage = currentHero?.backdropUrl ?: currentHero?.posterUrl
    
    LaunchedEffect(currentIndex) {
        if (backgroundImage != null) {
            // Switch ambilight layers with transition
            onLayerChange(if (activeLayer == 1) 2 else 1)
        }
    }
    
    Box(
        modifier = Modifier
            .fillMaxSize()
            .offset(x = (-18).dp, y = (-12).dp) // CSS: inset: -12% -18% -16% -18%
            .size(width = 1400.dp, height = 1200.dp) // Larger for blur effect
            .zIndex(0f)
    ) {
        // Ambilight layer 1
        AsyncImage(
            model = backgroundImage,
            contentDescription = null,
            modifier = Modifier
                .fillMaxSize()
                .scale(1.08f) // CSS: transform: scale(1.08)
                .blur(140.dp) // CSS: filter: blur(140px)
                .alpha(if (activeLayer == 1) 0.68f else 0f), // CSS: opacity transition
            contentScale = ContentScale.Crop
        )
        
        // Ambilight layer 2
        AsyncImage(
            model = backgroundImage,
            contentDescription = null,
            modifier = Modifier
                .fillMaxSize()
                .scale(1.08f)
                .blur(140.dp)
                .alpha(if (activeLayer == 2) 0.68f else 0f),
            contentScale = ContentScale.Crop
        )
    }
}

@Composable
private fun TopNavigation(
    selectedProfile: Profile?,
    onProfileClick: () -> Unit,
    onSearchClick: () -> Unit,
    onSettingsClick: () -> Unit,
    onNotificationsClick: () -> Unit,
    isScrolled: Boolean,
    heroBackgroundImage: String?,
    modifier: Modifier = Modifier
) {
    // Fixed position top nav - EXACT CSS replica
    Box(
        modifier = modifier
            .fillMaxWidth()
    ) {
        // Background layers - EXACT CSS pseudo-elements
        
        // ::before pseudo-element - hero background blur
        if (!isScrolled && heroBackgroundImage != null) {
            AsyncImage(
                model = heroBackgroundImage,
                contentDescription = null,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(200.dp) // Larger for blur effect
                    .offset(x = (-80).dp, y = (-60).dp) // CSS: inset: -60px -80px 0
                    .blur(80.dp) // CSS: filter: blur(80px) saturate(140%)
                    .alpha(0.55f) // CSS: opacity: 0.55
                    .zIndex(-2f),
                contentScale = ContentScale.Crop
            )
        }
        
        // ::after pseudo-element - gradient overlay
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(120.dp) // Enough height for gradient
                .background(
                    brush = if (isScrolled) {
                        Brush.verticalGradient(listOf(Color(0xF2040404), Color(0xF2040404)))
                    } else {
                        // CSS: linear-gradient(180deg, rgba(10, 10, 11, 0.86), rgba(10, 10, 10, 0.32), rgba(10, 10, 10, 0))
                        Brush.verticalGradient(
                            colors = listOf(
                                Color(0xDC0A0A0B), // rgba(10, 10, 11, 0.86)
                                Color(0x520A0A0A), // rgba(10, 10, 10, 0.32)
                                Color(0x000A0A0A)  // rgba(10, 10, 10, 0)
                            )
                        )
                    }
                )
                .zIndex(-1f)
        )
        
        // Main navigation content
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 56.dp, vertical = 22.dp) // CSS: padding: 22px 56px
        ) {
            // nav-inner container - EXACT CSS structure
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .widthIn(max = 1440.dp), // CSS: max-width: 1440px
                horizontalArrangement = Arrangement.SpaceBetween, // CSS: justify-content: space-between
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Left section - Profile only (EXACT HTML structure)
                Row(
                    horizontalArrangement = Arrangement.spacedBy(18.dp), // CSS: gap: 18px
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    ProfileButton(
                        profile = selectedProfile,
                        onClick = onProfileClick
                    )
                }
            
                // Center - Menu navigation (EXACT HTML structure)
                Row(
                    horizontalArrangement = Arrangement.spacedBy(26.dp), // CSS: gap: 26px
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    // Search button with EXACT CSS class "menu-item search-home"
                    IconButton(
                        onClick = onSearchClick,
                        modifier = Modifier
                            .size(40.dp) // CSS: width: 40px, height: 40px
                            .background(
                                Color.Transparent,
                                RoundedCornerShape(12.dp) // CSS: border-radius: 12px
                            )
                    ) {
                        Icon(
                            Icons.Default.Search,
                            contentDescription = "Search",
                            tint = Color(0xE6FFE2DB), // CSS: rgba(255, 226, 219, 0.9)
                            modifier = Modifier.size(20.dp)
                        )
                    }
                
                    // Menu items - EXACT CSS styling
                    MenuButton(text = "Home", isActive = true)
                    MenuButton(text = "Discover", isActive = false)
                    MenuButton(text = "Series", isActive = false)
                    MenuButton(text = "Films", isActive = false)
                    MenuButton(text = "My List", isActive = false)
                }
            
                // Right section - Brand section (EXACT HTML structure)
                Row(
                    horizontalArrangement = Arrangement.spacedBy(18.dp), // CSS: gap: 18px (from .left-section)
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    // Notifications button with EXACT CSS class "notifications-btn"
                    IconButton(
                        onClick = onNotificationsClick,
                        modifier = Modifier
                            .size(36.dp) // CSS: width: 36px, height: 36px
                            .background(
                                Color.Transparent,
                                RoundedCornerShape(10.dp) // CSS: border-radius: 10px
                            )
                    ) {
                        Icon(
                            Icons.Default.Notifications,
                            contentDescription = "Notifications",
                            tint = Color(0xDBFFECE1), // CSS: rgba(255, 236, 225, 0.86)
                            modifier = Modifier.size(18.dp)
                        )
                    }
                
                    // Settings button with EXACT CSS class "settings-btn"
                    IconButton(
                        onClick = onSettingsClick,
                        modifier = Modifier
                            .size(36.dp)
                            .background(
                                Color.Transparent,
                                RoundedCornerShape(10.dp)
                            )
                    ) {
                        Icon(
                            Icons.Default.Settings,
                            contentDescription = "Settings",
                            tint = Color(0xDBFFECE1),
                            modifier = Modifier.size(18.dp)
                        )
                    }
                
                    // Brand name with EXACT CSS class "brand-name"
                    Text(
                        text = "LANFLIX",
                        color = Color(0xDBFFECE1), // CSS: rgba(255, 236, 225, 0.86)
                        fontSize = 13.sp, // CSS: 0.82rem
                        fontWeight = FontWeight.SemiBold, // CSS: font-weight: 600
                        letterSpacing = (0.12).sp, // CSS: letter-spacing: 0.12em (converted to sp)
                        modifier = Modifier.padding(start = 12.dp) // CSS: gap: 12px from brand
                    )
                }
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
            .background(
                Color(0x59000000), // CSS: rgba(0, 0, 0, 0.35)
                RoundedCornerShape(8.dp)
            )
            .padding(horizontal = 12.dp, vertical = 6.dp)
            .padding(start = 6.dp), // CSS: padding: 6px 12px 6px 6px
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        // Profile avatar
        Box(
            modifier = Modifier
                .size(32.dp) // CSS: width: 32px, height: 32px
                .background(
                    if (profile != null) {
                        Brush.linearGradient(
                            colors = listOf(
                                Color(android.graphics.Color.parseColor(profile.avatarColorPrimary)),
                                Color(android.graphics.Color.parseColor(profile.avatarColorSecondary))
                            )
                        )
                    } else {
                        Brush.linearGradient(
                            colors = listOf(Color(0xFFff6b6b), Color(0xFFff6b6b))
                        )
                    },
                    RoundedCornerShape(6.dp) // CSS: border-radius: 6px
                ),
            contentAlignment = Alignment.Center
        ) {
            // Profile icon - matching CSS ::before pseudo-element
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                Box(
                    modifier = Modifier
                        .size(12.dp)
                        .background(
                            Color(0xE6FFFFFF), // CSS: rgba(255, 255, 255, 0.9)
                            androidx.compose.foundation.shape.CircleShape
                        )
                )
                Box(
                    modifier = Modifier
                        .size(12.dp)
                        .background(
                            Color(0xE6FFFFFF),
                            androidx.compose.foundation.shape.CircleShape
                        )
                )
            }
        }
        
        // Arrow icon (simplified)
        Text(
            text = "▼",
            color = Color(0xB3FFE2DB), // CSS: rgba(255, 226, 219, 0.7)
            fontSize = 10.sp
        )
    }
}

@Composable
private fun MenuButton(
    text: String,
    isActive: Boolean,
    onClick: () -> Unit = {}
) {
    Button(
        onClick = onClick,
        colors = ButtonDefaults.buttonColors(
            containerColor = if (isActive) {
                Color(0xF2FFFFFF) // CSS: rgba(255, 255, 255, 0.95) when active
            } else {
                Color.Transparent
            }
        ),
        shape = RoundedCornerShape(20.dp), // CSS: border-radius: 20px
        modifier = Modifier.padding(horizontal = 18.dp, vertical = 10.dp) // CSS: padding: 10px 18px
    ) {
        Text(
            text = text,
            color = if (isActive) {
                Color.Black // CSS: color: #000000 when active
            } else {
                Color(0xD1FFFFFF) // CSS: rgba(255, 255, 255, 0.82)
            },
            fontSize = 16.sp, // CSS: 0.98rem
            fontWeight = FontWeight.Medium, // CSS: font-weight: 500
            letterSpacing = (0.02).sp // CSS: letter-spacing: 0.02em (converted to sp)
        )
    }
}

@Composable
private fun HeroStage(
    heroContent: List<Content>,
    currentIndex: Int,
    onIndexChange: (Int) -> Unit,
    onContentClick: (String, String) -> Unit,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(640.dp) // CSS: height: 640px
            .padding(horizontal = 72.dp) // CSS: padding: 0 72px
    ) {
        if (heroContent.isNotEmpty()) {
            HeroCarousel(
                heroContent = heroContent,
                currentIndex = currentIndex,
                onIndexChange = onIndexChange,
                onContentClick = onContentClick
            )
        } else {
            EmptyHeroState()
        }
    }
}

@Composable
private fun HeroCarousel(
    heroContent: List<Content>,
    currentIndex: Int,
    onIndexChange: (Int) -> Unit,
    onContentClick: (String, String) -> Unit
) {
    val currentHero = heroContent.getOrNull(currentIndex)
    
    Box(
        modifier = Modifier
            .fillMaxSize()
            .clip(RoundedCornerShape(32.dp)) // CSS: border-radius: 32px
            .background(Color(0xFF090909)) // CSS: background: #090909
            .pointerInput(Unit) {
                detectHorizontalDragGestures { _, dragAmount ->
                    if (dragAmount > 50) {
                        // Swipe right - previous
                        val newIndex = if (currentIndex > 0) currentIndex - 1 else heroContent.size - 1
                        onIndexChange(newIndex)
                    } else if (dragAmount < -50) {
                        // Swipe left - next
                        val newIndex = if (currentIndex < heroContent.size - 1) currentIndex + 1 else 0
                        onIndexChange(newIndex)
                    }
                }
            }
    ) {
        currentHero?.let { hero ->
            // Hero background
            AsyncImage(
                model = hero.backdropUrl ?: hero.posterUrl,
                contentDescription = null,
                modifier = Modifier
                    .fillMaxSize()
                    .clip(RoundedCornerShape(28.dp)), // CSS: border-radius: 28px (inset 4px)
                contentScale = ContentScale.Crop
            )
            
            // Hero overlay - EXACT CSS gradient
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .clip(RoundedCornerShape(28.dp))
                    .background(
                        brush = Brush.linearGradient(
                            colors = listOf(
                                Color(0xD60A0A0A), // rgba(10, 10, 10, 0.84)
                                Color(0x730A0A0A)  // rgba(10, 10, 10, 0.45)
                            ),
                            start = Offset(0f, 0f),
                            end = Offset(1f, 1f) // 120deg gradient
                        )
                    )
            )
            
            // Hero content
            HeroContent(
                hero = hero,
                onPlayClick = { onContentClick(hero.id, hero.type.name.lowercase()) },
                onInfoClick = { onContentClick(hero.id, hero.type.name.lowercase()) }
            )
        }
    }
}

@Composable
private fun HeroContent(
    hero: Content,
    onPlayClick: () -> Unit,
    onInfoClick: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 72.dp, vertical = 164.dp) // CSS: padding: 164px 72px 120px
            .padding(bottom = 44.dp), // Adjust for bottom padding
        verticalArrangement = Arrangement.spacedBy(32.dp) // CSS: gap: 32px
    ) {
        Column(
            modifier = Modifier.widthIn(max = 620.dp), // CSS: max-width: 620px
            verticalArrangement = Arrangement.spacedBy(24.dp) // CSS: gap: 24px
        ) {
            // Hero tag
            Row(
                modifier = Modifier
                    .background(
                        Color(0x1FFFFFFF), // CSS: rgba(255, 255, 255, 0.12)
                        RoundedCornerShape(999.dp)
                    )
                    .padding(horizontal = 14.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Your Library • ${hero.genres.take(2).joinToString(", ")}",
                    color = Color(0xC7FFFFFF), // CSS: var(--text-secondary)
                    fontSize = 13.sp, // CSS: 0.8rem
                    letterSpacing = (0.1).sp, // CSS: letter-spacing: 0.1em (converted to sp)
                    fontWeight = FontWeight.Medium
                )
            }
            
            // Hero title
            Text(
                text = hero.title,
                color = Color.White,
                fontSize = 51.sp, // CSS: clamp(1.8rem, 3.5vw, 3.2rem) - using large size
                fontWeight = FontWeight.Bold, // CSS: font-weight: 700
                letterSpacing = (-0.02).sp, // CSS: letter-spacing: -0.02em (converted to sp)
                lineHeight = 1.15.em, // CSS: line-height: 1.15
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
            )
            
            // Hero meta
            Row(
                horizontalArrangement = Arrangement.spacedBy(16.dp) // CSS: gap: 16px
            ) {
                val metaItems = listOfNotNull(
                    hero.type.name.replaceFirstChar { it.uppercase() },
                    hero.year?.toString(),
                    hero.rating,
                    hero.duration?.let { "${it}m" }
                )
                
                metaItems.forEachIndexed { index, item ->
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        if (index > 0) {
                            Text(
                                text = "•",
                                color = Color(0x52FFFFFF), // CSS: opacity: 0.32
                                fontSize = 16.sp
                            )
                        }
                        Text(
                            text = item,
                            color = Color(0xC7FFFFFF), // CSS: var(--text-secondary)
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Medium // CSS: font-weight: 500
                        )
                    }
                }
            }
            
            // Hero description
            Text(
                text = hero.description ?: "No description available.",
                color = Color(0x99FFFFFF), // CSS: var(--text-muted)
                fontSize = 16.sp, // CSS: 1rem
                lineHeight = 1.7.em, // CSS: line-height: 1.7
                maxLines = 3,
                overflow = TextOverflow.Ellipsis
            )
            
            // Hero actions
            Row(
                horizontalArrangement = Arrangement.spacedBy(12.dp) // CSS: gap: 12px
            ) {
                // Primary CTA
                Button(
                    onClick = onPlayClick,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0xFFe50914) // CSS: var(--accent)
                    ),
                    shape = RoundedCornerShape(999.dp), // CSS: border-radius: 999px
                    modifier = Modifier.padding(horizontal = 28.dp, vertical = 14.dp)
                ) {
                    Text(
                        text = "▶ PLAY",
                        color = Color.White,
                        fontWeight = FontWeight.SemiBold, // CSS: font-weight: 600
                        letterSpacing = (0.04).sp // CSS: letter-spacing: 0.04em (converted to sp)
                    )
                }
                
                // Ghost CTA
                Button(
                    onClick = onInfoClick,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0x14FFFFFF) // CSS: rgba(255, 255, 255, 0.08)
                    ),
                    shape = RoundedCornerShape(999.dp),
                    modifier = Modifier.padding(horizontal = 28.dp, vertical = 14.dp)
                ) {
                    Text(
                        text = "MORE INFO",
                        color = Color.White,
                        fontWeight = FontWeight.SemiBold,
                        letterSpacing = (0.04).sp
                    )
                }
            }
        }
        
        Spacer(modifier = Modifier.weight(1f))
        
        // Hero secondary info
        Row(
            modifier = Modifier
                .align(Alignment.End)
                .background(
                    Color(0xB80F0F10), // CSS: rgba(15, 15, 16, 0.72)
                    RoundedCornerShape(18.dp)
                )
                .padding(horizontal = 22.dp, vertical = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "Downloaded",
                color = Color(0xFFe50914), // CSS: var(--accent)
                fontWeight = FontWeight.SemiBold // CSS: font-weight: 600
            )
            Text(
                text = "Ready to watch",
                color = Color(0xC7FFFFFF), // CSS: var(--text-secondary)
                fontSize = 15.sp // CSS: 0.95rem
            )
        }
    }
}

@Composable
private fun EmptyHeroState() {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .clip(RoundedCornerShape(32.dp))
            .background(
                brush = Brush.linearGradient(
                    colors = listOf(Color(0xFF1a1a1a), Color(0xFF2d2d2d))
                )
            ),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(24.dp)
        ) {
            Text(
                text = "No Content Yet",
                color = Color.White,
                fontSize = 48.sp,
                fontWeight = FontWeight.Bold
            )
            Text(
                text = "Your library is empty. Go to Discovery to find and download content to watch!",
                color = Color(0x99FFFFFF),
                fontSize = 16.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 32.dp)
            )
        }
    }
}

@Composable
private fun ContentSections(
    recentlyAdded: List<Content>,
    discoverPreview: List<Content>,
    onContentClick: (String, String) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(80.dp) // CSS: margin-top: 80px between sections
    ) {
        // Recently Added section
        if (recentlyAdded.isNotEmpty()) {
            ContentSection(
                title = "Recently Added",
                content = recentlyAdded,
                onContentClick = onContentClick
            )
        }
        
        // Discover Preview section
        if (discoverPreview.isNotEmpty()) {
            ContentSection(
                title = "Discover New Content",
                content = discoverPreview,
                onContentClick = onContentClick,
                showBrowseAll = true
            )
        }
        
        // Empty state
        if (recentlyAdded.isEmpty() && discoverPreview.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 60.dp),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(20.dp)
                ) {
                    Text(
                        text = "Your Library is Empty",
                        color = Color.White,
                        fontSize = 32.sp,
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(
                        text = "Go to Discovery to find and download content!",
                        color = Color(0x99FFFFFF),
                        fontSize = 18.sp
                    )
                }
            }
        }
    }
}

@Composable
private fun ContentSection(
    title: String,
    content: List<Content>,
    onContentClick: (String, String) -> Unit,
    showBrowseAll: Boolean = false
) {
    // Spotlight section - EXACT CSS replica
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(
                Color(0x99141417), // CSS: rgba(15, 15, 16, 0.6)
                RoundedCornerShape(32.dp)
            )
            .padding(horizontal = 36.dp, vertical = 32.dp) // CSS: padding: 32px 36px 44px
    ) {
        // Spotlight header
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 24.dp), // CSS: margin-bottom: 24px
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = title,
                color = Color.White,
                fontSize = 25.sp, // CSS: 1.6rem
                fontWeight = FontWeight.SemiBold // CSS: font-weight: 600
            )
            
            if (showBrowseAll) {
                Button(
                    onClick = { /* Navigate to discover */ },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color.Transparent
                    )
                ) {
                    Text(
                        text = "Browse All →",
                        color = Color(0xC7FFFFFF)
                    )
                }
            }
        }
        
        // Content row
        LazyRow(
            horizontalArrangement = Arrangement.spacedBy(16.dp), // CSS: gap: 16px
            modifier = Modifier.padding(vertical = 20.dp) // CSS: padding: 20px 0
        ) {
            items(content) { item ->
                MovieCard(
                    content = item,
                    onClick = { onContentClick(item.id, item.type.name.lowercase()) }
                )
            }
        }
    }
}

@Composable
private fun MovieCard(
    content: Content,
    onClick: () -> Unit
) {
    var isExpanded by remember { mutableStateOf(false) }
    val coroutineScope = rememberCoroutineScope()
    
    // Movie card - EXACT CSS replica
    Box(
        modifier = Modifier
            .width(if (isExpanded) 480.dp else 180.dp) // CSS: width: 180px, expanded: 480px
            .height(320.dp) // CSS: height: 320px
            .clip(RoundedCornerShape(16.dp)) // CSS: border-radius: 16px
            .background(Color(0x0AFFFFFF)) // CSS: rgba(255, 255, 255, 0.04)
            .clickable { 
                isExpanded = !isExpanded
                if (isExpanded) {
                    // Delay click to show expansion
                    coroutineScope.launch {
                        delay(300)
                        onClick()
                    }
                }
            } // CSS: transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1)
    ) {
        // Poster images
        AsyncImage(
            model = if (isExpanded) content.backdropUrl else content.posterUrl,
            contentDescription = content.title,
            modifier = Modifier.fillMaxSize(),
            contentScale = ContentScale.Crop
        )
        
        // Movie overlay (visible when expanded)
        if (isExpanded) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(
                        brush = Brush.verticalGradient(
                            colors = listOf(
                                Color.Transparent, // 0%
                                Color(0x4D000000), // 50% - rgba(0, 0, 0, 0.3)
                                Color(0xCC000000)  // 100% - rgba(0, 0, 0, 0.8)
                            )
                        )
                    )
                    .alpha(if (isExpanded) 1f else 0f)
            )
        }
        
        // Compact title (visible when not expanded)
        if (!isExpanded) {
            Text(
                text = content.title,
                color = Color.Transparent, // Will be styled with shadow
                fontSize = 16.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier
                    .align(Alignment.BottomStart)
                    .padding(16.dp),
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
            )
        }
        
        // Movie info (visible when expanded)
        if (isExpanded) {
            Column(
                modifier = Modifier
                    .align(Alignment.BottomStart)
                    .padding(24.dp)
                    .fillMaxWidth()
            ) {
                Text(
                    text = content.title,
                    color = Color.White,
                    fontSize = 29.sp, // CSS: 1.8rem
                    fontWeight = FontWeight.Bold, // CSS: font-weight: 700
                    lineHeight = 1.2.em,
                    modifier = Modifier.padding(bottom = 8.dp)
                )
                
                // Meta information
                Row(
                    horizontalArrangement = Arrangement.spacedBy(12.dp),
                    modifier = Modifier.padding(bottom = 12.dp)
                ) {
                    val metaItems = listOfNotNull(
                        content.genres.firstOrNull(),
                        content.year?.toString(),
                        content.duration?.let { "${it}m" },
                        content.rating
                    )
                    
                    metaItems.forEachIndexed { index, item ->
                        if (index > 0) {
                            Text(
                                text = "•",
                                color = Color(0x99FFFFFF),
                                fontSize = 14.sp
                            )
                        }
                        Text(
                            text = item,
                            color = Color(0xCCFFFFFF), // CSS: rgba(255, 255, 255, 0.8)
                            fontSize = 14.sp
                        )
                    }
                }
                
                // Description
                Text(
                    text = content.description ?: "No description available.",
                    color = Color(0xE6FFFFFF), // CSS: rgba(255, 255, 255, 0.9)
                    fontSize = 15.sp,
                    lineHeight = 1.5.em,
                    maxLines = 3,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}