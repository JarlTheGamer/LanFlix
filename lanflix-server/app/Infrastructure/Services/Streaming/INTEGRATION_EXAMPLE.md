# Session Management Integration Example

This document shows how to integrate the transcoding session management system into streaming workflows.

## Complete Streaming Flow with Session Management

### 1. Start a Stream

```csharp
public class StreamingController : ControllerBase
{
    private readonly ITranscodingSessionManager _sessionManager;
    private readonly IMediaAnalyzer _mediaAnalyzer;
    private readonly IStreamingStrategySelector _strategySelector;
    private readonly ITranscodingPipeline _transcodingPipeline;

    [HttpPost("stream/{contentId}/start")]
    public async Task<IActionResult> StartStream(
        int contentId,
        [FromBody] StartStreamRequest request)
    {
        // 1. Get content from database
        var content = await _context.Contents.FindAsync(contentId);
        
        // 2. Analyze media file
        var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(content.FilePath);
        
        // 3. Select streaming strategy
        var strategy = _strategySelector.SelectOptimalStrategy(
            mediaInfo,
            request.ClientCapabilities,
            request.UserPreferences);
        
        // 4. Create stream session
        var session = new StreamSession
        {
            ProfileId = request.ProfileId,
            ContentId = contentId,
            Mode = strategy.Mode,
            ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ClientUserAgent = Request.Headers.UserAgent.ToString()
        };
        
        var sessionId = await _sessionManager.CreateSessionAsync(session);
        
        // 5. Return session info to client
        return Ok(new
        {
            SessionId = sessionId,
            StreamUrl = $"/api/stream/{sessionId}/stream",
            Mode = strategy.Mode
        });
    }
}
```

### 2. Stream Media with Progress Reporting

```csharp
[HttpGet("stream/{sessionId}/stream")]
public async Task StreamMedia(string sessionId)
{
    // 1. Get session
    var session = await _sessionManager.GetSessionAsync(sessionId);
    if (session == null)
    {
        return NotFound();
    }
    
    // 2. Update activity
    await _sessionManager.UpdateSessionActivityAsync(sessionId);
    
    // 3. Build transcode request
    var transcodeRequest = new TranscodeRequest
    {
        InputPath = session.Content.FilePath,
        Mode = session.Mode,
        SourceMedia = session.Content.MediaInfo,
        SessionId = sessionId, // For progress tracking
        TotalDuration = session.Content.MediaInfo.Duration.TotalSeconds,
        TargetVideoCodec = session.TargetVideoCodec,
        TargetAudioCodec = session.TargetAudioCodec,
        TargetVideoBitrate = session.TargetBitrate,
        HwAccelMethod = _hwAccelDetector.GetPreferredMethod()
    };
    
    // 4. Stream with automatic progress reporting
    Response.ContentType = "video/mp2t";
    
    await foreach (var chunk in _transcodingPipeline.StreamAsync(
        transcodeRequest, 
        HttpContext.RequestAborted))
    {
        await Response.Body.WriteAsync(chunk, HttpContext.RequestAborted);
        
        // Update activity periodically
        await _sessionManager.UpdateSessionActivityAsync(sessionId);
    }
}
```

### 3. Update Playback Progress (Heartbeat)

```csharp
[HttpPost("stream/{sessionId}/progress")]
public async Task<IActionResult> UpdateProgress(
    string sessionId,
    [FromBody] ProgressUpdate update)
{
    await _sessionManager.UpdateSessionActivityAsync(
        sessionId,
        positionTicks: update.PositionTicks);
    
    return Ok();
}
```

### 4. Stop Stream

```csharp
[HttpDelete("stream/{sessionId}/stop")]
public async Task<IActionResult> StopStream(string sessionId)
{
    await _sessionManager.EndSessionAsync(sessionId);
    return NoContent();
}
```

## Client-Side Integration

### JavaScript/TypeScript Client

