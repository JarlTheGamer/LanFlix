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

        btnHeroResume.setOnClickListener {
            heroItem?.let { showDetail(it) }
        }

        // Fetch live content directly from C# Lanflix API server
        lifecycleScope.launch {
            val content = apiClient.getHomeContent()
            if (content.isNotEmpty()) {
                heroItem = content.first()
                txtHeroTitle.text = heroItem?.displayTitle
                txtHeroSynopsis.text = heroItem?.overview ?: ""

                val backdropUrl = heroItem?.resolvedBackdropUrl ?: heroItem?.resolvedPosterUrl
                com.lanflix.utils.ImageFetcher.loadImage(imgHeroBackdrop, backdropUrl)

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
