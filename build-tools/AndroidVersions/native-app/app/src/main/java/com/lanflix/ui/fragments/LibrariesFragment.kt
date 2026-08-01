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

    private lateinit var apiClient: LanflixApiClient
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
        apiClient = LanflixApiClient(requireContext())

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

        posterAdapter.updateItems(emptyList())

        lifecycleScope.launch {
            val fetchedMovies = apiClient.getMovies()
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
