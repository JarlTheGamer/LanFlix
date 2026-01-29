using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Services.AppUpdate;
using Lanflix.Infrastructure.Services.Authentication;
using Lanflix.Infrastructure.Services.Caching;
using Lanflix.Infrastructure.Services.ExternalApis;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Library;
using Lanflix.Infrastructure.Services.Metadata;
using Lanflix.Infrastructure.Services.Settings;
using Lanflix.Infrastructure.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Database Context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lanflix.db"));
        
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

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
        services.AddSingleton<ITranscodingSessionManager, TranscodingSessionManager>();
        
        // Transcoding Settings
        services.AddScoped<TranscodingSettingsProvider>();
        
        // Authentication Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ILegacyTokenService, LegacyTokenService>();
        
        // External API Services (TmdbClient registered above with HttpClient)
        services.AddScoped<IRadarrClient, RadarrClient>();
        services.AddScoped<ISonarrClient, SonarrClient>();
        services.AddScoped<IProwlarrClient, ProwlarrClient>();
        services.AddScoped<IBazarrClient, BazarrClient>();

        // App Update Services
        services.AddScoped<IServerUpdateService, ServerUpdateService>();
        services.AddScoped<IAppUpdateService, AppUpdateService>();

        // FFmpeg Services
        services.AddScoped<IMediaAnalyzer, MediaAnalyzer>();
        services.AddScoped<IHardwareAccelerationDetector, EnhancedHardwareAccelerationDetector>();
        services.AddScoped<ITranscodingPipeline, EnhancedTranscodingPipeline>();
        services.AddScoped<IProgressBroadcaster, SimpleProgressBroadcaster>();
        
        // Audio Services
        services.AddScoped<Lanflix.Infrastructure.Services.Audio.AudioTrackSelector>();

        return services;
    }
}