using Lanflix.Application.Common.DTOs;

namespace Lanflix.Application.Common.Interfaces;

public class DeviceInfoDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string PairingCode { get; set; } = string.Empty;
    public bool IsPaired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

public interface IDeviceService
{
    Task<DeviceInfoDto> RegisterDeviceAsync(string? deviceId, string clientType, string ipAddress, string userAgent);
    Task<DeviceInfoDto?> GetDeviceStatusAsync(string deviceId);
    Task<List<DeviceInfoDto>> GetAllDevicesAsync();
    Task<bool> PairDeviceByCodeAsync(string pairingCode);
    Task<bool> UnpairDeviceAsync(string deviceId);
    Task UpdateLastSeenAsync(string deviceId, string ipAddress);
    Task<bool> GetRequirePairingAsync();
    Task SetRequirePairingAsync(bool enabled);
}
