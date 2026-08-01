using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Social;

public interface ISocialDbContext
{
    DbSet<SocialRelationship> SocialRelationships { get; }
    DbSet<SocialReview> SocialReviews { get; }
    DbSet<SocialActivity> SocialActivities { get; }
    DbSet<SocialComment> SocialComments { get; }
    DbSet<SocialReaction> SocialReactions { get; }
    DbSet<SocialBlock> SocialBlocks { get; }
    DbSet<SocialMute> SocialMutes { get; }
    DbSet<SocialPrivacy> SocialPrivacy { get; }
    DbSet<SocialNotification> SocialNotifications { get; }
    DbSet<SocialReport> SocialReports { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class SocialRelationshipConfiguration : IEntityTypeConfiguration<SocialRelationship>
{
    public void Configure(EntityTypeBuilder<SocialRelationship> b)
    {
        b.ToTable("SocialRelationships"); b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(x => new { x.SourceAccountId, x.TargetAccountId, x.Kind }).IsUnique();
        b.HasIndex(x => new { x.TargetAccountId, x.Kind, x.Status });
    }
}

public sealed class SocialReviewConfiguration : IEntityTypeConfiguration<SocialReview>
{
    public void Configure(EntityTypeBuilder<SocialReview> b)
    {
        b.ToTable("SocialReviews"); b.HasKey(x => x.Id); b.Property(x => x.Body).HasMaxLength(4000);
        b.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(x => new { x.AccountId, x.ContentId }).IsUnique(); b.HasIndex(x => x.ContentId);
    }
}

public sealed class SocialActivityConfiguration : IEntityTypeConfiguration<SocialActivity>
{
    public void Configure(EntityTypeBuilder<SocialActivity> b)
    {
        b.ToTable("SocialActivities"); b.HasKey(x => x.Id); b.Property(x => x.Kind).HasMaxLength(32); b.Property(x => x.Body).HasMaxLength(2000);
        b.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16); b.HasIndex(x => x.CreatedAtUtc); b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.ReviewId).IsUnique();
    }
}

public sealed class SocialCommentConfiguration : IEntityTypeConfiguration<SocialComment>
{
    public void Configure(EntityTypeBuilder<SocialComment> b)
    {
        b.ToTable("SocialComments"); b.HasKey(x => x.Id); b.Property(x => x.Body).HasMaxLength(1000);
        b.HasIndex(x => new { x.ActivityId, x.CreatedAtUtc }); b.HasIndex(x => x.AccountId);
        b.HasOne<SocialActivity>().WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SocialReactionConfiguration : IEntityTypeConfiguration<SocialReaction>
{
    public void Configure(EntityTypeBuilder<SocialReaction> b)
    {
        b.ToTable("SocialReactions"); b.HasKey(x => x.Id); b.Property(x => x.Kind).HasMaxLength(16);
        b.HasIndex(x => new { x.ActivityId, x.AccountId }).IsUnique();
        b.HasOne<SocialActivity>().WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SocialSafetyConfiguration :
    IEntityTypeConfiguration<SocialBlock>, IEntityTypeConfiguration<SocialMute>, IEntityTypeConfiguration<SocialPrivacy>
{
    public void Configure(EntityTypeBuilder<SocialBlock> b)
    { b.ToTable("SocialBlocks"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.BlockedAccountId }).IsUnique(); b.HasIndex(x => x.BlockedAccountId); }
    public void Configure(EntityTypeBuilder<SocialMute> b)
    { b.ToTable("SocialMutes"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.MutedAccountId }).IsUnique(); }
    public void Configure(EntityTypeBuilder<SocialPrivacy> b)
    { b.ToTable("SocialPrivacy"); b.HasKey(x => x.Id); b.HasIndex(x => x.AccountId).IsUnique(); b.Property(x => x.DefaultVisibility).HasConversion<string>().HasMaxLength(16); }
}

public sealed class SocialNotificationConfiguration : IEntityTypeConfiguration<SocialNotification>
{
    public void Configure(EntityTypeBuilder<SocialNotification> b)
    {
        b.ToTable("SocialNotifications"); b.HasKey(x => x.Id); b.Property(x => x.Kind).HasMaxLength(32);
        b.Property(x => x.ResourceType).HasMaxLength(32); b.Property(x => x.ResourceId).HasMaxLength(64);
        b.HasIndex(x => new { x.AccountId, x.ReadAtUtc, x.CreatedAtUtc });
    }
}

public sealed class SocialReportConfiguration : IEntityTypeConfiguration<SocialReport>
{
    public void Configure(EntityTypeBuilder<SocialReport> b)
    {
        b.ToTable("SocialReports"); b.HasKey(x => x.Id); b.Property(x => x.TargetType).HasMaxLength(32);
        b.Property(x => x.TargetId).HasMaxLength(64); b.Property(x => x.Reason).HasMaxLength(2000); b.Property(x => x.Resolution).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16); b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
