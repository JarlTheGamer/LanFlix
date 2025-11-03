package com.lanflix.android.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

enum class ContentFilter {
    ALL, SERIES, MOVIES
}

@Composable
fun SpotlightTabs(
    selectedTab: ContentFilter,
    onTabSelected: (ContentFilter) -> Unit,
    modifier: Modifier = Modifier
) {
    // Exact CSS styling from .spotlight-tabs
    Row(
        modifier = modifier
            .background(
                Color(0x0FFFFFFF), // CSS: rgba(255, 255, 255, 0.06)
                RoundedCornerShape(999.dp) // CSS: border-radius: 999px
            )
            .padding(4.dp), // CSS: padding: 4px
        horizontalArrangement = Arrangement.spacedBy(8.dp) // CSS: gap: 8px
    ) {
        ContentFilter.values().forEach { tab ->
            TabButton(
                text = getTabTitle(tab),
                isSelected = selectedTab == tab,
                onClick = { onTabSelected(tab) }
            )
        }
    }
}

@Composable
private fun TabButton(
    text: String,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(999.dp)) // CSS: border-radius: 999px
            .background(
                if (isSelected) {
                    Color(0x2EFFFFFF) // CSS: rgba(255, 255, 255, 0.18) when active
                } else {
                    Color.Transparent
                }
            )
            .clickable { onClick() }
            .padding(horizontal = 18.dp, vertical = 10.dp), // CSS: padding: 10px 18px
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            color = if (isSelected) {
                Color.White // CSS: var(--text-primary) when active
            } else {
                Color(0x99FFFFFF) // CSS: var(--text-muted)
            },
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium // CSS: font-weight: 500
        )
    }
}

private fun getTabTitle(filter: ContentFilter): String {
    return when (filter) {
        ContentFilter.ALL -> "All"
        ContentFilter.SERIES -> "Series"
        ContentFilter.MOVIES -> "Movies"
    }
}