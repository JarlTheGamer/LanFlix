using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class StreamSessionConfiguration : IEntityTypeConfiguration<StreamSession>
{
    public void Configure(EntityTypeBuilder<StreamSession> builder)
    {
        builder.HasKey(s => s.Id);

        // Indexes for performance
        builder.HasIndex(s => s.SessionId)
            .IsUnique();

        builder.HasIndex(s => s.ProfileId);

        builder.HasIndex(s => s.ContentId);

        builder.HasIndex(s => s.EpisodeId);

        builder.HasIndex(s => s.IsActive);

        builder.HasIndex(s => new { s.ProfileId, s.IsActive });

        builder.HasIndex(s => s.StartedAt);

        builder.HasIndex(s => s.LastActivityAt);

        // Required fields
        builder.Property(s => s.SessionId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.StartedAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.ClientIpAddress)
            .HasMaxLength(50);

        builder.Property(s => s.ClientUserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.TranscodingProcessId)
            .HasMaxLength(50);

        builder.Property(s => s.TargetVideoCodec)
            .HasMaxLength(50);

        builder.Property(s => s.TargetAudioCodec)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(s => s.Content)
            .WithMany()
            .HasForeignKey(s => s.ContentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Episode)
            .WithMany()
            .HasForeignKey(s => s.EpisodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
