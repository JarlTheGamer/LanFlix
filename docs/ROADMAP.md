# 🚀 Lanflix Master Feature Roadmap

This document outlines the strategic product roadmap to elevate Lanflix beyond Jellyfin and Plex, combining zero-latency performance with luxury aesthetics and seamless media features.

---

## 🎯 Phase 1: High-Impact Video Player & Content Upgrades

### 1. ⏩ Automated Intro & Credits Skipping
- **Chromaprint Audio Fingerprinting & Chapter Analysis**: Automatically detect intro sequences and credits across TV series episodes.
- **1-Click / Auto-Skip UI**: Display a subtle "Skip Intro" button during theme songs and automatically transition to the next episode when credits roll.

### 2. 🍿 SyncPlay (Watch Party Rooms)
- **Real-Time Synchronized Playback**: Create private rooms to watch movies or TV shows with friends and family in exact synchronization (pause, play, and seek synced via WebSockets).
- **In-Player Live Chat**: Integrated lightweight chat drawer over the video player.

### 3. 🎬 Automatic Collections & Franchise Box Sets
- **TMDb Collection Grouping**: Automatically group movie series (e.g., *Marvel Cinematic Universe*, *Harry Potter*, *Star Wars*, *The Lord of the Rings*) into unified collection cards.
- **Custom Collections**: Allow users to create custom playlists and curated movie marathons.

### 4. 💬 Inline OpenSubtitles & Subscene Fetcher
- **In-Player Subtitle Search**: Search and download subtitles in any language directly from the video player menu without leaving the playback screen.

---

## 🔒 Phase 2: User Experience, Security & Mobile

### 5. ✈️ Mobile & Web Offline Downloads ("Sync to Device")
- **1-Click Offline Download**: Allow users to download pre-transcoded or direct-play media directly to local device storage on iOS, Android, and Web for offline watching during travel.

### 6. 🛡️ Advanced Parental Controls & Content Rating Filters
- **Rating Restrictions**: Restrict specific profiles to maximum content ratings (e.g., G, PG, PG-13, TV-MA).
- **Profile PIN Lock**: 4-digit PIN protection for Admin and adult profiles.
- **Scheduled Access**: Set time limits and bedtime locks for kids' profiles.

---

## 🎵 Phase 3: Expanded Media Types & Live TV

### 7. 🎶 Music & Audiobook Library Support
- **High-Fidelity Audio Server**: FLAC, MP3, and AAC playback with album art, MusicBrainz metadata tagging, and synchronized lyrics.
- **Audiobook Player**: Dedicated progress tracking, chapter support, and playback speed adjustment (1.25x, 1.5x, 2.0x).

### 8. 📺 Live TV & EPG Recording (IPTV / HDHomeRun)
- **Tuner Support**: Native support for HDHomeRun tuners and custom M3U/M3U8 IPTV playlists.
- **EPG & DVR**: Electronic Program Guide (XMLTV) with scheduled background DVR show recording.

---

## ✨ Phase 4: Luxury Aesthetics & Media Polish

### 9. 🎨 Dynamic Background Color Theft & Glassmorphism
- **Dynamic Color Palettes**: Automatically extract dominant colors from movie posters to create smooth, animated ambient background gradients (Apple TV style).
- **Glassmorphism Theme**: Enhanced semi-transparent UI panels with real-time backdrop blurring (`backdrop-filter: blur(20px)`).

### 10. 🎼 TV Series Theme Songs
- **Background Theme Music**: Automatically play theme music (e.g. *Game of Thrones*, *The Office*, *Stranger Things*) when viewing TV show details pages.

### 11. 🖼️ Visual Chapter Cards & Marker Browsing
- **Chapter Gallery**: Extract chapter thumbnails during library scans to allow browsing episodes by visual scenes.

---

## ⚡ Phase 5: Next-Gen Video Engine & Performance

### 12. 💎 AV1 & HEVC Zero-Transcode Remuxing
- **4K HDR10+ & Dolby Vision**: Direct container remuxing for Dolby Vision (Profiles 5, 7, 8) with dynamic hardware tone-mapping.

### 13. 🎧 Spatial Audio & Dolby Atmos Passthrough
- **Uncompressed Bitstream**: Direct bitstream passthrough for Dolby Atmos, TrueHD, DTS:X, and 7.1 surround sound.

### 14. 🧠 RAM Disk Transcoding (`/dev/shm`)
- **Zero SSD Wear**: Option to perform live transcode buffering directly in RAM for zero SSD write wear and instant seek response.

---

