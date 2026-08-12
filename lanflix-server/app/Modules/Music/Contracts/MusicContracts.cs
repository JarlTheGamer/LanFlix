namespace Lanflix.Modules.Music;

public sealed record MusicArtistDto(long Id, string Name, string? ArtworkUrl);
public sealed record MusicAlbumDto(long Id, string Title, int? Year, MusicArtistDto Artist, string? ArtworkUrl, int TrackCount);
public sealed record MusicTrackDto(long Id, string Title, int TrackNumber, int? DiscNumber, long DurationMilliseconds,
    IReadOnlyList<string> Genres, string Codec, int? BitrateKbps, int? SampleRateHz, int? Channels,
    MusicArtistDto Artist, MusicAlbumDto Album, string StreamUrl, bool ServerAvailable);
public sealed record MusicHomeDto(IReadOnlyList<MusicAlbumDto> RecentlyAdded, IReadOnlyList<MusicArtistDto> Artists);
public sealed record MusicPlaylistDto(long Id, string Name, IReadOnlyList<MusicTrackDto> Tracks, DateTime UpdatedAtUtc);
public sealed record MusicLyricsDto(long TrackId, string Text, bool IsSynchronized, string Source);
public sealed record MusicWaveformDto(long TrackId, IReadOnlyList<float> Amplitudes);
public sealed record MusicScanResult(int Imported, int Updated, int Removed, int Skipped, int AlbumsRemoved, int ArtistsRemoved);
public sealed record MusicArtworkFile(string Path, string ContentType, string ETag);
public sealed record MusicMetadataHint(string AlbumTitle, int? Year, string TrackTitle, string ArtistName, long DurationMilliseconds);
public sealed record MusicMetadataMatch(string? AlbumArtist, string? AlbumMusicBrainzId, string? TrackMusicBrainzId, int? TrackNumber, int? DiscNumber);
public sealed record CreatePlaylistRequest(string Name);
public sealed record RenamePlaylistRequest(string Name);
public sealed record AddPlaylistTrackRequest(long TrackId);
public sealed record ReplaceQueueRequest(IReadOnlyList<long> TrackIds);
public sealed record RecordPlayRequest(long PositionMilliseconds, bool Completed);

public interface IMusicCatalog
{
    Task<MusicHomeDto> GetHomeAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<MusicAlbumDto>> GetAlbumsAsync(string? query, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<MusicTrackDto>> GetAlbumTracksAsync(long albumId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MusicTrackDto>> GetTracksByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task<MusicTrackDto?> GetTrackAsync(long trackId, CancellationToken cancellationToken);
    Task<MusicTrack?> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken);
    Task<MusicArtworkFile?> GetAlbumArtworkAsync(long albumId, CancellationToken cancellationToken);
    Task<MusicWaveformDto?> GetWaveformAsync(long trackId, CancellationToken cancellationToken);
    Task<MusicScanResult> ScanAsync(CancellationToken cancellationToken);
}

/// <summary>Online enrichment only for missing embedded music tags. Results are persisted by the scanner.</summary>
public interface IMusicMetadataProvider
{
    Task<MusicMetadataMatch?> FindAsync(MusicMetadataHint hint, CancellationToken cancellationToken);
}
