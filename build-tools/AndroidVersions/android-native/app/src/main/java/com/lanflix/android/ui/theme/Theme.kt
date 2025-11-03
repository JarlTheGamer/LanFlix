package com.lanflix.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

// Exact colors from your web app CSS
private val BgPrimary = Color(0xFF0b0b0c) // --bg-primary
private val BgSecondary = Color(0xFF141417) // --bg-secondary  
private val Accent = Color(0xFFe50914) // --accent (Netflix red)
private val TextPrimary = Color(0xFFffffff) // --text-primary
private val TextSecondary = Color(0xC7ffffff) // --text-secondary (rgba(255, 255, 255, 0.78))
private val TextMuted = Color(0x99ffffff) // --text-muted (rgba(255, 255, 255, 0.6))

private val DarkColorScheme = darkColorScheme(
    primary = Accent,
    secondary = BgSecondary,
    background = BgPrimary,
    surface = BgSecondary,
    onPrimary = Color.White,
    onSecondary = TextPrimary,
    onBackground = TextPrimary,
    onSurface = TextPrimary
)

private val LightColorScheme = lightColorScheme(
    primary = Accent,
    secondary = BgSecondary,
    background = Color.White,
    surface = Color(0xFFF5F5F5),
    onPrimary = Color.White,
    onSecondary = Color.White,
    onBackground = Color.Black,
    onSurface = Color.Black
)

@Composable
fun LanflixTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) {
        DarkColorScheme
    } else {
        LightColorScheme
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}