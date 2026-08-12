using System.Text.Json;
using System.Text.Json.Serialization;
using Lanflix.Modules.Music;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Adapters.Music;

/// <summary>
/// Resolves incomplete embedded tags once, persists the selected MusicBrainz
/// release and its track list, and then serves every later scan from SQLite.
/// The process-wide gate observes MusicBrainz's public one-request-per-second
/// guidance even when several scans are queued.
/// </summary>
internal sealed class MusicBrainzMetadataProvider(
    IHttpClientFactory clients,
    IMusicDbContext db,
    ILogger<MusicBrainzMetadataProvider> logger) : IMusicMetadataProvider
{
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTime LastRequestUtc = DateTime.MinValue;

    public async Task<MusicMetadataMatch?> FindAsync(MusicMetadataHint hint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hint.AlbumTitle) || hint.AlbumTitle == "Unknown Album" || string.IsNullOrWhiteSpace(hint.TrackTitle))
            return null;

        var key = LookupKey(hint.AlbumTitle, hint.Year);
        var cached = await db.MusicMetadataCaches.AsNoTracking().SingleOrDefaultAsync(x => x.LookupKey == key, ct);
        if (cached is not null)
            return Match(cached.ReleaseMusicBrainzId, cached.AlbumArtist, DeserializeTracks(cached.TrackListJson), hint);

        var release = await FindReleaseAsync(hint.AlbumTitle, hint.Year, ct);
        if (release is null) return null;

        var trackListJson = JsonSerializer.Serialize(release.Tracks);
        db.MusicMetadataCaches.Add(MusicMetadataCache.Create(key, release.Id, release.Artist, trackListJson));
        await db.SaveChangesAsync(ct);
        return Match(release.Id, release.Artist, release.Tracks, hint);
    }

    private static MusicMetadataMatch? Match(string releaseId, string? albumArtist, IReadOnlyList<ReleaseTrack> tracks, MusicMetadataHint hint)
    {
        var wanted = Normalize(hint.TrackTitle);
        var track = tracks.Where(x => Normalize(x.Title) == wanted)
            .OrderBy(x => hint.DurationMilliseconds > 0 && x.LengthMilliseconds > 0 ? Math.Abs(x.LengthMilliseconds - hint.DurationMilliseconds) : 0)
            .FirstOrDefault();
        return track is null ? null : new MusicMetadataMatch(albumArtist, releaseId, track.RecordingId, track.Position, track.DiscNumber);
    }

    private async Task<ReleaseDetails?> FindReleaseAsync(string title, int? year, CancellationToken ct)
    {
        var search = await GetJsonAsync<ReleaseSearchResponse>($"release/?fmt=json&limit=8&query={Uri.EscapeDataString($"release:\"{title}\"")}", ct);
        var candidate = search?.Releases?.Where(x => string.Equals(Normalize(x.Title ?? string.Empty), Normalize(title), StringComparison.Ordinal))
            .OrderByDescending(x => ScoreRelease(x, year)).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate?.Id)) return null;

        var release = await GetJsonAsync<ReleaseResponse>($"release/{candidate.Id}?fmt=json&inc=recordings+artist-credits", ct);
        var tracks = release?.Media?.SelectMany((medium, index) => medium.Tracks?.Select(track => new ReleaseTrack(
                track.Title ?? string.Empty, track.Position ?? 0, medium.Position ?? index + 1, track.Length ?? 0, track.Recording?.Id)) ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && x.Position > 0).ToArray() ?? [];
        if (tracks.Length == 0) return null;
        var artist = release?.ArtistCredit?.Select(x => x.Name ?? x.Artist?.Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return new ReleaseDetails(candidate.Id, artist, tracks);
    }

    private async Task<T?> GetJsonAsync<T>(string relativePath, CancellationToken ct)
    {
        await RequestGate.WaitAsync(ct);
        try
        {
            var delay = TimeSpan.FromSeconds(1) - (DateTime.UtcNow - LastRequestUtc);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            using var response = await clients.CreateClient("MusicBrainz").GetAsync(relativePath, ct);
            LastRequestUtc = DateTime.UtcNow;
            if (!response.IsSuccessStatusCode) { logger.LogWarning("MusicBrainz returned {StatusCode} for {Path}", (int)response.StatusCode, relativePath); return default; }
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
        }
        catch (HttpRequestException exception) { logger.LogWarning(exception, "MusicBrainz lookup failed for {Path}", relativePath); return default; }
        finally { RequestGate.Release(); }
    }

    private static IReadOnlyList<ReleaseTrack> DeserializeTracks(string json)
    {
        try { return JsonSerializer.Deserialize<ReleaseTrack[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }
    private static int ScoreRelease(ReleaseSearchItem item, int? year) => (year is not null && item.Date?.StartsWith(year.Value.ToString(), StringComparison.Ordinal) == true ? 100 : 0) + (item.Score ?? 0);
    private static string LookupKey(string title, int? year) => $"{Normalize(title)}|{year?.ToString() ?? "?"}";
    private static string Normalize(string value) => new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private sealed record ReleaseDetails(string Id, string? Artist, IReadOnlyList<ReleaseTrack> Tracks);
    private sealed record ReleaseTrack(string Title, int Position, int DiscNumber, long LengthMilliseconds, string? RecordingId);
    private sealed class ReleaseSearchResponse { [JsonPropertyName("releases")] public ReleaseSearchItem[]? Releases { get; init; } }
    private sealed class ReleaseSearchItem { [JsonPropertyName("id")] public string? Id { get; init; } [JsonPropertyName("title")] public string? Title { get; init; } [JsonPropertyName("date")] public string? Date { get; init; } [JsonPropertyName("score")] public int? Score { get; init; } }
    private sealed class ReleaseResponse { [JsonPropertyName("media")] public Medium[]? Media { get; init; } [JsonPropertyName("artist-credit")] public ArtistCredit[]? ArtistCredit { get; init; } }
    private sealed class Medium { [JsonPropertyName("position")] public int? Position { get; init; } [JsonPropertyName("tracks")] public ReleaseTrackJson[]? Tracks { get; init; } }
    private sealed class ReleaseTrackJson { [JsonPropertyName("title")] public string? Title { get; init; } [JsonPropertyName("position")] public int? Position { get; init; } [JsonPropertyName("length")] public long? Length { get; init; } [JsonPropertyName("recording")] public Recording? Recording { get; init; } }
    private sealed class Recording { [JsonPropertyName("id")] public string? Id { get; init; } }
    private sealed class ArtistCredit { [JsonPropertyName("name")] public string? Name { get; init; } [JsonPropertyName("artist")] public Artist? Artist { get; init; } }
    private sealed class Artist { [JsonPropertyName("name")] public string? Name { get; init; } }
}
