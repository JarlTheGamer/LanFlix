using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Simple progress broadcaster that logs progress updates and notifications
/// </summary>
public class SimpleProgressBroadcaster : IProgressBroadcaster
{
    private readonly ILogger<SimpleProgressBroadcaster> _logger;

    public SimpleProgressBroadcaster(ILogger<SimpleProgressBroadcaster> logger)
    {
        _logger = logger;
    }

    public Task BroadcastProgressAsync(string sessionId, TranscodingProgress progress)
    {
        _logger.LogInformation("Transcoding progress for session {SessionId}: {Progress}% - {CurrentTime}s",
            sessionId, progress.PercentComplete, progress.CurrentTime);

        // In a real implementation, this would broadcast to SignalR clients or similar
        return Task.CompletedTask;
    }

    public Task BroadcastNewContentAsync(int contentId, string title, string type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("New content added: {ContentId} - {Title} ({Type})", contentId, title, type);

        // In a real implementation, this would broadcast to SignalR clients or similar
        return Task.CompletedTask;
    }
}