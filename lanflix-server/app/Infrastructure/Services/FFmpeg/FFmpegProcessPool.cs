using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Manages a pool of FFmpeg processes for efficient resource utilization
/// </summary>
public class FFmpegProcessPool : IDisposable
{
    private readonly ILogger<FFmpegProcessPool> _logger;
    private readonly ConcurrentDictionary<string, ProcessInfo> _activeProcesses;
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly int _maxConcurrentProcesses;
    private bool _disposed;

    public FFmpegProcessPool(
        ILogger<FFmpegProcessPool> logger,
        int maxConcurrentProcesses = 5)
    {
        _logger = logger;
        _maxConcurrentProcesses = maxConcurrentProcesses;
        _activeProcesses = new ConcurrentDictionary<string, ProcessInfo>();
        _poolSemaphore = new SemaphoreSlim(maxConcurrentProcesses, maxConcurrentProcesses);
    }

    /// <summary>
    /// Acquires a slot in the process pool
    /// </summary>
    /// <param name="processId">Unique identifier for the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process slot that must be disposed when done</returns>
    public async Task<ProcessSlot> AcquireSlotAsync(string processId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Acquiring process slot for {ProcessId}", processId);

        await _poolSemaphore.WaitAsync(cancellationToken);

        var processInfo = new ProcessInfo
        {
            ProcessId = processId,
            StartTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow
        };

        if (!_activeProcesses.TryAdd(processId, processInfo))
        {
            _poolSemaphore.Release();
            throw new InvalidOperationException($"Process {processId} is already active");
        }

        _logger.LogInformation(
            "Process slot acquired for {ProcessId}. Active processes: {ActiveCount}/{MaxCount}",
            processId,
            _activeProcesses.Count,
            _maxConcurrentProcesses);

        return new ProcessSlot(this, processId, processInfo);
    }

    /// <summary>
    /// Releases a process slot
    /// </summary>
    internal void ReleaseSlot(string processId)
    {
        if (_activeProcesses.TryRemove(processId, out var processInfo))
        {
            var duration = DateTime.UtcNow - processInfo.StartTime;
            
            _logger.LogInformation(
                "Process slot released for {ProcessId}. Duration: {Duration:F2}s. Active processes: {ActiveCount}/{MaxCount}",
                processId,
                duration.TotalSeconds,
                _activeProcesses.Count,
                _maxConcurrentProcesses);

            _poolSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates the last activity time for a process
    /// </summary>
    public void UpdateActivity(string processId)
    {
        if (_activeProcesses.TryGetValue(processId, out var processInfo))
        {
            processInfo.LastActivityTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets information about all active processes
    /// </summary>
    public IReadOnlyCollection<ProcessInfo> GetActiveProcesses()
    {
        return _activeProcesses.Values.ToList();
    }

    /// <summary>
    /// Performs health check on active processes
    /// </summary>
    public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleProcesses = new List<string>();
        var staleThreshold = TimeSpan.FromSeconds(30);

        foreach (var kvp in _activeProcesses)
        {
            var timeSinceActivity = now - kvp.Value.LastActivityTime;
            if (timeSinceActivity > staleThreshold)
            {
                staleProcesses.Add(kvp.Key);
                _logger.LogWarning(
                    "Stale process detected: {ProcessId}, Last activity: {TimeSinceActivity:F2}s ago",
                    kvp.Key,
                    timeSinceActivity.TotalSeconds);
            }
        }

        var result = new HealthCheckResult
        {
            IsHealthy = staleProcesses.Count == 0,
            ActiveProcessCount = _activeProcesses.Count,
            MaxProcessCount = _maxConcurrentProcesses,
            StaleProcesses = staleProcesses,
            AvailableSlots = _poolSemaphore.CurrentCount
        };

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Terminates stale processes that haven't had activity
    /// </summary>
    public async Task CleanupStaleProcessesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleProcesses = _activeProcesses
            .Where(kvp => now - kvp.Value.LastActivityTime > staleThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var processId in staleProcesses)
        {
            _logger.LogWarning("Cleaning up stale process: {ProcessId}", processId);
            
            if (_activeProcesses.TryGetValue(processId, out var processInfo) && processInfo.Process != null)
            {
                try
                {
                    if (!processInfo.Process.HasExited)
                    {
                        processInfo.Process.Kill(entireProcessTree: true);
                        _logger.LogInformation("Terminated stale process: {ProcessId}", processId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to terminate stale process: {ProcessId}", processId);
                }
            }

            ReleaseSlot(processId);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing FFmpeg process pool");

        // Terminate all active processes
        foreach (var kvp in _activeProcesses)
        {
            if (kvp.Value.Process != null && !kvp.Value.Process.HasExited)
            {
                try
                {
                    kvp.Value.Process.Kill(entireProcessTree: true);
                    _logger.LogInformation("Terminated process on disposal: {ProcessId}", kvp.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to terminate process on disposal: {ProcessId}", kvp.Key);
                }
            }
        }

        _activeProcesses.Clear();
        _poolSemaphore.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Represents a slot in the process pool
/// </summary>
public class ProcessSlot : IDisposable
{
    private readonly FFmpegProcessPool _pool;
    private readonly string _processId;
    private readonly ProcessInfo _processInfo;
    private bool _disposed;

    internal ProcessSlot(FFmpegProcessPool pool, string processId, ProcessInfo processInfo)
    {
        _pool = pool;
        _processId = processId;
        _processInfo = processInfo;
    }

    /// <summary>
    /// Associates a process with this slot
    /// </summary>
    public void AttachProcess(Process process)
    {
        _processInfo.Process = process;
    }

    /// <summary>
    /// Updates the last activity time
    /// </summary>
    public void UpdateActivity()
    {
        _pool.UpdateActivity(_processId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pool.ReleaseSlot(_processId);
        _disposed = true;
    }
}

/// <summary>
/// Information about an active process
/// </summary>
public class ProcessInfo
{
    public required string ProcessId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime LastActivityTime { get; set; }
    public Process? Process { get; set; }
}

/// <summary>
/// Result of a health check
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; init; }
    public int ActiveProcessCount { get; init; }
    public int MaxProcessCount { get; init; }
    public int AvailableSlots { get; init; }
    public List<string> StaleProcesses { get; init; } = new();
}
