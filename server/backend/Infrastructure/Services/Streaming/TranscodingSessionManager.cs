using System.Collections.Concurrent;
using System.Diagnostics;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Streaming;

/// <summary>
/// Manages transcoding sessions with in-memory tracking and database persistence
/// </summary>
public class TranscodingSessionManager : ITranscodingSessionManager
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TranscodingSessionManager> _logger;
    private readonly TranscodingFileCleanupService? _fileCleanupService;
    
    // In-memory cache of active sessions for fast lookup
    private readonly ConcurrentDictionary<string, SessionTrackingInfo> _activeSessions = new();

    public TranscodingSessionManager(
        IApplicationDbContext context,
        ILogger<TranscodingSessionManager> logger,
        TranscodingFileCleanupService? fileCleanupService = null)
    {
        _context = context;
        _logger = logger;
        _fileCleanupService = fileCleanupService;
    }

    public async Task<string> CreateSessionAsync(
        StreamSession session,
        string? processId = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure unique session ID
        if (string.IsNullOrEmpty(session.SessionId))
        {
            session.SessionId = Guid.NewGuid().ToString();
        }

        session.StartedAt = DateTime.UtcNow;
        session.LastActivityAt = DateTime.UtcNow;
        session.IsActive = true;
        session.TranscodingProcessId = processId;

        // Add to database
        _context.StreamSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        // Track in memory
        var trackingInfo = new SessionTrackingInfo
        {
            SessionId = session.SessionId,
            ProcessId = processId,
            StartedAt = session.StartedAt,
            LastActivityAt = session.LastActivityAt,
            Mode = session.Mode
        };

        _activeSessions.TryAdd(session.SessionId, trackingInfo);

        _logger.LogInformation(
            "Created session {SessionId} for content {ContentId}, mode: {Mode}, process: {ProcessId}",
            session.SessionId,
            session.ContentId,
            session.Mode,
            processId ?? "none");

        return session.SessionId;
    }

    public async Task<StreamSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        // Check in-memory cache first
        if (!_activeSessions.ContainsKey(sessionId))
        {
            return null;
        }

        // Fetch from database with related entities
        var session = await _context.StreamSessions
            .Include(s => s.Profile)
            .Include(s => s.Content)
            .Include(s => s.Episode)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive, cancellationToken);

        return session;
    }

    public async Task UpdateSessionActivityAsync(
        string sessionId,
        long? positionTicks = null,
        CancellationToken cancellationToken = default)
    {
        // Update in-memory tracking
        if (_activeSessions.TryGetValue(sessionId, out var trackingInfo))
        {
            trackingInfo.LastActivityAt = DateTime.UtcNow;
        }

        // Update database
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive, cancellationToken);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            
            if (positionTicks.HasValue)
            {
                session.CurrentPositionTicks = positionTicks.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Updated activity for session {SessionId}, position: {Position}",
                sessionId,
                positionTicks);
        }
    }

    public async Task EndSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ending session {SessionId}", sessionId);

        // Remove from in-memory tracking
        if (_activeSessions.TryRemove(sessionId, out var trackingInfo))
        {
            // Terminate FFmpeg process if exists
            if (!string.IsNullOrEmpty(trackingInfo.ProcessId))
            {
                await TerminateFFmpegProcessAsync(trackingInfo.ProcessId);
            }
        }

        // Clean up temporary files
        if (_fileCleanupService != null)
        {
            await _fileCleanupService.CleanupSessionFilesAsync(sessionId);
        }

        // Update database
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive, cancellationToken);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Session {SessionId} ended, duration: {Duration}",
                sessionId,
                session.EndedAt.Value - session.StartedAt);
        }
    }

    public async Task<IReadOnlyList<StreamSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sessions = await _context.StreamSessions
            .Include(s => s.Profile)
            .Include(s => s.Content)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return sessions.AsReadOnly();
    }

    public async Task<int> CleanupOrphanedSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting orphaned session cleanup");

        // Find sessions that are marked as active but have no in-memory tracking
        // This typically happens after server restart
        var orphanedSessions = await _context.StreamSessions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var cleanedCount = 0;

        foreach (var session in orphanedSessions)
        {
            // If not in memory cache, it's orphaned
            if (!_activeSessions.ContainsKey(session.SessionId))
            {
                _logger.LogWarning(
                    "Found orphaned session {SessionId}, started at {StartedAt}",
                    session.SessionId,
                    session.StartedAt);

                // Terminate any associated FFmpeg process
                if (!string.IsNullOrEmpty(session.TranscodingProcessId))
                {
                    await TerminateFFmpegProcessAsync(session.TranscodingProcessId);
                }

                // Mark as inactive
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} orphaned sessions", cleanedCount);
        }

        return cleanedCount;
    }

    public async Task<int> CleanupAbandonedSessionsAsync(
        TimeSpan inactivityThreshold,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - inactivityThreshold;
        var cleanedCount = 0;

        _logger.LogDebug(
            "Checking for abandoned sessions (inactive since {CutoffTime})",
            cutoffTime);

        // Find sessions with no recent activity
        var abandonedSessions = await _context.StreamSessions
            .Where(s => s.IsActive && s.LastActivityAt < cutoffTime)
            .ToListAsync(cancellationToken);

        foreach (var session in abandonedSessions)
        {
            _logger.LogWarning(
                "Found abandoned session {SessionId}, last activity: {LastActivity}",
                session.SessionId,
                session.LastActivityAt);

            // End the session (this will also terminate FFmpeg process)
            await EndSessionAsync(session.SessionId, cancellationToken);
            cleanedCount++;
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} abandoned sessions (inactive for {Threshold})",
                cleanedCount,
                inactivityThreshold);
        }

        return cleanedCount;
    }

    public int GetActiveSessionCount()
    {
        return _activeSessions.Count;
    }

    private async Task TerminateFFmpegProcessAsync(string processId)
    {
        try
        {
            // Try to parse process ID as integer
            if (int.TryParse(processId, out var pid))
            {
                var process = Process.GetProcessById(pid);
                
                if (!process.HasExited)
                {
                    _logger.LogInformation("Terminating FFmpeg process {ProcessId}", processId);
                    process.Kill(entireProcessTree: true);
                    
                    // Wait briefly for process to exit
                    await Task.Run(() => process.WaitForExit(5000));
                    
                    _logger.LogInformation("FFmpeg process {ProcessId} terminated", processId);
                }
            }
        }
        catch (ArgumentException)
        {
            // Process not found - already terminated
            _logger.LogDebug("FFmpeg process {ProcessId} not found (already terminated)", processId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to terminate FFmpeg process {ProcessId}",
                processId);
        }
    }

    /// <summary>
    /// Internal tracking information for active sessions
    /// </summary>
    private class SessionTrackingInfo
    {
        public required string SessionId { get; init; }
        public string? ProcessId { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime LastActivityAt { get; set; }
        public StreamingMode Mode { get; init; }
    }
}
