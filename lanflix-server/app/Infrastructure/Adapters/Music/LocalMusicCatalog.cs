using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Buffers.Binary;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Music;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Adapters.Music;

internal sealed partial class LocalMusicCatalog(
    ApplicationDbContext db,
    IConfiguration configuration,
    ISettingsService settings,
    IMusicMetadataProvider metadataProvider,
    ILogger<LocalMusicCatalog> logger) : IMusicCatalog
{
    private static readonly SemaphoreSlim ScanLock = new(1, 1);
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".m4a", ".aac", ".flac", ".ogg", ".opus", ".wav", ".wma", ".aiff", ".aif" };

    public async Task<MusicHomeDto> GetHomeAsync(int limit, CancellationToken ct)
    {
        var albums = await db.MusicAlbums.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(limit).ToListAsync(ct);
        var artists = await db.MusicArtists.AsNoTracking().OrderBy(x => x.Name).Take(limit).ToListAsync(ct);
        return new(await MapAlbumsAsync(albums, ct), artists.Select(MapArtist).ToArray());
    }

    public async Task<IReadOnlyList<MusicAlbumDto>> GetAlbumsAsync(string? query, int limit, CancellationToken ct)
    {
        var albums = db.MusicAlbums.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) { var value = query.Trim(); albums = albums.Where(x => EF.Functions.Like(x.Title, $"%{value}%")); }
        return await MapAlbumsAsync(await albums.OrderByDescending(x => x.Year).ThenBy(x => x.Title).Take(limit).ToListAsync(ct), ct);
    }

    public async Task<IReadOnlyList<MusicTrackDto>> GetAlbumTracksAsync(long albumId, CancellationToken ct) => await MapTracksAsync(await db.MusicTracks.AsNoTracking().Where(x => x.AlbumId == albumId).OrderBy(x => x.DiscNumber).ThenBy(x => x.TrackNumber).ToListAsync(ct), ct);

    public async Task<IReadOnlyList<MusicTrackDto>> GetTracksByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var mapped = await MapTracksAsync(await db.MusicTracks.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct), ct);
        var lookup = mapped.ToDictionary(x => x.Id);
        return ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToArray();
    }

    public async Task<MusicTrackDto?> GetTrackAsync(long trackId, CancellationToken ct)
    { var value = await db.MusicTracks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == trackId, ct); return value is null ? null : (await MapTracksAsync([value], ct)).Single(); }

    public async Task<MusicTrack?> GetPlayableTrackAsync(long trackId, CancellationToken ct)
    {
        var track = await db.MusicTracks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == trackId, ct);
        return track is not null && File.Exists(track.FilePath) ? track : null;
    }

    public async Task<MusicArtworkFile?> GetAlbumArtworkAsync(long albumId, CancellationToken ct)
    {
        var path = await db.MusicAlbums.AsNoTracking().Where(x => x.Id == albumId).Select(x => x.ArtworkPath).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        var file = new FileInfo(path);
        return new(path, ContentType(file.Extension), $"\"{file.Length:x}-{file.LastWriteTimeUtc.Ticks:x}\"");
    }

    public async Task<MusicWaveformDto?> GetWaveformAsync(long trackId, CancellationToken ct)
    {
        var track = await db.MusicTracks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == trackId, ct);
        if (track is null || !File.Exists(track.FilePath)) return null;

        var folder = Path.Combine(AppContext.BaseDirectory, "cache", "music", "waveforms");
        Directory.CreateDirectory(folder);
        var cachePath = Path.Combine(folder, $"track-{track.Id}-{track.FileModifiedUtc.Ticks}.json");
        if (File.Exists(cachePath))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<float[]>(await File.ReadAllTextAsync(cachePath, ct));
                if (cached is { Length: > 0 }) return new MusicWaveformDto(track.Id, cached);
            }
            catch (JsonException) { File.Delete(cachePath); }
        }

        var executable = ResolveFfmpeg();
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "-v", "error", "-i", track.FilePath, "-map", "0:a:0", "-ac", "1", "-ar", "8000", "-f", "s16le", "pipe:1" })
            start.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(start);
            if (process is null) return null;
            await using var pcm = new MemoryStream();
            var copyTask = process.StandardOutput.BaseStream.CopyToAsync(pcm, ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);
            await Task.WhenAll(copyTask, process.WaitForExitAsync(ct));
            var error = await errorTask;
            if (process.ExitCode != 0)
            {
                logger.LogWarning("FFmpeg waveform analysis failed for {Path}: {Error}", track.FilePath, error);
                return null;
            }

            var amplitudes = BuildWaveform(pcm.ToArray(), 96);
            if (amplitudes.Length == 0) return null;
            var temporary = cachePath + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(amplitudes), ct);
            File.Move(temporary, cachePath, true);
            return new MusicWaveformDto(track.Id, amplitudes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not analyze waveform for {Path}", track.FilePath);
            return null;
        }
    }

    public async Task<MusicScanResult> ScanAsync(CancellationToken ct)
    {
        await ScanLock.WaitAsync(ct);
        try
        {
            var roots = await GetRootsAsync(ct);
            if (roots.Length == 0)
            {
                logger.LogWarning("Music scan skipped because no existing music folders are configured");
                return new(0, 0, 0, 0, 0, 0);
            }
            var discovered = EnumerateFiles(roots);
            logger.LogInformation("Music scan found {FileCount} supported files beneath {Roots}", discovered.Count, roots);
            var forceMetadataRefresh = !string.Equals(
                await settings.GetSettingAsync("Lanflix:Music:ScannerVersion", ct), "3", StringComparison.Ordinal);
            var existing = await db.MusicTracks.ToDictionaryAsync(x => x.FilePath, PathComparer(), ct);
            var artists = await db.MusicArtists.ToDictionaryAsync(x => x.NormalizedName, StringComparer.Ordinal, ct);
            var albums = await db.MusicAlbums.ToListAsync(ct);
            var imported = 0; var updated = 0; var skipped = 0;

            foreach (var path in discovered)
            {
                ct.ThrowIfCancellationRequested();
                var info = new FileInfo(path);
                existing.TryGetValue(path, out var track);
                if (!forceMetadataRefresh && track is not null && track.FileSize == info.Length && track.FileModifiedUtc == info.LastWriteTimeUtc) { await SaveLyricsAsync(track.Id, path, null, ct); skipped++; continue; }
                try
                {
                    using var file = TagLib.File.Create(path);
                    var tag = file.Tag;
                    var artistName = First(tag.Performers) ?? First(tag.AlbumArtists) ?? "Unknown Artist";
                    var albumTitle = string.IsNullOrWhiteSpace(tag.Album) ? "Unknown Album" : tag.Album.Trim();
                    var year = tag.Year is >= 1000 and <= 9999 ? (int?)tag.Year : null;
                    var embeddedAlbumArtist = First(tag.AlbumArtists);
                    var embeddedTrackNumber = (int)tag.Track;
                    var embeddedDiscNumber = tag.Disc == 0 ? null : (int?)tag.Disc;
                    var embeddedReleaseId = tag.MusicBrainzReleaseId;
                    var embeddedRecordingId = tag.MusicBrainzTrackId;
                    var title = string.IsNullOrWhiteSpace(tag.Title) ? Path.GetFileNameWithoutExtension(path) : tag.Title.Trim();
                    MusicMetadataMatch? online = null;
                    if (string.IsNullOrWhiteSpace(embeddedAlbumArtist) || embeddedTrackNumber <= 0 ||
                        string.IsNullOrWhiteSpace(embeddedReleaseId) || string.IsNullOrWhiteSpace(embeddedRecordingId))
                    {
                        online = await metadataProvider.FindAsync(new(albumTitle, year, title, artistName,
                            (long)file.Properties.Duration.TotalMilliseconds), ct);
                    }
                    var artistKey = artistName.Trim().ToUpperInvariant();
                    if (!artists.TryGetValue(artistKey, out var artist))
                    {
                        artist = MusicArtist.Create(artistName, tag.MusicBrainzArtistId); db.MusicArtists.Add(artist); await db.SaveChangesAsync(ct); artists[artistKey] = artist;
                    }
                    else artist.Update(artistName, tag.MusicBrainzArtistId);

                    var albumArtistName = embeddedAlbumArtist ?? online?.AlbumArtist ??
                        (albumTitle == "Unknown Album" ? artistName : "Various Artists");
                    var albumArtistKey = albumArtistName.Trim().ToUpperInvariant();
                    if (!artists.TryGetValue(albumArtistKey, out var albumArtist))
                    {
                        albumArtist = MusicArtist.Create(albumArtistName); db.MusicArtists.Add(albumArtist); await db.SaveChangesAsync(ct); artists[albumArtistKey] = albumArtist;
                    }
                    var albumKey = albumTitle.ToUpperInvariant();
                    var album = albums.FirstOrDefault(x => x.ArtistId == albumArtist.Id && x.NormalizedTitle == albumKey && x.Year == year);
                    if (album is null)
                    {
                        album = MusicAlbum.Create(albumArtist.Id, albumTitle, year, embeddedReleaseId ?? online?.AlbumMusicBrainzId); db.MusicAlbums.Add(album); await db.SaveChangesAsync(ct); albums.Add(album);
                    }
                    else album.Update(albumTitle, year, embeddedReleaseId ?? online?.AlbumMusicBrainzId);

                    var metadata = new TrackMetadata(
                        title,
                        path, MimeType(info.Extension), file.Properties.Description ?? info.Extension.TrimStart('.'),
                        JsonSerializer.Serialize(tag.Genres.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)),
                        embeddedRecordingId ?? online?.TrackMusicBrainzId, embeddedTrackNumber > 0 ? embeddedTrackNumber : online?.TrackNumber ?? 0,
                        embeddedDiscNumber ?? online?.DiscNumber,
                        file.Properties.AudioBitrate, file.Properties.AudioSampleRate, file.Properties.AudioChannels,
                        (long)file.Properties.Duration.TotalMilliseconds, info.Length, info.LastWriteTimeUtc);
                    if (track is null) { track = MusicTrack.Create(artist.Id, album.Id, metadata); db.MusicTracks.Add(track); imported++; }
                    else { track.Update(artist.Id, album.Id, metadata); updated++; }
                    await db.SaveChangesAsync(ct);
                    await SaveArtworkAsync(album, path, tag.Pictures.FirstOrDefault(), ct);
                    await SaveLyricsAsync(track.Id, path, tag.Lyrics, ct);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                { skipped++; logger.LogWarning(exception, "Skipping unreadable music file {Path}", path); }
            }

            var missing = existing.Values.Where(x => IsUnderRoots(x.FilePath, roots) && !discovered.Contains(x.FilePath)).ToArray();
            if (missing.Length > 0) { db.MusicTracks.RemoveRange(missing); await db.SaveChangesAsync(ct); }
            var orphanAlbums = await db.MusicAlbums.Where(album => !db.MusicTracks.Any(track => track.AlbumId == album.Id)).ToArrayAsync(ct);
            foreach (var album in orphanAlbums) DeleteArtwork(album.ArtworkPath);
            if (orphanAlbums.Length > 0) { db.MusicAlbums.RemoveRange(orphanAlbums); await db.SaveChangesAsync(ct); }
            var orphanArtists = await db.MusicArtists.Where(artist =>
                !db.MusicAlbums.Any(album => album.ArtistId == artist.Id) &&
                !db.MusicTracks.Any(track => track.ArtistId == artist.Id)).ToArrayAsync(ct);
            if (orphanArtists.Length > 0) { db.MusicArtists.RemoveRange(orphanArtists); await db.SaveChangesAsync(ct); }
            var result = new MusicScanResult(imported, updated, missing.Length, skipped, orphanAlbums.Length, orphanArtists.Length);
            await settings.UpdateSettingAsync("Lanflix:Music:ScannerVersion", "3", ct);
            logger.LogInformation("Music scan completed: {Imported} imported, {Updated} updated, {Removed} removed, {Skipped} skipped", result.Imported, result.Updated, result.Removed, result.Skipped);
            return result;
        }
        finally { ScanLock.Release(); }
    }

    private async Task SaveArtworkAsync(MusicAlbum album, string mediaPath, TagLib.IPicture? picture, CancellationToken ct)
    {
        byte[]? bytes = picture?.Data?.Data;
        var extension = picture?.MimeType?.ToLowerInvariant() switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        if (bytes is not { Length: > 0 })
        {
            var directory = Path.GetDirectoryName(mediaPath)!;
            var sidecar = new[] { "cover.jpg", "folder.jpg", "cover.png", "folder.png" }.Select(name => Path.Combine(directory, name)).FirstOrDefault(File.Exists);
            if (sidecar is null) return;
            var file = new FileInfo(sidecar); if (file.Length > 20 * 1024 * 1024) return;
            bytes = await File.ReadAllBytesAsync(sidecar, ct); extension = file.Extension.ToLowerInvariant();
        }
        if (bytes.Length > 20 * 1024 * 1024) return;
        var folder = Path.Combine(AppContext.BaseDirectory, "cache", "music", "artwork"); Directory.CreateDirectory(folder);
        // Album IDs are only unique inside one database. Hashing the source
        // path prevents parallel scans/databases from overwriting each
        // other's artwork and also invalidates the cache when the source moves.
        var cacheKey = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(mediaPath))))[..24].ToLowerInvariant();
        var path = Path.Combine(folder, $"album-{cacheKey}{extension}");
        if (!File.Exists(path) || new FileInfo(path).Length != bytes.Length) await File.WriteAllBytesAsync(path, bytes, ct);
        if (!string.Equals(album.ArtworkPath, path, StringComparison.OrdinalIgnoreCase)) { album.SetArtwork(path); await db.SaveChangesAsync(ct); }
    }

    private async Task SaveLyricsAsync(long trackId, string mediaPath, string? embedded, CancellationToken ct)
    {
        var text = embedded;
        var source = "embedded";
        if (string.IsNullOrWhiteSpace(text))
        {
            var sidecar = Path.ChangeExtension(mediaPath, ".lrc");
            if (File.Exists(sidecar)) { text = await File.ReadAllTextAsync(sidecar, ct); source = "sidecar"; }
        }
        if (string.IsNullOrWhiteSpace(text)) return;
        var synchronized = LrcTimestamp().IsMatch(text);
        var entity = await db.MusicLyrics.SingleOrDefaultAsync(x => x.TrackId == trackId, ct);
        if (entity is null) db.MusicLyrics.Add(MusicLyrics.Create(trackId, text, synchronized, source));
        else if (entity.Text != text || entity.IsSynchronized != synchronized || entity.Source != source) entity.Update(text, synchronized, source);
        else return;
        await db.SaveChangesAsync(ct);
    }

    private async Task<string[]> GetRootsAsync(CancellationToken ct)
    {
        var configured = configuration.GetSection("Music:Folders").Get<string[]>() ?? [];
        var generatedDefault = configuration["Lanflix:MediaPaths:Music"];
        var saved = await settings.GetSettingAsync("Lanflix:MediaPaths:Music", ct);
        return configured
            .Append(saved)
            .Append(generatedDefault)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Path.GetFullPath(x!))
            .Where(Directory.Exists)
            .Distinct(PathComparer())
            .ToArray();
    }
    private static HashSet<string> EnumerateFiles(IEnumerable<string> roots)
    {
        var result = new HashSet<string>(PathComparer());
        foreach (var root in roots) try { foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)) if (AudioExtensions.Contains(Path.GetExtension(path))) result.Add(Path.GetFullPath(path)); } catch (Exception) { }
        return result;
    }
    private static bool IsUnderRoots(string path, IEnumerable<string> roots) => roots.Any(root => Path.GetFullPath(path).StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

    private async Task<IReadOnlyList<MusicAlbumDto>> MapAlbumsAsync(IReadOnlyList<MusicAlbum> albums, CancellationToken ct)
    {
        if (albums.Count == 0) return [];
        var artistIds = albums.Select(x => x.ArtistId).Distinct().ToArray(); var albumIds = albums.Select(x => x.Id).ToArray();
        var artists = await db.MusicArtists.AsNoTracking().Where(x => artistIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var counts = await db.MusicTracks.AsNoTracking().Where(x => albumIds.Contains(x.AlbumId)).GroupBy(x => x.AlbumId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return albums.Where(x => artists.ContainsKey(x.ArtistId)).Select(x => new MusicAlbumDto(x.Id, x.Title, x.Year, MapArtist(artists[x.ArtistId]), ArtworkUrl(x), counts.GetValueOrDefault(x.Id))).ToArray();
    }

    private async Task<IReadOnlyList<MusicTrackDto>> MapTracksAsync(IReadOnlyList<MusicTrack> tracks, CancellationToken ct)
    {
        if (tracks.Count == 0) return [];
        var artistIds = tracks.Select(x => x.ArtistId).Distinct().ToArray(); var albumIds = tracks.Select(x => x.AlbumId).Distinct().ToArray();
        var artists = await db.MusicArtists.AsNoTracking().Where(x => artistIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var albums = await db.MusicAlbums.AsNoTracking().Where(x => albumIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var counts = await db.MusicTracks.AsNoTracking().Where(x => albumIds.Contains(x.AlbumId)).GroupBy(x => x.AlbumId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return tracks.Where(x => artists.ContainsKey(x.ArtistId) && albums.ContainsKey(x.AlbumId)).Select(track =>
        {
            var artist = MapArtist(artists[track.ArtistId]); var album = albums[track.AlbumId];
            return new MusicTrackDto(track.Id, track.Title, track.TrackNumber, track.DiscNumber, track.DurationMilliseconds,
                DeserializeGenres(track.GenresJson), track.Codec, track.BitrateKbps, track.SampleRateHz, track.Channels, artist,
                new(album.Id, album.Title, album.Year, artist, ArtworkUrl(album), counts.GetValueOrDefault(album.Id)),
                $"/api/v2/music/tracks/{track.Id}/file", File.Exists(track.FilePath));
        }).ToArray();
    }

    private static IReadOnlyList<string> DeserializeGenres(string json) { try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch (JsonException) { return []; } }
    private static MusicArtistDto MapArtist(MusicArtist value) => new(value.Id, value.Name, null);
    private static string? ArtworkUrl(MusicAlbum value) => string.IsNullOrWhiteSpace(value.ArtworkPath) ? null : $"/api/v2/music/albums/{value.Id}/artwork";
    private static string? First(IEnumerable<string?>? values) => values?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
    private static StringComparer PathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static void DeleteArtwork(string? path) { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) try { File.Delete(path); } catch (IOException) { } }
    private static string ContentType(string extension) => extension.ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
    private static string MimeType(string extension) => extension.ToLowerInvariant() switch { ".flac" => "audio/flac", ".ogg" => "audio/ogg", ".opus" => "audio/ogg", ".wav" or ".aif" or ".aiff" => "audio/wav", ".m4a" or ".aac" => "audio/mp4", ".wma" => "audio/x-ms-wma", _ => "audio/mpeg" };
    private string ResolveFfmpeg()
    {
        var configured = configuration["FFmpeg:Path"] ?? configuration["FFmpegPath"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var local = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
        return File.Exists(local) ? local : OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
    }
    private static float[] BuildWaveform(byte[] pcm, int bins)
    {
        var sampleCount = pcm.Length / sizeof(short);
        if (sampleCount == 0) return [];
        var values = new float[bins];
        var peak = 0f;
        for (var bin = 0; bin < bins; bin++)
        {
            var from = (long)bin * sampleCount / bins;
            var to = Math.Max(from + 1, (long)(bin + 1) * sampleCount / bins);
            double squareSum = 0;
            for (var sample = from; sample < to; sample++)
            {
                var value = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan((int)sample * 2, 2)) / 32768f;
                squareSum += value * value;
            }
            values[bin] = (float)Math.Sqrt(squareSum / (to - from));
            peak = Math.Max(peak, values[bin]);
        }
        if (peak <= 0f) return values.Select(_ => .08f).ToArray();
        for (var index = 0; index < values.Length; index++) values[index] = Math.Clamp(values[index] / peak, .08f, 1f);
        return values;
    }
    [GeneratedRegex(@"\[\d{1,3}:\d{2}(?:\.\d{1,3})?\]")]
    private static partial Regex LrcTimestamp();
}
