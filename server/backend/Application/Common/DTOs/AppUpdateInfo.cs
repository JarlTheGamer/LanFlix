namespace Lanflix.Application.Common.DTOs;

public class AppUpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public int VersionCode { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool IsForceUpdate { get; set; }
    public string MinimumSupportedVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
}
