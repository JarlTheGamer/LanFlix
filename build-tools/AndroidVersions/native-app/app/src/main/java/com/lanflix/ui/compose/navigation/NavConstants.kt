package com.lanflix.ui.compose.navigation

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.LiveTv
import androidx.compose.material.icons.filled.TravelExplore
import androidx.compose.material.icons.filled.VideoLibrary
import androidx.compose.material.icons.outlined.Download
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.LiveTv
import androidx.compose.material.icons.outlined.TravelExplore
import androidx.compose.material.icons.outlined.VideoLibrary
import androidx.compose.ui.graphics.vector.ImageVector

enum class Destination(val label: String, val selected: ImageVector, val unselected: ImageVector) {
    Home("Home", Icons.Filled.Home, Icons.Outlined.Home),
    Libraries("Libraries", Icons.Filled.VideoLibrary, Icons.Outlined.VideoLibrary),
    Live("Live TV", Icons.Filled.LiveTv, Icons.Outlined.LiveTv),
    Demand("On Demand", Icons.Filled.Download, Icons.Outlined.Download),
    Discover("Discover", Icons.Filled.TravelExplore, Icons.Outlined.TravelExplore)
}

enum class AppOverlay { Search, Profile, Settings, Account, Activity, Notifications }
