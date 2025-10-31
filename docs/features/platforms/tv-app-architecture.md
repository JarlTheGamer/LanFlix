# Android TV App Architecture & Quality Plan

## Current State Analysis

### Issues Identified

1. **Not a Real TV App**
   - Web app with D-pad navigation bolted on
   - No leanback UI design
   - No TV-optimized layouts (10-foot UI)
   - Desktop UI scaled to TV screen
   - Poor focus management

2. **Navigation Problems**
   - Basic keyboard event handling
   - No spatial navigation
   - Focus gets lost easily
   - No visual focus indicators
   - No back button handling

3. **Performance Issues**
   - Web rendering on TV hardware
   - No hardware acceleration optimization
   - Slow UI responsiveness
   - High memory usage

4. **Missing TV Features**
   - No Android TV launcher integration
   - No recommendations row
   - No voice search
   - No TV input framework
   - No picture-in-picture
   - No live channels integration
   - No content ratings

5. **User Experience**
   - Tiny text on TV screens
   - Poor contrast ratios
   - No overscan compensation
   - Complex navigation paths
   - No TV-specific gestures

## Recommended Architecture

### Option 1: Native Android TV App (Recommended)

**Tech Stack:**
```
- Kotlin
- Jetpack Compose for TV
- ExoPlayer for video playback
- Leanback Library (optional, for traditional approach)
- Coil for image loading
- Retrofit for API calls
- Hilt for dependency injection
- Room for local database
- Coroutines & Flow for async
```

**Pros:**
- True native TV experience
- Best performance
- Full access to TV APIs
- Google's official TV guidelines
- Hardware acceleration
- Proper focus management

**Cons:**
- Requires Kotlin/Android knowledge
- Separate codebase from mobile

### Option 2: React Native TV

**Tech Stack:**
```
- React Native TV (fork of React Native)
- TypeScript
- React Navigation for TV
- React Native Video
```

**Pros:**
- Share code with mobile app
- Familiar React patterns
- Faster development

**Cons:**
- Less mature than native
- Limited TV-specific features
- Performance not as good as native

## Recommended Approach: Native Android TV (Kotlin + Compose)

### Phase 1: Foundation (Weeks 1-2)

**1.1 Project Setup**
```kotlin
// build.gradle.kts
plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("com.google.dagger.hilt.android")
}

android {
    namespace = "com.lanflix.tv"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.lanflix.tv"
        minSdk = 21  // Android TV minimum
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"
    }

    buildFeatures {
        compose = true
    }

    composeOptions {
        kotlinCompilerExtensionVersion = "1.5.3"
    }
}

dependencies {
    // Compose for TV
    implementation("androidx.tv:tv-foundation:1.0.0-alpha10")
    implementation("androidx.tv:tv-material:1.0.0-alpha10")
    
    // ExoPlayer
    implementation("androidx.media3:media3-exoplayer:1.2.0")
    implementation("androidx.media3:media3-ui:1.2.0")
    implementation("androidx.media3:media3-exoplayer-hls:1.2.0")
    
    // Leanback (optional)
    implementation("androidx.leanback:leanback:1.2.0-alpha04")
    
    // Networking
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")
    
    // Image loading
    implementation("io.coil-kt:coil-compose:2.5.0")
    
    // DI
    implementation("com.google.dagger:hilt-android:2.48")
    kapt("com.google.dagger:hilt-compiler:2.48")
    
    // Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")
}
```

**1.2 AndroidManifest.xml**
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <!-- TV-specific features -->
    <uses-feature
        android:name="android.hardware.touchscreen"
        android:required="false" />
    <uses-feature
        android:name="android.software.leanback"
        android:required="true" />

    <!-- Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <application
        android:name=".LanflixTVApp"
        android:allowBackup="true"
        android:icon="@drawable/ic_launcher"
        android:banner="@drawable/app_banner"
        android:label="@string/app_name"
        android:theme="@style/Theme.LanflixTV">

        <activity
            android:name=".ui.MainActivity"
            android:exported="true"
            android:screenOrientation="landscape"
            android:configChanges="keyboard|keyboardHidden|navigation">
            
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LEANBACK_LAUNCHER" />
            </intent-filter>
        </activity>

        <!-- Search activity -->
        <activity
            android:name=".ui.SearchActivity"
            android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.SEARCH" />
            </intent-filter>
            <meta-data
                android:name="android.app.searchable"
                android:resource="@xml/searchable" />
        </activity>

        <!-- Recommendations service -->
        <service
            android:name=".service.RecommendationService"
            android:enabled="true" />

    </application>
