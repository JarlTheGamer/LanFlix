package com.lanflix.ui.compose.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.blur
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.LanflixUiState
import com.lanflix.ui.compose.components.Hero
import com.lanflix.ui.compose.components.MediaShelf
import com.lanflix.ui.compose.components.MusicPreview
import com.lanflix.ui.compose.components.OfflineNotice
import com.lanflix.ui.compose.theme.DefaultArtworkPalette

@Composable
fun HomeScreen(
    state: LanflixUiState,
    onSelect: (ContentItem) -> Unit,
    onRetry: () -> Unit,
    onOpenMusic: () -> Unit
) {
    val hero = state.library.firstOrNull()
    var targetPalette by remember(hero?.id) { mutableStateOf(DefaultArtworkPalette) }
    LaunchedEffect(hero?.id) { targetPalette = DefaultArtworkPalette }
    val artworkPalette = targetPalette
    Box(
        Modifier.fillMaxSize().background(Color(0xFF090A0E))
    ) {
        if (hero != null) {
            AsyncImage(
                model = hero.resolvedBackdropUrl ?: hero.resolvedPosterUrl,
                contentDescription = null,
                modifier = Modifier.fillMaxSize().blur(45.dp).alpha(.75f),
                contentScale = ContentScale.Crop
            )
            Box(
                Modifier.fillMaxSize().background(
                    Brush.radialGradient(
                        colors = listOf(artworkPalette.glow.copy(alpha = .55f), Color.Transparent),
                        center = Offset(900f, 650f),
                        radius = 1700f
                    )
                )
            )
            Box(
                Modifier.fillMaxSize().background(
                    Brush.radialGradient(
                        colors = listOf(artworkPalette.accent.copy(alpha = .42f), Color.Transparent),
                        center = Offset(100f, 1500f),
                        radius = 1500f
                    )
                )
            )
            Box(
                Modifier.fillMaxSize().background(
                    Brush.verticalGradient(
                        0f to Color.Black.copy(alpha = .35f),
                        .25f to Color.Transparent,
                        .65f to artworkPalette.glow.copy(alpha = .20f),
                        1f to Color.Black.copy(alpha = .55f)
                    )
                )
            )
        }
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(bottom = 92.dp)
        ) {
            item { Hero(item = hero, loading = state.loading, onSelect = onSelect, onRetry = onRetry, palette = artworkPalette, onArtworkPalette = { targetPalette = it }) }
            // Note: OfflineNotice is currently left in LanflixApp.kt as a simple local component or could be moved.
            // I'll check if I should move it. The plan says "Keep only the main LanflixApp orchestration and OfflineNotice".
            // So I'll expect it to be passed or handled.
            // Wait, HomeScreen uses OfflineNotice in the original code:
            // if (!state.online) item { OfflineNotice() }
            // I'll move OfflineNotice to components or keep it here if it's only used here.
            // The plan said: "Keep only ... OfflineNotice".
            // If I keep it in LanflixApp, I can't easily use it here without making it public or passing it.
            // I'll move it to components/MediaComponents.kt for now.
            if (!state.online) item { OfflineNotice() }
            if (state.library.isNotEmpty()) {
                item { MediaShelf("Continue Watching", state.library.take(8), onSelect) }
                item { MediaShelf("Recently Added", state.library.drop(1).take(10), onSelect) }
                item { MediaShelf("Because it’s movie night", state.library.shuffled().take(8), onSelect) }
            }
            item { MusicPreview(onClick = onOpenMusic) }
        }
    }
}
