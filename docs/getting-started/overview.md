# Lanflix Overview

Lanflix is a complete self-hosted streaming media application that combines content discovery, automated downloads, and personal media streaming into a unified platform.

## What is Lanflix?

Lanflix provides a Netflix-like experience for your personal media collection with:
- **Content Discovery** - Browse trending movies and TV shows via TMDB
- **Automated Downloads** - Queue content through Sonarr, Radarr, and Prowlarr
- **Media Streaming** - Stream your library across all devices
- **Multi-Profile Support** - Individual profiles with separate watch history
- **Smart Storage** - Automatic deletion with keep-watching notifications

## Key Features

### 🎬 Content Discovery
- Browse trending movies and TV shows
- Search across all content types
- Detailed metadata (cast, crew, ratings, descriptions)
- Similar content recommendations
- Trailer previews

### ⬇️ Automated Downloads
- One-click download queuing
- Automatic quality selection
- Real-time progress tracking
- Sonarr (TV) and Radarr (Movies) integration
- Prowlarr indexer management

### 📺 Media Streaming
- High-quality video playback
- Multi-language subtitle support
- Resume from where you left off
- Keyboard shortcuts and remote control support
- Fullscreen and picture-in-picture modes

### 👥 Multi-Profile Support
- Individual profiles for each user
- Separate watch history and watchlists
- Personalized recommendations
- Parental controls (planned)

### 💾 Smart Storage Management
- Automatic deletion of watched content
- Keep-watching notifications before deletion
- Configurable retention policies
- Storage usage monitoring

### 📱 Cross-Platform
- **Web** - Modern responsive interface
- **Android TV** - Optimized for TV remotes
- **Android Mobile** - Touch-optimized
- **Desktop** - Electron app (planned)

## Technology Stack

### Backend
- **Runtime**: Node.js 18+
- **Framework**: Express.js
- **Language**: TypeScript
- **Database**: SQLite with Sequelize ORM
- **Caching**: Redis (optional) + in-memory
- **Logging**: Winston
- **Jobs**: node-cron

### Frontend
- **Build Tool**: Vite
- **Language**: Vanilla JavaScript (ES6+)
- **Styling**: CSS3 with custom properties
- **Video**: Custom HTML5 player
- **State Management**: Custom state manager

### External Services
- **TMDB** - Metadata and images
- **Sonarr** - TV series management
- **Radarr** - Movie management
- **Prowlarr** - Indexer aggregation

## Use Cases

### 🏠 Home Media Server
Perfect for families who want:
- Netflix-like interface for their media
- Automatic new episode downloads
- Individual profiles per family member
- Parental controls and content filtering

### 👤 Personal Streaming Platform
Ideal for individuals who want:
- Centralized media library
- Cross-device streaming
- Watch progress synchronization
- Smart storage management

### 🎯 Media Enthusiast Setup
Great for power users who want:
- Integration with existing *arr stack
- Customizable quality profiles
- Advanced metadata management
- API access for automation

## Comparison with Alternatives

| Feature | Lanflix | Plex | Jellyfin | Emby |
|---------|---------|------|----------|------|
| **Cost** | Free | Freemium | Free | Freemium |
| **Open Source** | ✅ | ❌ | ✅ | ❌ |
| ***arr Integration** | ✅ Native | ⚠️ Plugins | ⚠️ Plugins | ⚠️ Plugins |
| **Resource Usage** | Low | Medium | Medium | Medium |
| **Modern UI** | ✅ | ✅ | ⚠️ | ✅ |
| **Mobile Apps** | Android | All | All | All |
| **Setup Complexity** | Easy | Easy | Medium | Easy |

## Project Goals

1. **Simplicity** - Easy to install, configure, and use
2. **Performance** - Fast and responsive on all devices
3. **Integration** - Seamless *arr stack integration
4. **Modern** - Contemporary UI/UX design
5. **Extensible** - Easy to add new features

## Architecture Highlights

### Service-Oriented Backend
- **Routes** - Handle HTTP requests
- **Services** - Business logic layer
- **Clients** - External API interfaces
- **Models** - Database schema
- **Middleware** - Cross-cutting concerns

### Modular Frontend
- **Modules** - Reusable components
- **Pages** - HTML entry points
- **Styles** - Scoped CSS files
- **State** - Centralized management

### Multi-Layer Caching
- **Memory Cache** - Fast frequent data access
- **Redis Cache** - Shared across instances
- **Image Cache** - Local poster/backdrop storage
- **Metadata Cache** - Reduced external API calls

### Offline Support
Graceful degradation when services are unavailable:
- Cached metadata continues working
- Library browsing remains functional
- Streaming continues uninterrupted
- Background sync when services return

## License

Lanflix is released under the MIT License.

## Next Steps

- [Quick Start Guide](./quick-start.md) - Get started in 5 minutes
- [Installation Guide](./installation.md) - Detailed setup instructions
- [Configuration Guide](./configuration.md) - Environment setup
