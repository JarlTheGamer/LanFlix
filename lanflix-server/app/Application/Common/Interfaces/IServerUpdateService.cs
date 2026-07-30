namespace Lanflix.Application.Common.Interfaces;

public interface IServerUpdateService
{
    string GetCurrentVersion();
    Task<ServerUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, CancellationToken cancellationToken = default);
    UpdateProgressStatus GetUpdateProgress();
}

public class ServerUpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool IsUpdateAvailable { get; set; }
}

public class UpdateProgressStatus
{
    public string Status { get; set; } = "Idle"; // Idle, Downloading, Extracting, Applying, Complete, Failed
    public int Percentage { get; set; } = 0;
    public string Message { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; } = 0;
    public long TotalBytes { get; set; } = 0;
}