</manifest>
```

**1.3 Folder Structure**
```
tv/
├── app/
│   ├── src/
│   │   ├── main/
│   │   │   ├── java/com/lanflix/tv/
│   │   │   │   ├── data/
│   │   │   │   │   ├── api/          # API interfaces
│   │   │   │   │   ├── model/        # Data models
│   │   │   │   │   ├── repository/   # Repositories
│   │   │   │   │   └── local/        # Local database
│   │   │   │   ├── di/               # Dependency injection
│   │   │   │   ├── domain/           # Business logic
│   │   │   │   │   ├── usecase/
│   │   │   │   │   └── model/
│   │   │   │   ├── ui/               # UI layer
│   │   │   │   │   ├── home/
│   │   │   │   │   ├── player/
│   │   │   │   │   ├── search/
│   │   │   │   │   ├── settings/
│   │   │   │   │   └── components/   # Reusable components
│   │   │   │   ├── util/             # Utilities
│   │   │   │   └── LanflixTVApp.kt
│   │   │   ├── res/
│   │   │   │   ├── drawable/
│   │   │   │   ├── layout/
│   │   │   │   ├── values/
│   │   │   │   └── xml/
│   │   │   └── AndroidManifest.xml
│   │   └── test/
│   └── build.gradle.kts
└── build.gradle.kts
```

### Phase 2: Core UI Components (Weeks 3-4)

**2.1 TV-Optimized Home Screen**
```kotlin
@Composable
fun HomeScreen(
    viewModel: HomeViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    TvLazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(
            start = 48.dp,  // TV overscan safe area
            top = 27.dp,
            end = 48.dp,
            bottom = 27.dp
        )
    ) {
        // Continue Watching Row
        item {
            MediaRow(
                title = "Continue Watching",
                items = uiState.continueWatching,
                onItemClick = { viewModel.playContent(it) }
            )
        }
        
        // Recently Added Row
        item {
            MediaRow(
                title = "Recently Added",
                items = uiState.recentlyAdded,
                onItemClick = { viewModel.playContent(it) }
            )
        }
        
        // Categories
        uiState.categories.forEach { category ->
            item {
                MediaRow(
                    title = category.name,
                    items = category.items,
                    onItemClick = { viewModel.playContent(it) }
                )
            }
        }
    }
}

@Composable
fun MediaRow(
    title: String,
    items: List<MediaItem>,
    onItemClick: (MediaItem) -> Unit
) {
    Column(modifier = Modifier.padding(vertical = 16.dp)) {
        Text(
            text = title,
            style = MaterialTheme.typography.headlineMedium,
            color = Color.White,
            modifier = Modifier.padding(bottom = 16.dp)
        )
        
        TvLazyRow(
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            items(items) { item ->
                MediaCard(
                    item = item,
                    onClick = { onItemClick(item) }
                )
            }
        }
    }
}

