# SignalR Hubs - Real-time Communication

This directory contains SignalR hubs for real-time communication between the server and clients.

## NotificationHub

The `NotificationHub` provides real-time notifications and progress updates to connected clients.

### Features

- **Authentication**: All connections require authentication via JWT tokens
- **Group Subscriptions**: Clients can subscribe to specific notification groups
- **Progress Broadcasting**: Real-time updates for transcoding and library scanning
- **Redis Backplane**: Optional Redis support for multi-server deployments

### Available Methods

#### Client-to-Server Methods

1. **SubscribeToLibraryUpdates()**
   - Subscribe to library scan progress and new content notifications
   - Group: `library-updates`

2. **UnsubscribeFromLibraryUpdates()**
   - Unsubscribe from library update notifications

3. **SubscribeToTranscodingProgress(string sessionId)**
   - Subscribe to transcoding progress for a specific streaming session
   - Group: `session-{sessionId}`

4. **UnsubscribeFromTranscodingProgress(string sessionId)**
   - Unsubscribe from transcoding progress updates

5. **SubscribeToStreamingNotifications()**
   - Subscribe to general streaming notifications
   - Group: `streaming-notifications`

6. **UnsubscribeFromStreamingNotifications()**
   - Unsubscribe from streaming notifications

#### Server-to-Client Events

1. **LibraryScanProgress**
   - Sent during library scanning operations
   - Payload:
     ```json
     {
       "percentage": 45,
       "currentItem": "/path/to/movie.mkv",
       "timestamp": "2024-01-15T10:30:00Z"
     }
     ```

2. **NewContentAdded**
   - Sent when new content is added to the library
   - Payload:
     ```json
     {
       "contentId": 123,
       "title": "Movie Title",
       "contentType": "Movie",
       "timestamp": "2024-01-15T10:30:00Z"
     }
     ```

3. **TranscodingProgress**
   - Sent during active transcoding sessions (every 2 seconds)
   - Payload:
     ```json
     {
       "sessionId": "abc-123",
       "percentComplete": 45.5,
       "currentTime": 1234.5,
       "totalDuration": 2700.0,
       "speed": 1.2,
       "bitrate": 8000000,
       "fps": 24.0
     }
     ```

## Connection

### Endpoint

```
ws://localhost:5000/hubs/notifications
wss://localhost:5001/hubs/notifications
```

### Authentication

Include JWT token in the connection:

**JavaScript/TypeScript:**
```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: () => getAuthToken()
  })
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
      if (retryContext.elapsedMilliseconds < 60000) {
        return 5000; // Retry every 5 seconds for the first minute
      } else {
        return null; // Stop retrying after 1 minute
      }
    }
  })
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Subscribe to events
connection.on("LibraryScanProgress", (data) => {
  console.log(`Library scan: ${data.percentage}%`);
});

connection.on("NewContentAdded", (data) => {
  console.log(`New content: ${data.title}`);
});

connection.on("TranscodingProgress", (data) => {
  console.log(`Transcoding: ${data.percentComplete}%`);
});

// Start connection
await connection.start();

// Subscribe to library updates
await connection.invoke("SubscribeToLibraryUpdates");

// Subscribe to transcoding progress for a session
await connection.invoke("SubscribeToTranscodingProgress", sessionId);
```

**C# Client:**
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5001/hubs/notifications", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(GetAuthToken());
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<LibraryScanProgressDto>("LibraryScanProgress", (data) =>
{
    Console.WriteLine($"Library scan: {data.Percentage}%");
});

connection.On<NewContentDto>("NewContentAdded", (data) =>
{
    Console.WriteLine($"New content: {data.Title}");
});

connection.On<TranscodingProgress>("TranscodingProgress", (data) =>
{
    Console.WriteLine($"Transcoding: {data.PercentComplete}%");
});

await connection.StartAsync();
await connection.InvokeAsync("SubscribeToLibraryUpdates");
```

## Redis Backplane Configuration

For multi-server deployments, enable Redis backplane in `appsettings.json`:

```json
{
  "Lanflix": {
    "Cache": {
      "Redis": {
        "Enabled": true,
        "ConnectionString": "localhost:6379",
        "InstanceName": "lanflix:"
      }
    }
  }
}
```

When Redis is enabled, SignalR will automatically use it as a backplane to synchronize messages across multiple server instances.

### Redis Backplane Features

- **Automatic Failover**: Continues operation if Redis becomes unavailable
- **Connection Pooling**: Efficient connection management
- **Channel Prefix**: All SignalR messages use `lanflix:signalr:` prefix
- **Retry Logic**: Automatic reconnection with exponential backoff

## Performance Considerations

1. **Message Throttling**: Transcoding progress updates are throttled to every 2 seconds
2. **Group Management**: Clients are automatically removed from groups on disconnect
3. **Connection Limits**: Maximum 1 parallel invocation per client to prevent flooding
4. **Message Size**: Maximum message size is 32KB
5. **Keep-Alive**: 15-second keep-alive interval to detect disconnections

## Monitoring

Connection events are logged with the following information:
- Connection/disconnection events with user identifiers
- Failed broadcast attempts with warnings
- Progress update frequency and content

## Error Handling

- **Connection Failures**: Automatic reconnection with exponential backoff
- **Broadcast Failures**: Logged as warnings, do not interrupt service operation
- **Authentication Failures**: Connection rejected with 401 Unauthorized

## Security

- **Authentication Required**: All connections must provide valid JWT tokens
- **Authorization**: Future enhancement for role-based access to specific groups
- **CORS**: Configured to allow credentials for SignalR WebSocket connections
- **Rate Limiting**: Prevents abuse through connection limits

## Future Enhancements

1. **User-specific Notifications**: Direct messages to specific users
2. **Notification History**: Store and retrieve missed notifications
3. **Priority Queues**: Different priority levels for different notification types
4. **Analytics**: Track notification delivery and client engagement
5. **Push Notifications**: Integration with mobile push notification services
