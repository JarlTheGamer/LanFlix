# Transcoding Session Management

This document describes the transcoding session management implementation for the Lanflix backend.

## Overview

The session management system tracks active streaming sessions, manages FFmpeg transcoding processes, and provides real-time progress updates to clients via SignalR.

## Components

### 1. TranscodingSessionManager

**Location**: `Infrastructure/Services/Streaming/TranscodingSessionManager.cs`

**Purpose**: Central manager for all transcoding sessions with dual tracking (in-memory + database).

**Key Features**:
- Creates sessions with unique IDs
- Tracks active sessions in memory for fast lookup
- Persists session data to database
- Updates session activity timestamps
- Ends sessions and performs cleanup
- Detects and cleans up orphaned sessions (after server restart)
- Detects and cleans up abandoned sessions (no activity for 30 seconds)
- Terminates associated FFmpeg processes when sessions end

**Usage**:
```csharp
// Inject the service
private readonly ITranscodingSessionManager _sessionManager;

// Create a session
var sessionId = await _sessionManager.CreateSessionAsync(session, processId);

// Update activity (heartbeat)
await _sessionManager.UpdateSessionActivityAsync(sessionId, positionTicks);

// End session
await _sessionManager.EndSessionAsync(sessionId);

// Get active sessions
var activeSessions = await _sessionManager.GetActiveSessionsAsync();
```

### 2. SessionCleanupService

**Location**: `Infrastructure/Services/BackgroundJobs/SessionCleanupService.cs`

**Purpose**: Background service that monitors and cleans up abandoned sessions.

**Key Features**:
- Runs as a hosted background service
- Performs initial cleanup of orphaned sessions on startup
- Checks for abandoned sessions every 30 seconds
- Considers sessions abandoned if no activity for 30 seconds
- Automatically terminates FFmpeg processes for dead sessions

**Configuration**:
- Check interval: 30 seconds
- Inactivity threshold: 30 seconds

### 3. TranscodingFileCleanupService

**Location**: `Infrastructure/Services/Streaming/TranscodingFileCleanupService.cs`

**Purpose**: Manages temporary transcoding files.

**Key Features**:
- Creates session-specific temporary directories
- Cleans up files when sessions end
- Removes old temporary files based on age
- Tracks total temporary file size

**Configuration**:
```json
{
  "Lanflix": {
    "Transcoding": {
      "TempPath": "D:/Temp/Transcoding"
    }
  }
}
```

### 4. FFmpegProgressParser

**Location**: `Infrastructure/Services/FFmpeg/FFmpegProgressParser.cs`

**Purpose**: Parses FFmpeg stderr output to extract progress information.

**Extracted Metrics**:
- Frame number
- Frames per second (FPS)
- Bitrate
- Output size
- Current time position
- Processing speed (relative to playback)
- Percent complete

### 5. TranscodingPipelineWithProgress

**Location**: `Infrastructure/Services/FFmpeg/TranscodingPipelineWithProgress.cs`

**Purpose**: Enhanced transcoding pipeline with real-time progress reporting.

**Key Features**:
- Monitors FFmpeg stderr for progress information
- Parses progress data using FFmpegProgressParser
- Broadcasts progress via SignalR every 2 seconds
- Sends final 100% progress update on completion
- Falls back to basic logging if SignalR is unavailable

### 6. NotificationHub

**Location**: `WebApi/Hubs/NotificationHub.cs`

**Purpose**: SignalR hub for real-time client notifications.

**Client Methods**:
- `SubscribeToLibraryUpdates()` - Subscribe to library scan notifications
- `SubscribeToTranscodingProgress(sessionId)` - Subscribe to session progress
- `SubscribeToStreamingNotifications()` - Subscribe to all streaming events

**Server Events**:
- `TranscodingProgress` - Sent every 2 seconds during transcoding
- `LibraryScanProgress` - Sent during library scanning
- `NewContentAdded` - Sent when new content is added

## SignalR Integration

### Client Connection

```javascript
// Connect to the hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .build();

// Subscribe to transcoding progress
await connection.invoke("SubscribeToTranscodingProgress", sessionId);

// Listen for progress updates
connection.on("TranscodingProgress", (progress) => {
    console.log(`Progress: ${progress.percentComplete}%`);
    console.log(`Speed: ${progress.speed}x`);
    console.log(`Time: ${progress.currentTime}/${progress.totalDuration}`);
});

await connection.start();
```

### Progress Data Structure

```json
{
  "sessionId": "abc-123",
  "frame": 1234,
  "fps": 45.2,
  "bitrate": 8000000,
  "totalSize": 10485760,
  "currentTime": 12.5,
  "totalDuration": 120.0,
  "percentComplete": 10.4,
  "speed": 1.87,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Database Schema

The `StreamSession` entity tracks sessions in the database:

```csharp
public class StreamSession
{
    public string SessionId { get; set; }
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public StreamingMode Mode { get; set; }
    public string? TranscodingProcessId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastActivityAt { get; set; }
    public long CurrentPositionTicks { get; set; }
    // ... additional properties
}
```

## Cleanup Behavior

### Orphaned Sessions
- Detected on server startup
- Sessions marked as active but not in memory
- Typically caused by server crashes or restarts
- FFmpeg processes are terminated
- Sessions marked as inactive

### Abandoned Sessions
- Detected every 30 seconds by background service
- Sessions with no activity for 30+ seconds
- Client disconnected without proper cleanup
- FFmpeg processes are terminated
- Temporary files are deleted
- Sessions marked as inactive

## Requirements Satisfied

This implementation satisfies the following requirements:

- **8.1**: Session creation with unique IDs ✓
- **8.2**: Detection of client disconnection within 30 seconds ✓
- **8.3**: Termination of abandoned FFmpeg processes ✓
- **8.4**: Cleanup of temporary transcoding files ✓
- **8.5**: Registry of active transcoding sessions ✓
- **8.6**: Cleanup of orphaned processes on restart ✓
- **8.7**: Progress reporting during transcoding ✓
- **12.4**: Broadcasting progress via SignalR ✓

## Testing

To test the session management:

1. Start a streaming session
2. Monitor the session in the database
3. Disconnect the client without stopping the stream
4. Wait 30 seconds
5. Verify the session is cleaned up automatically
6. Check that FFmpeg process is terminated
7. Verify temporary files are deleted

## Future Enhancements

- Add session statistics and metrics
- Implement session priority management
- Add support for session migration between servers
- Implement adaptive quality based on network conditions
- Add session recording/replay for debugging
