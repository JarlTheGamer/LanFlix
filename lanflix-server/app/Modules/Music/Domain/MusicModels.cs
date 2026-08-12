using Lanflix.SharedKernel;

namespace Lanflix.Modules.Music;

public sealed class MusicArtist : Entity<long>
{
    private MusicArtist() { }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? MusicBrainzId { get; private set; }
    public string? ArtworkPath { get; private set; }
    public static MusicArtist Create(string name, string? musicBrainzId = null) => new() { Name = Clean(name, "Unknown Artist"), NormalizedName = Normalize(name, "Unknown Artist"), MusicBrainzId = NullIfBlank(musicBrainzId) };
    public void Update(string name, string? musicBrainzId) { Name = Clean(name, "Unknown Artist"); NormalizedName = Normalize(name, "Unknown Artist"); MusicBrainzId = NullIfBlank(musicBrainzId) ?? MusicBrainzId; MarkUpdated(); }
    public void SetArtwork(string? path) { ArtworkPath = NullIfBlank(path); MarkUpdated(); }
    private static string Clean(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string Normalize(string? value, string fallback) => Clean(value, fallback).ToUpperInvariant();
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MusicAlbum : Entity<long>
{
    private MusicAlbum() { }
    public long ArtistId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string NormalizedTitle { get; private set; } = string.Empty;
    public int? Year { get; private set; }
    public string? MusicBrainzId { get; private set; }
    public string? ArtworkPath { get; private set; }
    public static MusicAlbum Create(long artistId, string title, int? year, string? musicBrainzId = null) => new() { ArtistId = artistId, Title = Clean(title), NormalizedTitle = Clean(title).ToUpperInvariant(), Year = ValidYear(year), MusicBrainzId = NullIfBlank(musicBrainzId) };
    public void Update(string title, int? year, string? musicBrainzId) { Title = Clean(title); NormalizedTitle = Title.ToUpperInvariant(); Year = ValidYear(year); MusicBrainzId = NullIfBlank(musicBrainzId) ?? MusicBrainzId; MarkUpdated(); }
    public void SetArtwork(string? path) { ArtworkPath = NullIfBlank(path); MarkUpdated(); }
    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown Album" : value.Trim();
    private static int? ValidYear(int? value) => value is >= 1000 and <= 9999 ? value : null;
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MusicTrack : Entity<long>
{
    private MusicTrack() { }
    public long ArtistId { get; private set; }
    public long AlbumId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = "audio/mpeg";
    public string Codec { get; private set; } = string.Empty;
    public string GenresJson { get; private set; } = "[]";
    public string? MusicBrainzId { get; private set; }
    public int TrackNumber { get; private set; }
    public int? DiscNumber { get; private set; }
    public int? BitrateKbps { get; private set; }
    public int? SampleRateHz { get; private set; }
    public int? Channels { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public long FileSize { get; private set; }
    public DateTime FileModifiedUtc { get; private set; }
    public static MusicTrack Create(long artistId, long albumId, TrackMetadata value) { var track = new MusicTrack(); track.Apply(artistId, albumId, value); return track; }
    public void Update(long artistId, long albumId, TrackMetadata value) { Apply(artistId, albumId, value); MarkUpdated(); }
    private void Apply(long artistId, long albumId, TrackMetadata value)
    {
        ArtistId = artistId; AlbumId = albumId; Title = string.IsNullOrWhiteSpace(value.Title) ? Path.GetFileNameWithoutExtension(value.FilePath) : value.Title.Trim();
        FilePath = Path.GetFullPath(value.FilePath); MimeType = value.MimeType; Codec = value.Codec; GenresJson = value.GenresJson;
        MusicBrainzId = string.IsNullOrWhiteSpace(value.MusicBrainzId) ? null : value.MusicBrainzId.Trim(); TrackNumber = Math.Max(value.TrackNumber, 0);
        DiscNumber = value.DiscNumber is > 0 ? value.DiscNumber : null; BitrateKbps = value.BitrateKbps is > 0 ? value.BitrateKbps : null;
        SampleRateHz = value.SampleRateHz is > 0 ? value.SampleRateHz : null; Channels = value.Channels is > 0 ? value.Channels : null;
        DurationMilliseconds = Math.Max(value.DurationMilliseconds, 0); FileSize = Math.Max(value.FileSize, 0); FileModifiedUtc = value.FileModifiedUtc;
    }
}

public sealed record TrackMetadata(string Title, string FilePath, string MimeType, string Codec, string GenresJson, string? MusicBrainzId, int TrackNumber, int? DiscNumber, int? BitrateKbps, int? SampleRateHz, int? Channels, long DurationMilliseconds, long FileSize, DateTime FileModifiedUtc);

/// <summary>
/// Durable response cache for a MusicBrainz release lookup. The full track list
/// is intentionally kept with the release: a later rescan can repair missing
/// local track numbers and recording IDs without calling MusicBrainz again.
/// </summary>
public sealed class MusicMetadataCache : Entity<long>
{
    private MusicMetadataCache() { }
    public string LookupKey { get; private set; } = string.Empty;
    public string ReleaseMusicBrainzId { get; private set; } = string.Empty;
    public string? AlbumArtist { get; private set; }
    public string TrackListJson { get; private set; } = "[]";
    public DateTime ResolvedAtUtc { get; private set; }

    public static MusicMetadataCache Create(string lookupKey, string releaseMusicBrainzId, string? albumArtist, string trackListJson) => new()
    {
        LookupKey = Require(lookupKey), ReleaseMusicBrainzId = Require(releaseMusicBrainzId),
        AlbumArtist = BlankToNull(albumArtist), TrackListJson = string.IsNullOrWhiteSpace(trackListJson) ? "[]" : trackListJson,
        ResolvedAtUtc = DateTime.UtcNow
    };

    public void Refresh(string releaseMusicBrainzId, string? albumArtist, string trackListJson)
    {
        ReleaseMusicBrainzId = Require(releaseMusicBrainzId); AlbumArtist = BlankToNull(albumArtist);
        TrackListJson = string.IsNullOrWhiteSpace(trackListJson) ? "[]" : trackListJson; ResolvedAtUtc = DateTime.UtcNow; MarkUpdated();
    }

    private static string Require(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Music metadata value is required.", nameof(value)) : value.Trim();
    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MusicPlaylist : Entity<long>
{
    private MusicPlaylist() { }
    public Guid AccountId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public static MusicPlaylist Create(Guid accountId, string name) => new() { AccountId = accountId, Name = ValidateName(name) };
    public void Rename(string name) { Name = ValidateName(name); MarkUpdated(); }
    private static string ValidateName(string name) => string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Playlist name is required.", nameof(name)) : name.Trim();
}

public sealed class MusicPlaylistTrack : Entity<long>
{
    private MusicPlaylistTrack() { }
    public long PlaylistId { get; private set; }
    public long TrackId { get; private set; }
    public int Position { get; private set; }
    public static MusicPlaylistTrack Create(long playlistId, long trackId, int position) => new() { PlaylistId = playlistId, TrackId = trackId, Position = Math.Max(position, 0) };
    public void MoveTo(int position) { Position = Math.Max(position, 0); MarkUpdated(); }
}

public sealed class MusicFavorite : Entity<long>
{
    private MusicFavorite() { }
    public Guid AccountId { get; private set; }
    public long TrackId { get; private set; }
    public static MusicFavorite Create(Guid accountId, long trackId) => new() { AccountId = accountId, TrackId = trackId };
}

public sealed class MusicPlayHistory : Entity<long>
{
    private MusicPlayHistory() { }
    public Guid AccountId { get; private set; }
    public long TrackId { get; private set; }
    public long PositionMilliseconds { get; private set; }
    public bool Completed { get; private set; }
    public DateTime PlayedAtUtc { get; private set; }
    public static MusicPlayHistory Create(Guid accountId, long trackId, long positionMilliseconds, bool completed) => new() { AccountId = accountId, TrackId = trackId, PositionMilliseconds = Math.Max(positionMilliseconds, 0), Completed = completed, PlayedAtUtc = DateTime.UtcNow };
}

public sealed class MusicQueueItem : Entity<long>
{
    private MusicQueueItem() { }
    public Guid AccountId { get; private set; }
    public long TrackId { get; private set; }
    public int Position { get; private set; }
    public static MusicQueueItem Create(Guid accountId, long trackId, int position) => new() { AccountId = accountId, TrackId = trackId, Position = Math.Max(position, 0) };
    public void MoveTo(int position) { Position = Math.Max(position, 0); MarkUpdated(); }
}

public sealed class MusicLyrics : Entity<long>
{
    private MusicLyrics() { }
    public long TrackId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsSynchronized { get; private set; }
    public string Source { get; private set; } = "local";
    public static MusicLyrics Create(long trackId, string text, bool synchronized, string source) => new() { TrackId = trackId, Text = text, IsSynchronized = synchronized, Source = source };
    public void Update(string text, bool synchronized, string source) { Text = text; IsSynchronized = synchronized; Source = source; MarkUpdated(); }
}