## 🛠️ Phase 6: Metadata Editing & External Players

### 15. ✏️ In-Browser Metadata & NFO Editor
- **Custom Artwork & NFO Locking**: Edit titles, overview, genres, upload custom posters/backdrops, and lock metadata fields from auto-updates.

### 16. 🚀 External Player Handoff (VLC / MPV / IINA / MX Player)
- **1-Click External Playback**: Launch media directly into VLC, MPV, IINA, or MX Player on desktop and mobile.

### 17. 📅 Upcoming TV Airings & Missing Episodes Calendar
- **Release Calendar**: Integrated release schedule for upcoming TV episodes and missing seasons in your library.

---

## 📱 Phase 7: Native Apps & Ecosystem

### 18. 📺 Native Apple TV (tvOS), Android TV & Fire TV Apps
- **10-Foot Remote UI**: Dedicated native Swift and Kotlin TV applications built specifically for remote control navigation.

### 19. 📻 AirPlay 2 & Chromecast Native Streaming
- **1-Click TV Casting**: Stream directly from Web or Mobile devices to Smart TVs.

### 20. 🎮 Discord Rich Presence & Trakt.tv / Letterboxd Auto-Scrobbling
- **Automated Social Syncing**: Show current watching status on Discord and auto-sync watch history with Trakt.tv and Letterboxd.

### 21. 🌐 Cross-Server Federation ("Lanflix Link")
- **Friend Server Sharing**: Securely link and stream from trusted friends' Lanflix servers without third-party cloud accounts.

---

## 🏆 Phase 8: End-Game Player Innovation & X-Ray Capabilities *(Final Phase)*

### 22. 🎞️ YouTube-Style Precise Scrubbing & Filmstrip Seeking
- **Filmstrip Track Preview**: Dragging or swiping up on the progress bar expands a high-resolution filmstrip preview of exact video frames for precise scrubbing.
- **Most Replayed Heatmap Curve**: Subtle graph overlay on the timeline highlighting peak replayed / most popular scenes.

### 23. 🔍 Amazon Prime-Style "X-Ray" In-Scene Actor & Soundtrack Overlay
- **In-Scene Cast Recognition**: Pausing the video reveals an interactive "X-Ray" overlay showing the exact actors appearing in the current scene, their character names, actor headshots, and bio details.
- **Scene Soundtrack Identification**: Identifies the song/music track currently playing in the active scene with 1-click links to play or view details.
- **Main Cast & Crew Deep Dive**: Tap any actor in the scene overlay to view their full filmography across your Lanflix library.

---

## 📊 Summary Parity & Advantage Matrix

| Feature | Jellyfin | Plex | Lanflix Target |
| :--- | :---: | :---: | :---: |
| **Zero-Latency DirectPlay** | ⚠️ Moderate | ⚠️ Moderate | ⚡ **Ultra-Fast** |
| **Zero-Config mDNS & Pairing** | ❌ Manual | ❌ Account Needed | ✅ **Native** |
| **Built-in Servarr Suite** | ❌ Plugins | ❌ No | ✅ **Native** |
| **Intro & Credits Skipping** | ⚠️ Plugin | ✅ Paid (Pass) | 🚀 **Phase 1** |
| **SyncPlay Watch Party** | ✅ Yes | ❌ Removed | 🚀 **Phase 1** |
| **Collections & Box Sets** | ✅ Yes | ✅ Yes | 🚀 **Phase 1** |
| **Offline Sync / Downloads** | ⚠️ Third-Party | ✅ Paid (Pass) | 🚀 **Phase 2** |
| **Parental Rating Locks** | ✅ Yes | ✅ Yes | 🚀 **Phase 2** |
| **Live TV & EPG DVR** | ✅ Yes | ✅ Paid (Pass) | 🚀 **Phase 3** |
| **TV Show Theme Songs** | ⚠️ Plugin | ✅ Yes | 🚀 **Phase 4** |
| **Metadata & NFO Editor** | ✅ Yes | ✅ Yes | 🚀 **Phase 6** |
| **External Player Handoff (VLC/MPV)** | ✅ Yes | ❌ No | 🚀 **Phase 6** |
| **Native Apple TV / Fire TV Apps** | ⚠️ Swiftfin | ✅ Yes | 🚀 **Phase 7** |
| **YouTube-Style Precise Scrubbing** | ❌ No | ❌ No | 🏆 **Phase 8 (Final)** |
| **Prime-Style X-Ray Scene Actors** | ❌ No | ❌ No | 🏆 **Phase 8 (Final)** |
