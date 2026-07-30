using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/system")]
public class SystemTelemetryController : ControllerBase
{
    private readonly ILogger<SystemTelemetryController> _logger;
    private static readonly DateTime ServerStartTime = DateTime.UtcNow;

    public SystemTelemetryController(ILogger<SystemTelemetryController> logger)
    {
        _logger = logger;
    }

    [HttpGet("telemetry")]
    public IActionResult GetTelemetry()
    {
        try
        {
            var networkStats = GetNetworkStats();
            var activeDevices = GetActiveDevices();

            return Ok(new
            {
                status = "Online",
                uptimeSeconds = (long)(DateTime.UtcNow - ServerStartTime).TotalSeconds,
                activeDevicesCount = activeDevices.Count,
                devices = activeDevices,
                network = networkStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system telemetry");
            return StatusCode(500, new { error = "Failed to retrieve telemetry" });
        }
    }

    private object GetNetworkStats()
    {
        long bytesSent = 0;
        long bytesReceived = 0;
        double speedMbps = 0;

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                bytesSent += stats.BytesSent;
                bytesReceived += stats.BytesReceived;
                if (ni.Speed / 1_000_000.0 > speedMbps)
                {
                    speedMbps = ni.Speed / 1_000_000.0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read network interfaces stats");
        }

        return new
        {
            status = "Online",
            downloadSpeedMbps = speedMbps > 0 ? speedMbps : 100.0,
            bytesSent,
            bytesReceived
        };
    }

    private List<object> GetActiveDevices()
    {
        var devices = new List<object>();

        // Current request device info
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers.UserAgent.ToString();
        var deviceName = ParseDeviceName(userAgent);

        devices.Add(new
        {
            id = "current-session",
            name = deviceName,
            ipAddress = clientIp,
            lastActive = "Now",
            isCurrent = true,
            status = "Active"
        });

        return devices;
    }

    private static string ParseDeviceName(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Web Client";
        if (userAgent.Contains("AndroidTV", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("LanflixApp", StringComparison.OrdinalIgnoreCase))
            return "Lanflix Android TV App";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android Device";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iOS Device";
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows Web Browser";
        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
            return "Mac Web Browser";

        return "Web Browser";
    }
}
