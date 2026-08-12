using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Lanflix.Infrastructure.Services.Playback.Ffmpeg;
using Lanflix.Infrastructure.Services.Playback.Planning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Playback.Sessions;

internal sealed record ManagedPlaybackSession(string Id, string Manifest);
internal sealed record ManagedSessionDiagnostics(
    string Id, string ClientType, string Method, string Reason,
    DateTime CreatedAtUtc, DateTime LastAccessUtc, int SegmentCount,
    int CachedSegments, bool FfmpegRunning);

internal sealed class ManagedTranscodeSessionManager : BackgroundService
{
    private const double SegmentDuration = 6;
    private const int BatchSize = 8;
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _keys = new();
    private readonly SemaphoreSlim _processSlots = new(2, 2);
    private readonly FfmpegCommandBuilder _commands;
    private readonly ILogger<ManagedTranscodeSessionManager> _logger;
    private readonly string _ffmpegPath;
    private readonly string _root;

    public ManagedTranscodeSessionManager(FfmpegCommandBuilder commands, ILogger<ManagedTranscodeSessionManager> logger)
    {
        _commands = commands;
        _logger = logger;
        _ffmpegPath = FindExecutable("ffmpeg.exe", "ffmpeg");
        _root = Path.Combine(Path.GetTempPath(), "lanflix", "transcodes");
        Directory.CreateDirectory(_root);
        CleanupOrphans();
    }

    public ManagedPlaybackSession GetOrCreate(string sourcePath, string clientType, PlaybackPlan plan)
    {
        var stamp = File.GetLastWriteTimeUtc(sourcePath).Ticks;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{sourcePath}|{stamp}|{clientType}|{plan.Method}|{plan.Width}x{plan.Height}|{plan.OutputVideoCodec}")))[..24].ToLowerInvariant();
        if (_keys.TryGetValue(key, out var existingId) && _sessions.TryGetValue(existingId, out var existing))
        {
            existing.Touch();
            return new(existing.Id, BuildManifest(existing));
        }

        var id = Guid.NewGuid().ToString("N");
        var directory = Path.Combine(_root, id);
        Directory.CreateDirectory(directory);
        var session = new Session(id, key, sourcePath, clientType, directory, plan);
        _sessions[id] = session;
        _keys[key] = id;
        _logger.LogInformation("Created managed playback session {SessionId}: {Method} ({Reason})", id, plan.Method, plan.Reason);
        BeginWarmup(session);
        return new(id, BuildManifest(session));
    }

