using Lanflix.Infrastructure.Adapters.Library;
using Lanflix.Infrastructure.Adapters.Playback;
using Lanflix.Infrastructure.Adapters.Downloads;
using Lanflix.Infrastructure.Services.ExternalApis;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Modules.Downloads;
using Lanflix.Infrastructure.Adapters.Administration;
using Lanflix.Infrastructure.Services.AppUpdate;
using Lanflix.Infrastructure.Services.Library;
using Lanflix.Infrastructure.Services.Metadata;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Streaming;
using Lanflix.Modules.Administration;
using Lanflix.Modules.Library;
using Lanflix.Modules.Playback;
using Lanflix.Modules.Discovery;
using Lanflix.Infrastructure.Adapters.Discovery;
using Lanflix.Infrastructure.Adapters.Subtitles;
using Lanflix.Modules.Subtitles;
using Lanflix.Infrastructure.Adapters.Realtime;
using Lanflix.Modules.Realtime;
using Lanflix.Modules.Music;
using Lanflix.Infrastructure.Adapters.Music;
using Lanflix.Modules.LiveTV;
using Lanflix.Infrastructure.Adapters.LiveTV;
using Lanflix.Modules.Social;
using Lanflix.Infrastructure.Adapters.Social;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Infrastructure.Adapters;

public static class AdapterRegistration
{
    public static IServiceCollection AddLanflixModuleAdapters(this IServiceCollection services)
    {
        services.AddScoped<ILibraryCatalog, SqliteLibraryCatalog>();
        services.AddScoped<IPlaybackSourceCatalog, SqlitePlaybackSourceCatalog>();
        services.AddScoped<IAdaptivePlaybackService, AdaptivePlaybackService>();
        services.AddScoped<IDownloadQueue, ExternalDownloadQueue>();
        services.AddScoped<IRadarrClient, RadarrClient>();
        services.AddScoped<ISonarrClient, SonarrClient>();
        services.AddScoped<IBazarrClient, BazarrClient>();
        services.AddScoped<IProwlarrClient, ProwlarrClient>();
        services.AddScoped<IAdministrationOperations, AdministrationOperations>();
        services.AddScoped<IApplicationReleaseCatalog, ApplicationReleaseCatalog>();
        services.AddScoped<IDiscoveryProvider, ExternalDiscoveryProvider>();
        services.AddScoped<ISubtitleCatalog, FfmpegSubtitleCatalog>();
        services.AddSingleton<IProgressBroadcaster, SignalRProgressBroadcaster>();
        services.AddScoped<IMusicCatalog, LocalMusicCatalog>();
        services.AddScoped<ILiveTvCatalog, LiveTvCatalog>();
        services.AddScoped<ISocialResourceDirectory, SocialResourceDirectory>();
        services.AddScoped<ISocialNotificationPublisher, SignalRSocialNotificationPublisher>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IMediaAnalyzer, MediaAnalyzer>();
        services.AddScoped<IIntroScanner, AudioFingerprintIntroScanner>();
        services.AddSingleton<IReleaseMetadataService, ReleaseMetadataService>();
        services.AddScoped<IServerUpdateService, ServerUpdateService>();
        services.AddSingleton<ITranscodingSessionManager, TranscodingSessionManager>();
        services.AddSingleton<Lanflix.Application.Common.Interfaces.IImageCacheService, Lanflix.Infrastructure.Services.Image.ImageCacheService>();

        // Background Jobs & Hosted Services
        services.AddHostedService<Lanflix.Infrastructure.Services.BackgroundJobs.LibraryScanJob>();
        services.AddHostedService<Lanflix.Infrastructure.Services.BackgroundJobs.ServerUpdateCheckJob>();
        services.AddHostedService<Lanflix.Infrastructure.Services.BackgroundJobs.SessionCleanupService>();
        services.AddHostedService<Lanflix.Infrastructure.Services.Discovery.MDnsDiscoveryService>();
        services.AddHostedService<Lanflix.Infrastructure.Services.Discovery.DiscoveryPrewarmService>();
        return services;
    }
}
