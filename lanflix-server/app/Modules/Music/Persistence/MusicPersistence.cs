using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Music;

public interface IMusicDbContext
{
    DbSet<MusicArtist> MusicArtists { get; }
    DbSet<MusicAlbum> MusicAlbums { get; }
    DbSet<MusicTrack> MusicTracks { get; }
    DbSet<MusicPlaylist> MusicPlaylists { get; }
    DbSet<MusicPlaylistTrack> MusicPlaylistTracks { get; }
    DbSet<MusicFavorite> MusicFavorites { get; }
    DbSet<MusicPlayHistory> MusicPlayHistory { get; }
    DbSet<MusicQueueItem> MusicQueueItems { get; }
    DbSet<MusicLyrics> MusicLyrics { get; }
    DbSet<MusicMetadataCache> MusicMetadataCaches { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class MusicArtistConfiguration : IEntityTypeConfiguration<MusicArtist>
{
    public void Configure(EntityTypeBuilder<MusicArtist> b) { b.ToTable("MusicArtists"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(256).IsRequired(); b.Property(x => x.NormalizedName).HasMaxLength(256).IsRequired(); b.Property(x => x.MusicBrainzId).HasMaxLength(64); b.Property(x => x.ArtworkPath).HasMaxLength(2048); b.HasIndex(x => x.NormalizedName).IsUnique(); b.HasIndex(x => x.MusicBrainzId); }
}
public sealed class MusicAlbumConfiguration : IEntityTypeConfiguration<MusicAlbum>
{
    public void Configure(EntityTypeBuilder<MusicAlbum> b) { b.ToTable("MusicAlbums"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(256).IsRequired(); b.Property(x => x.NormalizedTitle).HasMaxLength(256).IsRequired(); b.Property(x => x.MusicBrainzId).HasMaxLength(64); b.Property(x => x.ArtworkPath).HasMaxLength(2048); b.HasIndex(x => new { x.ArtistId, x.NormalizedTitle }).IsUnique(); b.HasOne<MusicArtist>().WithMany().HasForeignKey(x => x.ArtistId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicTrackConfiguration : IEntityTypeConfiguration<MusicTrack>
{
    public void Configure(EntityTypeBuilder<MusicTrack> b) { b.ToTable("MusicTracks"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(512).IsRequired(); b.Property(x => x.FilePath).HasMaxLength(2048).IsRequired(); b.Property(x => x.MimeType).HasMaxLength(96); b.Property(x => x.Codec).HasMaxLength(64); b.Property(x => x.GenresJson).HasColumnType("TEXT"); b.Property(x => x.MusicBrainzId).HasMaxLength(64); b.HasIndex(x => x.FilePath).IsUnique(); b.HasIndex(x => x.MusicBrainzId); b.HasIndex(x => new { x.AlbumId, x.DiscNumber, x.TrackNumber }); b.HasOne<MusicArtist>().WithMany().HasForeignKey(x => x.ArtistId).OnDelete(DeleteBehavior.Restrict); b.HasOne<MusicAlbum>().WithMany().HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicPlaylistConfiguration : IEntityTypeConfiguration<MusicPlaylist>
{
    public void Configure(EntityTypeBuilder<MusicPlaylist> b) { b.ToTable("MusicPlaylists"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.HasIndex(x => new { x.AccountId, x.Name }).IsUnique(); }
}
public sealed class MusicPlaylistTrackConfiguration : IEntityTypeConfiguration<MusicPlaylistTrack>
{
    public void Configure(EntityTypeBuilder<MusicPlaylistTrack> b) { b.ToTable("MusicPlaylistTracks"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.PlaylistId, x.Position }).IsUnique(); b.HasIndex(x => new { x.PlaylistId, x.TrackId }).IsUnique(); b.HasOne<MusicPlaylist>().WithMany().HasForeignKey(x => x.PlaylistId).OnDelete(DeleteBehavior.Cascade); b.HasOne<MusicTrack>().WithMany().HasForeignKey(x => x.TrackId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicFavoriteConfiguration : IEntityTypeConfiguration<MusicFavorite>
{
    public void Configure(EntityTypeBuilder<MusicFavorite> b) { b.ToTable("MusicFavorites"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.TrackId }).IsUnique(); b.HasOne<MusicTrack>().WithMany().HasForeignKey(x => x.TrackId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicPlayHistoryConfiguration : IEntityTypeConfiguration<MusicPlayHistory>
{
    public void Configure(EntityTypeBuilder<MusicPlayHistory> b) { b.ToTable("MusicPlayHistory"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.PlayedAtUtc }); b.HasOne<MusicTrack>().WithMany().HasForeignKey(x => x.TrackId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicQueueItemConfiguration : IEntityTypeConfiguration<MusicQueueItem>
{
    public void Configure(EntityTypeBuilder<MusicQueueItem> b) { b.ToTable("MusicQueueItems"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.AccountId, x.Position }).IsUnique(); b.HasOne<MusicTrack>().WithMany().HasForeignKey(x => x.TrackId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicLyricsConfiguration : IEntityTypeConfiguration<MusicLyrics>
{
    public void Configure(EntityTypeBuilder<MusicLyrics> b) { b.ToTable("MusicLyrics"); b.HasKey(x => x.Id); b.Property(x => x.Text).HasColumnType("TEXT").IsRequired(); b.Property(x => x.Source).HasMaxLength(128); b.HasIndex(x => x.TrackId).IsUnique(); b.HasOne<MusicTrack>().WithOne().HasForeignKey<MusicLyrics>(x => x.TrackId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class MusicMetadataCacheConfiguration : IEntityTypeConfiguration<MusicMetadataCache>
{
    public void Configure(EntityTypeBuilder<MusicMetadataCache> b)
    {
        b.ToTable("MusicMetadataCaches"); b.HasKey(x => x.Id);
        b.Property(x => x.LookupKey).HasMaxLength(768).IsRequired();
        b.Property(x => x.ReleaseMusicBrainzId).HasMaxLength(64);
        b.Property(x => x.AlbumArtist).HasMaxLength(512);
        b.Property(x => x.TrackListJson).HasColumnType("TEXT").IsRequired();
        b.HasIndex(x => x.LookupKey).IsUnique();
        b.HasIndex(x => x.ReleaseMusicBrainzId);
    }
}
