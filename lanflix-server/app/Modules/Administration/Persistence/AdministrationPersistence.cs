using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Administration;

public interface IAdministrationDbContext
{
    DbSet<BackgroundJobRun> BackgroundJobRuns { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class BackgroundJobRunConfiguration : IEntityTypeConfiguration<BackgroundJobRun>
{
    public void Configure(EntityTypeBuilder<BackgroundJobRun> builder)
    {
        builder.ToTable("BackgroundJobRuns");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(16).IsRequired();
        builder.Property(item => item.Result).HasMaxLength(4000);
        builder.Property(item => item.Error).HasMaxLength(2000);
        builder.HasIndex(item => new { item.Name, item.CreatedAtUtc });
        builder.HasIndex(item => item.Status);
    }
}
