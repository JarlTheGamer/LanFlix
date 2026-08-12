using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Services.AppUpdate;
using Lanflix.Infrastructure.Services.Authentication;
using Lanflix.Infrastructure.Services.Devices;
using Lanflix.Infrastructure.Services.Caching;
using Lanflix.Infrastructure.Services.ExternalApis;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Library;
using Lanflix.Infrastructure.Services.Metadata;
using Lanflix.Infrastructure.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lanflix.Modules.Identity;
using Lanflix.Modules.Metadata;

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lanflix.db";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(30)));
        
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IArtworkPaletteDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddIdentityModule();
        services.AddMetadataModule();

        // Register HttpClient for external API services
        services.AddHttpClient();
        
        // Register named HttpClient for TMDB with base address
        services.AddHttpClient<ITmdbClient, TmdbClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Memory Cache
        services.AddMemoryCache();

        // Register ALL Infrastructure Services
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IMetadataService, MetadataService>();
        
        // Transcoding Settings & Analysis
        services.AddScoped<TranscodingSettingsProvider>();
        services.AddScoped<IMediaAnalyzer, MediaAnalyzer>();
        services.AddScoped<IIntroScanner, AudioFingerprintIntroScanner>();
        
        // Authentication Services
        services.AddScoped<ITokenService, TokenService>();
        
        // External API Services (TmdbClient registered above with HttpClient)
        services.AddScoped<IRadarrClient, RadarrClient>();
        services.AddScoped<ISonarrClient, SonarrClient>();
        services.AddScoped<IProwlarrClient, ProwlarrClient>();
        services.AddScoped<IBazarrClient, BazarrClient>();

        // App Update Services
        services.AddSingleton<IReleaseMetadataService, ReleaseMetadataService>();
        services.AddScoped<IServerUpdateService, ServerUpdateService>();
        services.AddScoped<IAppUpdateService, AppUpdateService>();

        // Device Pairing & Management Services
        services.AddSingleton<IDeviceService, DeviceService>();

        // Discovery Services (mDNS lanflix.local & TMDB Pre-warm)
        services.AddHostedService<Lanflix.Infrastructure.Services.Discovery.MDnsDiscoveryService>();
        services.AddHostedService<Lanflix.Infrastructure.Services.Discovery.DiscoveryPrewarmService>();

        // FFmpeg Services
        services.AddScoped<IHardwareAccelerationDetector, EnhancedHardwareAccelerationDetector>();
        services.AddScoped<IProgressBroadcaster, SimpleProgressBroadcaster>();

        // Audio Services
        services.AddScoped<Lanflix.Infrastructure.Services.Audio.AudioTrackSelector>();

        return services;
    }
}