    public async Task<string?> GetSegmentAsync(string sessionId, int segmentIndex, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(sessionId, out var session) || segmentIndex < 0 || segmentIndex >= session.SegmentCount)
            return null;
        session.Touch();
        var path = session.SegmentPath(segmentIndex);
        if (File.Exists(path) && new FileInfo(path).Length > 0) return path;

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0) return path;
            // Align every request to the same block. Sequential Media3
            // requests can then reuse one FFmpeg run instead of creating
            // overlapping 0-7, 4-11, 7-14 batches.
            var firstSegment = segmentIndex / BatchSize * BatchSize;
            var remaining = session.SegmentCount - firstSegment;
            var batchCount = Math.Min(BatchSize, remaining);
            await _processSlots.WaitAsync(cancellationToken);
            try
            {
                // A disconnected HTTP request must not kill useful shared
                // transcode work. The session owns FFmpeg and server shutdown
                // or explicit session deletion owns its cancellation.
                await GenerateBatchAsync(session, firstSegment, batchCount, false, session.Lifetime.Token);
            }
            finally
            {
                _processSlots.Release();
            }
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException($"FFmpeg did not create requested segment {segmentIndex}.");
            return path;
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private void BeginWarmup(Session session)
    {
        session.Warmup = Task.Run(async () =>
        {
            await session.Gate.WaitAsync(session.Lifetime.Token);
            try
            {
                if (File.Exists(session.SegmentPath(0))) return;
                await _processSlots.WaitAsync(session.Lifetime.Token);
                try
                {
                    await GenerateBatchAsync(session, 0, Math.Min(BatchSize, session.SegmentCount), false,
                        session.Lifetime.Token);
                }
                finally { _processSlots.Release(); }
            }
            finally { session.Gate.Release(); }
        });
        _ = session.Warmup.ContinueWith(task =>
            _logger.LogWarning(task.Exception, "Playback warmup failed for session {SessionId}", session.Id),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }

    public Task StopAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session)) Remove(session);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ManagedSessionDiagnostics> GetDiagnostics() => _sessions.Values
        .OrderByDescending(session => session.LastAccessUtc)
        .Select(session => new ManagedSessionDiagnostics(
            session.Id, session.ClientType, session.Plan.Method.ToString(), session.Plan.Reason,
            session.CreatedAtUtc, session.LastAccessUtc, session.SegmentCount,
            Directory.Exists(session.Directory) ? Directory.EnumerateFiles(session.Directory, "segment-*.ts").Count() : 0,
            session.Process is { HasExited: false }))
        .ToArray();

    private async Task GenerateBatchAsync(Session session, int firstSegment, int count, bool softwareFallback, CancellationToken ct)
    {
        for (var index = firstSegment; index < firstSegment + count; index++)
        {
            var oldSegment = session.SegmentPath(index);
            if (File.Exists(oldSegment)) File.Delete(oldSegment);
        }
        var spec = new FfmpegSegmentBatch(session.SourcePath, session.Directory, firstSegment, count,
            SegmentDuration, session.Plan);
        var arguments = _commands.BuildSegmentBatch(spec, softwareFallback);
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        _logger.LogInformation("Starting FFmpeg batch for session {SessionId}, segments {First}-{Last}, fallback={Fallback}",
            session.Id, firstSegment, firstSegment + count - 1, softwareFallback);
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        session.Process = process;
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
            throw;
        }
        finally
        {
            session.Process = null;
        }
        var stderr = await stderrTask;
        if (process.ExitCode == 0) return;

        if (!softwareFallback && session.Plan.HardwareAcceleration != Lanflix.Domain.ValueObjects.HwAccelMethod.None)
        {
            _logger.LogWarning("Hardware FFmpeg batch failed for {SessionId}; retrying in software. Exit={ExitCode}: {Error}",
                session.Id, process.ExitCode, Tail(stderr));
            await GenerateBatchAsync(session, firstSegment, count, true, ct);
            return;
        }
        throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}: {Tail(stderr)}");
    }

    private static string BuildManifest(Session session)
    {
        var text = new StringBuilder();
        text.AppendLine("#EXTM3U");
        text.AppendLine("#EXT-X-VERSION:3");
        text.AppendLine($"#EXT-X-TARGETDURATION:{(int)SegmentDuration}");
        text.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        text.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
        text.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");
        for (var index = 0; index < session.SegmentCount; index++)
        {
            var start = index * SegmentDuration;
            var length = Math.Min(SegmentDuration, session.DurationSeconds - start);
            text.AppendLine($"#EXTINF:{length.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)},");
            text.AppendLine($"/api/v2/playback/sessions/{session.Id}/segments/{index}.ts");
        }
        text.AppendLine("#EXT-X-ENDLIST");
        return text.ToString();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(20);
            foreach (var session in _sessions.Values.Where(item => item.LastAccessUtc < cutoff).ToArray())
                if (_sessions.TryRemove(session.Id, out var removed)) Remove(removed);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var session in _sessions.Values) Remove(session);
        _sessions.Clear();
        _keys.Clear();
        return base.StopAsync(cancellationToken);
    }

    private void Remove(Session session)
    {
        _keys.TryRemove(session.Key, out _);
        session.Lifetime.Cancel();
        try { if (session.Process is { HasExited: false }) session.Process.Kill(true); } catch { }
        try { if (Directory.Exists(session.Directory)) Directory.Delete(session.Directory, true); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not clean playback session directory {Directory}", session.Directory); }
    }

    private static string FindExecutable(params string[] names)
    {
        var candidates = names.SelectMany(name => new[]
        {
            Path.Combine(AppContext.BaseDirectory, name),
            Path.Combine("C:\\ffmpeg\\bin", name),
            name
        });
        foreach (var candidate in candidates)
        {
            if (Path.IsPathRooted(candidate) && File.Exists(candidate)) return candidate;
            if (!Path.IsPathRooted(candidate)) return candidate;
        }
        throw new FileNotFoundException("FFmpeg was not found.");
    }

    private static string Tail(string text) => text.Length <= 2000 ? text : text[^2000..];

    private void CleanupOrphans()
    {
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            try
            {
                var info = new DirectoryInfo(directory);
                if (info.LastWriteTimeUtc < DateTime.UtcNow - TimeSpan.FromHours(1))
                    info.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove orphaned transcode directory {Directory}", directory);
            }
        }
    }

    private sealed class Session(string id, string key, string sourcePath, string clientType, string directory, PlaybackPlan plan)
    {
        public string Id { get; } = id;
        public string Key { get; } = key;
        public string SourcePath { get; } = sourcePath;
        public string ClientType { get; } = clientType;
        public string Directory { get; } = directory;
        public PlaybackPlan Plan { get; } = plan;
        public double DurationSeconds { get; } = Math.Max(0.001, plan.Media.Duration.TotalSeconds);
        public int SegmentCount { get; } = Math.Max(1, (int)Math.Ceiling(plan.Media.Duration.TotalSeconds / SegmentDuration));
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public CancellationTokenSource Lifetime { get; } = new();
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
        public DateTime LastAccessUtc { get; private set; } = DateTime.UtcNow;
        public Process? Process { get; set; }
        public Task? Warmup { get; set; }
        public void Touch() => LastAccessUtc = DateTime.UtcNow;
        public string SegmentPath(int index) => Path.Combine(Directory, $"segment-{index:D5}.ts");
    }
}
