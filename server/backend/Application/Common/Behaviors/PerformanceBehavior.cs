using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lanflix.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowQueryThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogWarning(
                "Slow query detected: {RequestName} took {ElapsedMilliseconds}ms (threshold: {ThresholdMs}ms)",
                requestName,
                stopwatch.ElapsedMilliseconds,
                SlowQueryThresholdMs);
        }

        return response;
    }
}
