using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Persistence.Repositories;
using Lanflix.Infrastructure.Services.Authentication;
using Lanflix.Infrastructure.Services.Caching;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Configuration
        // Only register DbContext if it hasn't been registered already (e.g., in tests)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var postgresConnection = configuration.GetConnectionString("PostgresConnection");
        
        // Skip database registration if both connection strings are null/empty (test scenario)
        // OR if DbContext has already been registered (test scenario)
        var shouldRegisterDb = (!string.IsNullOrEmpty(connectionString) || !string.IsNullOrEmpty(postgresConnection))
                            && !services.Any(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        
        if (shouldRegisterDb)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(postgresConnection))
                {
                    options.UseNpgsql(postgresConnection);
                }
                else
                {
                    options.UseSqlite(connectionString);
                }
            });
        }

        // Register repositories and services
        // Only register IApplicationDbContext if it hasn't been registered already (e.g., in tests)
        if (!services.Any(d => d.ServiceType == typeof(IApplicationDbContext)))
        {
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        }
        
        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IContentRepository, ContentRepository>();
        
        // Register query result cache
        services.AddSingleton<QueryResultCache>();

        // Caching
        services.AddMemoryCache();
        
        // Redis (if configured)
        var redisConnection = configuration["Lanflix:Cache:Redis:ConnectionString"];
        var redisEnabled = configuration.GetValue<bool>("Lanflix:Cache:Redis:Enabled", false);
        
        if (redisEnabled && !string.IsNullOrWhiteSpace(redisConnection))
        {
            try
            {
                // Register Redis connection multiplexer
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var options = ConfigurationOptions.Parse(redisConnection);
                    options.AbortOnConnectFail = false; // Don't throw on connection failure
                    options.ConnectTimeout = 5000;
                    options.SyncTimeout = 5000;
                    return ConnectionMultiplexer.Connect(options);
                });
                
                // Register distributed cache
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = configuration["Lanflix:Cache:Redis:InstanceName"] ?? "lanflix:";
                });
                
                // Register Redis cache service
                services.AddSingleton<RedisCacheService>();
                
                // Register Hybrid cache as the primary cache service
                services.AddSingleton<ICacheService, HybridCacheService>();
            }
            catch (Exception)
            {
                // Fall back to memory cache if Redis connection fails
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }
        else
        {
            // Use memory cache only if Redis is not configured
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // HTTP Clients with connection pooling
        var tmdbBaseUrl = configuration["Lanflix:ExternalApis:Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3/";
        services.AddHttpClient<ITmdbClient, Infrastructure.Services.ExternalApis.TmdbClient>(client =>
        {
            client.BaseAddress = new Uri(tmdbBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "Lanflix/2.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // Connection pooling configuration for optimal performance
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            
            // Enable automatic decompression
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            
            // Connection settings
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ResponseDrainTimeout = TimeSpan.FromSeconds(5),
            
            // Enable HTTP/2
            EnableMultipleHttp2Connections = true
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(30)); // Handler lifetime for connection pool rotation

        // Radarr HTTP Client - Always register, URL will be fetched from database at runtime
        services.AddHttpClient<IRadarrClient, Infrastructure.Services.ExternalApis.RadarrClient>((sp, client) =>
        {
            // BaseAddress will be set dynamically in the client based on database settings
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        // Sonarr HTTP Client - Always register, URL will be fetched from database at runtime
        services.AddHttpClient<ISonarrClient, Infrastructure.Services.ExternalApis.SonarrClient>((sp, client) =>
        {
            // BaseAddress will be set dynamically in the client based on database settings
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        // Prowlarr HTTP Client - Always register, URL will be fetched from database at runtime
        services.AddHttpClient<IProwlarrClient, Infrastructure.Services.ExternalApis.ProwlarrClient>((sp, client) =>
        {
            // BaseAddress will be set dynamically in the client based on database settings
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        // Authentication Services
        services.AddScoped<ITokenService, TokenService>();

        // App Update Service
        services.AddSingleton<IAppUpdateService, Infrastructure.Services.AppUpdate.AppUpdateService>();
        
        // Server Update Service
        services.AddSingleton<IServerUpdateService, Infrastructure.Services.AppUpdate.ServerUpdateService>();

        // Settings Service (Scoped because it uses IApplicationDbContext)
        services.AddScoped<ISettingsService, Infrastructure.Services.Settings.SettingsService>();

        // Metadata Service (Scoped for downloading and saving metadata to media folders)
        services.AddScoped<IMetadataService, Infrastructure.Services.Metadata.MetadataService>();

        // Library Service (Scoped for scanning media folders)
        services.AddScoped<ILibraryService, Infrastructure.Services.Library.LibraryService>();

        // Background Jobs
        services.AddHostedService<Infrastructure.Services.BackgroundJobs.LibraryScanJob>();

        // FFmpeg Services
        services.AddSingleton<IMediaAnalyzer, MediaAnalyzer>();
        services.AddSingleton<IHardwareAccelerationDetector, HardwareAccelerationDetector>();
        services.AddSingleton<FFmpegProgressParser>();
        services.AddSingleton<ITranscodingPipeline, TranscodingPipelineWithProgress>();
        
        // FFmpeg Process Pool
        var maxConcurrentTranscodes = configuration.GetValue<int>("Lanflix:Transcoding:MaxConcurrentTranscodes", 5);
        // Ensure at least 1 concurrent transcode
        if (maxConcurrentTranscodes < 1) maxConcurrentTranscodes = 2;
        
        services.AddSingleton(sp => 
            new FFmpegProcessPool(
                sp.GetRequiredService<ILogger<FFmpegProcessPool>>(),
                maxConcurrentTranscodes));
        
        // FFmpeg Process Monitor (background service)
        services.AddHostedService<FFmpegProcessMonitor>();

        // Streaming Services
        services.AddSingleton<TranscodingFileCleanupService>();
        services.AddScoped<ITranscodingSessionManager, TranscodingSessionManager>();
        
        // Session cleanup background service
        services.AddHostedService<Infrastructure.Services.BackgroundJobs.SessionCleanupService>();
        
        // Server update check background service
        services.AddHostedService<Infrastructure.Services.BackgroundJobs.ServerUpdateCheckJob>();

        // Background Jobs
        // services.AddHangfire(config => config.UseMemoryStorage());
        // services.AddHangfireServer();

        return services;
    }
}
