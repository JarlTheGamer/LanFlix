using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Playback;

public interface IPlaybackDbContext
{
    DbSet<PlaybackProgress> PlaybackProgress { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class PlaybackProgressConfiguration : IEntityTypeConfiguration<PlaybackProgress>
{
    public void Configure(EntityTypeBuilder<PlaybackProgress> builder)
    {
        builder.ToTable("PlaybackProgress");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.MediaKind).HasMaxLength(16).IsRequired();
        builder.HasIndex(item => new { item.AccountId, item.MediaKind, item.MediaId }).IsUnique();
        builder.HasIndex(item => new { item.AccountId, item.UpdatedAtUtc });
    }
}
