using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.HasKey(c => c.Id);

        // Indexes for performance
        // UNIQUE constraint on TmdbId + Type combination (like old backend)
        // This allows the same TMDB ID for both movie and series if they exist
        builder.HasIndex(c => new { c.TmdbId, c.Type })
            .IsUnique();

        builder.HasIndex(c => c.TmdbId);

        builder.HasIndex(c => c.Type);

        builder.HasIndex(c => c.AddedAt);

        builder.HasIndex(c => new { c.Type, c.AddedAt });

        builder.HasIndex(c => c.Title);

        builder.HasIndex(c => c.IsDeleted);

        // Required fields
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.OriginalTitle)
            .HasMaxLength(500);

        builder.Property(c => c.Overview)
            .HasMaxLength(2000);

        builder.Property(c => c.PosterPath)
            .HasMaxLength(500);

        builder.Property(c => c.BackdropPath)
            .HasMaxLength(500);

        // JSON column for MediaInfo
        builder.OwnsOne(c => c.MediaInfo, mi =>
        {
            mi.ToJson();
            mi.OwnsOne(m => m.Video);
            mi.OwnsMany(m => m.Audio);
            mi.OwnsMany(m => m.Subtitles);
        });

        // Relationships
        builder.HasMany(c => c.Episodes)
            .WithOne(e => e.Content)
            .HasForeignKey(e => e.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.WatchHistories)
            .WithOne(w => w.Content)
            .HasForeignKey(w => w.ContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
