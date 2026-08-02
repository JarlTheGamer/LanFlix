@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.ui.compose.screens

import android.app.Activity
import android.content.pm.ActivityInfo
import android.net.Uri
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.media3.common.C
import androidx.media3.common.MediaItem
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
import com.lanflix.settings.DevicePreferences
import com.lanflix.settings.DevicePreferencesRepository
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.delay
import java.io.File

@Composable
fun PlayerScreen(item: ContentItem, onBack: () -> Unit) {
    val context = LocalContext.current
    val activity = context as? Activity
    val sessionStore = remember { LanflixSessionStore(context) }
    val playbackPreferencesRepository = remember { DevicePreferencesRepository(context.applicationContext) }
    val api = remember { LanflixApiClient(context) }

    val playbackPreferences by playbackPreferencesRepository.preferences.collectAsStateWithLifecycle(initialValue = DevicePreferences())
    var playbackInfo by remember(item.id) { mutableStateOf<PlaybackInfo?>(null) }
    LaunchedEffect(item.id) { if (item.localFilePath == null) playbackInfo = api.getPlaybackInfo(item) }
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
    val uri = remember(item, playbackPreferences.playbackQuality) {
        item.localFilePath?.let { Uri.fromFile(File(it)) } ?: run {
            val kind = if (item.type.equals("episode", true)) "episode" else "movie"
            val client = if (playbackPreferences.playbackQuality == "Data saver") "mobile-low" else "direct"
            Uri.parse("${ServerManager.activeServerUrl}/api/v2/playback/$kind/${item.id}/file?client=$client")
        }
    }
    val player = remember(uri) {
        val dataSourceFactory = DataSource.Factory {
            val token = sessionStore.accessToken
            val headers = token?.let { mapOf("Authorization" to "Bearer $it") }.orEmpty()
            DefaultHttpDataSource.Factory()
                .setConnectTimeoutMs(8_000)
                .setReadTimeoutMs(8_000)
                .setAllowCrossProtocolRedirects(true)
                .setDefaultRequestProperties(headers)
                .createDataSource()
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
            setMediaItem(MediaItem.fromUri(uri)); prepare(); playWhenReady = true
        }
    }
    DisposableEffect(player) { onDispose { player.release() } }
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
    Box(Modifier.fillMaxSize().background(Color.Black)) {
        AndroidView(factory = {
            PlayerView(it).apply {
                this.player = player
                useController = true
                controllerShowTimeoutMs = 4_000
                resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIT
                setShowRewindButton(true)
                setShowFastForwardButton(true)
                setShowPreviousButton(false)
                setShowNextButton(false)
                setShowSubtitleButton(true)
            }
        }, modifier = Modifier.fillMaxSize())
        IconButton(onClick = onBack, modifier = Modifier.padding(10.dp).clip(CircleShape).background(Color.Black.copy(alpha = .48f))) { Icon(Icons.Filled.ArrowBack, "Back", tint = Color.White) }
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
