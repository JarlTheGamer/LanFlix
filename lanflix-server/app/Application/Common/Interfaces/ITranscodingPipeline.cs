using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for transcoding media files using FFmpeg
/// </summary>
public interface ITranscodingPipeline
{
    /// <summary>
    /// Streams transcoded media data
    /// </summary>
    /// <param name="request">Transcoding request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of memory chunks</returns>
    IAsyncEnumerable<ReadOnlyMemory<byte>> StreamAsync(
        TranscodeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcodes media to a physical file
    /// </summary>
    /// <param name="request">Transcoding request parameters</param>
    /// <param name="outputPath">Path to the output file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TranscodeToFileAsync(
        TranscodeRequest request,
        string outputPath,
        CancellationToken cancellationToken = default);
}
