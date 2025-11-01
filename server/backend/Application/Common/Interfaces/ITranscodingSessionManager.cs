using Lanflix.Domain.Entities;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Manages transcoding sessions including creation, tracking, and cleanup
/// </summary>
public interface ITranscodingSessionManager
{
    /// <summary>
    /// Creates a new transcoding session with a unique ID
    /// </summary>
    /// <param name="session">The stream session to track</param>
    /// <param name="processId">Optional FFmpeg process ID if transcoding is active</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created session ID</returns>
    Task<string> CreateSessionAsync(
        StreamSession session,
        string? processId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an active session by ID
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stream session or null if not found</returns>
    Task<StreamSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last activity timestamp for a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="positionTicks">Optional current playback position</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionActivityAsync(
        string sessionId,
        long? positionTicks = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a session and performs cleanup
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EndSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<IReadOnlyList<StreamSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and cleans up orphaned sessions (e.g., after server restart)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of orphaned sessions cleaned up</returns>
    Task<int> CleanupOrphanedSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and cleans up abandoned sessions (no activity for specified duration)
    /// </summary>
    /// <param name="inactivityThreshold">Time span of inactivity before considering a session abandoned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of abandoned sessions cleaned up</returns>
    Task<int> CleanupAbandonedSessionsAsync(
        TimeSpan inactivityThreshold,
        CancellationToken cancellationToken = default);
}
