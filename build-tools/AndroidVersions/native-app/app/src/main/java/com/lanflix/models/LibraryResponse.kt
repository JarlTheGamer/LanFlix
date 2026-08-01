package com.lanflix.models

import com.google.gson.annotations.SerializedName

data class LibraryResponse(
    @SerializedName("items") val items: List<ContentItem> = emptyList(),
    @SerializedName("total") val total: Int = 0,
    @SerializedName("page") val page: Int = 1,
    @SerializedName("pageSize") val pageSize: Int = 50
)
