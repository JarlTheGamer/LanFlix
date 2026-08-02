package com.lanflix.ui.compose.theme

import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.core.graphics.ColorUtils
import androidx.core.graphics.drawable.toBitmap
import androidx.palette.graphics.Palette
import com.lanflix.ui.compose.LanflixGold
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

data class ArtworkPalette(val base: Color, val depth: Color, val glow: Color, val accent: Color)

val DefaultArtworkPalette = ArtworkPalette(
    base = Color(0xFF0F1720),
    depth = Color(0xFF070B10),
    glow = Color(0xFF1B5375),
    accent = LanflixGold
)

@Composable
fun animatedArtworkPalette(target: ArtworkPalette): ArtworkPalette = target

fun com.lanflix.models.ServerArtworkPalette.toComposePalette() = ArtworkPalette(
    base = runCatching { Color(android.graphics.Color.parseColor(base)) }.getOrDefault(DefaultArtworkPalette.base),
    depth = runCatching { Color(android.graphics.Color.parseColor(depth)) }.getOrDefault(DefaultArtworkPalette.depth),
    glow = runCatching { Color(android.graphics.Color.parseColor(glow)) }.getOrDefault(DefaultArtworkPalette.glow),
    accent = runCatching { Color(android.graphics.Color.parseColor(accent)) }.getOrDefault(DefaultArtworkPalette.accent)
)

suspend fun extractArtworkPalette(drawable: android.graphics.drawable.Drawable): ArtworkPalette = withContext(Dispatchers.Default) {
    runCatching {
        val sourceBitmap = drawable.toBitmap(width = 192, height = 192)
        val readableBitmap = if (sourceBitmap.config == android.graphics.Bitmap.Config.HARDWARE) {
            sourceBitmap.copy(android.graphics.Bitmap.Config.ARGB_8888, false)
        } else sourceBitmap
        val palette = Palette.from(readableBitmap).maximumColorCount(24).generate()

        val swatches = listOfNotNull(
            palette.vibrantSwatch,
            palette.lightVibrantSwatch,
            palette.darkVibrantSwatch,
            palette.dominantSwatch,
            palette.mutedSwatch
        )
        val signatureSwatch = swatches.maxByOrNull { swatch ->
            val hsv = FloatArray(3)
            android.graphics.Color.colorToHSV(swatch.rgb, hsv)
            val sat = hsv[1]
            val lightness = hsv[2]
            val vividness = if (sat > 0.30f && lightness in 0.18f..0.88f) 2.5f else 0.4f
            swatch.population * sat * vividness
        }

        val signatureRgb = signatureSwatch?.rgb ?: 0xFF143D5A.toInt()
        val accentRgb = swatches.firstOrNull { it.rgb != signatureRgb }?.rgb ?: signatureRgb

        ArtworkPalette(
            base = artworkTone(signatureRgb, .22f, .28f, minSat = .65f, maxSat = .85f),
            depth = artworkTone(signatureRgb, .12f, .16f, minSat = .55f, maxSat = .75f),
            glow = artworkTone(signatureRgb, .42f, .65f, minSat = .78f, maxSat = 1.0f),
            accent = artworkTone(accentRgb, .52f, .78f, minSat = .75f, maxSat = 1.0f)
        )
    }.getOrDefault(DefaultArtworkPalette)
}

fun artworkTone(rgb: Int, minValue: Float, maxValue: Float, minSat: Float = .30f, maxSat: Float = .96f): Color {
    val hsv = FloatArray(3)
    android.graphics.Color.colorToHSV(rgb, hsv)
    hsv[1] = hsv[1].coerceIn(minSat, maxSat)
    hsv[2] = hsv[2].coerceIn(minValue, maxValue)
    return Color(android.graphics.Color.HSVToColor(hsv))
}

fun darkenArtworkColor(color: Color, blackAmount: Float): Color = Color(
    ColorUtils.blendARGB(color.toArgb(), android.graphics.Color.BLACK, blackAmount)
)

fun shiftArtworkHue(color: Color, degrees: Float): Color {
    val hsv = FloatArray(3)
    android.graphics.Color.colorToHSV(color.toArgb(), hsv)
    hsv[0] = (hsv[0] + degrees) % 360f
    hsv[1] = hsv[1].coerceAtLeast(.68f)
    hsv[2] = hsv[2].coerceAtLeast(.58f)
    return Color(android.graphics.Color.HSVToColor(hsv))
}
