package com.lanflix.ui.compose

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

val LanflixGold = Color(0xFFE5A00D)
val LanflixBackground = Color(0xFF06070B)
val LanflixSurface = Color(0xFF101218)
val LanflixSurfaceRaised = Color(0xFF181B22)
val LanflixMuted = Color(0xFFA7ABB5)

private val LanflixColors = darkColorScheme(
    primary = LanflixGold,
    onPrimary = Color.Black,
    background = LanflixBackground,
    onBackground = Color.White,
    surface = LanflixSurface,
    onSurface = Color.White,
    surfaceVariant = LanflixSurfaceRaised,
    onSurfaceVariant = LanflixMuted,
    outline = Color.White.copy(alpha = 0.16f)
)

@Composable
fun LanflixTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = LanflixColors, content = content)
}
