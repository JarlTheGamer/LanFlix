package com.lanflix.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.lanflix.android.domain.model.Profile
import com.lanflix.android.domain.model.ServerInfo
import com.lanflix.android.ui.content.ContentDetailsScreen
import com.lanflix.android.ui.discovery.ServerDiscoveryScreen
import com.lanflix.android.ui.home.HomeScreen
import com.lanflix.android.ui.player.VideoPlayerScreen
import com.lanflix.android.ui.profile.ProfileSelectionScreen
import com.lanflix.android.ui.search.SearchScreen
import com.lanflix.android.ui.theme.LanflixTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        
        setContent {
            LanflixTheme {
                LanflixApp()
            }
        }
    }
}

@Composable
fun LanflixApp() {
    val navController = rememberNavController()
    var selectedProfile by remember { mutableStateOf<Profile?>(null) }
    var selectedServer by remember { mutableStateOf<ServerInfo?>(null) }
    
    NavHost(
        navController = navController,
        startDestination = "server_discovery"
    ) {
        composable("server_discovery") {
            ServerDiscoveryScreen(
                onServerSelected = { server ->
                    selectedServer = server
                    // Server URL is automatically saved by ServerDiscoveryRepository
                    navController.navigate("profile_selection") {
                        // Clear the server discovery from back stack
                        popUpTo("server_discovery") { inclusive = true }
                    }
                }
            )
        }
        
        composable("profile_selection") {
            ProfileSelectionScreen(
                onProfileSelected = { profile ->
                    selectedProfile = profile
                    navController.navigate("home")
                }
            )
        }
        
        composable("home") {
            HomeScreen(
                selectedProfile = selectedProfile,
                onProfileClick = {
                    navController.navigate("profile_selection")
                },
                onContentClick = { contentId, contentType ->
                    navController.navigate("content/$contentId")
                },
                onSearchClick = {
                    navController.navigate("search")
                },
                onSettingsClick = {
                    // TODO: Navigate to settings
                },
                onNotificationsClick = {
                    // TODO: Navigate to notifications
                }
            )
        }
        
        composable("search") {
            SearchScreen(
                onBackClick = { navController.popBackStack() },
                onContentClick = { contentId, contentType ->
                    navController.navigate("content/$contentId")
                }
            )
        }
        
        composable(
            "content/{contentId}",
            arguments = listOf(navArgument("contentId") { type = NavType.StringType })
        ) { backStackEntry ->
            val contentId = backStackEntry.arguments?.getString("contentId") ?: ""
            ContentDetailsScreen(
                contentId = contentId,
                onBackClick = { navController.popBackStack() },
                onPlayClick = {
                    navController.navigate("player/$contentId")
                }
            )
        }
        
        composable(
            "player/{contentId}",
            arguments = listOf(navArgument("contentId") { type = NavType.StringType })
        ) { backStackEntry ->
            val contentId = backStackEntry.arguments?.getString("contentId") ?: ""
            VideoPlayerScreen(
                contentId = contentId,
                onBackClick = { navController.popBackStack() }
            )
        }
    }
}

