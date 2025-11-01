using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories and services
        // services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

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

        // Background Jobs
        // services.AddHangfire(config => config.UseMemoryStorage());
        // services.AddHangfireServer();

        return services;
    }
}
