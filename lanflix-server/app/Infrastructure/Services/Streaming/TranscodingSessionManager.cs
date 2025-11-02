using System.Collections.Concurrent;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Streaming;

/// <summary>
/// Manages transcoding sessions to prevent duplicate processes and handle concurrent requests
/// </summary>
public class TranscodingSessionManager : ITranscodingSessionManager, IDisposable
{
    private readonly ILogger<TranscodingSessionManager> _logger;
    private readonly ConcurrentDictionary<string, SessionInfo> _activeSessions;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
    private bool _disposed;

    public TranscodingSessionManager(ILogger<TranscodingSessionManager> logger)
    {
        _logger = logger;
        _activeSessions = new ConcurrentDictionary<string, SessionInfo>();
        
        // Cleanup expired sessions every 5 minutes
        _cleanupTimer = new Timer(async _ => await CleanupExpiredSessionsAsync(), 
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public int ActiveSessionCount => _activeSessions.Count;

    public int GetActiveSessionCount() => ActiveSessionCount;

    public async Task<StreamResult> GetOrCreateSessionAsync(
        string sessionKey,
        Func<CancellationToken, Task<StreamResult>> sessionFactory,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TranscodingSessionManager));

        // Try to get existing session
        if (_activeSessions.TryGetValue(sessionKey, out var existingSession))
        {
            _logger.LogInformation("Reusing existing transcoding session: {SessionKey}", sessionKey);
            existingSession.LastAccessed = DateTime.UtcNow;
            existingSession.AccessCount++;
            
            // Return the existing stream result
            return existingSession.StreamResult;
        }

        _logger.LogInformation("Creating new transcoding session: {SessionKey}", sessionKey);

        try
        {
            // Create new session
            var streamResult = await sessionFactory(cancellationToken);
            
            var sessionInfo = new SessionInfo
            {
                SessionKey = sessionKey,
                StreamResult = streamResult,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 1
            };

            // Store the session
            _activeSessions.TryAdd(sessionKey, sessionInfo);
            
            _logger.LogInformation("Created transcoding session: {SessionKey}, Active sessions: {Count}", 
                sessionKey, _activeSessions.Count);

            return streamResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create transcoding session: {SessionKey}", sessionKey);
            throw;
        }
    }

    public void RemoveSession(string sessionKey)
    {
        if (_activeSessions.TryRemove(sessionKey, out var session))
        {
            _logger.LogInformation("Removed transcoding session: {SessionKey}, Active sessions: {Count}", 
                sessionKey, _activeSessions.Count);
            
            // Dispose the stream if it's disposable
            if (session.StreamResult.DataStream is IDisposable disposableStream)
            {
                try
                {
                    disposableStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing stream for session: {SessionKey}", sessionKey);
                }
            }
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        if (_disposed)
            return;

        var expiredSessions = new List<string>();
        var cutoffTime = DateTime.UtcNow - _sessionTimeout;

        foreach (var kvp in _activeSessions)
        {
            if (kvp.Value.LastAccessed < cutoffTime)
            {
                expiredSessions.Add(kvp.Key);
            }
        }

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaning up {Count} expired transcoding sessions", expiredSessions.Count);
            
            foreach (var sessionKey in expiredSessions)
            {
                RemoveSession(sessionKey);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<int> CleanupOrphanedSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return 0;

        // For now, just clean up all existing sessions as they're from a previous run
        var sessionCount = _activeSessions.Count;
        
        if (sessionCount > 0)
        {
            _logger.LogInformation("Cleaning up {Count} orphaned sessions from previous server run", sessionCount);
            
            foreach (var sessionKey in _activeSessions.Keys.ToList())
            {
                RemoveSession(sessionKey);
            }
        }

        await Task.CompletedTask;
        return sessionCount;
    }

    public async Task<int> CleanupAbandonedSessionsAsync(TimeSpan inactivityThreshold, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return 0;

        var abandonedSessions = new List<string>();
        var cutoffTime = DateTime.UtcNow - inactivityThreshold;

        foreach (var kvp in _activeSessions)
        {
            if (kvp.Value.LastAccessed < cutoffTime)
            {
                abandonedSessions.Add(kvp.Key);
            }
        }

        if (abandonedSessions.Count > 0)
        {
            _logger.LogInformation("Cleaning up {Count} abandoned transcoding sessions", abandonedSessions.Count);
            
            foreach (var sessionKey in abandonedSessions)
            {
                RemoveSession(sessionKey);
            }
        }

        await Task.CompletedTask;
        return abandonedSessions.Count;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _cleanupTimer?.Dispose();
        
        // Clean up all active sessions
        foreach (var sessionKey in _activeSessions.Keys.ToList())
        {
            RemoveSession(sessionKey);
        }
        
        _activeSessions.Clear();
        
        _logger.LogInformation("TranscodingSessionManager disposed");
    }

    private class SessionInfo
    {
        public required string SessionKey { get; init; }
        public required StreamResult StreamResult { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }
}