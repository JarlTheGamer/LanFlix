using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.HealthChecks;

/// <summary>
/// Health check to verify FFmpeg is installed and accessible
/// </summary>
public class FFmpegHealthCheck : IHealthCheck
{
    private readonly ILogger<FFmpegHealthCheck> _logger;

    public FFmpegHealthCheck(ILogger<FFmpegHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to execute ffmpeg -version
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && output.Contains("ffmpeg version"))
            {
                // Extract version information
                var versionLine = output.Split('\n')[0];
                
                return HealthCheckResult.Healthy(
                    $"FFmpeg is available: {versionLine}",
                    new Dictionary<string, object>
                    {
                        ["version"] = versionLine,
                        ["exitCode"] = process.ExitCode
                    });
            }

            return HealthCheckResult.Degraded(
                "FFmpeg executed but returned unexpected output",
                data: new Dictionary<string, object>
                {
                    ["exitCode"] = process.ExitCode,
                    ["output"] = output
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg health check failed");
            
            return HealthCheckResult.Unhealthy(
                "FFmpeg is not available or not in PATH",
                ex,
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}
