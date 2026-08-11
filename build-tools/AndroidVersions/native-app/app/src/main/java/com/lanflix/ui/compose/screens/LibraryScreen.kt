package com.lanflix.ui.compose.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.lanflix.api.MusicHome
import com.lanflix.api.MusicAlbum
import com.lanflix.models.ContentItem
import com.lanflix.ui.compose.components.EmptyState
import com.lanflix.ui.compose.components.PosterCard

@Composable
fun LibraryScreen(
    media: List<ContentItem>,
    music: MusicHome?,
    selectedFilter: String,
    onFilterSelected: (String) -> Unit,
    onOpenMusic: () -> Unit,
    onSelect: (ContentItem) -> Unit,
    onMusicAlbum: (MusicAlbum) -> Unit,
    onMusicPlay: (com.lanflix.api.MusicTrack, List<com.lanflix.api.MusicTrack>) -> Unit
) {
    Column(Modifier.fillMaxSize().padding(top = 68.dp, bottom = 58.dp)) {
        LazyRow(contentPadding = PaddingValues(horizontal = 14.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            items(listOf("Movies", "Series", "Music", "Collections")) { filter ->
                FilterChip(
                    selected = filter != "Music" && selectedFilter == filter,
                    onClick = { if (filter == "Music") onOpenMusic() else onFilterSelected(filter) },
                    label = { Text(filter) }
                )
            }
        }
        val filtered = when (selectedFilter) {
            "Movies" -> media.filter { it.type.equals("movie", true) }
            "Series" -> media.filter { it.type.equals("series", true) }
            else -> emptyList()
        }
        if (filtered.isEmpty()) EmptyState("No $selectedFilter yet", "When this library is scanned, it will appear here.") else {
            LazyVerticalGrid(
                columns = GridCells.Fixed(3),
                contentPadding = PaddingValues(10.dp),
                horizontalArrangement = Arrangement.spacedBy(9.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) { items(filtered, key = { "${it.type}-${it.id}" }) { PosterCard(it, onSelect, Modifier.fillMaxWidth()) } }
        }
    }
}

// I need to move MusicLibrary too or include it here.
// MusicLibrary was in LanflixApp.kt. I'll move it to components/MediaComponents.kt or keep it here.
// Since it's specific to the library/music flow, I'll move it to components/MediaComponents.kt to be shared.
// Wait, I'll check if I already moved it. No, I didn't.
