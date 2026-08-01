using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Devices;

public sealed class DeviceService(IDevicesDbContext db)
{
    public async Task<DeviceDto> RegisterAsync(
        RegisterDeviceRequest request, ClaimsPrincipal user, string? ipAddress, DateTime now, CancellationToken ct)
    {
        var id = NormalizeId(request.DeviceId) ?? Guid.NewGuid().ToString("N");
        var name = Clean(request.DeviceName, "Lanflix device", 120);
        var clientType = Clean(request.ClientType, "unknown", 32);
        var rawAccountId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(rawAccountId, out var accountId)) throw new UnauthorizedAccessException();
        var device = await db.ClientDevices.SingleOrDefaultAsync(item => item.Id == id, ct);
        if (device is not null && device.AccountId != accountId)
        {
            id = Guid.NewGuid().ToString("N");
            device = null;
        }
        if (device is not null)
        {
            device.Seen(name, clientType, accountId, ipAddress, now);
            await db.SaveChangesAsync(ct);
            return device.ToDto();
        }

        device = ClientDevice.Create(id, name, clientType, accountId, ipAddress, now);
        db.ClientDevices.Add(device);
        await db.SaveChangesAsync(ct);
        return device.ToDto();
    }

    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct) =>
        (await db.ClientDevices.AsNoTracking().OrderByDescending(item => item.LastSeenAtUtc).ToListAsync(ct))
        .Select(item => item.ToDto()).ToArray();

    public async Task<DeviceDto?> GetAsync(string id, CancellationToken ct) =>
        (await db.ClientDevices.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct))?.ToDto();

    public async Task<bool> RemoveAsync(string id, CancellationToken ct)
    {
        var device = await db.ClientDevices.SingleOrDefaultAsync(item => item.Id == id, ct);
        if (device is null) return false;
        db.ClientDevices.Remove(device);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string? NormalizeId(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 64 ? null : value;
    }
    private static string Clean(string? value, string fallback, int max) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
