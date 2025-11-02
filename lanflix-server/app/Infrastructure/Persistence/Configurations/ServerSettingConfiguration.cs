using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Infrastructure.Persistence.Configurations;

public class ServerSettingConfiguration : IEntityTypeConfiguration<ServerSetting>
{
    public void Configure(EntityTypeBuilder<ServerSetting> builder)
    {
        builder.ToTable("ServerSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Create unique index on Key
        builder.HasIndex(s => s.Key)
            .IsUnique();
    }
}
