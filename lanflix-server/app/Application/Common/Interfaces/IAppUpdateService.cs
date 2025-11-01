using Lanflix.Application.Common.DTOs;

namespace Lanflix.Application.Common.Interfaces;

public interface IAppUpdateService
{
    Task<AppUpdateInfo?> GetLatestVersionAsync(string platform, string currentVersion, string architecture, CancellationToken cancellationToken = default);
    Task<string?> GetApkPathAsync(string version, string architecture, CancellationToken cancellationToken = default);
    Task<AppUpdateInfo> SaveReleaseAsync(Stream apkStream, AppReleaseInfo releaseInfo, CancellationToken cancellationToken = default);
    Task<List<AppUpdateInfo>> GetVersionHistoryAsync(string platform, CancellationToken cancellationToken = default);
}
