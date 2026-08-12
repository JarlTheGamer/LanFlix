@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.ui.compose.screens

import android.app.Activity
import android.content.pm.ActivityInfo
import android.net.Uri
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.media3.common.C
import androidx.media3.common.MediaItem
import androidx.media3.common.MimeTypes
import androidx.media3.datasource.DataSource
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.DefaultLoadControl
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.SeekParameters
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import androidx.media3.ui.AspectRatioFrameLayout
import androidx.media3.ui.PlayerView
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.PlaybackInfo
import com.lanflix.auth.LanflixSessionStore
import com.lanflix.models.ContentItem
import com.lanflix.player.AndroidPlaybackCapabilities
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import com.lanflix.utils.RefreshingHttpDataSource
import com.lanflix.webview.ServerManager
import android.view.ScaleGestureDetector
import kotlinx.coroutines.delay
import java.io.File

/** Resize modes used for pinch-to-zoom: Fit (letterboxed) ↔ Zoom (fill/crop). */
private val ZOOM_MODES = listOf(
    AspectRatioFrameLayout.RESIZE_MODE_FIT  to "fit",
    AspectRatioFrameLayout.RESIZE_MODE_ZOOM to "zoom",
)

@Composable
fun PlayerScreen(item: ContentItem, onBack: () -> Unit) {
    val context = LocalContext.current
    val activity = context as? Activity
    val sessionStore = remember { LanflixSessionStore(context) }
    val playbackPreferencesRepository = remember { DevicePreferencesRepository(context.applicationContext) }
    val api = remember { LanflixApiClient(context) }

    val playbackPreferences by playbackPreferencesRepository.preferences.collectAsStateWithLifecycle(initialValue = DevicePreferences())
    var playbackInfo by remember(item.id) { mutableStateOf<PlaybackInfo?>(null) }
    var playbackErrorMessage by remember(item.id) { mutableStateOf<String?>(null) }
    var planLoading by remember(item.id) { mutableStateOf(item.localFilePath == null) }
    val playbackClient = remember(
        playbackPreferences.playbackQuality,
        playbackPreferences.preferredAudioLanguage
    ) {
        if (playbackPreferences.playbackQuality == "Data saver") "mobile-low"
        else AndroidPlaybackCapabilities.clientProfile(
            context.applicationContext,
            playbackPreferences.preferredAudioLanguage
        )
    }
    LaunchedEffect(item.id, playbackClient) {
        if (item.localFilePath == null) {
            planLoading = true
            playbackInfo = api.getPlaybackInfo(item, playbackClient)
            playbackErrorMessage = if (playbackInfo == null) "The server could not prepare this video." else null
            planLoading = false
        }
    }
    var initialPositionApplied by remember(item.id) { mutableStateOf(false) }
    DisposableEffect(activity) {
        if (activity == null) return@DisposableEffect onDispose { }
        val previousOrientation = activity.requestedOrientation
        val controller = WindowCompat.getInsetsController(activity.window, activity.window.decorView)
        controller.systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        controller.hide(WindowInsetsCompat.Type.systemBars())
        activity.window.addFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        activity.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE
        onDispose {
            controller.show(WindowInsetsCompat.Type.systemBars())
            activity.window.clearFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            activity.requestedOrientation = previousOrientation
        }
    }
    val uri = remember(item, playbackPreferences.playbackQuality, playbackInfo?.progress?.positionMilliseconds, playbackInfo?.playbackMode) {
        item.localFilePath?.let { Uri.fromFile(File(it)) } ?: playbackInfo?.let {
            val kind = if (item.type.equals("episode", true)) "episode" else "movie"
            // DirectPlay must use the dedicated file endpoint. It is the only
            // playback route that advertises HTTP range support, which keeps
            // ExoPlayer seeking instant for media the device can already play.
            val client = if (playbackInfo?.playbackMode.equals("DirectPlay", ignoreCase = true)) {
                "direct"
            } else {
                playbackClient
            }
            val startSeconds = playbackInfo?.progress?.positionMilliseconds?.takeIf { it > 0L }?.div(1000.0)
            val useHls = !playbackInfo?.playbackMode.isNullOrBlank() &&
                playbackInfo?.playbackMode != "Unknown" &&
                !playbackInfo?.playbackMode.equals("DirectPlay", ignoreCase = true)
            Uri.parse(buildString {
                append("${ServerManager.activeServerUrl}/api/v2/playback/$kind/${item.id}/")
                append(if (useHls) "hls/playlist.m3u8?client=${Uri.encode(client)}" else "file?client=${Uri.encode(client)}")
                if (startSeconds != null) append("&startTime=$startSeconds")
            })
        }
    }
    val mediaItem = remember(uri, playbackInfo?.subtitles, playbackPreferences.preferredSubtitleLanguage) {
        uri?.let { mediaUri ->
            val preferred = playbackPreferences.preferredSubtitleLanguage.trim().lowercase()
            val subtitle = playbackInfo?.subtitles?.firstOrNull { it.language.lowercase().startsWith(preferred) }
                ?: playbackInfo?.subtitles?.firstOrNull { it.isDefault }
                ?: playbackInfo?.subtitles?.firstOrNull()
            val builder = MediaItem.Builder().setUri(mediaUri)
            if (subtitle != null && subtitle.url.isNotBlank()) {
                val subtitleUri = if (subtitle.url.startsWith("http")) subtitle.url
                    else "${ServerManager.activeServerUrl}${subtitle.url}"
                builder.setSubtitleConfigurations(listOf(
                    MediaItem.SubtitleConfiguration.Builder(Uri.parse(subtitleUri))
                        .setMimeType(MimeTypes.TEXT_VTT)
                        .setLanguage(subtitle.language)
                        .setLabel(subtitle.title)
                        .build()
                ))
            }
            builder.build()
        }
    }
    val player = remember(mediaItem) {
        // Build a factory that creates a DefaultHttpDataSource with the current token.
        // Re-invoked on each new data source, so a refreshed token is always used.
        fun makeHttpSource(): androidx.media3.datasource.DefaultHttpDataSource {
            val token = sessionStore.accessToken
            val headers = token?.let { mapOf("Authorization" to "Bearer $it") }.orEmpty()
            return DefaultHttpDataSource.Factory()
                .setConnectTimeoutMs(8_000)
                // Initial managed-HLS segments may need to be encoded before
                // they can be returned. An 8-second timeout caused Media3 to
                // cancel and duplicate otherwise healthy FFmpeg batches.
                .setReadTimeoutMs(60_000)
                .setAllowCrossProtocolRedirects(true)
                .setDefaultRequestProperties(headers)
                .createDataSource()
        }
        // Wrapping in RefreshingHttpDataSource adds transparent retry-on-401:
        // if the access token expires mid-stream, it refreshes and retries automatically.
        val dataSourceFactory = DataSource.Factory {
            RefreshingHttpDataSource(
                innerFactory = ::makeHttpSource,
                onRefreshToken = { api.refreshSession() }
            )
        }
        val loadControl = DefaultLoadControl.Builder()
            .setBufferDurationsMs(
                /* minBufferMs = */ 15_000,
                /* maxBufferMs = */ 30_000,
                /* bufferForPlaybackMs = */ 1_000,
                /* bufferForPlaybackAfterRebufferMs = */ 2_000
            )
            .setTargetBufferBytes(64 * 1024 * 1024)
            .setPrioritizeTimeOverSizeThresholds(false)
            .build()
        ExoPlayer.Builder(context)
            .setLoadControl(loadControl)
            .setSeekBackIncrementMs(10_000)
            .setSeekForwardIncrementMs(10_000)
            .setMediaSourceFactory(DefaultMediaSourceFactory(dataSourceFactory)).build().apply {
            setSeekParameters(SeekParameters.CLOSEST_SYNC)
            trackSelectionParameters = trackSelectionParameters.buildUpon()
                .setPreferredAudioLanguage(playbackPreferences.preferredAudioLanguage.ifBlank { null })
                .setPreferredTextLanguage(playbackPreferences.preferredSubtitleLanguage.ifBlank { null })
                .setSelectUndeterminedTextLanguage(playbackPreferences.automaticSubtitles)
                .setTrackTypeDisabled(C.TRACK_TYPE_TEXT, !playbackPreferences.automaticSubtitles)
                .build()
            if (mediaItem != null) {
                setMediaItem(mediaItem)
                prepare()
                playWhenReady = true
            }
        }
    }
    DisposableEffect(player) { onDispose { player.release() } }
    LaunchedEffect(player, playbackInfo?.progress?.positionMilliseconds) {
        val position = playbackInfo?.progress?.positionMilliseconds ?: return@LaunchedEffect
        if (position > 0L && !initialPositionApplied) {
            while (player.playbackState == androidx.media3.common.Player.STATE_IDLE ||
                player.playbackState == androidx.media3.common.Player.STATE_BUFFERING) {
                delay(50)
            }
            player.seekTo(position)
            initialPositionApplied = true
        }
    }

    // Track the real video aspect ratio so the zoom ring hugs the video frame,
    // not the black letterbox bars.
    var videoAspectRatio by remember { mutableStateOf(16f / 9f) }
    DisposableEffect(player) {
        val listener = object : androidx.media3.common.Player.Listener {
            override fun onVideoSizeChanged(videoSize: androidx.media3.common.VideoSize) {
                if (videoSize.width > 0 && videoSize.height > 0)
                    videoAspectRatio = videoSize.width.toFloat() / videoSize.height.toFloat()
            }
            override fun onPlayerError(error: androidx.media3.common.PlaybackException) {
                playbackErrorMessage = error.localizedMessage ?: "Playback failed."
            }
        }
        player.addListener(listener)
        onDispose { player.removeListener(listener) }
    }

    var positionMs by remember { mutableStateOf(0L) }
    LaunchedEffect(player) {
        while (true) {
            positionMs = player.currentPosition.coerceAtLeast(0L)
            delay(300)
        }
    }
    val introEndMs = playbackInfo?.introEndSeconds?.times(1000)?.toLong()
    val introStartMs = playbackInfo?.introStartSeconds?.times(1000)?.toLong() ?: 0L
    val showSkipIntro = introEndMs != null && positionMs in introStartMs until introEndMs

    // Zoom / resize-mode state — kept as explicit MutableState refs so the
    // ScaleGestureDetector (created once via remember) can capture the *object*
    // rather than a stale value.
    val zoomModeIndexState = remember { mutableStateOf(0) }
    var zoomModeIndex by zoomModeIndexState
    val (currentResizeMode, _) = ZOOM_MODES[zoomModeIndex]
    // Use an incrementing Int as the LaunchedEffect key so that rapid repeated
    // pinches always trigger a new animation — a Boolean key silently no-ops
    // when set to true again while already true, freezing the animation.
    var zoomRingTrigger by remember { mutableStateOf(0) }
    val glintAnim = remember { Animatable(0f) }
    LaunchedEffect(zoomRingTrigger) {
        if (zoomRingTrigger > 0) {
            // Cancels any in-progress animation and restarts from 0
            glintAnim.snapTo(0f)
            glintAnim.animateTo(1f, tween(80))   // fast flash in
            glintAnim.animateTo(0f, tween(420))  // slow glisten fade
        }
    }

    // Pinch-to-zoom: ScaleGestureDetector attached directly to PlayerView.
    // Captures state *objects* (not values) → always reads the latest index.
    // isPinching / suppressUntil are primitive arrays so the detector and the
    // touch listener share the same mutable slot without triggering recomposition.
    val isPinching = remember { BooleanArray(1) { false } }
    val suppressUntil = remember { LongArray(1) { 0L } }
    val scaleDetector = remember {
        ScaleGestureDetector(
            context,
            object : ScaleGestureDetector.SimpleOnScaleGestureListener() {
                // Accumulate scale across the gesture so small increments add up
                private var accumulatedScale = 1f
                override fun onScaleBegin(detector: ScaleGestureDetector): Boolean {
                    accumulatedScale = 1f
                    isPinching[0] = true
                    return true
                }
                override fun onScale(detector: ScaleGestureDetector): Boolean {
                    accumulatedScale *= detector.scaleFactor
                    val cur = zoomModeIndexState.value
                    when {
                        accumulatedScale > 1.25f && cur == 0 -> {
                            zoomModeIndexState.value = 1
                            zoomRingTrigger++
                        }
                        accumulatedScale < 0.80f && cur != 0 -> {
                            zoomModeIndexState.value = 0
                            zoomRingTrigger++
                        }
                    }
                    return true
                }
                override fun onScaleEnd(detector: ScaleGestureDetector) {
                    isPinching[0] = false
                    // Suppress for 350 ms after lift so ACTION_UP doesn't show controls
                    suppressUntil[0] = System.currentTimeMillis() + 350L
                }
            }
        )
    }

    Box(Modifier.fillMaxSize().background(Color.Black)) {
        AndroidView(
            factory = { ctx ->
                PlayerView(ctx).apply {
                    this.player = player
                    useController = true
                    controllerShowTimeoutMs = 4_000
                    resizeMode = currentResizeMode
                    setShowRewindButton(true)
                    setShowFastForwardButton(true)
                    setShowPreviousButton(false)
                    setShowNextButton(false)
                    setShowSubtitleButton(true)
                }
            },
            update = { view ->
                // AndroidView instances survive recomposition. The playback
                // plan changes the URI (loading -> direct/HLS), so explicitly
                // attach the newly-created ExoPlayer instead of leaving the
                // view bound to the empty planning player.
                if (view.player !== player) view.player = player
                // Keep resizeMode in sync whenever state changes
                view.resizeMode = currentResizeMode
                // Forward events to PlayerView only when NOT pinching.
                // suppressUntil adds a 350 ms dead-zone after finger lift so
                // ACTION_UP doesn't accidentally show the playback controls.
                view.setOnTouchListener { v, event ->
                    scaleDetector.onTouchEvent(event)
                    val suppressed = isPinching[0] || System.currentTimeMillis() < suppressUntil[0]
                    if (suppressed) true else v.onTouchEvent(event)
                }
            },
            modifier = Modifier.fillMaxSize()
        )

        if (planLoading) {
            CircularProgressIndicator(
                modifier = Modifier.align(Alignment.Center),
                color = Color.White,
                strokeWidth = 2.dp
            )
        }

        playbackErrorMessage?.let { message ->
            Text(
                text = message,
                color = Color.White,
                modifier = Modifier
                    .align(Alignment.Center)
                    .background(Color.Black.copy(alpha = .78f), RoundedCornerShape(12.dp))
                    .padding(horizontal = 18.dp, vertical = 12.dp)
            )
        }

        // ── Back button (top-start) ──────────────────────────────────────────
        IconButton(
            onClick = onBack,
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(10.dp)
                .clip(CircleShape)
                .background(Color.Black.copy(alpha = .48f))
        ) {
            Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White)
        }

        // ── Glowing border glisten — 3-layer neon glow around the video frame ──
        val gv = glintAnim.value
        if (gv > 0f) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .drawWithContent {
                        drawContent()
                        // Compute video rect (letterbox-aware)
                        val screenAr = size.width / size.height
                        val (vLeft, vTop, vWidth, vHeight) = if (
                            currentResizeMode == AspectRatioFrameLayout.RESIZE_MODE_ZOOM
                        ) {
                            listOf(0f, 0f, size.width, size.height)
                        } else {
                            if (videoAspectRatio >= screenAr) {
                                val vh = size.width / videoAspectRatio
                                listOf(0f, (size.height - vh) / 2f, size.width, vh)
                            } else {
                                val vw = size.height * videoAspectRatio
                                listOf((size.width - vw) / 2f, 0f, vw, size.height)
                            }
                        }
                        val topLeft = androidx.compose.ui.geometry.Offset(vLeft, vTop)
                        val rectSize = androidx.compose.ui.geometry.Size(vWidth, vHeight)
                        // Layer 1 — wide outer bloom (soft, diffuse)
                        drawRect(Color.White.copy(alpha = gv * 0.10f), topLeft, rectSize, style = Stroke(14.dp.toPx()))
                        // Layer 2 — mid glow
                        drawRect(Color.White.copy(alpha = gv * 0.22f), topLeft, rectSize, style = Stroke(5.dp.toPx()))
                        // Layer 3 — sharp bright inner edge (the glisten)
                        drawRect(Color.White.copy(alpha = gv * 0.85f), topLeft, rectSize, style = Stroke(1.5.dp.toPx()))
                    }
            )
        }

        // ── Skip intro button (bottom-end) ───────────────────────────────────
        if (showSkipIntro) {
            Button(
                onClick = { player.seekTo(introEndMs!!) },
                modifier = Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = 58.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                shape = RoundedCornerShape(20.dp)
            ) { Text("Skip intro", fontWeight = FontWeight.Bold) }
        }
    }
}
