package com.lanflix.android.ui.profile

import androidx.compose.animation.animateContentSize
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.lanflix.android.domain.model.Profile
import com.lanflix.android.ui.theme.LanflixTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProfileSelectionScreen(
    onProfileSelected: (Profile) -> Unit,
    viewModel: ProfileViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    LaunchedEffect(Unit) {
        try {
            println("ProfileSelectionScreen: Loading profiles...")
            viewModel.loadProfiles()
        } catch (e: Exception) {
            println("ProfileSelectionScreen: Error in LaunchedEffect: ${e.message}")
            e.printStackTrace()
        }
    }
    
    LanflixTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0xFF050505)) // Exact body background from CSS
        ) {
            // Background animation matching web design
            BackgroundAnimation()
            
            // Profile selection container - exact layout from web
            Row(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(horizontal = 80.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Left section - profiles vertical bar
                Column(
                    modifier = Modifier.weight(0.4f),
                    horizontalAlignment = Alignment.Start,
                    verticalArrangement = Arrangement.Center
                ) {
                    when {
                        uiState.isLoading -> {
                            Box(
                                modifier = Modifier
                                    .padding(32.dp)
                                    .background(
                                        Color(0x0AFFFFFF), // rgba(255, 255, 255, 0.04)
                                        RoundedCornerShape(24.dp)
                                    )
                                    .padding(40.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                CircularProgressIndicator(
                                    color = Color(0xFFe50914), // --accent color
                                    strokeWidth = 2.dp
                                )
                            }
                        }
                        
                        uiState.error != null -> {
                            ErrorState(
                                error = uiState.error ?: "Profile loading error",
                                onRetry = { viewModel.loadProfiles() }
                            )
                        }
                        
                        else -> {
                            ProfilesVerticalBar(
                                profiles = uiState.profiles,
                                onProfileClick = onProfileSelected
                            )
                        }
                    }
                }
                
                // Right section - title and subtitle
                Column(
                    modifier = Modifier
                        .weight(0.6f)
                        .padding(start = 80.dp),
                    horizontalAlignment = Alignment.Start,
                    verticalArrangement = Arrangement.Center
                ) {
                    Text(
                        text = "Who's watching?",
                        color = Color.White,
                        fontSize = 80.sp, // clamp(3rem, 6vw, 5rem) - using large size
                        fontWeight = FontWeight.ExtraBold, // font-weight: 800
                        letterSpacing = (-0.03).em, // letter-spacing: -0.03em
                        lineHeight = 1.1.em, // line-height: 1.1
                        modifier = Modifier.padding(bottom = 16.dp)
                    )
                    
                    Text(
                        text = "Select your profile to continue",
                        color = Color(0xC7FFFFFF), // --text-secondary
                        fontSize = 19.sp, // 1.2rem
                        fontWeight = FontWeight.Normal // font-weight: 400
                    )
                }
            }
        }
    }
}

@Composable
private fun ProfilesVerticalBar(
    profiles: List<Profile>,
    onProfileClick: (Profile) -> Unit
) {
    // Exact styling from .profiles-vertical-bar CSS
    LazyColumn(
        verticalArrangement = Arrangement.spacedBy(24.dp),
        modifier = Modifier
            .padding(horizontal = 24.dp, vertical = 32.dp)
    ) {
        itemsIndexed(profiles) { index, profile ->
            ProfileItem(
                profile = profile,
                onClick = { onProfileClick(profile) }
            )
        }
    }
}

@Composable
private fun ErrorState(
    error: String,
    onRetry: () -> Unit
) {
    Column(
        modifier = Modifier
            .background(
                Color(0x0AFFFFFF), // rgba(255, 255, 255, 0.04)
                RoundedCornerShape(24.dp)
            )
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "Error loading profiles",
            color = Color(0xFFe50914), // --accent color for errors
            fontSize = 18.sp,
            fontWeight = FontWeight.Medium
        )
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = error,
            color = Color(0x99FFFFFF), // --text-muted
            fontSize = 14.sp,
            textAlign = TextAlign.Center
        )
        Spacer(modifier = Modifier.height(16.dp))
        Button(
            onClick = onRetry,
            colors = ButtonDefaults.buttonColors(
                containerColor = Color(0xFFe50914) // --accent
            ),
            shape = RoundedCornerShape(999.dp) // border-radius: 999px
        ) {
            Text("Retry", color = Color.White)
        }
    }
}

