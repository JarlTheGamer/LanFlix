namespace Lanflix.Modules.Administration;

public sealed record ApplicationReleaseDto(
    int VersionCode, string VersionName, string DownloadUrl, string? ReleaseNotes, bool Mandatory, long? FileSize, string? Checksum);

public interface IApplicationReleaseCatalog
{
    Task<ApplicationReleaseDto?> GetLatestAsync(int currentVersionCode, CancellationToken cancellationToken);
    Task<ReleaseFileDto?> GetFileAsync(string fileName, CancellationToken cancellationToken);
}

public sealed record ReleaseFileDto(string Path, string DownloadName);
