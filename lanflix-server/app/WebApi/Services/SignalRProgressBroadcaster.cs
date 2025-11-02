using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Lanflix.WebApi.Services;

/// <summary>
/// SignalR-based implementation of real-time notification broadcaster
/// </summary>
public class SignalRProgressBroadcaster : IProgressBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRProgressBroadcaster> _logger;

    public SignalRProgressBroadcaster(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRProgressBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastProgressAsync(string sessionId, TranscodingProgress progress)
    {
        try
        {
            await _hubContext.Clients
                .Group($"session-{sessionId}")
                .SendAsync("TranscodingProgress", progress);

            _logger.LogDebug(
                "Broadcasted transcoding progress for session {SessionId}: {Percent}%",
                sessionId,
                progress.PercentComplete);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast transcoding progress");
        }
    }

    public async Task BroadcastLibraryScanProgressAsync(
        int percentage,
        string? currentItem = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group("library-updates")
                .SendAsync("LibraryScanProgress", new
                {
                    Percentage = percentage,
                    CurrentItem = currentItem,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

            _logger.LogDebug("Broadcasted library scan progress: {Percent}%", percentage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast library scan progress");
        }
    }

    public async Task BroadcastNewContentAsync(int contentId, string title, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group("library-updates")
                .SendAsync("NewContentAdded", new
                {
                    ContentId = contentId,
                    Title = title,
                    ContentType = type,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

            _logger.LogInformation(
                "Broadcasted new content notification: {Title} (ID: {ContentId})",
                title,
                contentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast new content notification");
        }
    }
}
