package com.lanflix.webview.ota

import java.io.Serializable

data class UpdateInfo(
    val versionName: String,
    val versionCode: Int,
    val downloadUrl: String,
    val releaseNotes: String? = null,
    val mandatory: Boolean = false,
    val fileSize: Long = 0,
    val checksum: String? = null
) : Serializable

data class UpdateResponse(
    val hasUpdate: Boolean,
    val updateInfo: UpdateInfo?
)