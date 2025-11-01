using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for analyzing media files using FFprobe
/// </summary>
public interface IMediaAnalyzer
{
    /// <summary>
    /// Analyzes a media file and extracts comprehensive information
    /// </summary>
    /// <param name="filePath">Full path to the media file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MediaInfo containing all stream information</returns>
    Task<MediaInfo> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);
}
