using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Identity;

public interface IIdentityDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<RefreshSession> RefreshSessions { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<AuditRecord> AuditRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.NormalizedUsername).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(96).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
    }
}

public sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable("RefreshSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.AccountId, x.RevokedAtUtc });
        builder.Property(x => x.DeviceName).HasMaxLength(160);
        builder.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CodeHash).IsUnique();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
    }
}

public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("AuditRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(256);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.AccountId);
    }
}
