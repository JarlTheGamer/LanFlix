using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Realtime;

public interface IRealtimeDbContext
{
    DbSet<SyncPlayRoom> SyncPlayRooms { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class SyncPlayRoomConfiguration : IEntityTypeConfiguration<SyncPlayRoom>
{
    public void Configure(EntityTypeBuilder<SyncPlayRoom> builder)
    {
        builder.ToTable("SyncPlayRooms");
        builder.HasKey(room => room.Id);
        builder.Property(room => room.Code).HasMaxLength(16).IsRequired();
        builder.Property(room => room.ContentType).HasMaxLength(16).IsRequired();
        builder.HasIndex(room => room.Code).IsUnique();
        builder.HasIndex(room => room.ExpiresAtUtc);
        builder.HasIndex(room => room.HostAccountId);
    }
}
