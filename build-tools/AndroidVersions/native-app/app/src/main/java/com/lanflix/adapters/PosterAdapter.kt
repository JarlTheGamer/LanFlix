package com.lanflix.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.bumptech.glide.load.engine.DiskCacheStrategy
import com.lanflix.models.ContentItem
import com.lanflix.webview.R

class PosterAdapter(
    private var items: List<ContentItem> = emptyList(),
    private val onItemClick: (ContentItem) -> Unit
) : RecyclerView.Adapter<PosterAdapter.PosterViewHolder>() {

    fun updateItems(newItems: List<ContentItem>) {
        items = newItems
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): PosterViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_poster_card, parent, false)
        return PosterViewHolder(view)
    }

    override fun onBindViewHolder(holder: PosterViewHolder, position: Int) {
        holder.bind(items[position])
    }

    override fun getItemCount(): Int = items.size

    inner class PosterViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val imgPoster: ImageView = itemView.findViewById(R.id.img_poster)
        private val txtTitle: TextView = itemView.findViewById(R.id.txt_title)
        private val txtSubtitle: TextView = itemView.findViewById(R.id.txt_subtitle)
        private val txtBadge: TextView = itemView.findViewById(R.id.txt_badge)

        fun bind(item: ContentItem) {
            txtTitle.text = item.displayTitle
            txtSubtitle.text = item.releaseDate?.take(4) ?: item.itemCount?.let { "$it items" } ?: ""

            if (item.itemCount != null && item.itemCount > 0) {
                txtBadge.visibility = View.VISIBLE
                txtBadge.text = item.itemCount.toString()
            } else {
                txtBadge.visibility = View.GONE
            }

            val posterUrl = item.resolvedPosterUrl
            com.lanflix.utils.ImageFetcher.loadImage(imgPoster, posterUrl)

            itemView.setOnClickListener { onItemClick(item) }
        }
    }
}
