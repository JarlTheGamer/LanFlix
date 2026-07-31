using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Register or ping a device on client startup
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers.UserAgent.ToString();

        var device = await _deviceService.RegisterDeviceAsync(request.DeviceId, request.ClientType ?? "web", ip, userAgent);
        return Ok(device);
    }

    /// <summary>
    /// Check if a specific device ID is paired
    /// </summary>
    [HttpGet("status/{deviceId}")]
    public async Task<IActionResult> GetDeviceStatus(string deviceId)
    {
        var device = await _deviceService.GetDeviceStatusAsync(deviceId);
        if (device == null)
        {
            return NotFound(new { message = "Device not found" });
        }

        // Also update last seen timestamp
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _deviceService.UpdateLastSeenAsync(deviceId, ip);

        return Ok(device);
    }

    /// <summary>
    /// Get all registered devices (for Admin / Settings UI)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllDevices()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        return Ok(devices);
    }

    /// <summary>
    /// Pair a device using its 6-character pairing code
    /// </summary>
    [HttpPost("pair")]
    public async Task<IActionResult> PairDevice([FromBody] PairDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Pairing code is required" });
        }

        var success = await _deviceService.PairDeviceByCodeAsync(request.Code);
        if (!success)
        {
            return BadRequest(new { message = "Invalid or expired pairing code" });
        }

        return Ok(new { message = "Device successfully paired!" });
    }

    /// <summary>
    /// Unpair / revoke a device
    /// </summary>
    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> UnpairDevice(string deviceId)
    {
        var success = await _deviceService.UnpairDeviceAsync(deviceId);
        if (!success)
        {
            return NotFound(new { message = "Device not found" });
        }

        return Ok(new { message = "Device unpaired successfully" });
    }

    /// <summary>
    /// Get global Require Device Pairing setting status
    /// </summary>
    [HttpGet("require-pairing")]
    public async Task<IActionResult> GetRequirePairing()
    {
        var enabled = await _deviceService.GetRequirePairingAsync();
        return Ok(new { requirePairing = enabled });
    }

    /// <summary>
    /// Update global Require Device Pairing setting
    /// </summary>
    [HttpPost("require-pairing")]
    public async Task<IActionResult> SetRequirePairing([FromBody] SetRequirePairingRequest request)
    {
        await _deviceService.SetRequirePairingAsync(request.Enabled);
        return Ok(new { message = "Require device pairing setting updated", requirePairing = request.Enabled });
    }
}

public class SetRequirePairingRequest
{
    public bool Enabled { get; set; }
}

public class RegisterDeviceRequest
{
    public string? DeviceId { get; set; }
    public string? ClientType { get; set; }
}

public class PairDeviceRequest
{
    public string Code { get; set; } = string.Empty;
}
