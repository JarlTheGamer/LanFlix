using Lanflix.Domain.Entities;

namespace Lanflix.Application.Common.Interfaces;

public interface IIntroScanner
{
    /// <summary>
    /// Scans a season's episodes using audio fingerprinting cross-correlation
    /// to automatically detect and save intro start/end timestamps.
    /// </summary>
    Task ScanSeasonIntrosAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default);
}
