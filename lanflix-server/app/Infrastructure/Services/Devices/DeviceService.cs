using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Devices;

public class DeviceService : IDeviceService
{
    private readonly ILogger<DeviceService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly ConcurrentDictionary<string, DeviceInfoDto> _devices = new();
    private static bool _isLoaded = false;
    private static readonly object _initLock = new();

    public DeviceService(ILogger<DeviceService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;
        lock (_initLock)
        {
            if (_isLoaded) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var setting = context.ServerSettings.FirstOrDefault(s => s.Key == "Lanflix:PairedDevices");
                if (setting != null && !string.IsNullOrEmpty(setting.Value))
                {
                    var loaded = JsonSerializer.Deserialize<List<DeviceInfoDto>>(setting.Value);
                    if (loaded != null)
                    {
                        foreach (var dev in loaded)
                        {
                            _devices[dev.DeviceId] = dev;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load paired devices from database");
            }
            _isLoaded = true;
        }
    }

    private async Task PersistAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var json = JsonSerializer.Serialize(_devices.Values.ToList());
            
            var setting = await context.ServerSettings.FirstOrDefaultAsync(s => s.Key == "Lanflix:PairedDevices");
            if (setting == null)
            {
                setting = new Domain.Entities.ServerSetting
                {
                    Key = "Lanflix:PairedDevices",
                    Value = json,
                    UpdatedAt = DateTime.UtcNow
                };
                context.ServerSettings.Add(setting);
            }
            else
            {
                setting.Value = json;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist devices to database");
        }
    }

    public async Task<DeviceInfoDto> RegisterDeviceAsync(string? deviceId, string clientType, string ipAddress, string userAgent)
    {
        EnsureLoaded();

        if (!string.IsNullOrEmpty(deviceId) && _devices.TryGetValue(deviceId, out var existing))
        {
            existing.LastSeen = DateTime.UtcNow;
            existing.IPAddress = ipAddress;
            return existing;
        }

        var newDeviceId = string.IsNullOrEmpty(deviceId) ? Guid.NewGuid().ToString("N") : deviceId;
        var pairingCode = GeneratePairingCode();
        var deviceName = ParseDeviceName(userAgent, clientType);

        bool requirePairing = await GetRequirePairingAsync();
        bool hasPairedDevices = _devices.Values.Any(d => d.IsPaired);
        // Auto-pair if pairing is disabled globally OR if no paired device exists yet
        bool autoPair = !requirePairing || !hasPairedDevices;

        var device = new DeviceInfoDto
        {
            DeviceId = newDeviceId,
            DeviceName = deviceName,
            ClientType = string.IsNullOrEmpty(clientType) ? "web" : clientType,
            IPAddress = ipAddress,
            PairingCode = pairingCode,
            IsPaired = autoPair,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _devices[newDeviceId] = device;
        await PersistAsync();

        _logger.LogInformation("Registered new device: {DeviceName} ({DeviceId}) with pairing code {Code}, Paired: {AutoPair}",
            deviceName, newDeviceId, pairingCode, autoPair);

        return device;
    }

    public Task<DeviceInfoDto?> GetDeviceStatusAsync(string deviceId)
    {
        EnsureLoaded();
        if (_devices.TryGetValue(deviceId, out var device))
        {
            return Task.FromResult<DeviceInfoDto?>(device);
        }
        return Task.FromResult<DeviceInfoDto?>(null);
    }

    public Task<List<DeviceInfoDto>> GetAllDevicesAsync()
    {
        EnsureLoaded();
        return Task.FromResult(_devices.Values.OrderByDescending(d => d.LastSeen).ToList());
    }

    public async Task<bool> PairDeviceByCodeAsync(string pairingCode)
    {
        EnsureLoaded();
        var cleanCode = pairingCode.Trim().ToUpperInvariant();
        var target = _devices.Values.FirstOrDefault(d => 
            string.Equals(d.PairingCode, cleanCode, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(d.DeviceId, cleanCode, StringComparison.OrdinalIgnoreCase));

        if (target == null) return false;

        target.IsPaired = true;
        target.LastSeen = DateTime.UtcNow;
        _devices[target.DeviceId] = target;
        await PersistAsync();

        _logger.LogInformation("Successfully paired device {DeviceName} ({DeviceId}) via code {Code}",
            target.DeviceName, target.DeviceId, cleanCode);

        return true;
    }

    public async Task<bool> GetRequirePairingAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var setting = await context.ServerSettings.FirstOrDefaultAsync(s => s.Key == "Lanflix:RequireDevicePairing");
            if (setting != null && bool.TryParse(setting.Value, out var val))
            {
                return val;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read RequireDevicePairing setting");
        }
        return true; // Default to true for security
    }

    public async Task SetRequirePairingAsync(bool enabled)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var setting = await context.ServerSettings.FirstOrDefaultAsync(s => s.Key == "Lanflix:RequireDevicePairing");
            if (setting == null)
            {
                setting = new Domain.Entities.ServerSetting
                {
                    Key = "Lanflix:RequireDevicePairing",
                    Value = enabled.ToString(),
                    UpdatedAt = DateTime.UtcNow
                };
                context.ServerSettings.Add(setting);
            }
            else
            {
                setting.Value = enabled.ToString();
                setting.UpdatedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
            _logger.LogInformation("Updated RequireDevicePairing setting to {Enabled}", enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update RequireDevicePairing setting");
        }
    }

    public async Task<bool> UnpairDeviceAsync(string deviceId)
    {
        EnsureLoaded();
        if (_devices.TryRemove(deviceId, out var removed))
        {
            await PersistAsync();
            _logger.LogInformation("Unpaired device {DeviceName} ({DeviceId})", removed.DeviceName, deviceId);
            return true;
        }
        return false;
    }

    public async Task UpdateLastSeenAsync(string deviceId, string ipAddress)
    {
        EnsureLoaded();
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.LastSeen = DateTime.UtcNow;
            device.IPAddress = ipAddress;
            await PersistAsync();
        }
    }

    private static string GeneratePairingCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Omit ambiguous characters like O, 0, 1, I
        var bytes = new byte[6];
        RandomNumberGenerator.Fill(bytes);
        var result = new char[6];
        for (int i = 0; i < 6; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    private static string ParseDeviceName(string userAgent, string clientType)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Lanflix Client";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) && userAgent.Contains("AFT", StringComparison.OrdinalIgnoreCase))
            return "Fire TV";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android Device";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "Apple iOS Device";
        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            return "Chrome Browser";
        if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            return "Firefox Browser";
        if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
            return "Safari Browser";
        if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            return "Edge Browser";

        return !string.IsNullOrEmpty(clientType) ? clientType : "Web Client";
    }
}
