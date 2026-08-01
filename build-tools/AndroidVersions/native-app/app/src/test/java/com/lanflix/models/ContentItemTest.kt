package com.lanflix.models

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class ContentItemTest {
    @Test
    fun metadataOnlyItemIsNotOfflinePlayable() {
        assertFalse(ContentItem(id = 1, title = "Movie").isOfflinePlayable)
    }

    @Test
    fun itemWithCompletedLocalPathIsMarkedOfflinePlayable() {
        assertTrue(ContentItem(id = 1, title = "Movie", localFilePath = "/offline/movie.mp4").isOfflinePlayable)
    }
}
