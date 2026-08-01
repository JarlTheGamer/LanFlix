using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Devices;

public interface IDevicesDbContext
{
    DbSet<ClientDevice> ClientDevices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class ClientDeviceConfiguration : IEntityTypeConfiguration<ClientDevice>
{
    public void Configure(EntityTypeBuilder<ClientDevice> builder)
    {
        builder.ToTable("ClientDevices");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasMaxLength(64);
        builder.Property(item => item.Name).HasMaxLength(120).IsRequired();
        builder.Property(item => item.ClientType).HasMaxLength(32).IsRequired();
        builder.Property(item => item.LastIpAddress).HasMaxLength(64);
        builder.HasIndex(item => new { item.AccountId, item.LastSeenAtUtc });
        builder.HasIndex(item => item.LastSeenAtUtc);
    }
}
