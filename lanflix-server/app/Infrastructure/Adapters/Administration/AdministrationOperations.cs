using System.Diagnostics;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Modules.Administration;
using Microsoft.Extensions.Configuration;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Music;
using Lanflix.Modules.LiveTV;
using Lanflix.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Adapters.Administration;

internal sealed class AdministrationOperations(
    ILibraryService library,
    IServerUpdateService updates,
    ISettingsService settings,
    IConfiguration configuration,
    ApplicationDbContext db,
    IMusicCatalog music,
    ILiveTvCatalog liveTv) : IAdministrationOperations
{
    private static readonly DateTime StartedAtUtc = DateTime.UtcNow;

    public async Task<string> ExecuteJobAsync(string name, CancellationToken cancellationToken) => name switch
    {
        "library-scan" => Format(await library.ScanLibraryAsync(cancellationToken)),
        "music-scan" => Format(await music.ScanAsync(cancellationToken)),
        "live-tv-refresh" => await RefreshLiveTvAsync(cancellationToken),
        "update-check" => (await updates.CheckForUpdatesAsync(cancellationToken)) is { IsUpdateAvailable: true } info
            ? $"Update {info.Version} is available"
            : "Server is up to date",
        "cleanup-transcodes" => CleanupTranscodes(),
        _ => throw new InvalidOperationException($"Unsupported administration job: {name}")
    };

    public ServerTelemetryDto GetTelemetry()
    {
        using var process = Process.GetCurrentProcess();
        var root = Path.GetPathRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
        var drive = new DriveInfo(root);
        return new ServerTelemetryDto("online", (long)(DateTime.UtcNow - StartedAtUtc).TotalSeconds,
            process.WorkingSet64, Environment.ProcessorCount, drive.AvailableFreeSpace, drive.TotalSize,
            Environment.MachineName, Environment.OSVersion.VersionString, Environment.Version.ToString());
    }

    public async Task<UpdateCheckDto> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        var current = updates.GetCurrentVersion();
        var result = await updates.CheckForUpdatesAsync(cancellationToken);
        return result is null
            ? new UpdateCheckDto(current, false, null, null, null, null)
            : new UpdateCheckDto(current, result.IsUpdateAvailable, result.Version, result.ReleaseDate, result.FileSize, result.ReleaseNotes);
    }

    public Task<bool> ApplyUpdateAsync(string downloadUrl, CancellationToken cancellationToken)
        => updates.DownloadAndApplyUpdateAsync(downloadUrl, cancellationToken);

    public object GetUpdateProgress() => updates.GetUpdateProgress();

    public async Task<AdministrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var telemetry = GetTelemetry();
        return new AdministrationOverviewDto(
            await db.Accounts.CountAsync(cancellationToken),
            await db.Contents.CountAsync(x => x.Type == ContentType.Movie, cancellationToken),
            await db.Contents.CountAsync(x => x.Type == ContentType.Series, cancellationToken),
            await db.Episodes.CountAsync(cancellationToken),
            await db.MusicTracks.CountAsync(cancellationToken),
            await db.LiveTvChannels.CountAsync(x => x.Enabled, cancellationToken),
            await db.BackgroundJobRuns.CountAsync(x => x.Status == "pending" || x.Status == "running", cancellationToken),
            await db.SocialReports.CountAsync(x => x.Status == Lanflix.Modules.Social.ReportStatus.Open, cancellationToken),
            telemetry.WorkingSetBytes, telemetry.UptimeSeconds);
    }

    public Task<IReadOnlyList<LogFileDto>> GetLogsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(AppContext.BaseDirectory, "logs");
        IReadOnlyList<LogFileDto> result = !Directory.Exists(directory) ? [] : Directory.EnumerateFiles(directory, "*.log", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path)).OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(100).Select(file => new LogFileDto(file.Name, file.Length, file.LastWriteTimeUtc)).ToArray();
        return Task.FromResult(result);
    }

    public async Task<LogContentDto?> ReadLogAsync(string name, int lineCount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name || !name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) return null;
        var path = Path.Combine(AppContext.BaseDirectory, "logs", name);
        if (!File.Exists(path)) return null;
        var lines = new Queue<string>(lineCount);
        await foreach (var line in File.ReadLinesAsync(path, cancellationToken))
        {
            if (lines.Count == lineCount) lines.Dequeue();
            lines.Enqueue(line);
        }
        return new LogContentDto(name, lines.ToArray());
    }

    public async Task<AdministrationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
        => await MapAsync(await settings.GetSettingsAsync(cancellationToken), cancellationToken);

    public async Task<AdministrationSettingsDto> UpdateSettingsAsync(
        AdministrationSettingsDto request, CancellationToken cancellationToken)
    {
        var current = await settings.GetSettingsAsync(cancellationToken);
        current.MediaPaths.Movies = NormalizePath(request.Libraries.Movies);
        current.MediaPaths.Series = NormalizePath(request.Libraries.Series);
        current.Transcoding.EnableHardwareAcceleration = request.Playback.HardwareAcceleration;
        current.Transcoding.PreferredHwAccel = request.Playback.HardwareAccelerator;
        current.Transcoding.MaxConcurrentTranscodes = Math.Clamp(request.Playback.MaxConcurrentTranscodes, 1, 8);
        current.Transcoding.TempPath = NormalizePath(request.Playback.TranscodePath);
        current.Streaming.EnableDirectPlay = request.Playback.DirectPlay;
        current.Streaming.EnableDirectStream = request.Playback.DirectStream;
        Apply(current.ExternalApis.Tmdb, request.Tmdb);
        Apply(current.ExternalApis.Radarr, request.Radarr);
        Apply(current.ExternalApis.Sonarr, request.Sonarr);
        Apply(current.ExternalApis.Prowlarr, request.Prowlarr);
        Apply(current.ExternalApis.Subtitles.Bazarr, request.Bazarr);
        await settings.UpdateSettingsAsync(current, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Libraries.Music))
            await settings.UpdateSettingAsync("Lanflix:MediaPaths:Music", NormalizePath(request.Libraries.Music), cancellationToken);
        return await MapAsync(await settings.GetSettingsAsync(cancellationToken), cancellationToken);
    }

    private string CleanupTranscodes()
    {
        var configured = configuration["Lanflix:Transcoding:TempPath"];
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data", "transcodes")
            : Path.GetFullPath(configured);
        if (!Directory.Exists(directory)) return "No transcoding cache exists";
        var deleted = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTimeUtc > DateTime.UtcNow.AddHours(-24)) continue;
            try { info.Delete(); deleted++; } catch (IOException) { }
        }
        return $"Removed {deleted} expired transcoding files";
    }

    private static string Format(LibraryScanResult result) =>
        $"Added {result.Added}, updated {result.Updated}, removed {result.Removed}, errors {result.Errors.Count}";

    private static string Format(MusicScanResult result) =>
        $"Imported {result.Imported}, updated {result.Updated}, removed {result.Removed}, skipped {result.Skipped}";

    private async Task<string> RefreshLiveTvAsync(CancellationToken cancellationToken)
    {
        var sources = await liveTv.GetSourcesAsync(cancellationToken);
        var channels = 0;
        var programs = 0;
        var errors = new List<string>();
        foreach (var source in sources.Where(x => x.Enabled))
        {
            var result = await liveTv.RefreshAsync(source.Id, cancellationToken);
            channels += result.ChannelsImported + result.ChannelsUpdated;
            programs += result.ProgramsImported;
            if (!string.IsNullOrWhiteSpace(result.Error)) errors.Add($"{source.Name}: {result.Error}");
        }
        return $"Refreshed {sources.Count(x => x.Enabled)} sources, {channels} channels and {programs} programs" +
            (errors.Count == 0 ? string.Empty : $"; errors: {string.Join(" | ", errors)}");
    }

    private async Task<AdministrationSettingsDto> MapAsync(
        Lanflix.Application.Common.DTOs.ServerSettingsDto value,
        CancellationToken cancellationToken)
    {
        var music = await settings.GetSettingAsync("Lanflix:MediaPaths:Music", cancellationToken) ?? string.Empty;
        return new AdministrationSettingsDto(
            new LibraryPathsDto(value.MediaPaths.Movies, value.MediaPaths.Series, music),
            new PlaybackSettingsDto(value.Streaming.EnableDirectPlay, value.Streaming.EnableDirectStream,
                value.Transcoding.EnableHardwareAcceleration, value.Transcoding.PreferredHwAccel,
                value.Transcoding.MaxConcurrentTranscodes, value.Transcoding.TempPath),
            Integration(value.ExternalApis.Tmdb.BaseUrl, value.ExternalApis.Tmdb.ApiKey),
            Integration(value.ExternalApis.Radarr.Url, value.ExternalApis.Radarr.ApiKey),
            Integration(value.ExternalApis.Sonarr.Url, value.ExternalApis.Sonarr.ApiKey),
            Integration(value.ExternalApis.Prowlarr.Url, value.ExternalApis.Prowlarr.ApiKey),
            Integration(value.ExternalApis.Subtitles.Bazarr.Url, value.ExternalApis.Subtitles.Bazarr.ApiKey));
    }

    private static IntegrationSettingsDto Integration(string url, string key) => new(url, !string.IsNullOrWhiteSpace(key));
    private static void Apply(Lanflix.Application.Common.DTOs.ExternalServiceSettings target, IntegrationSettingsDto source)
    { target.Url = source.Url.Trim(); if (!string.IsNullOrWhiteSpace(source.ApiKey)) target.ApiKey = source.ApiKey.Trim(); }
    private static void Apply(Lanflix.Application.Common.DTOs.TmdbSettings target, IntegrationSettingsDto source)
    { target.BaseUrl = source.Url.Trim(); if (!string.IsNullOrWhiteSpace(source.ApiKey)) target.ApiKey = source.ApiKey.Trim(); }
    private static string NormalizePath(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : Path.GetFullPath(value.Trim());
}