@Composable
fun MediaCard(
    item: MediaItem,
    onClick: () -> Unit
) {
    var isFocused by remember { mutableStateOf(false) }
    
    Card(
        onClick = onClick,
        modifier = Modifier
            .width(200.dp)
            .height(300.dp)
            .onFocusChanged { isFocused = it.isFocused }
            .scale(if (isFocused) 1.1f else 1.0f)  // Scale on focus
            .border(
                width = if (isFocused) 4.dp else 0.dp,
                color = if (isFocused) Color.White else Color.Transparent,
                shape = RoundedCornerShape(8.dp)
            ),
        shape = RoundedCornerShape(8.dp)
    ) {
        Box {
            AsyncImage(
                model = item.posterUrl,
                contentDescription = item.title,
                modifier = Modifier.fillMaxSize(),
                contentScale = ContentScale.Crop
            )
            
            // Gradient overlay for title
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(
                        Brush.verticalGradient(
                            colors = listOf(
                                Color.Transparent,
                                Color.Black.copy(alpha = 0.7f)
                            )
                        )
                    )
            )
            
            Text(
                text = item.title,
                style = MaterialTheme.typography.bodyLarge,
                color = Color.White,
                modifier = Modifier
                    .align(Alignment.BottomStart)
                    .padding(12.dp)
            )
        }
    }
}
```

**2.2 Video Player with TV Controls**
```kotlin
@Composable
fun VideoPlayerScreen(
    contentId: Int,
    viewModel: PlayerViewModel = hiltViewModel()
) {
    val playerState by viewModel.playerState.collectAsState()
    var showControls by remember { mutableStateOf(true) }
    val hideControlsJob = remember { mutableStateOf<Job?>(null) }
    
    DisposableEffect(Unit) {
        viewModel.initializePlayer(contentId)
        onDispose {
            viewModel.releasePlayer()
        }
    }
    
    Box(modifier = Modifier.fillMaxSize()) {
        // ExoPlayer view
        AndroidView(
            factory = { context ->
                PlayerView(context).apply {
                    player = viewModel.exoPlayer
                    useController = false  // Custom controls
                    layoutParams = ViewGroup.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        ViewGroup.LayoutParams.MATCH_PARENT
                    )
                }
            },
            modifier = Modifier.fillMaxSize()
        )
        
        // Custom TV controls
        AnimatedVisibility(
            visible = showControls,
            enter = fadeIn(),
            exit = fadeOut()
        ) {
            TVPlayerControls(
                playerState = playerState,
                onPlayPause = { viewModel.togglePlayPause() },
                onSeek = { viewModel.seekTo(it) },
                onBack = { viewModel.onBackPressed() },
                onShowControls = {
                    showControls = true
                    hideControlsJob.value?.cancel()
                    hideControlsJob.value = viewModel.viewModelScope.launch {
                        delay(5000)
                        showControls = false
                    }
                }
            )
        }
    }
    
    // Handle D-pad events
    LaunchedEffect(Unit) {
        // Show controls on any D-pad press
        // Implementation depends on your event handling
    }
}

@Composable
fun TVPlayerControls(
    playerState: PlayerState,
    onPlayPause: () -> Unit,
    onSeek: (Long) -> Unit,
    onBack: () -> Unit,
    onShowControls: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black.copy(alpha = 0.5f))
    ) {
        Column(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .padding(48.dp)
        ) {
            // Progress bar
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = formatTime(playerState.currentPosition),
                    color = Color.White,
                    style = MaterialTheme.typography.bodyLarge
                )
                
                Slider(
                    value = playerState.currentPosition.toFloat(),
                    onValueChange = { onSeek(it.toLong()) },
                    valueRange = 0f..playerState.duration.toFloat(),
                    modifier = Modifier
                        .weight(1f)
                        .padding(horizontal = 16.dp)
                )
                
                Text(
                    text = formatTime(playerState.duration),
                    color = Color.White,
                    style = MaterialTheme.typography.bodyLarge
                )
            }
            
            Spacer(modifier = Modifier.height(24.dp))
            
            // Control buttons
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.Center,
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Rewind button
                IconButton(
                    onClick = { onSeek(playerState.currentPosition - 10000) },
                    modifier = Modifier.size(64.dp)
                ) {
                    Icon(
                        imageVector = Icons.Default.Replay10,
                        contentDescription = "Rewind 10s",
                        tint = Color.White,
                        modifier = Modifier.size(48.dp)
                    )
                }
                
                Spacer(modifier = Modifier.width(32.dp))
                
                // Play/Pause button
                IconButton(
                    onClick = onPlayPause,
                    modifier = Modifier.size(80.dp)
                ) {
                    Icon(
                        imageVector = if (playerState.isPlaying) 
                            Icons.Default.Pause else Icons.Default.PlayArrow,
                        contentDescription = if (playerState.isPlaying) "Pause" else "Play",
                        tint = Color.White,
                        modifier = Modifier.size(64.dp)
                    )
                }
                
                Spacer(modifier = Modifier.width(32.dp))
                
                // Forward button
                IconButton(
                    onClick = { onSeek(playerState.currentPosition + 30000) },
                    modifier = Modifier.size(64.dp)
                ) {
                    Icon(
                        imageVector = Icons.Default.Forward30,
                        contentDescription = "Forward 30s",
                        tint = Color.White,
                        modifier = Modifier.size(48.dp)
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(16.dp))
            
            // Title and metadata
            Text(
                text = playerState.title,
                style = MaterialTheme.typography.headlineMedium,
                color = Color.White
            )
            
            Text(
                text = playerState.metadata,
                style = MaterialTheme.typography.bodyMedium,
                color = Color.White.copy(alpha = 0.7f)
            )
        }
    }
}
```

### Phase 3: Advanced TV Features (Weeks 5-6)

**3.1 Voice Search Integration**
```kotlin
class SearchActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        if (Intent.ACTION_SEARCH == intent.action) {
            val query = intent.getStringExtra(SearchManager.QUERY)
            performSearch(query)
        }
    }
    
    private fun performSearch(query: String?) {
        // Implement search logic
    }
}

