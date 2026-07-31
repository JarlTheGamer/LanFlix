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

    [HttpGet("server-info")]
    public IActionResult GetServerInfo()
    {
        var localIp = GetPrimaryLanIPv4Address() ?? "127.0.0.1";
        var port = HttpContext.Connection.LocalPort > 0 ? HttpContext.Connection.LocalPort : 5037;
        var scheme = HttpContext.Request.Scheme ?? "http";

        return Ok(new
        {
            lanIp = localIp,
            port = port,
            baseUrl = $"{scheme}://{localIp}:{port}",
            serverName = Environment.MachineName
        });
    }

    private static string? GetPrimaryLanIPv4Address()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var name = ni.Name.ToLowerInvariant();
                var desc = ni.Description.ToLowerInvariant();

                if (name.Contains("vethernet") || name.Contains("wsl") || name.Contains("virtual") ||
                    name.Contains("hyper-v") || name.Contains("vmnet") || name.Contains("docker") || name.Contains("zerotier") ||
                    desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("wsl"))
                {
                    continue;
                }

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ua.Address))
                    {
                        return ua.Address.ToString();
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private object GetNetworkStats()
    {
        long bytesSent = 0;
        long bytesReceived = 0;
        double maxSpeedMbps = 0;

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            foreach (var ni in interfaces)
            {
                try
                {
                    var stats = ni.GetIPStatistics();
                    bytesSent += stats.BytesSent;
                    bytesReceived += stats.BytesReceived;

                    double mbps = ni.Speed / 1_000_000.0;
                    if (mbps > maxSpeedMbps && mbps < 100_000) // Ignore invalid/unrealistic values
                    {
                        maxSpeedMbps = mbps;
                    }
                }
                catch {}
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read network interfaces stats");
        }

        return new
        {
            status = "Online",
            downloadSpeedMbps = maxSpeedMbps > 0 ? maxSpeedMbps : 1000.0,
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
