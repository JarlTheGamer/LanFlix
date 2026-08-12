using System.Reflection;
using FluentValidation;
using Lanflix.Application.Common.Behaviors;
using Lanflix.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register MediatR pipeline behaviors
        // Order matters: Logging -> Performance -> Caching -> Validation -> Handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register default transcoding settings (can be overridden in Infrastructure)
        services.AddSingleton(new TranscodingSettings
        {
            EnableHardwareAcceleration = true,
            ThreadCount = 0, // Auto-detect
            EnableToneMapping = true,
            ToneMappingAlgorithm = ToneMappingAlgorithm.Hable,
            AllowSoftwareFallback = true,
            MaxConcurrentTranscodes = 2,
            EnableLowPowerEncoding = false,
            EncodingPreset = EncodingPreset.Medium,
            EnableBFrames = true,
            EnableAdaptiveBitrate = true,
            SegmentDuration = 6,
            PlaylistLength = 6,
            DeleteSegmentsAfterStreaming = true
        });

        return services;
    }
}
