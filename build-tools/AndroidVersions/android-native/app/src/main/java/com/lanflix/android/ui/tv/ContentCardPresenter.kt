package com.lanflix.android.ui.tv

import android.annotation.SuppressLint
import android.graphics.drawable.Drawable
import android.view.ViewGroup
import androidx.core.content.ContextCompat
import androidx.leanback.widget.ImageCardView
import androidx.leanback.widget.Presenter
import com.bumptech.glide.Glide
import com.bumptech.glide.request.target.CustomTarget
import com.bumptech.glide.request.transition.Transition
import com.lanflix.android.R
import com.lanflix.android.domain.model.Content

@SuppressLint("UseCompatLoadingForDrawables")
class ContentCardPresenter : Presenter() {

    private var selectedBackgroundColor: Int = 0
    private var defaultBackgroundColor: Int = 0

    override fun onCreateViewHolder(parent: ViewGroup): ViewHolder {
        selectedBackgroundColor = ContextCompat.getColor(parent.context, R.color.lanflix_primary)
        defaultBackgroundColor = ContextCompat.getColor(parent.context, R.color.lanflix_surface)

        val cardView = object : ImageCardView(parent.context) {
            override fun setSelected(selected: Boolean) {
                updateCardBackgroundColor(this, selected)
                super.setSelected(selected)
            }
        }

        cardView.isFocusable = true
        cardView.isFocusableInTouchMode = true
        updateCardBackgroundColor(cardView, false)

        return ViewHolder(cardView)
    }

    override fun onBindViewHolder(viewHolder: ViewHolder, item: Any) {
        val content = item as Content
        val cardView = viewHolder.view as ImageCardView

        cardView.titleText = content.title
        cardView.contentText = content.year?.toString() ?: ""
        
        val cardWidth = cardView.resources.getDimensionPixelSize(R.dimen.movie_card_width)
        val cardHeight = cardView.resources.getDimensionPixelSize(R.dimen.movie_card_height)
        cardView.setMainImageDimensions(cardWidth, cardHeight)

        // Load poster image using Glide (Netflix-style image loading)
        if (!content.posterUrl.isNullOrEmpty()) {
            Glide.with(viewHolder.view.context)
                .load(content.posterUrl)
                .centerCrop()
                .error(R.drawable.placeholder_poster)
                .into(object : CustomTarget<Drawable>() {
                    override fun onResourceReady(
                        resource: Drawable,
                        transition: Transition<in Drawable>?
                    ) {
                        cardView.mainImage = resource
                    }

                    override fun onLoadCleared(placeholder: Drawable?) {
                        // Handle cleanup if needed
                    }
                })
        } else {
            cardView.mainImage = ContextCompat.getDrawable(
                cardView.context,
                R.drawable.placeholder_poster
            )
        }
    }

    override fun onUnbindViewHolder(viewHolder: ViewHolder) {
        val cardView = viewHolder.view as ImageCardView

        // Remove references to images so that the garbage collector can free up memory
        cardView.badgeImage = null
        cardView.mainImage = null
    }

    private fun updateCardBackgroundColor(view: ImageCardView, selected: Boolean) {
        val color = if (selected) selectedBackgroundColor else defaultBackgroundColor

        // Both background colors should be set because the view's
        // background is temporarily visible during animations.
        view.setBackgroundColor(color)
        view.setInfoAreaBackgroundColor(color)
    }
}