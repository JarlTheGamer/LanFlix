using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.Id);

        // Indexes for performance
        builder.HasIndex(p => p.Name);

        builder.HasIndex(p => p.IsDefault);

        // Required fields
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.AvatarPath)
            .HasMaxLength(500);

        builder.Property(p => p.PinCode)
            .HasMaxLength(10);

        // JSON column for UserPreferences
        builder.OwnsOne(p => p.Preferences, prefs =>
        {
            prefs.ToJson();
        });

        // Relationships
        builder.HasMany(p => p.WatchHistories)
            .WithOne(w => w.Profile)
            .HasForeignKey(w => w.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Watchlists)
            .WithOne(w => w.Profile)
            .HasForeignKey(w => w.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.StreamSessions)
            .WithOne(s => s.Profile)
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
