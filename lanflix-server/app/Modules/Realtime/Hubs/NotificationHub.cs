using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Lanflix.Modules.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        if (Guid.TryParse(value, out var accountId))
            await Groups.AddToGroupAsync(Context.ConnectionId, AccountGroup(accountId));
        await base.OnConnectedAsync();
    }

    public Task SubscribeToLibraryUpdates() => Groups.AddToGroupAsync(Context.ConnectionId, "library-updates");
    public Task UnsubscribeFromLibraryUpdates() => Groups.RemoveFromGroupAsync(Context.ConnectionId, "library-updates");
    public Task SubscribeToTranscodingProgress(string sessionId) => Groups.AddToGroupAsync(Context.ConnectionId, $"transcode:{sessionId}");
    public Task UnsubscribeFromTranscodingProgress(string sessionId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"transcode:{sessionId}");
    public Task SubscribeToPlaybackNotifications() => Groups.AddToGroupAsync(Context.ConnectionId, "playback-notifications");
    public Task UnsubscribeFromPlaybackNotifications() => Groups.RemoveFromGroupAsync(Context.ConnectionId, "playback-notifications");

    public static string AccountGroup(Guid accountId) => $"account:{accountId:N}";
}
