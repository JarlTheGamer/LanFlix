package com.lanflix.android.ui.components

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import coil.compose.AsyncImage
import com.lanflix.android.domain.model.Content
import kotlinx.coroutines.delay

@Composable
fun MovieCard(
    content: Content,
    isExpanded: Boolean,
    isFocused: Boolean,
    onClick: () -> Unit,
    onPlayClick: () -> Unit,
    onInfoClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val configuration = LocalConfiguration.current
    val isTablet = configuration.screenWidthDp <= 768
    val isMobile = configuration.screenWidthDp <= 480
    
    // Responsive dimensions matching CSS media queries
    val cardWidth by animateDpAsState(
        targetValue = when {
            isExpanded -> when {
                isMobile -> 320.dp
                isTablet -> 380.dp
                else -> 480.dp
            }
            else -> when {
                isMobile -> 120.dp
                isTablet -> 140.dp
                else -> 180.dp
            }
        },
        animationSpec = tween(
            durationMillis = 600,
            easing = CubicBezierEasing(0.4f, 0f, 0.2f, 1f)
        ), // CSS: transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1)
        label = "card_width"
    )
    
    val cardHeight = when {
        isExpanded && isMobile -> 180.dp
        isExpanded && isTablet -> 214.dp
        isExpanded -> 320.dp
        isMobile -> 214.dp
        isTablet -> 250.dp
        else -> 320.dp
    }
    
    // Transform animations
    val scale by animateFloatAsState(
        targetValue = if (isFocused && !isExpanded) 1.05f else 1f,
        animationSpec = tween(200),
        label = "card_scale"
    )
    
    val elevation by animateDpAsState(
        targetValue = when {
            isExpanded -> 20.dp
            isFocused -> 16.dp
            else -> 8.dp
        },
        label = "card_elevation"
    )
    
    Card(
        modifier = modifier
            .width(cardWidth)
            .height(cardHeight)
            .zIndex(if (isExpanded) 10f else 0f) // CSS: z-index: 10 when expanded
            .clickable { onClick() }
            .then(
                if (isFocused && !isExpanded) {
                    Modifier.offset(y = (-8).dp) // CSS: transform: translateY(-8px) when focused
                } else {
                    Modifier
                }
            ),
        shape = RoundedCornerShape(16.dp), // CSS: border-radius: 16px
        colors = CardDefaults.cardColors(
            containerColor = Color(0x0AFFFFFF) // CSS: rgba(255, 255, 255, 0.04)
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = elevation
        )
    ) {
        Box(
            modifier = Modifier.fillMaxSize()
        ) {
            // Movie poster container
            Box(
                modifier = Modifier.fillMaxSize()
            ) {
                // Regular poster (visible when not expanded)
                AsyncImage(
                    model = content.posterUrl,
                    contentDescription = content.title,
                    modifier = Modifier
                        .fillMaxSize()
                        .alpha(if (isExpanded) 0f else 1f), // CSS: opacity transition
                    contentScale = ContentScale.Crop
                )
                
                // Expanded poster (backdrop, visible when expanded)
                AsyncImage(
                    model = content.backdropUrl ?: content.posterUrl,
                    contentDescription = content.title,
                    modifier = Modifier
                        .fillMaxSize()
                        .alpha(if (isExpanded) 1f else 0f), // CSS: opacity transition
                    contentScale = ContentScale.Crop
                )
            }
            
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
                    color = Color.Transparent, // CSS: color: transparent
                    fontSize = 16.sp, // CSS: 1rem
                    fontWeight = FontWeight.SemiBold, // CSS: font-weight: 600
                    modifier = Modifier
                        .align(Alignment.BottomStart)
                        .padding(16.dp)
                        .alpha(if (isExpanded) 0f else 1f), // CSS: opacity transition
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }
            
            // Movie info (visible when expanded)
            if (isExpanded) {
                MovieInfo(
                    content = content,
                    onPlayClick = onPlayClick,
                    onInfoClick = onInfoClick,
                    modifier = Modifier
                        .align(Alignment.BottomStart)
                        .fillMaxWidth()
                )
            }
            
            // Focus indicator
            if (isFocused) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(
                            Color.Transparent,
                            RoundedCornerShape(16.dp)
                        )
                        .padding(4.dp)
                        .background(
                            Color.Transparent,
                            RoundedCornerShape(12.dp)
                        )
                        // CSS: outline: 4px solid rgba(255, 255, 255, 0.9)
                )
            }
        }
    }
}

@Composable
private fun MovieInfo(
    content: Content,
    onPlayClick: () -> Unit,
    onInfoClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    var isVisible by remember { mutableStateOf(false) }
    
    LaunchedEffect(Unit) {
        delay(300) // CSS: transition delay 0.3s
        isVisible = true
    }
    
    Column(
        modifier = modifier
            .background(
                brush = Brush.verticalGradient(
                    colors = listOf(
                        Color.Transparent, // 0%
                        Color(0xB3000000), // 40% - rgba(0, 0, 0, 0.7)
                        Color(0xE6000000)  // 100% - rgba(0, 0, 0, 0.9)
                    )
                )
            )
            .padding(24.dp)
            .alpha(if (isVisible) 1f else 0f)
            .offset(y = if (isVisible) 0.dp else 100.dp) // CSS: transform: translateY(100%) -> translateY(0)
    ) {
        // Movie title
        Text(
            text = content.title,
            color = Color.White,
            fontSize = 29.sp, // CSS: 1.8rem
            fontWeight = FontWeight.Bold, // CSS: font-weight: 700
            lineHeight = 1.2.em, // CSS: line-height: 1.2
            modifier = Modifier.padding(bottom = 8.dp)
        )
        
        // Movie meta
        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp), // CSS: gap: 12px
            modifier = Modifier.padding(bottom = 12.dp)
        ) {
            val metaItems = buildList {
                content.genres.firstOrNull()?.let { add(it) }
                content.year?.let { add(it.toString()) }
                content.duration?.let { add("${it}m") }
                content.rating?.let { add(it) }
            }
            
            metaItems.forEachIndexed { index, item ->
                if (index > 0) {
                    Text(
                        text = "•",
                        color = Color(0x99FFFFFF), // CSS: opacity: 0.6
                        fontSize = 14.sp
                    )
                }
                Text(
                    text = item,
                    color = Color(0xCCFFFFFF), // CSS: rgba(255, 255, 255, 0.8)
                    fontSize = 14.sp // CSS: 0.9rem
                )
            }
        }
        
        // Movie description
        Text(
            text = content.description ?: "No description available.",
            color = Color(0xE6FFFFFF), // CSS: rgba(255, 255, 255, 0.9)
            fontSize = 15.sp, // CSS: 0.95rem
            lineHeight = 1.5.em, // CSS: line-height: 1.5
            maxLines = 3,
            overflow = TextOverflow.Ellipsis
        )
        
        Spacer(modifier = Modifier.height(16.dp))
        
        // Action buttons
        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Button(
                onClick = onPlayClick,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFFe50914) // CSS: var(--accent)
                ),
                shape = RoundedCornerShape(4.dp)
            ) {
                Text(
                    text = "▶ Play",
                    color = Color.White,
                    fontWeight = FontWeight.SemiBold
                )
            }
            
            OutlinedButton(
                onClick = onInfoClick,
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = Color.White
                ),
                border = androidx.compose.foundation.BorderStroke(1.dp, Color(0x4DFFFFFF)),
                shape = RoundedCornerShape(4.dp)
            ) {
                Text(
                    text = "More Info",
                    fontWeight = FontWeight.SemiBold
                )
            }
        }
    }
}