// res/xml/searchable.xml
<?xml version="1.0" encoding="utf-8"?>
<searchable xmlns:android="http://schemas.android.com/apk/res/android"
    android:label="@string/app_name"
    android:hint="@string/search_hint"
    android:searchMode="showSearchLabelAsBadge"
    android:voiceSearchMode="showVoiceSearchButton|launchRecognizer"
    android:searchSuggestAuthority="com.lanflix.tv.suggestions"
    android:searchSuggestIntentAction="android.intent.action.VIEW" />
```

**3.2 Recommendations Row**
```kotlin
class RecommendationService : IntentService("RecommendationService") {
    override fun onHandleIntent(intent: Intent?) {
        val recommendations = fetchRecommendations()
        
        recommendations.forEach { content ->
            val notification = NotificationCompat.BigPictureStyle(
                NotificationCompat.Builder(this, CHANNEL_ID)
                    .setContentTitle(content.title)
                    .setContentText(content.description)
                    .setSmallIcon(R.drawable.ic_notification)
                    .setLargeIcon(loadBitmap(content.posterUrl))
                    .setContentIntent(createPendingIntent(content))
            )
            .setBigContentTitle(content.title)
            .setSummaryText(content.description)
            .build()
            
            notificationManager.notify(content.id, notification.build())
        }
    }
}
```

**3.3 Picture-in-Picture**
```kotlin
class PlayerActivity : AppCompatActivity() {
    override fun onUserLeaveHint() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            enterPictureInPictureMode(
                PictureInPictureParams.Builder()
                    .setAspectRatio(Rational(16, 9))
                    .build()
            )
        }
    }
    
    override fun onPictureInPictureModeChanged(
        isInPictureInPictureMode: Boolean,
        newConfig: Configuration
    ) {
        if (isInPictureInPictureMode) {
            // Hide controls
            hideControls()
        } else {
            // Show controls
            showControls()
        }
    }
}
```

### Phase 4: Performance & Polish (Weeks 7-8)

**4.1 ExoPlayer Optimization**
```kotlin
class PlayerViewModel @Inject constructor(
    private val context: Application
) : ViewModel() {
    
    val exoPlayer: ExoPlayer by lazy {
        ExoPlayer.Builder(context)
            .setLoadControl(
                DefaultLoadControl.Builder()
                    .setBufferDurationsMs(
                        15000,  // Min buffer
                        50000,  // Max buffer
                        2500,   // Playback buffer
                        5000    // Playback rebuffer
                    )
                    .build()
            )
            .setRenderersFactory(
                DefaultRenderersFactory(context).apply {
                    setExtensionRendererMode(
                        DefaultRenderersFactory.EXTENSION_RENDERER_MODE_PREFER
                    )
                }
            )
            .build()
    }
    
    fun initializePlayer(contentId: Int) {
        viewModelScope.launch {
            val streamUrl = repository.getStreamUrl(contentId)
            
            val mediaItem = MediaItem.Builder()
                .setUri(streamUrl)
                .setMediaMetadata(
                    MediaMetadata.Builder()
                        .setTitle(content.title)
                        .setArtworkUri(Uri.parse(content.posterUrl))
                        .build()
                )
                .build()
            
            exoPlayer.setMediaItem(mediaItem)
            exoPlayer.prepare()
            exoPlayer.play()
        }
    }
}
```

**4.2 Image Caching Strategy**
```kotlin
@Module
@InstallIn(SingletonComponent::class)
object ImageLoadingModule {
    
