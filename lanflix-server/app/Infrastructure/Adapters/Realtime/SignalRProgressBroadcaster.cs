using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Modules.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Lanflix.Infrastructure.Adapters.Realtime;

internal sealed class SignalRProgressBroadcaster(IHubContext<NotificationHub> hub) : IProgressBroadcaster
{
    public Task BroadcastProgressAsync(string sessionId, TranscodingProgress progress) =>
        hub.Clients.Group($"transcode:{sessionId}").SendAsync("TranscodingProgress", progress);

    public Task BroadcastLibraryScanProgressAsync(int percentage, string? currentItem = null, CancellationToken cancellationToken = default) =>
        hub.Clients.Group("library-updates").SendAsync("LibraryScanProgress", new { percentage, currentItem, timestampUtc = DateTime.UtcNow }, cancellationToken);

    public Task BroadcastNewContentAsync(int contentId, string title, string type, CancellationToken cancellationToken = default) =>
        hub.Clients.Group("library-updates").SendAsync("NewContentAdded", new { contentId, title, type, timestampUtc = DateTime.UtcNow }, cancellationToken);
}
