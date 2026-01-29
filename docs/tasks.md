# Lanflix Comprehensive Cleanup & Refactoring Status

This is the exhaustive list of all source files in the project (excluding build artifacts and dependencies).

## 🏗️ Core Infrastructure & Build
- [x] `lanflix-server/build.ps1` (Modern PowerShell Build System)
- [x] `lanflix-server/app/WebApi/Program.cs` (Refactored/Simplified)
- [x] `lanflix-server/app/WebApi/Extensions/ServiceCollectionExtensions.cs` (Created)
- [x] `lanflix-server/app/WebApi/Extensions/WebApplicationExtensions.cs` (Created)
- [ ] `lanflix-server/app/WebApi/appsettings.json` (Needs production check)

## 🎥 Media & Transcoding Subsystem (Infrastructure)
- [x] `Infrastructure/Services/FFmpeg/Builders/TranscodingArgumentBuilder.cs` (New)
- [x] `Infrastructure/Services/FFmpeg/EnhancedTranscodingPipeline.cs` (Refactored)
- [x] `Infrastructure/Services/FFmpeg/MediaAnalyzer.cs` (Refactored)
- [x] `Infrastructure/Services/FFmpeg/EnhancedHardwareAccelerationDetector.cs` (Refactored)
- [x] `Infrastructure/Services/FFmpeg/SubtitleService.cs` (New)
- [x] `Infrastructure/Services/Streaming/TranscodingSessionManager.cs` (Refactored)
- [ ] `Infrastructure/Services/Streaming/TranscodingFileCleanupService.cs`
- [ ] `Infrastructure/Services/Audio/AudioNormalizationService.cs`
- [ ] `Infrastructure/Services/BackgroundJobs/BackupJob.cs`
- [ ] `Infrastructure/Services/BackgroundJobs/LibraryScanJob.cs`
- [ ] `Infrastructure/Services/Caching/RedisCacheService.cs`
- [ ] `Infrastructure/Services/Metadata/TmdbMetadataProvider.cs`
- [x] `Infrastructure/Services/Settings/SettingsService.cs` (Refactored)
- [x] `Infrastructure/Services/ExternalApis/TmdbClient.cs` (Modernized)
- [x] `Infrastructure/Services/ExternalApis/SonarrClient.cs` (Modernized)
- [x] `Infrastructure/Services/ExternalApis/RadarrClient.cs` (Modernized)
- [ ] `Infrastructure/Persistence/ApplicationDbContext.cs` (Check EF Core mappings)

## � Web API (Controllers & Middleware)
- [x] `WebApi/Controllers/TranscodingController.cs` (Refactored)
- [x] `WebApi/Controllers/ContentController.cs` (Refactored)
- [x] `WebApi/Controllers/LibraryController.cs` (Refactored)
- [x] `WebApi/Controllers/SeriesController.cs` (Refactored)
- [x] `WebApi/Controllers/MoviesController.cs` (Refactored)
- [x] `WebApi/Controllers/SettingsController.cs` (Refactored)
- [x] `WebApi/Controllers/ProfilesController.cs` (Refactored)
- [x] `WebApi/Controllers/AppUpdateController.cs` (Refactored)
- [x] `WebApi/Controllers/AuthController.cs` (Refactored)
- [x] `WebApi/Controllers/DownloadsController.cs` (Refactored)
- [x] `WebApi/Controllers/JobsController.cs` (Refactored)
- [x] `WebApi/Controllers/NotificationsController.cs` (Refactored)
- [x] `WebApi/Controllers/ServerUpdateController.cs` (Refactored)
- [x] `WebApi/Controllers/StreamController.cs` (Refactored)
- [x] `WebApi/Controllers/TokenMigrationController.cs` (Refactored)
- [x] `WebApi/Controllers/VideosController.cs` (Refactored)
- [x] `WebApi/Middleware/ApiVersionDetectionMiddleware.cs` (Checked)
- [x] `WebApi/Middleware/ExceptionHandlingMiddleware.cs` (Refactored)
- [x] `WebApi/Middleware/LegacyResponseFormatterMiddleware.cs` (Refactored)
- [ ] `WebApi/Hubs/NotificationHub.cs`

## 🧠 Application Logic (Commands & Queries)
- [ ] `Application/Common/Interfaces/IApplicationDbContext.cs`
- [x] `Application/Features/Streaming/Services/EnhancedStreamingService.cs` (Refactored)
- [ ] `Application/Features/Library/Queries/GetMoviesQuery.cs`
- [ ] `Application/Features/Library/Queries/GetSeriesQuery.cs`
- [ ... and 40+ other Application service/query files ]

## � Mobile (Android-Tools) - ~1,800 Files
- [ ] `build-tools/AndroidVersions/Settings`
- [ ] `build-tools/AndroidVersions/Source`
- [ ] `build-tools/AndroidVersions/Assets`
- [Note: This project is extremely large and may contain outdated binary blobs.]

## 🗑️ Recently Deleted (Cleanup Log)
- [x] Root `package.json` & `package-lock.json`
- [x] `build-tools/scripts/` (Legacy Node.js build scripts)
- [x] `docs/getting-started/` (Obsolete documentation)
- [x] `lanflix-server/app/WebApi/WeatherForecast.cs`
- [x] `lanflix-server/app/WebApi/Lanflix.Server.http`
