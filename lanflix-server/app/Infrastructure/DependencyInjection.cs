using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Settings;
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

        // Register Settings Service
        services.AddScoped<ISettingsService, SettingsService>();

        // Register FFmpeg services
        services.AddScoped<IMediaAnalyzer, MediaAnalyzer>();
        services.AddScoped<IHardwareAccelerationDetector, EnhancedHardwareAccelerationDetector>();
        services.AddScoped<ITranscodingPipeline, EnhancedTranscodingPipeline>();
        services.AddScoped<IProgressBroadcaster, SimpleProgressBroadcaster>();

        return services;
    }
}