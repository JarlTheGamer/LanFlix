using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Interface for broadcasting real-time notifications to clients
/// </summary>
public interface IProgressBroadcaster
{
    /// <summary>
    /// Broadcasts transcoding progress to subscribed clients
    /// </summary>
    /// <param name="progress">The progress information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastProgressAsync(TranscodingProgress progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts library scan progress to subscribed clients
    /// </summary>
    /// <param name="percentage">Percentage complete (0-100)</param>
    /// <param name="currentItem">Current item being scanned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastLibraryScanProgressAsync(int percentage, string? currentItem = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts notification when new content is added to the library
    /// </summary>
    /// <param name="contentId">The ID of the newly added content</param>
    /// <param name="title">The title of the content</param>
    /// <param name="contentType">The type of content (Movie, Series)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastNewContentAsync(int contentId, string title, string contentType, CancellationToken cancellationToken = default);
}
