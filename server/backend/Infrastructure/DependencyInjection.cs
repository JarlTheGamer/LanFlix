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

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var postgresConnection = configuration.GetConnectionString("PostgresConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(postgresConnection))
            {
                options.UseNpgsql(postgresConnection);
            }
            else
            {
                options.UseSqlite(connectionString ?? "Data Source=lanflix.db");
            }
        });

        // Register repositories and services
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
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
        
        if (redisEnabled && !string.IsNullOrEmpty(redisConnection))
        {
            // Register Redis connection multiplexer
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnection));
            
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
        else
        {
            // Use memory cache only if Redis is not configured
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // HTTP Clients
        // services.AddHttpClient<ITmdbClient, TmdbClient>();

        // Authentication Services
        services.AddScoped<ITokenService, TokenService>();

        // App Update Service
        services.AddSingleton<IAppUpdateService, Infrastructure.Services.AppUpdate.AppUpdateService>();

        // Settings Service
        services.AddSingleton<ISettingsService, Infrastructure.Services.Settings.SettingsService>();

        // FFmpeg Services
        services.AddSingleton<IMediaAnalyzer, MediaAnalyzer>();
        services.AddSingleton<IHardwareAccelerationDetector, HardwareAccelerationDetector>();
        services.AddSingleton<FFmpegProgressParser>();
        services.AddSingleton<ITranscodingPipeline, TranscodingPipelineWithProgress>();
        
        // FFmpeg Process Pool
        var maxConcurrentTranscodes = configuration.GetValue<int>("Lanflix:Transcoding:MaxConcurrentTranscodes", 5);
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

        // Background Jobs
        // services.AddHangfire(config => config.UseMemoryStorage());
        // services.AddHangfireServer();

        return services;
    }
}
