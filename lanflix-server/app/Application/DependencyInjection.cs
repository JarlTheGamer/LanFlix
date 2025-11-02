using System.Reflection;
using FluentValidation;
using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Application.Features.Streaming.Strategies;
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

        // Register Streaming Strategies (order by priority)
        services.AddScoped<IStreamingStrategy, DirectPlayStrategy>();
        services.AddScoped<IStreamingStrategy, DirectStreamStrategy>();
        services.AddScoped<IStreamingStrategy, TranscodeVideoStrategy>();
        services.AddScoped<IStreamingStrategy, FullTranscodeStrategy>();
        
        // Register Streaming Strategy Selector
        services.AddScoped<StreamingStrategySelector>();

        return services;
    }
}