```typescript
class StreamingClient {
    private connection: signalR.HubConnection;
    private sessionId: string;
    
    async startStream(contentId: number) {
        // 1. Connect to SignalR hub
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();
        
        await this.connection.start();
        
        // 2. Start stream
        const response = await fetch(`/api/stream/${contentId}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                profileId: 1,
                clientCapabilities: {
                    supportedVideoCodecs: ['h264', 'hevc'],
                    supportedAudioCodecs: ['aac', 'mp3'],
                    maxBitrate: 8000000
                }
            })
        });
        
        const data = await response.json();
        this.sessionId = data.sessionId;
        
        // 3. Subscribe to progress updates
        await this.connection.invoke(
            "SubscribeToTranscodingProgress", 
            this.sessionId);
        
        // 4. Listen for progress
        this.connection.on("TranscodingProgress", (progress) => {
            this.onProgress(progress);
        });
        
        // 5. Set video source
        const video = document.querySelector('video');
        video.src = data.streamUrl;
        
        // 6. Start heartbeat
        this.startHeartbeat();
    }
    
    private startHeartbeat() {
        setInterval(async () => {
            const video = document.querySelector('video');
            const positionTicks = video.currentTime * 10000000; // Convert to ticks
            
            await fetch(`/api/stream/${this.sessionId}/progress`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ positionTicks })
            });
        }, 10000); // Every 10 seconds
    }
    
    private onProgress(progress: TranscodingProgress) {
        console.log(`Transcoding: ${progress.percentComplete.toFixed(1)}%`);
        console.log(`Speed: ${progress.speed.toFixed(2)}x`);
        console.log(`Time: ${progress.currentTime}/${progress.totalDuration}`);
        
        // Update UI
        this.updateProgressBar(progress.percentComplete);
    }
    
    async stopStream() {
        // 1. Stop stream
        await fetch(`/api/stream/${this.sessionId}/stop`, {
            method: 'DELETE'
        });
        
        // 2. Disconnect SignalR
        await this.connection.stop();
    }
}
```

## Background Service Behavior

### On Server Startup

```
[INFO] Session cleanup service started
[INFO] Performing startup cleanup of orphaned sessions
[WARN] Found orphaned session abc-123, started at 2024-01-01 10:00:00
[INFO] Terminating FFmpeg process 12345
[INFO] FFmpeg process 12345 terminated
[INFO] Cleaned up temp directory for session abc-123
[WARN] Cleaned up 3 orphaned sessions from previous server run
```

### During Normal Operation

```
[DEBUG] Checking for abandoned sessions (inactive since 2024-01-01 10:29:30)
[WARN] Found abandoned session xyz-789, last activity: 2024-01-01 10:29:00
[INFO] Ending session xyz-789
[INFO] Terminating FFmpeg process 67890
[INFO] FFmpeg process 67890 terminated
[INFO] Cleaning up temp files for session xyz-789
[INFO] Cleaned up temp directory for session xyz-789
[INFO] Session xyz-789 ended, duration: 00:15:30
[INFO] Cleaned up 1 abandoned sessions (inactive for 00:00:30)
```

## Monitoring Active Sessions

### Get All Active Sessions

```csharp
[HttpGet("admin/sessions")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetActiveSessions()
{
    var sessions = await _sessionManager.GetActiveSessionsAsync();
    
    return Ok(sessions.Select(s => new
    {
        s.SessionId,
        s.ProfileId,
        s.ContentId,
        s.Mode,
        s.StartedAt,
        s.LastActivityAt,
        Duration = DateTime.UtcNow - s.StartedAt,
        IsTranscoding = !string.IsNullOrEmpty(s.TranscodingProcessId)
    }));
}
```

### Force Cleanup

```csharp
[HttpPost("admin/sessions/cleanup")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ForceCleanup()
{
    var orphanedCount = await _sessionManager.CleanupOrphanedSessionsAsync();
    var abandonedCount = await _sessionManager.CleanupAbandonedSessionsAsync(
        TimeSpan.FromSeconds(30));
    
    return Ok(new
    {
        OrphanedSessionsCleaned = orphanedCount,
        AbandonedSessionsCleaned = abandonedCount
    });
}
```

## Testing the Implementation

### Unit Test Example

```csharp
[Fact]
public async Task EndSession_ShouldTerminateFFmpegProcess()
{
    // Arrange
    var session = new StreamSession
    {
        SessionId = "test-123",
        ProfileId = 1,
        ContentId = 1,
        Mode = StreamingMode.FullTranscode,
        TranscodingProcessId = "12345"
    };
    
    await _sessionManager.CreateSessionAsync(session, "12345");
    
    // Act
    await _sessionManager.EndSessionAsync("test-123");
    
    // Assert
    var retrievedSession = await _sessionManager.GetSessionAsync("test-123");
    Assert.Null(retrievedSession); // Should not be in active sessions
    
    var dbSession = await _context.StreamSessions
        .FirstAsync(s => s.SessionId == "test-123");
    Assert.False(dbSession.IsActive);
    Assert.NotNull(dbSession.EndedAt);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task SessionCleanup_ShouldDetectAbandonedSessions()
{
    // Arrange
    var session = new StreamSession
    {
        SessionId = "abandoned-123",
        ProfileId = 1,
        ContentId = 1,
        LastActivityAt = DateTime.UtcNow.AddMinutes(-5) // 5 minutes ago
    };
    
    await _sessionManager.CreateSessionAsync(session);
    
    // Act
    var cleanedCount = await _sessionManager.CleanupAbandonedSessionsAsync(
        TimeSpan.FromSeconds(30));
    
    // Assert
    Assert.Equal(1, cleanedCount);
}
```

## Performance Considerations

### Memory Usage
- In-memory session tracking: ~1KB per session
- 1000 concurrent sessions: ~1MB memory
- Database queries are optimized with indexes

### Database Queries
- Session creation: 1 INSERT
- Activity update: 1 UPDATE
- Session end: 1 UPDATE + cleanup operations
- Cleanup check: 1 SELECT (filtered by LastActivityAt)

### SignalR Overhead
- Progress updates: ~200 bytes per message
- Throttled to 1 update per 2 seconds
- Minimal CPU impact

## Troubleshooting

### Sessions Not Cleaning Up
1. Check background service is running
2. Verify database connection
3. Check logs for errors
4. Manually trigger cleanup via admin endpoint

### Progress Not Broadcasting
1. Verify SignalR connection
2. Check client subscription
3. Verify IProgressBroadcaster is registered
4. Check for firewall/proxy issues

### FFmpeg Processes Not Terminating
1. Check process permissions
2. Verify process ID is correct
3. Check for zombie processes
4. Manually kill processes if needed
