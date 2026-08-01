namespace Lanflix.Modules.Administration;

public sealed record ServerTelemetryDto(
    string Status, long UptimeSeconds, long WorkingSetBytes, int ProcessorCount,
    long AvailableStorageBytes, long TotalStorageBytes, string MachineName,
    string OperatingSystem, string RuntimeVersion);

public sealed record JobDto(
    Guid Id, string Name, string Status, DateTime CreatedAtUtc,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc, string? Result, string? Error);

public sealed record TriggerJobRequest(string Name);
public sealed record UpdateCheckDto(
    string CurrentVersion, bool UpdateAvailable, string? LatestVersion,
    DateTime? ReleaseDate, long? FileSize, string? ReleaseNotes);
public sealed record ApplyUpdateRequest(string DownloadUrl);
public sealed record AdministrationOverviewDto(int Accounts, int Movies, int Series, int Episodes, int MusicTracks,
    int LiveTvChannels, int PendingJobs, int OpenReports, long WorkingSetBytes, long UptimeSeconds);
public sealed record LogFileDto(string Name, long SizeBytes, DateTime LastModifiedUtc);
public sealed record LogContentDto(string Name, IReadOnlyList<string> Lines);
public sealed record LibraryPathsDto(string Movies, string Series, string Music);
public sealed record PlaybackSettingsDto(
    bool DirectPlay, bool DirectStream, bool HardwareAcceleration,
    string HardwareAccelerator, int MaxConcurrentTranscodes, string TranscodePath);
public sealed record IntegrationSettingsDto(string Url, bool Configured, string? ApiKey = null);
public sealed record AdministrationSettingsDto(
    LibraryPathsDto Libraries, PlaybackSettingsDto Playback,
    IntegrationSettingsDto Tmdb, IntegrationSettingsDto Radarr,
    IntegrationSettingsDto Sonarr, IntegrationSettingsDto Prowlarr,
    IntegrationSettingsDto Bazarr);

public interface IAdministrationOperations
{
    Task<string> ExecuteJobAsync(string name, CancellationToken cancellationToken);
    ServerTelemetryDto GetTelemetry();
    Task<UpdateCheckDto> CheckForUpdatesAsync(CancellationToken cancellationToken);
    Task<bool> ApplyUpdateAsync(string downloadUrl, CancellationToken cancellationToken);
    object GetUpdateProgress();
    Task<AdministrationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
    Task<AdministrationSettingsDto> UpdateSettingsAsync(AdministrationSettingsDto settings, CancellationToken cancellationToken);
    Task<AdministrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LogFileDto>> GetLogsAsync(CancellationToken cancellationToken);
    Task<LogContentDto?> ReadLogAsync(string name, int lineCount, CancellationToken cancellationToken);
}
