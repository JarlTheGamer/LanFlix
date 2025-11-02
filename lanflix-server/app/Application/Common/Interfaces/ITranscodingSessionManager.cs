using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Manages transcoding sessions to prevent duplicate processes and handle concurrent requests
/// </summary>
public interface ITranscodingSessionManager
{
    /// <summary>
    /// Gets or creates a transcoding session for the given request
    /// </summary>
    /// <param name="sessionKey">Unique key identifying the session</param>
    /// <param name="sessionFactory">Factory function to create new session if needed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream result from existing or new session</returns>
    Task<StreamResult> GetOrCreateSessionAsync(
        string sessionKey,
        Func<CancellationToken, Task<StreamResult>> sessionFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a session from the manager
    /// </summary>
    /// <param name="sessionKey">Session key to remove</param>
    void RemoveSession(string sessionKey);

    /// <summary>
    /// Gets active session count
    /// </summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Gets active session count (method version for compatibility)
    /// </summary>
    /// <returns>Number of active sessions</returns>
    int GetActiveSessionCount();

    /// <summary>
    /// Cleans up expired or inactive sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();

    /// <summary>
    /// Cleans up orphaned sessions (from previous server runs)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupOrphanedSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up abandoned sessions (inactive for specified duration)
    /// </summary>
    /// <param name="inactivityThreshold">Duration of inactivity before cleanup</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupAbandonedSessionsAsync(TimeSpan inactivityThreshold, CancellationToken cancellationToken = default);
}