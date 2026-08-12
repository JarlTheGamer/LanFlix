package com.lanflix.player

import android.content.Context
import android.media.MediaCodecList
import android.media.MediaCodecInfo
import android.os.Build
import android.view.WindowManager

/** Reports decoders actually exposed by this Android device to the server. */
object AndroidPlaybackCapabilities {
    fun clientProfile(context: Context, preferredAudioLanguage: String? = null): String {
        val codecTypes = runCatching {
            MediaCodecList(MediaCodecList.ALL_CODECS).codecInfos
                .asSequence()
                .filterNot { it.isEncoder }
                .flatMap { it.supportedTypes.asSequence() }
                .map { it.lowercase() }
                .toSet()
        }.getOrDefault(emptySet())

        val video = linkedSetOf<String>()
        if ("video/avc" in codecTypes) video += "h264"
        if ("video/hevc" in codecTypes) video += "hevc"
        val supportsHevc10 = runCatching {
            MediaCodecList(MediaCodecList.ALL_CODECS).codecInfos
                .asSequence().filterNot { it.isEncoder }
                .filter { info -> info.supportedTypes.any { it.equals("video/hevc", true) } }
                .flatMap { it.getCapabilitiesForType("video/hevc").profileLevels.asSequence() }
                .any { level -> level.profile == MediaCodecInfo.CodecProfileLevel.HEVCProfileMain10 ||
                    level.profile == MediaCodecInfo.CodecProfileLevel.HEVCProfileMain10HDR10 }
        }.getOrDefault(false)
        if (supportsHevc10) video += "hevc10"
        if ("video/x-vnd.on2.vp8" in codecTypes) video += "vp8"
        if ("video/x-vnd.on2.vp9" in codecTypes) video += "vp9"
        if ("video/av01" in codecTypes) video += "av1"
        if (video.isEmpty()) video += "h264"

        val audio = linkedSetOf("aac", "mp3")
        if ("audio/flac" in codecTypes) audio += "flac"
        if ("audio/opus" in codecTypes) audio += "opus"
        if ("audio/vorbis" in codecTypes) audio += "vorbis"
        if ("audio/ac3" in codecTypes) audio += "ac3"
        if ("audio/eac3" in codecTypes || "audio/eac3-joc" in codecTypes) audio += "eac3"

        val display = context.getSystemService(WindowManager::class.java)?.defaultDisplay
        // Report decoder limits, not panel dimensions. A 1080p phone can
        // decode and scale a supported 4K stream without server conversion.
        val decoderSize = runCatching {
            MediaCodecList(MediaCodecList.ALL_CODECS).codecInfos
                .asSequence()
                .filterNot { it.isEncoder }
                .flatMap { info -> info.supportedTypes.asSequence().map { info to it } }
                .filter { (_, type) -> type.startsWith("video/") }
                .mapNotNull { (info, type) ->
                    info.getCapabilitiesForType(type).videoCapabilities?.let {
                        it.supportedWidths.upper to it.supportedHeights.upper
                    }
                }
                .maxByOrNull { (width, height) -> width.toLong() * height }
        }.getOrNull()
        val width = decoderSize?.first ?: display?.mode?.physicalWidth?.coerceAtLeast(1920) ?: 1920
        val height = decoderSize?.second ?: display?.mode?.physicalHeight?.coerceAtLeast(1080) ?: 1080
        val hdrFormats = linkedSetOf<String>()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            val types = (display?.hdrCapabilities?.supportedHdrTypes ?: intArrayOf()).toSet()
            if (android.view.Display.HdrCapabilities.HDR_TYPE_HDR10 in types) hdrFormats += "hdr10"
            if (android.view.Display.HdrCapabilities.HDR_TYPE_HLG in types) hdrFormats += "hlg"
            if (android.view.Display.HdrCapabilities.HDR_TYPE_DOLBY_VISION in types && "video/dolby-vision" in codecTypes) hdrFormats += "dv"
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q &&
                android.view.Display.HdrCapabilities.HDR_TYPE_HDR10_PLUS in types) hdrFormats += "hdr10plus"
        }

        return "android-v1|v=${video.joinToString(",")}|a=${audio.joinToString(",")}" +
            "|c=mp4,m4v,mov,mkv,webm,ts,mpegts|r=${width}x$height" +
            "|hdr=${hdrFormats.ifEmpty { setOf("none") }.joinToString(",")}" +
            "|al=${preferredAudioLanguage.orEmpty().trim().lowercase().take(8)}"
    }
}
