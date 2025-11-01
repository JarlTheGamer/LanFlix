using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lanflix.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time notifications and progress updates
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// Subscribe to library update notifications
    /// </summary>
    public async Task SubscribeToLibraryUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "library-updates");
    }

    /// <summary>
    /// Unsubscribe from library update notifications
    /// </summary>
    public async Task UnsubscribeFromLibraryUpdates()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "library-updates");
    }

    /// <summary>
    /// Subscribe to transcoding progress for a specific session
    /// </summary>
    /// <param name="sessionId">The session ID to monitor</param>
    public async Task SubscribeToTranscodingProgress(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    /// <summary>
    /// Unsubscribe from transcoding progress for a specific session
    /// </summary>
    /// <param name="sessionId">The session ID to stop monitoring</param>
    public async Task UnsubscribeFromTranscodingProgress(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    /// <summary>
    /// Subscribe to all streaming notifications
    /// </summary>
    public async Task SubscribeToStreamingNotifications()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "streaming-notifications");
    }

    /// <summary>
    /// Unsubscribe from all streaming notifications
    /// </summary>
    public async Task UnsubscribeFromStreamingNotifications()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "streaming-notifications");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        _logger.LogInformation("Client connected to NotificationHub: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected from NotificationHub with error: {UserId}", userId);
        }
        else
        {
            _logger.LogInformation("Client disconnected from NotificationHub: {UserId}", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
