namespace Lanflix.Modules.Devices;

public sealed record RegisterDeviceRequest(string? DeviceId, string ClientType, string? DeviceName);
public sealed record DeviceDto(
    string Id, string Name, string ClientType, Guid AccountId,
    DateTime CreatedAtUtc, DateTime LastSeenAtUtc);
