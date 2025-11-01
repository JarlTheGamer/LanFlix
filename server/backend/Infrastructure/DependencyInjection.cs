using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Persistence.Repositories;
using Lanflix.Infrastructure.Services.FFmpeg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = configuration["Lanflix:Cache:Redis:InstanceName"] ?? "lanflix:";
            });
        }

        // HTTP Clients
        // services.AddHttpClient<ITmdbClient, TmdbClient>();

        // FFmpeg Services
        services.AddSingleton<IMediaAnalyzer, MediaAnalyzer>();
        services.AddSingleton<IHardwareAccelerationDetector, HardwareAccelerationDetector>();
        services.AddSingleton<ITranscodingPipeline, TranscodingPipeline>();
        
        // FFmpeg Process Pool
        var maxConcurrentTranscodes = configuration.GetValue<int>("Lanflix:Transcoding:MaxConcurrentTranscodes", 5);
        services.AddSingleton(sp => 
            new FFmpegProcessPool(
                sp.GetRequiredService<ILogger<FFmpegProcessPool>>(),
                maxConcurrentTranscodes));
        
        // FFmpeg Process Monitor (background service)
        services.AddHostedService<FFmpegProcessMonitor>();

        // Background Jobs
        // services.AddHangfire(config => config.UseMemoryStorage());
        // services.AddHangfireServer();

        return services;
    }
}
