using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for broadcasting progress updates and notifications
/// </summary>
public interface IProgressBroadcaster
{
    /// <summary>
    /// Broadcasts progress update for a transcoding session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="progress">Progress information</param>
    /// <returns>Task representing the broadcast operation</returns>
    Task BroadcastProgressAsync(string sessionId, TranscodingProgress progress);

    /// <summary>
    /// Broadcasts new content notification
    /// </summary>
    /// <param name="contentId">Content identifier</param>
    /// <param name="title">Content title</param>
    /// <param name="type">Content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the broadcast operation</returns>
    Task BroadcastNewContentAsync(int contentId, string title, string type, CancellationToken cancellationToken = default);
}