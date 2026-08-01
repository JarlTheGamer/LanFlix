using System.Diagnostics;
using System.Text.Json;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Subtitles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Adapters.Subtitles;

/// <summary>Serves embedded and sidecar subtitles without exposing media paths to clients.</summary>
internal sealed class FfmpegSubtitleCatalog(
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<FfmpegSubtitleCatalog> logger) : ISubtitleCatalog
{
    public async Task<IReadOnlyList<SubtitleTrackDto>?> GetTracksAsync(
        int contentId, int? episodeId, CancellationToken cancellationToken)
    {
        var path = await ResolveMediaPathAsync(contentId, episodeId, cancellationToken);
        if (path is null) return null;
        var tracks = await ScanAsync(path, cancellationToken);
        return tracks.Select(track => new SubtitleTrackDto(
            track.Index, track.Language ?? "und", track.Title ?? $"Subtitle {track.Index + 1}", track.Format,
            track.IsForced, track.IsDefault, track.ExternalPath is null,
            $"/api/v2/subtitles/track/{contentId}/{track.Index}" + (episodeId is null ? string.Empty : $"?episodeId={episodeId}"))).ToArray();
    }

    public async Task<string?> GetWebVttAsync(
        int contentId, int subtitleIndex, int? episodeId, double? startSeconds, CancellationToken cancellationToken)
    {
        var path = await ResolveMediaPathAsync(contentId, episodeId, cancellationToken);
        if (path is null) return null;
        var track = (await ScanAsync(path, cancellationToken)).SingleOrDefault(item => item.Index == subtitleIndex);
        if (track is null) return null;

        if (track.ExternalPath is not null)
            return await ConvertExternalAsync(track.ExternalPath, startSeconds, cancellationToken);
        return await ExtractEmbeddedAsync(path, track.Index, startSeconds, cancellationToken);
    }

    private async Task<string?> ResolveMediaPathAsync(int contentId, int? episodeId, CancellationToken cancellationToken)
    {
        string? path;
        if (episodeId is { } id)
            path = await db.Episodes.AsNoTracking().Where(item => item.Id == id && item.ContentId == contentId)
                .Select(item => item.FilePath).SingleOrDefaultAsync(cancellationToken);
        else
            path = await db.Contents.AsNoTracking().Where(item => item.Id == contentId)
                .Select(item => item.FilePath).SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : path;
    }

    private async Task<IReadOnlyList<Track>> ScanAsync(string mediaPath, CancellationToken cancellationToken)
    {
        var tracks = new List<Track>();
        try
        {
            var output = await RunAsync(ProbePath(), ["-v", "error", "-print_format", "json", "-show_streams", "-select_streams", "s", mediaPath], cancellationToken);
            using var json = JsonDocument.Parse(output);
            if (json.RootElement.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    var index = stream.TryGetProperty("index", out var value) ? value.GetInt32() : tracks.Count;
                    var tags = stream.TryGetProperty("tags", out var tagValues) ? tagValues : default;
                    var disposition = stream.TryGetProperty("disposition", out var dispositionValues) ? dispositionValues : default;
                    tracks.Add(new Track(index,
                        tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty("language", out var language) ? language.GetString() : null,
                        tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty("title", out var title) ? title.GetString() : null,
                        stream.TryGetProperty("codec_name", out var codec) ? codec.GetString() ?? "unknown" : "unknown",
                        disposition.ValueKind == JsonValueKind.Object && disposition.TryGetProperty("forced", out var forced) && forced.GetInt32() == 1,
                        disposition.ValueKind == JsonValueKind.Object && disposition.TryGetProperty("default", out var isDefault) && isDefault.GetInt32() == 1,
                        null));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or JsonException)
        {
            logger.LogWarning(exception, "Unable to probe subtitles for {MediaPath}", mediaPath);
        }

        var directory = Path.GetDirectoryName(mediaPath);
        var stem = Path.GetFileNameWithoutExtension(mediaPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            foreach (var path in Directory.EnumerateFiles(directory, $"{stem}*"))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension is not (".srt" or ".vtt" or ".ass" or ".ssa")) continue;
                var filename = Path.GetFileNameWithoutExtension(path);
                tracks.Add(new Track(tracks.Count, LanguageFrom(filename), $"External - {LanguageFrom(filename) ?? "und"}", extension[1..],
                    filename.Contains(".forced", StringComparison.OrdinalIgnoreCase), false, path));
            }
        }
        return tracks;
    }

    private async Task<string?> ConvertExternalAsync(string path, double? startSeconds, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".vtt") return await File.ReadAllTextAsync(path, cancellationToken);
        if (extension == ".srt") return ToVtt(await File.ReadAllTextAsync(path, cancellationToken), startSeconds);
        return await ConvertWithFfmpegAsync(path, startSeconds, cancellationToken);
    }

    private async Task<string?> ExtractEmbeddedAsync(string mediaPath, int streamIndex, double? startSeconds, CancellationToken cancellationToken)
    {
        var output = Path.Combine(Path.GetTempPath(), $"lanflix-subtitle-{Guid.NewGuid():N}.vtt");
        try
        {
            var args = new List<string>();
            if (startSeconds is > 0) { args.Add("-ss"); args.Add(startSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            args.AddRange(["-i", mediaPath, "-map", $"0:{streamIndex}", "-f", "webvtt", output]);
            await RunAsync(FfmpegPath(), args, cancellationToken);
            return File.Exists(output) ? await File.ReadAllTextAsync(output, cancellationToken) : null;
        }
        finally { if (File.Exists(output)) File.Delete(output); }
    }

    private async Task<string?> ConvertWithFfmpegAsync(string input, double? startSeconds, CancellationToken cancellationToken)
    {
        var output = Path.Combine(Path.GetTempPath(), $"lanflix-subtitle-{Guid.NewGuid():N}.vtt");
        try
        {
            var args = new List<string>();
            if (startSeconds is > 0) { args.Add("-ss"); args.Add(startSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            args.AddRange(["-i", input, "-f", "webvtt", output]);
            await RunAsync(FfmpegPath(), args, cancellationToken);
            return File.Exists(output) ? await File.ReadAllTextAsync(output, cancellationToken) : null;
        }
        finally { if (File.Exists(output)) File.Delete(output); }
    }

    private static string ToVtt(string srt, double? _) => "WEBVTT\n\n" + srt.Replace("\r\n", "\n").Replace(',', '.');

    private async Task<string> RunAsync(string executable, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Unable to start {executable}");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileName(executable)} failed: {error}");
        return output;
    }

    private string FfmpegPath() => configuration["FFmpeg:Path"] ?? configuration["FFmpegPath"] ?? "ffmpeg";
    private string ProbePath() => configuration["FFprobe:Path"] ?? configuration["FFprobePath"] ?? "ffprobe";

    private static string? LanguageFrom(string filename)
    {
        var token = filename.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.ToLowerInvariant();
        return token switch { "en" or "eng" or "english" => "eng", "nl" or "nld" or "dut" => "nld", "de" or "ger" or "deu" => "deu", "fr" or "fra" => "fra", "es" or "spa" => "spa", _ => null };
    }

    private sealed record Track(int Index, string? Language, string? Title, string Format, bool IsForced, bool IsDefault, string? ExternalPath);
}
