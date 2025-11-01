using Lanflix.Application.Common.DTOs;

namespace Lanflix.Application.Common.Interfaces;

public interface ISettingsService
{
    Task<ServerSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(ServerSettingsDto settings, CancellationToken cancellationToken = default);
}
