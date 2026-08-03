@file:androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)

package com.lanflix.utils

import androidx.media3.datasource.DataSource
import androidx.media3.datasource.DataSpec
import androidx.media3.datasource.HttpDataSource
import androidx.media3.datasource.TransferListener

/**
 * A [DataSource] wrapper that transparently retries an HTTP request with a
 * refreshed Bearer token when the underlying source throws an
 * [HttpDataSource.InvalidResponseCodeException] with HTTP 401.
 *
 * This solves the ExoPlayer "direct play 401 mid-stream" problem: the access
 * token can expire between range requests and ExoPlayer's built-in
 * [androidx.media3.datasource.DefaultHttpDataSource] has no retry-on-401 logic.
 *
 * @param innerFactory   Creates the real HTTP data source (reads token freshly each call).
 * @param onRefreshToken Called when a 401 is encountered; should refresh the
 *                       access token and return true on success, false on failure.
 */
class RefreshingHttpDataSource(
    private val innerFactory: () -> HttpDataSource,
    private val onRefreshToken: () -> Boolean
) : DataSource {

    private var inner: HttpDataSource = innerFactory()

    override fun addTransferListener(transferListener: TransferListener) {
        inner.addTransferListener(transferListener)
    }

    override fun open(dataSpec: DataSpec): Long {
        return try {
            inner.open(dataSpec)
        } catch (e: HttpDataSource.InvalidResponseCodeException) {
            if (e.responseCode == 401 && onRefreshToken()) {
                // Replace the inner source so it picks up the fresh token
                runCatching { inner.close() }
                inner = innerFactory()
                inner.open(dataSpec)
            } else {
                throw e
            }
        }
    }

    override fun read(buffer: ByteArray, offset: Int, length: Int): Int =
        inner.read(buffer, offset, length)

    override fun getUri() = inner.uri

    override fun getResponseHeaders() = inner.responseHeaders

    override fun close() = inner.close()
}
