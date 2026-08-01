package com.lanflix.ui.fragments

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.GridLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.chip.Chip
import com.google.android.material.chip.ChipGroup
import com.lanflix.adapters.PosterAdapter
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.ui.detail.ContentDetailBottomSheet
import com.lanflix.webview.R
import kotlinx.coroutines.launch

class LibrariesFragment : Fragment() {

    private val apiClient = LanflixApiClient()
    private lateinit var posterAdapter: PosterAdapter
    private var moviesCache: List<ContentItem> = emptyList()
    private var collectionsCache: List<ContentItem> = emptyList()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_libraries, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val chipGroup: ChipGroup = view.findViewById(R.id.chip_group_filters)
        val recyclerGrid: RecyclerView = view.findViewById(R.id.recycler_grid)

        posterAdapter = PosterAdapter { showDetail(it) }
        recyclerGrid.layoutManager = GridLayoutManager(context, 3)
        recyclerGrid.adapter = posterAdapter

        chipGroup.setOnCheckedStateChangeListener { _, checkedIds ->
            if (checkedIds.contains(R.id.chip_collections)) {
                posterAdapter.updateItems(collectionsCache)
            } else {
                posterAdapter.updateItems(moviesCache)
            }
        }

        moviesCache = listOf(
            ContentItem(10, title = "10 Cloverfield Lane", posterUrl = "https://image.tmdb.org/t/p/w500/10cloverfield.jpg", rating = "PG-13", releaseDate = "2016"),
            ContentItem(11, title = "2001: A Space Odyssey", posterUrl = "https://image.tmdb.org/t/p/w500/2001.jpg", rating = "G", releaseDate = "1968"),
            ContentItem(12, title = "About Time", posterUrl = "https://image.tmdb.org/t/p/w500/abouttime.jpg", rating = "R", releaseDate = "2013"),
            ContentItem(13, title = "The Abyss", posterUrl = "https://image.tmdb.org/t/p/w500/abyss.jpg", rating = "PG-13", releaseDate = "1989"),
            ContentItem(14, title = "The Adam Project", posterUrl = "https://image.tmdb.org/t/p/w500/adamproject.jpg", rating = "PG-13", releaseDate = "2022"),
            ContentItem(15, title = "Alien Romulus", posterUrl = "https://image.tmdb.org/t/p/w500/alienromulus.jpg", rating = "R", releaseDate = "2024")
        )

        collectionsCache = listOf(
            ContentItem(101, collectionName = "Marvel Cinematic Universe", itemCount = 34, posterUrl = "https://image.tmdb.org/t/p/w500/mcu.jpg"),
            ContentItem(102, collectionName = "Harry Potter Collection", itemCount = 8, posterUrl = "https://image.tmdb.org/t/p/w500/hp.jpg"),
            ContentItem(103, collectionName = "Star Wars Saga", itemCount = 11, posterUrl = "https://image.tmdb.org/t/p/w500/starwars.jpg")
        )

        posterAdapter.updateItems(moviesCache)

        lifecycleScope.launch {
            val fetchedMovies = apiClient.getHomeContent()
            val fetchedCollections = apiClient.getCollections()
            if (fetchedMovies.isNotEmpty()) {
                moviesCache = fetchedMovies
                posterAdapter.updateItems(moviesCache)
            }
            if (fetchedCollections.isNotEmpty()) {
                collectionsCache = fetchedCollections
            }
        }
    }

    private fun showDetail(item: ContentItem) {
        val sheet = ContentDetailBottomSheet(item)
        sheet.show(parentFragmentManager, "ContentDetail")
    }
}
