using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Interface for streaming strategy implementations
/// </summary>
public interface IStreamingStrategy
{
    /// <summary>
    /// The streaming mode this strategy implements
    /// </summary>
    StreamingMode Mode { get; }

    /// <summary>
    /// Priority of this strategy (lower = higher priority)
    /// DirectPlay = 1, DirectStream = 2, TranscodeVideo = 3, FullTranscode = 4
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this strategy can handle the given media and client capabilities
    /// </summary>
    /// <param name="media">Media information</param>
    /// <param name="client">Client capabilities</param>
    /// <returns>True if this strategy can handle the request</returns>
    bool CanHandle(MediaInfo media, ClientCapabilities client);

    /// <summary>
    /// Executes the streaming strategy
    /// </summary>
    /// <param name="request">Stream request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream result</returns>
    Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken);
}
