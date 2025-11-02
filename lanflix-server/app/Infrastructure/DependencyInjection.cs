using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Services.FFmpeg;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register FFmpeg services
        services.AddScoped<IMediaAnalyzer, MediaAnalyzer>();
        services.AddScoped<IHardwareAccelerationDetector, EnhancedHardwareAccelerationDetector>();
        services.AddScoped<ITranscodingPipeline, EnhancedTranscodingPipeline>();
        services.AddScoped<IProgressBroadcaster, SimpleProgressBroadcaster>();

        return services;
    }
}