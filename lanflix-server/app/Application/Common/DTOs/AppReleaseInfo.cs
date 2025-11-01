namespace Lanflix.Application.Common.DTOs;

public class AppReleaseInfo
{
    public string Version { get; set; } = string.Empty;
    public int VersionCode { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool IsForceUpdate { get; set; }
    public string MinimumSupportedVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = "arm64-v8a";
}
