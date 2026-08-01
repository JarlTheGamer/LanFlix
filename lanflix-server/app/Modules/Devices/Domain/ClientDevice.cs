using Lanflix.SharedKernel;

namespace Lanflix.Modules.Devices;

public sealed class ClientDevice : Entity<string>
{
    private ClientDevice() { }
    public string Name { get; private set; } = string.Empty;
    public string ClientType { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public string? LastIpAddress { get; private set; }
    public DateTime LastSeenAtUtc { get; private set; }

    public static ClientDevice Create(
        string id, string name, string clientType, Guid accountId, string? ipAddress, DateTime now) => new()
    {
        Id = id,
        Name = name,
        ClientType = clientType,
        AccountId = accountId,
        LastIpAddress = ipAddress,
        LastSeenAtUtc = now,
        CreatedAtUtc = now
    };

    public void Seen(string name, string clientType, Guid accountId, string? ipAddress, DateTime now)
    {
        Name = name;
        ClientType = clientType;
        AccountId = accountId;
        LastIpAddress = ipAddress;
        LastSeenAtUtc = now;
        MarkUpdated();
    }

    public DeviceDto ToDto() => new(Id, Name, ClientType, AccountId, CreatedAtUtc, LastSeenAtUtc);
}
