using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.HasKey(e => e.Id);

        // Indexes for performance
        builder.HasIndex(e => e.ContentId);

        builder.HasIndex(e => e.TmdbId);

        builder.HasIndex(e => new { e.ContentId, e.SeasonNumber, e.EpisodeNumber })
            .IsUnique();

        builder.HasIndex(e => e.IsDeleted);

        // Required fields
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Overview)
            .HasMaxLength(2000);

        builder.Property(e => e.StillPath)
            .HasMaxLength(500);

        // JSON column for MediaInfo
        builder.OwnsOne(e => e.MediaInfo, mi =>
        {
            mi.ToJson();
            mi.OwnsOne(m => m.Video);
            mi.OwnsMany(m => m.Audio);
            mi.OwnsMany(m => m.Subtitles);
        });

        // Relationships
    }
}
