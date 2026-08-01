using System.Diagnostics;

namespace Lanflix.Infrastructure.Services.ExternalApis;

/// <summary>Serializes TMDb calls and keeps the process comfortably below burst limits.</summary>
public sealed class TmdbRateLimitHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static long _lastRequestTimestamp;
    private static readonly long MinimumInterval = (long)(Stopwatch.Frequency * 0.26);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var elapsed = Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastRequestTimestamp);
            if (elapsed < MinimumInterval)
            {
                var delay = TimeSpan.FromSeconds((MinimumInterval - elapsed) / (double)Stopwatch.Frequency);
                await Task.Delay(delay, cancellationToken);
            }
            Interlocked.Exchange(ref _lastRequestTimestamp, Stopwatch.GetTimestamp());
        }
        finally
        {
            Gate.Release();
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
