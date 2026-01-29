# Lanflix Architecture

Lanflix is a modern media streaming server built with a Focus on performance and simplicity.

## System Design

Lanflix follows a decoupled architecture where the Backend serves as a headless API and the Frontend is a single-page application (SPA).

```
[ Frontend (Vanilla JS + Vite) ] <--> [ Backend (.NET 9 Web API) ] <--> [ SQLite ]
                                             |
                                             +--> [ FFmpeg / FFprobe ]
                                             +--> [ External APIs (TMDB, Sonarr) ]
```

## Backend Architecture

The backend is organized using **Clean Architecture** principles:

- **Domain**: Contains core entities (`Content`, `Episode`, `Profile`), value objects, and enums. Zero dependencies.
- **Application**: Contains business logic, interfaces, and DTOs. It defines how the system works without knowing about the database or web framework.
- **Infrastructure**: Implements interfaces defined in Application. Handles database (EF Core), File System, and FFmpeg orchestration.
- **WebApi**: The entry point. Handles HTTP requests, SignalR hubs, and serves the static frontend files.

## Key Services

- **EnhancedStreamingService**: Decides whether to Direct Play, Direct Stream, or Transcode based on client capabilities and media metadata.
- **EnhancedTranscodingPipeline**: Orchestrates FFmpeg processes for real-time video transformation.
- **MediaAnalyzer**: Uses FFprobe to extract detailed technical metadata from media files.
- **TranscodingSessionManager**: Tracks active sessions to prevent redundant FFmpeg processes and handle seeking.

## Frontend Architecture

The frontend is a lightweight SPA built with Vanilla JavaScript and CSS.
- **Vite**: Used for bundling and dev-server proxying.
- **Modular JS**: Logic is broken into ES modules (e.g., `video-player.js`, `api-client.js`).
- **CSS Variables**: A centralized design system for consistent themes.

## Technology Stack

| Layer | Technology |
|-------|------------|
| Language | C# 12 / JavaScript (ESNext) |
| Framework | ASP.NET Core 9.0 |
| Frontend Bundler | Vite |
| Database | SQLite (WAL Mode) |
| ORM | Entity Framework Core |
| Real-time | SignalR |
| Logging | Serilog |
| Media Engine | FFmpeg |
