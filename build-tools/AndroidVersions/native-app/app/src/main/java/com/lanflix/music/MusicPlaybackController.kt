@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.music

import android.content.Context
import android.net.Uri
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.datasource.DataSource
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import com.lanflix.api.LanflixApiClient
import com.lanflix.api.MusicTrack
import com.lanflix.auth.LanflixSessionStore
import com.lanflix.utils.RefreshingHttpDataSource
import com.lanflix.webview.ServerManager
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

data class MusicPlaybackState(
    val queue: List<MusicTrack> = emptyList(),
    val currentIndex: Int = -1,
    val positionMilliseconds: Long = 0,
    val durationMilliseconds: Long = 0,
    val playing: Boolean = false
) {
    val currentTrack: MusicTrack? get() = queue.getOrNull(currentIndex)
}

class MusicPlaybackController private constructor(context: Context) {
    private val applicationContext = context.applicationContext
    private val api = LanflixApiClient.getInstance(applicationContext)
    private val session = LanflixSessionStore(applicationContext)
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val mutableState = MutableStateFlow(MusicPlaybackState())
    val state: StateFlow<MusicPlaybackState> = mutableState.asStateFlow()

    private fun source(): DefaultHttpDataSource = DefaultHttpDataSource.Factory()
        .setAllowCrossProtocolRedirects(true)
        .setDefaultRequestProperties(session.accessToken?.let { mapOf("Authorization" to "Bearer $it") }.orEmpty())
        .createDataSource()

    private val player = ExoPlayer.Builder(applicationContext)
        .setSeekBackIncrementMs(10_000)
        .setSeekForwardIncrementMs(10_000)
        .setMediaSourceFactory(DefaultMediaSourceFactory(DataSource.Factory {
            RefreshingHttpDataSource(::source) { api.refreshSession() }
        }))
        .build()

    init {
        player.addListener(object : Player.Listener {
            override fun onIsPlayingChanged(isPlaying: Boolean) = publish(isPlaying = isPlaying)
            override fun onMediaItemTransition(mediaItem: MediaItem?, reason: Int) {
                val index = player.currentMediaItemIndex.takeIf { it >= 0 } ?: -1
                publish(currentIndex = index, position = 0)
                recordCurrent(completed = false)
            }
            override fun onPlaybackStateChanged(playbackState: Int) {
                if (playbackState == Player.STATE_ENDED) recordCurrent(completed = true)
            }
        })
        scope.launch {
            while (isActive) {
                if (player.mediaItemCount > 0) publish()
                delay(500)
            }
        }
    }

    fun play(queue: List<MusicTrack>, selected: MusicTrack) {
        val playable = queue.filter { it.serverAvailable }
        if (playable.isEmpty()) return
        val selectedIndex = playable.indexOfFirst { it.id == selected.id }.coerceAtLeast(0)
        val currentIds = mutableState.value.queue.map { it.id }
        if (currentIds == playable.map { it.id } && player.mediaItemCount > 0) {
            if (player.currentMediaItemIndex != selectedIndex) player.seekToDefaultPosition(selectedIndex)
            player.play()
            return
        }
        val mediaItems = playable.map { track ->
            val url = if (track.streamUrl.startsWith("http")) track.streamUrl else "${ServerManager.activeServerUrl}${track.streamUrl}"
            MediaItem.Builder().setMediaId(track.id.toString()).setUri(Uri.parse(url)).build()
        }
        mutableState.value = MusicPlaybackState(playable, selectedIndex, durationMilliseconds = playable[selectedIndex].durationMilliseconds, playing = true)
        player.setMediaItems(mediaItems, selectedIndex, 0)
        player.prepare()
        player.play()
        recordCurrent(completed = false)
        scope.launch(Dispatchers.IO) { api.replaceMusicQueue(playable.map { it.id }) }
    }

    fun toggle() { if (player.isPlaying) player.pause() else player.play() }
    fun seekTo(positionMilliseconds: Long) = player.seekTo(positionMilliseconds.coerceAtLeast(0))
    fun seekBack() = player.seekBack()
    fun seekForward() = player.seekForward()
    fun previous() { if (player.hasPreviousMediaItem()) player.seekToPreviousMediaItem() else player.seekTo(0) }
    fun next() { if (player.hasNextMediaItem()) player.seekToNextMediaItem() }

    private fun publish(
        isPlaying: Boolean = player.isPlaying,
        currentIndex: Int = player.currentMediaItemIndex,
        position: Long = player.currentPosition.coerceAtLeast(0)
    ) {
        val previous = mutableState.value
        mutableState.value = previous.copy(
            currentIndex = currentIndex.takeIf { it in previous.queue.indices } ?: previous.currentIndex,
            positionMilliseconds = position,
            durationMilliseconds = player.duration.takeIf { it > 0 } ?: previous.currentTrack?.durationMilliseconds ?: 0,
            playing = isPlaying
        )
    }

    private fun recordCurrent(completed: Boolean) {
        val snapshot = mutableState.value
        val track = snapshot.currentTrack ?: return
        scope.launch(Dispatchers.IO) { api.recordMusicPlay(track.id, snapshot.positionMilliseconds, completed) }
    }

    companion object {
        @Volatile private var instance: MusicPlaybackController? = null
        fun get(context: Context): MusicPlaybackController = instance ?: synchronized(this) {
            instance ?: MusicPlaybackController(context).also { instance = it }
        }
    }
}