    @Provides
    @Singleton
    fun provideImageLoader(
        @ApplicationContext context: Context
    ): ImageLoader {
        return ImageLoader.Builder(context)
            .memoryCache {
                MemoryCache.Builder(context)
                    .maxSizePercent(0.25)  // Use 25% of app memory
                    .build()
            }
            .diskCache {
                DiskCache.Builder()
                    .directory(context.cacheDir.resolve("image_cache"))
                    .maxSizeBytes(512 * 1024 * 1024)  // 512MB
                    .build()
            }
            .respectCacheHeaders(false)
            .build()
    }
}
```

## TV-Specific Design Guidelines

### 1. Layout & Spacing
- **Overscan safe area:** 48dp horizontal, 27dp vertical margins
- **Focus size:** Minimum 48dp touch target
- **Text size:** Minimum 18sp for body text, 24sp for titles
- **Card spacing:** 12-16dp between items

### 2. Focus Management
```kotlin
// Custom focus behavior
Modifier.focusRequester(focusRequester)
    .onFocusChanged { focusState ->
        if (focusState.isFocused) {
            // Handle focus
            onFocused()
        }
    }
    .focusable()
```

### 3. Color & Contrast
- **Background:** Dark theme (AMOLED black preferred)
- **Text contrast:** Minimum 4.5:1 ratio
- **Focus indicator:** High contrast border (white/accent color)

### 4. Navigation Patterns
- **Horizontal:** Primary navigation (rows)
- **Vertical:** Secondary navigation (within rows)
- **Back button:** Always returns to previous screen
- **Home button:** Returns to home screen

## Testing Strategy

### 1. Unit Tests
```kotlin
@Test
fun `test player state updates correctly`() = runTest {
    val viewModel = PlayerViewModel(repository)
    
    viewModel.initializePlayer(1)
    advanceUntilIdle()
    
    val state = viewModel.playerState.value
    assertEquals(true, state.isPlaying)
    assertTrue(state.duration > 0)
}
```

### 2. UI Tests
```kotlin
@Test
fun testHomeScreenNavigation() {
    composeTestRule.setContent {
        HomeScreen()
    }
    
    // Test D-pad navigation
    composeTestRule.onNodeWithTag("media-card-0")
        .performKeyPress(KeyEvent(KeyEvent.ACTION_DOWN, KeyEvent.KEYCODE_DPAD_CENTER))
    
    composeTestRule.onNodeWithTag("player-screen")
        .assertIsDisplayed()
}
```

### 3. TV Device Testing
- Test on actual Android TV devices (Shield, Chromecast with Google TV)
- Test on TV emulator
- Test with different screen sizes (720p, 1080p, 4K)
- Test with different remote controls

## Build & Deployment

### Gradle Build Configuration
```kotlin
android {
    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            signingConfig = signingConfigs.getByName("release")
        }
    }
    
    bundle {
        language {
            enableSplit = false  // Include all languages
        }
        density {
            enableSplit = true
        }
        abi {
            enableSplit = true
        }
    }
}
```

### Play Store Requirements
- App banner (1920x1080)
- Screenshots (1920x1080, minimum 3)
- Feature graphic (1024x500)
- TV content rating
- Privacy policy
- Support for D-pad navigation

## Success Metrics

- App launch time < 3 seconds
- Video playback starts < 2 seconds
- 60fps UI animations
- Focus navigation response < 100ms
- Memory usage < 300MB
- Crash-free rate > 99.5%
- Play Store rating > 4.0

## Timeline Summary

- **Weeks 1-2:** Project setup & foundation
- **Weeks 3-4:** Core UI & player
- **Weeks 5-6:** Advanced TV features
- **Weeks 7-8:** Polish & testing
- **Week 9:** Beta testing
- **Week 10:** Play Store release

## Next Steps

1. Review and approve architecture
2. Set up Android TV project
3. Design TV UI mockups
4. Implement Phase 1
5. Test on actual TV devices
6. Submit to Play Store for review
