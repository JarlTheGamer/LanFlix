using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class WatchlistConfiguration : IEntityTypeConfiguration<Watchlist>
{
    public void Configure(EntityTypeBuilder<Watchlist> builder)
    {
        builder.HasKey(w => w.Id);

        // Indexes for performance
        builder.HasIndex(w => w.ProfileId);

        builder.HasIndex(w => w.ContentId);

        builder.HasIndex(w => new { w.ProfileId, w.ContentId })
            .IsUnique();

        builder.HasIndex(w => w.AddedAt);

        builder.HasIndex(w => new { w.ProfileId, w.AddedAt });

        // Required fields
        builder.Property(w => w.AddedAt)
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasMaxLength(1000);

        // Relationships are already defined in Profile and Content configurations
    }
}
