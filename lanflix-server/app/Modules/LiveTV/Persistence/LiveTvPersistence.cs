using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.LiveTV;

public interface ILiveTvDbContext
{
    DbSet<LiveTvSource> LiveTvSources { get; }
    DbSet<LiveTvChannel> LiveTvChannels { get; }
    DbSet<LiveTvProgram> LiveTvPrograms { get; }
    DbSet<LiveTvFavorite> LiveTvFavorites { get; }
    DbSet<LiveTvTunerLease> LiveTvTunerLeases { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
public sealed class LiveTvSourceConfiguration : IEntityTypeConfiguration<LiveTvSource>
{ public void Configure(EntityTypeBuilder<LiveTvSource> b) { b.ToTable("LiveTvSources"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(160); b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32); b.Property(x => x.SourceUri).HasMaxLength(2048); b.Property(x => x.GuideUri).HasMaxLength(2048); b.Property(x => x.LastError).HasMaxLength(1000); } }
public sealed class LiveTvChannelConfiguration : IEntityTypeConfiguration<LiveTvChannel>
{ public void Configure(EntityTypeBuilder<LiveTvChannel> b) { b.ToTable("LiveTvChannels"); b.HasKey(x => x.Id); b.Property(x => x.ExternalId).HasMaxLength(256); b.Property(x => x.Number).HasMaxLength(32); b.Property(x => x.Name).HasMaxLength(256); b.Property(x => x.LogoUrl).HasMaxLength(2048); b.Property(x => x.StreamUri).HasMaxLength(4096); b.Property(x => x.GroupName).HasMaxLength(256); b.HasIndex(x => new { x.SourceId, x.ExternalId }).IsUnique(); b.HasIndex(x => new { x.Enabled, x.Number }); b.HasOne<LiveTvSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class LiveTvProgramConfiguration : IEntityTypeConfiguration<LiveTvProgram>
{ public void Configure(EntityTypeBuilder<LiveTvProgram> b) { b.ToTable("LiveTvPrograms"); b.HasKey(x => x.Id); b.Property(x => x.ExternalId).HasMaxLength(512); b.Property(x => x.Title).HasMaxLength(512); b.Property(x => x.Description).HasMaxLength(4000); b.Property(x => x.Category).HasMaxLength(128); b.Property(x => x.EpisodeTitle).HasMaxLength(512); b.Property(x => x.ArtworkUrl).HasMaxLength(2048); b.HasIndex(x => new { x.ChannelId, x.ExternalId, x.StartsAtUtc }).IsUnique(); b.HasIndex(x => new { x.ChannelId, x.StartsAtUtc, x.EndsAtUtc }); b.HasOne<LiveTvChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class LiveTvFavoriteConfiguration : IEntityTypeConfiguration<LiveTvFavorite>
{ public void Configure(EntityTypeBuilder<LiveTvFavorite> b) { b.ToTable("LiveTvFavorites"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.ChannelId }).IsUnique(); b.HasOne<LiveTvChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class LiveTvTunerLeaseConfiguration : IEntityTypeConfiguration<LiveTvTunerLease>
{ public void Configure(EntityTypeBuilder<LiveTvTunerLease> b) { b.ToTable("LiveTvTunerLeases"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.SourceId, x.ExpiresAtUtc }); b.HasIndex(x => x.AccountId); b.HasOne<LiveTvChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade); b.HasOne<LiveTvSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade); } }
