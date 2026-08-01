package com.lanflix.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.lanflix.models.CastMember
import com.lanflix.webview.R

class CastAdapter(
    private var castList: List<CastMember> = emptyList()
) : RecyclerView.Adapter<CastAdapter.CastViewHolder>() {

    fun updateCast(newList: List<CastMember>) {
        castList = newList
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): CastViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_cast_avatar, parent, false)
        return CastViewHolder(view)
    }

    override fun onBindViewHolder(holder: CastViewHolder, position: Int) {
        holder.bind(castList[position])
    }

    override fun getItemCount(): Int = castList.size

    inner class CastViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val imgAvatar: ImageView = itemView.findViewById(R.id.img_cast_avatar)
        private val txtName: TextView = itemView.findViewById(R.id.txt_cast_name)
        private val txtRole: TextView = itemView.findViewById(R.id.txt_cast_role)

        fun bind(cast: CastMember) {
            txtName.text = cast.name
            txtRole.text = cast.role

            com.lanflix.utils.ImageFetcher.loadImage(imgAvatar, cast.profileUrl)
        }
    }
}
