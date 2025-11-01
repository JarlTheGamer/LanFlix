using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class WatchHistoryConfiguration : IEntityTypeConfiguration<WatchHistory>
{
    public void Configure(EntityTypeBuilder<WatchHistory> builder)
    {
        builder.HasKey(w => w.Id);

        // Indexes for performance
        builder.HasIndex(w => w.ProfileId);

        builder.HasIndex(w => w.ContentId);

        builder.HasIndex(w => w.EpisodeId);

        builder.HasIndex(w => new { w.ProfileId, w.ContentId, w.EpisodeId })
            .IsUnique();

        builder.HasIndex(w => w.LastWatchedAt);

        builder.HasIndex(w => new { w.ProfileId, w.LastWatchedAt });

        // Required fields
        builder.Property(w => w.PositionTicks)
            .IsRequired();

        builder.Property(w => w.LastWatchedAt)
            .IsRequired();

        // Relationships are already defined in Content, Episode, and Profile configurations
    }
}