@Composable
private fun ProfileItem(
    profile: Profile,
    onClick: () -> Unit
) {
    var isFocused by remember { mutableStateOf(false) }
    
    // Exact styling from .profile-item CSS
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier
            .clickable { onClick() }
            .background(
                Color(0x0AFFFFFF), // rgba(255, 255, 255, 0.04)
                RoundedCornerShape(16.dp)
            )
            .padding(horizontal = 16.dp, vertical = 20.dp)
            .animateContentSize()
            .then(
                if (isFocused) {
                    Modifier.scale(1.05f)
                } else {
                    Modifier.scale(1f)
                }
            )
    ) {
        // Profile avatar large - exact styling from CSS
        Box(
            modifier = Modifier
                .size(64.dp)
                .clip(RoundedCornerShape(12.dp)) // border-radius: 12px
                .background(
                    brush = Brush.linearGradient(
                        colors = listOf(
                            Color(android.graphics.Color.parseColor(profile.avatarColorPrimary)),
                            Color(android.graphics.Color.parseColor(profile.avatarColorSecondary))
                        ),
                        start = Offset(0f, 0f),
                        end = Offset(1f, 1f) // 135deg gradient
                    )
                )
                .then(
                    if (isFocused) {
                        Modifier.scale(1.15f)
                    } else {
                        Modifier.scale(1f)
                    }
                ),
            contentAlignment = Alignment.Center
        ) {
            // Profile icon - matching the ::before pseudo-element
            Column(
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Box(
                    modifier = Modifier
                        .size(18.dp)
                        .background(
                            Color(0xF2FFFFFF), // rgba(255, 255, 255, 0.95)
                            CircleShape
                        )
                )
                Spacer(modifier = Modifier.height(6.dp)) // 24px - 9px offset
                Box(
                    modifier = Modifier
                        .size(18.dp)
                        .background(
                            Color(0xF2FFFFFF), // rgba(255, 255, 255, 0.95)
                            CircleShape
                        )
                )
            }
        }
        
        Spacer(modifier = Modifier.height(12.dp))
        
        // Profile name - exact styling from CSS
        Text(
            text = profile.name,
            color = Color.White,
            fontSize = 16.sp, // 1rem
            fontWeight = FontWeight.SemiBold, // font-weight: 600
            textAlign = TextAlign.Center
        )
    }
}

@Composable
private fun BackgroundAnimation() {
    // Exact background animation from web - moving tiles
    Box(
        modifier = Modifier.fillMaxSize()
    ) {
        // Background container
        Box(
            modifier = Modifier
                .fillMaxSize()
                .offset(x = (-25).dp, y = (-25).dp)
                .size(width = 1500.dp, height = 1500.dp) // 150% width/height
                .rotate(-20f) // transform: rotate(-20deg)
        ) {
            // Create rows of moving tiles
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxSize()
            ) {
                items(25) { rowIndex ->
                    BackgroundRow(
                        isOddRow = rowIndex % 2 == 1
                    )
                }
            }
        }
        
        // Background overlay - exact gradient from CSS
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    brush = Brush.horizontalGradient(
                        colors = listOf(
                            Color(0xC4000000), // rgba(0, 0, 0, 0.767)
                            Color(0xAD000000), // rgba(0, 0, 0, 0.678) at 30%
                            Color(0x3B000000), // rgba(0, 0, 0, 0.233) at 70%
                            Color(0x00000000)  // rgba(0, 0, 0, 0) at 100%
                        ),
                        startX = 0f,
                        endX = Float.POSITIVE_INFINITY
                    )
                )
        )
    }
}

@Composable
private fun BackgroundRow(isOddRow: Boolean) {
    val infiniteTransition = rememberInfiniteTransition(label = "background_animation")
    
    val offsetX by infiniteTransition.animateFloat(
        initialValue = if (isOddRow) -50f else 0f,
        targetValue = if (isOddRow) 0f else -50f,
        animationSpec = infiniteRepeatable(
            animation = tween(80000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "row_offset"
    )
    
    LazyRow(
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        modifier = Modifier.offset(x = offsetX.dp)
    ) {
        items(60) { // 30 tiles * 2 for seamless loop
            BackgroundTile()
        }
    }
}

@Composable
private fun BackgroundTile() {
    // Placeholder movie posters - you can replace with actual images
    val placeholderImages = listOf(
        "https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg",
        "https://image.tmdb.org/t/p/w500/1M876KPjulVwppEpldhdc8V4o68.jpg",
        "https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg",
        "https://image.tmdb.org/t/p/w500/fqldf2t8ztc9aiwn3k6mlX3tvRT.jpg",
        "https://image.tmdb.org/t/p/w500/sWgBv7LV2PRoQgkxwlibdGXKz1S.jpg"
    )
    
    Box(
        modifier = Modifier
            .size(width = 140.dp, height = 95.dp)
            .clip(RoundedCornerShape(8.dp))
            .background(Color(0x4D000000)) // Fallback color
            .alpha(0.3f) // opacity: 0.3
    ) {
        // You can add AsyncImage here to load actual movie posters
        // For now, using a gradient as placeholder
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    brush = Brush.linearGradient(
                        colors = listOf(
                            Color(0xFF141417),
                            Color(0xFF0b0b0c)
                        )
                    )
                )
        )
    }
}