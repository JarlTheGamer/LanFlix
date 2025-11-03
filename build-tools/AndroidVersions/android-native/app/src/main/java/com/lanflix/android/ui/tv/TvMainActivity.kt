package com.lanflix.android.ui.tv

import android.app.Activity
import android.os.Bundle
import androidx.leanback.app.BrowseSupportFragment
import androidx.leanback.widget.*
import com.lanflix.android.R
import com.lanflix.android.domain.model.Content
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class TvMainActivity : Activity() {
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_tv_main)
        
        val fragment = TvBrowseFragment()
        fragmentManager.beginTransaction()
            .replace(R.id.tv_main_browse_fragment, fragment)
            .commit()
    }
}

class TvBrowseFragment : BrowseSupportFragment() {
    
    private lateinit var rowsAdapter: ArrayObjectAdapter
    
    override fun onActivityCreated(savedInstanceState: Bundle?) {
        super.onActivityCreated(savedInstanceState)
        
        setupUI()
        loadRows()
        setupEventListeners()
    }
    
    private fun setupUI() {
        // Set brand color and banner
        brandColor = resources.getColor(R.color.lanflix_primary, null)
        badgeDrawable = resources.getDrawable(R.drawable.lanflix_banner, null)
        
        // Set search icon
        isHeadersTransitionOnBackEnabled = true
        
        // Prepare the manager for different rows of content
        rowsAdapter = ArrayObjectAdapter(ListRowPresenter())
        adapter = rowsAdapter
    }
    
    private fun loadRows() {
        // Create different content categories (Netflix-style)
        val categories = listOf(
            "Continue Watching",
            "Popular Movies",
            "New Releases", 
            "TV Series",
            "Action Movies",
            "Comedy Movies",
            "Drama Series"
        )
        
        categories.forEach { category ->
            val listRowAdapter = ArrayObjectAdapter(ContentCardPresenter())
            
            // TODO: Load actual content from repository
            // For now, add placeholder content
            repeat(10) { index ->
                val content = createPlaceholderContent(category, index)
                listRowAdapter.add(content)
            }
            
            val header = HeaderItem(category)
            rowsAdapter.add(ListRow(header, listRowAdapter))
        }
    }
    
    private fun setupEventListeners() {
        onItemViewClickedListener = ItemViewClickedListener()
        onItemViewSelectedListener = ItemViewSelectedListener()
    }
    
    private fun createPlaceholderContent(category: String, index: Int): Content {
        return Content(
            id = "${category}_$index",
            title = "$category Item $index",
            type = if (category.contains("Series")) 
                com.lanflix.android.domain.model.ContentType.SERIES 
            else 
                com.lanflix.android.domain.model.ContentType.MOVIE,
            year = 2020 + (index % 4),
            description = "This is a placeholder description for $category item $index",
            posterUrl = "https://via.placeholder.com/300x450/6366F1/FFFFFF?text=${category.replace(" ", "+")}"
        )
    }
    
    private inner class ItemViewClickedListener : OnItemViewClickedListener {
        override fun onItemClicked(
            itemViewHolder: Presenter.ViewHolder?,
            item: Any?,
            rowViewHolder: RowPresenter.ViewHolder?,
            row: Row?
        ) {
            if (item is Content) {
                // Navigate to content details or start playback
                // TODO: Implement navigation
            }
        }
    }
    
    private inner class ItemViewSelectedListener : OnItemViewSelectedListener {
        override fun onItemSelected(
            itemViewHolder: Presenter.ViewHolder?,
            item: Any?,
            rowViewHolder: RowPresenter.ViewHolder?,
            row: Row?
        ) {
            // Handle item selection for background updates, etc.
        }
    }
}