namespace Lanflix.Application.Common.Interfaces;

public interface IServerUpdateService
{
    string GetCurrentVersion();
    Task<ServerUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, CancellationToken cancellationToken = default);
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
