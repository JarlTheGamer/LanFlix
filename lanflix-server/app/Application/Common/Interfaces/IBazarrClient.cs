using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

public interface IBazarrClient
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task SearchAndDownloadSubtitlesAsync(string path, string language, CancellationToken cancellationToken = default);
    Task SearchAndDownloadMovieSubtitlesAsync(int radarrId, string language, CancellationToken cancellationToken = default);
    Task SearchAndDownloadSeriesSubtitlesAsync(int sonarrId, string language, CancellationToken cancellationToken = default);
}
