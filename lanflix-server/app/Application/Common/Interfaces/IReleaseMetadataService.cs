namespace Lanflix.Application.Common.Interfaces;

public class AppReleaseMetadata
{
    public string VersionName { get; set; } = string.Empty;
    public int VersionCode { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool Mandatory { get; set; }
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
}

public interface IReleaseMetadataService
{
    Task<AppReleaseMetadata?> GetLatestAppReleaseAsync(int currentVersionCode, CancellationToken cancellationToken = default);
    Task<ServerUpdateInfo?> GetLatestServerReleaseAsync(string currentVersion, CancellationToken cancellationToken = default);
}
