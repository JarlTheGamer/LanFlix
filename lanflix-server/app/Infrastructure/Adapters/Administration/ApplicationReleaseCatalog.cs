using Lanflix.Application.Common.Interfaces;
using Lanflix.Modules.Administration;
using Microsoft.AspNetCore.Hosting;

namespace Lanflix.Infrastructure.Adapters.Administration;

internal sealed class ApplicationReleaseCatalog(
    IReleaseMetadataService releases,
    IWebHostEnvironment environment) : IApplicationReleaseCatalog
{
    public async Task<ApplicationReleaseDto?> GetLatestAsync(int currentVersionCode, CancellationToken cancellationToken)
    {
        var release = await releases.GetLatestAppReleaseAsync(currentVersionCode, cancellationToken);
        return release is null || release.VersionCode <= currentVersionCode ? null : new ApplicationReleaseDto(
            release.VersionCode, release.VersionName, release.DownloadUrl, release.ReleaseNotes,
            release.Mandatory, release.FileSize, release.Checksum);
    }

    public Task<ReleaseFileDto?> GetFileAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal) || !safeName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<ReleaseFileDto?>(null);
        var path = Path.Combine(environment.ContentRootPath, "releases", safeName);
        return Task.FromResult(File.Exists(path) ? new ReleaseFileDto(path, safeName) : null);
    }
}
