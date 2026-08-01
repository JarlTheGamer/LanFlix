package com.lanflix.ui.fragments

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.google.android.material.button.MaterialButton
import com.lanflix.adapters.PosterAdapter
import com.lanflix.api.LanflixApiClient
import com.lanflix.models.ContentItem
import com.lanflix.ui.detail.ContentDetailBottomSheet
import com.lanflix.webview.R
import kotlinx.coroutines.launch

class HomeFragment : Fragment() {

    private val apiClient = LanflixApiClient()
    private lateinit var continueAdapter: PosterAdapter
    private lateinit var recentAdapter: PosterAdapter
    private var heroItem: ContentItem? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_home, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val imgHeroBackdrop: ImageView = view.findViewById(R.id.hero_img_backdrop)
        val txtHeroTitle: TextView = view.findViewById(R.id.hero_txt_title)
        val txtHeroSynopsis: TextView = view.findViewById(R.id.hero_txt_synopsis)
        val btnHeroResume: MaterialButton = view.findViewById(R.id.hero_btn_resume)
        val recyclerContinue: RecyclerView = view.findViewById(R.id.recycler_continue)
        val recyclerRecent: RecyclerView = view.findViewById(R.id.recycler_recent)

        continueAdapter = PosterAdapter { showDetail(it) }
        recentAdapter = PosterAdapter { showDetail(it) }

        recyclerContinue.layoutManager = LinearLayoutManager(context, LinearLayoutManager.HORIZONTAL, false)
        recyclerContinue.adapter = continueAdapter

        recyclerRecent.layoutManager = LinearLayoutManager(context, LinearLayoutManager.HORIZONTAL, false)
        recyclerRecent.adapter = recentAdapter

        val sampleItems = listOf(
            ContentItem(1, title = "My Neighbor Totoro", overview = "A heartwarming tale of two sisters befriending forest spirits while navigating life challenges.", posterUrl = "https://image.tmdb.org/t/p/w500/rtGSpZFmYrXD2vvo4eeZqq3z1wL.jpg", backdropUrl = "https://image.tmdb.org/t/p/w1280/etj8E2o0xI1pW6aN3CheM28v20.jpg", rating = "PG", releaseDate = "1988"),
            ContentItem(2, title = "The Sandlot", overview = "In the summer of 1962, a new boy in town is taken under the wing of a young baseball prodigy.", posterUrl = "https://image.tmdb.org/t/p/w500/8c707C947sR6K0nZ.jpg", rating = "PG", releaseDate = "1993"),
            ContentItem(3, title = "The Wild Robot", overview = "After a shipwreck, an intelligent robot is stranded on an uninhabited island.", posterUrl = "https://image.tmdb.org/t/p/w500/wTnV3Dp6FJx2yXSp15iF1Z517g5.jpg", rating = "PG", releaseDate = "2024"),
            ContentItem(4, title = "Labyrinth", overview = "Sixteen-year-old Sarah is given 13 hours to solve a labyrinth and rescue her baby brother.", posterUrl = "https://image.tmdb.org/t/p/w500/r0L7rW0nJ34pQY83H8g4YnJ.jpg", rating = "PG", releaseDate = "1986"),
            ContentItem(5, title = "Jackie Brown", overview = "A flight attendant with a criminal past gets nabbed by the FBI for smuggling money.", posterUrl = "https://image.tmdb.org/t/p/w500/tdmL0c89o1XmN00h.jpg", rating = "R", releaseDate = "1997"),
            ContentItem(6, title = "American Ultra", overview = "A stoner who is actually a sleeper agent discovers his past when targeted by a rogue agent.", posterUrl = "https://image.tmdb.org/t/p/w500/hFz1K07174XN2a8s.jpg", rating = "R", releaseDate = "2015")
        )

        heroItem = sampleItems.first()
        txtHeroTitle.text = heroItem?.displayTitle
        txtHeroSynopsis.text = heroItem?.overview ?: ""
        val backdropUrl = heroItem?.resolvedBackdropUrl ?: heroItem?.resolvedPosterUrl
        com.lanflix.utils.ImageFetcher.loadImage(imgHeroBackdrop, backdropUrl)

        continueAdapter.updateItems(sampleItems)
        recentAdapter.updateItems(sampleItems.shuffled())

        btnHeroResume.setOnClickListener {
            heroItem?.let { showDetail(it) }
        }

        lifecycleScope.launch {
            val content = apiClient.getHomeContent()
            if (content.isNotEmpty()) {
                heroItem = content.first()
                txtHeroTitle.text = heroItem?.displayTitle
                txtHeroSynopsis.text = heroItem?.overview ?: ""
                val liveBackdrop = heroItem?.resolvedBackdropUrl ?: heroItem?.resolvedPosterUrl
                com.lanflix.utils.ImageFetcher.loadImage(imgHeroBackdrop, liveBackdrop)

                continueAdapter.updateItems(content.take(6))
                recentAdapter.updateItems(content.drop(2).take(6))
            }
        }
    }

    private fun showDetail(item: ContentItem) {
        val sheet = ContentDetailBottomSheet(item)
        sheet.show(parentFragmentManager, "ContentDetail")
    }
}
