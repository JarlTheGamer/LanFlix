package com.lanflix.ui.detail

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.google.android.material.bottomsheet.BottomSheetDialogFragment
import com.google.android.material.button.MaterialButton
import com.lanflix.adapters.CastAdapter
import com.lanflix.models.CastMember
import com.lanflix.models.ContentItem
import com.lanflix.webview.R

class ContentDetailBottomSheet(
    private val item: ContentItem
) : BottomSheetDialogFragment() {

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.dialog_content_detail, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val imgBackdrop: ImageView = view.findViewById(R.id.detail_img_backdrop)
        val txtTitle: TextView = view.findViewById(R.id.detail_txt_title)
        val txtRating: TextView = view.findViewById(R.id.detail_txt_rating)
        val txtYear: TextView = view.findViewById(R.id.detail_txt_year)
        val txtSynopsis: TextView = view.findViewById(R.id.detail_txt_synopsis)
        val btnResume: MaterialButton = view.findViewById(R.id.detail_btn_resume)
        val recyclerCast: RecyclerView = view.findViewById(R.id.detail_recycler_cast)

        txtTitle.text = item.displayTitle
        txtRating.text = item.rating ?: "PG-13"
        txtYear.text = item.releaseDate?.take(4) ?: "2024"
        txtSynopsis.text = item.overview ?: "No overview available for this title."

        val backdropUrl = item.resolvedBackdropUrl ?: item.resolvedPosterUrl
        com.lanflix.utils.ImageFetcher.loadImage(imgBackdrop, backdropUrl)

        val castAdapter = CastAdapter()
        recyclerCast.adapter = castAdapter
        castAdapter.updateCast(
            listOf(
                CastMember("David Bowie", "Jareth", "https://image.tmdb.org/t/p/w185/87d7l2K8x8u7k7g.jpg"),
                CastMember("Jennifer Connelly", "Sarah", "https://image.tmdb.org/t/p/w185/7q0g2g7Z102.jpg"),
                CastMember("Jim Henson", "Director", "https://image.tmdb.org/t/p/w185/jimhenson.jpg")
            )
        )

        btnResume.setOnClickListener {
            Toast.makeText(context, "Playing ${item.displayTitle}", Toast.LENGTH_SHORT).show()
            dismiss()
        }
    }
}
