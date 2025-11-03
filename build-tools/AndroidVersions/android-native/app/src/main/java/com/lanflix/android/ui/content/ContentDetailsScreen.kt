package com.lanflix.android.ui.content

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import androidx.hilt.navigation.compose.hiltViewModel
import coil.compose.AsyncImage
import com.lanflix.android.domain.model.Content
import com.lanflix.android.ui.theme.LanflixTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ContentDetailsScreen(
    contentId: String,
    onBackClick: () -> Unit,
    onPlayClick: () -> Unit,
    viewModel: ContentDetailsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    LaunchedEffect(contentId) {
        viewModel.loadContent(contentId)
    }
    
    LanflixTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0xFF050505)) // Exact body background
        ) {
            when {
                uiState.isLoading -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator(
                            color = Color(0xFFe50914) // --accent
                        )
                    }
                }
                
                uiState.error != null -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "Error loading content",
                                color = Color.White,
                                fontSize = 18.sp
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = uiState.error,
                                color = Color(0x99FFFFFF),
                                fontSize = 14.sp
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Button(
                                onClick = { viewModel.loadContent(contentId) },
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = Color(0xFFe50914)
                                )
                            ) {
                                Text("Retry")
                            }
                        }
                    }
                }
                
                uiState.content != null -> {
                    ContentDetailsContent(
                        content = uiState.content,
                        onBackClick = onBackClick,
                        onPlayClick = onPlayClick
                    )
                }
            }
        }
    }
}

@Composable
private fun ContentDetailsContent(
    content: Content,
    onBackClick: () -> Unit,
    onPlayClick: () -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize()
    ) {
        item {
            // Hero section with backdrop
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(600.dp)
            ) {
                // Backdrop image
                AsyncImage(
                    model = content.backdropUrl ?: content.posterUrl,
                    contentDescription = content.title,
                    modifier = Modifier.fillMaxSize(),
                    contentScale = ContentScale.Crop
                )
                
                // Gradient overlay
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(
                            brush = Brush.verticalGradient(
                                colors = listOf(
                                    Color.Transparent,
                                    Color(0x4D000000),
                                    Color(0xCC000000),
                                    Color(0xFF050505)
                                )
                            )
                        )
                )
                
                // Back button
                IconButton(
                    onClick = onBackClick,
                    modifier = Modifier
                        .padding(16.dp)
                        .background(
                            Color(0x80000000),
                            RoundedCornerShape(50)
                        )
                ) {
                    Icon(
                        Icons.Default.ArrowBack,
                        contentDescription = "Back",
                        tint = Color.White
                    )
                }
                
                // Content info
                Column(
                    modifier = Modifier
                        .align(Alignment.BottomStart)
                        .padding(32.dp)
                        .fillMaxWidth()
                ) {
                    Text(
                        text = content.title,
                        color = Color.White,
                        fontSize = 48.sp,
                        fontWeight = FontWeight.Bold,
                        lineHeight = 1.1.em
                    )
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    // Meta info
                    Row(
                        horizontalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        val metaItems = listOfNotNull(
                            content.type.name.replaceFirstChar { it.uppercase() },
                            content.year?.toString(),
                            content.rating,
                            content.duration?.let { "${it}m" }
                        )
                        
                        metaItems.forEachIndexed { index, item ->
                            if (index > 0) {
                                Text(
                                    text = "•",
                                    color = Color(0x99FFFFFF),
                                    fontSize = 16.sp
                                )
                            }
                            Text(
                                text = item,
                                color = Color(0xC7FFFFFF),
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    // Description
                    Text(
                        text = content.description ?: "No description available.",
                        color = Color(0x99FFFFFF),
                        fontSize = 16.sp,
                        lineHeight = 1.5.em,
                        maxLines = 3,
                        overflow = TextOverflow.Ellipsis
                    )
                    
                    Spacer(modifier = Modifier.height(24.dp))
                    
                    // Action buttons
                    Row(
                        horizontalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        Button(
                            onClick = onPlayClick,
                            colors = ButtonDefaults.buttonColors(
                                containerColor = Color(0xFFe50914)
                            ),
                            shape = RoundedCornerShape(8.dp),
                            modifier = Modifier.height(48.dp)
                        ) {
                            Icon(
                                Icons.Default.PlayArrow,
                                contentDescription = null,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = "Play",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                        
                        OutlinedButton(
                            onClick = { /* Add to list */ },
                            colors = ButtonDefaults.outlinedButtonColors(
                                contentColor = Color.White
                            ),
                            border = androidx.compose.foundation.BorderStroke(
                                1.dp, 
                                Color(0x4DFFFFFF)
                            ),
                            shape = RoundedCornerShape(8.dp),
                            modifier = Modifier.height(48.dp)
                        ) {
                            Icon(
                                Icons.Default.Add,
                                contentDescription = null,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = "My List",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                    }
                }
            }
        }
        
        item {
            // Additional details section
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(32.dp)
            ) {
                if (content.genres.isNotEmpty()) {
                    DetailSection(
                        title = "Genres",
                        content = content.genres.joinToString(", ")
                    )
                }
                
                if (content.cast.isNotEmpty()) {
                    DetailSection(
                        title = "Cast",
                        content = content.cast.take(5).joinToString(", ")
                    )
                }
                
                content.director?.let { director ->
                    DetailSection(
                        title = "Director",
                        content = director
                    )
                }
            }
        }
    }
}

@Composable
private fun DetailSection(
    title: String,
    content: String
) {
    Column(
        modifier = Modifier.padding(vertical = 8.dp)
    ) {
        Text(
            text = title,
            color = Color(0xC7FFFFFF),
            fontSize = 14.sp,
            fontWeight = FontWeight.SemiBold
        )
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = content,
            color = Color(0x99FFFFFF),
            fontSize = 14.sp,
            lineHeight = 1.4.em
        )
    }
}