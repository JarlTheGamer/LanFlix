using Lanflix.Application.Common.DTOs;

namespace Lanflix.Application.Common.Interfaces;

public interface ISettingsService
{
    Task<ServerSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(ServerSettingsDto settings, CancellationToken cancellationToken = default);
    Task UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);
    Task EnsureConfigFileExistsAsync(CancellationToken cancellationToken = default);
}
