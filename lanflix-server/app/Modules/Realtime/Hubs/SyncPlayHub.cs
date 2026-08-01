using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Realtime;

[Authorize]
public sealed class SyncPlayHub(IRealtimeDbContext db, SyncPlayConnectionRegistry connections) : Hub
{
    public async Task<SyncPlayRoomDto> CreateRoom(int contentId, string contentType, int? episodeId)
    {
        if (contentId <= 0 || contentType is not ("movie" or "series" or "episode"))
            throw new HubException("Invalid media selection.");

        SyncPlayRoom room;
        do
        {
            var code = $"SYNC-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            room = SyncPlayRoom.Create(code, AccountId(), contentId, contentType, episodeId);
        } while (await db.SyncPlayRooms.AnyAsync(item => item.Code == room.Code, Context.ConnectionAborted));

        db.SyncPlayRooms.Add(room);
        await db.SaveChangesAsync(Context.ConnectionAborted);
        connections.Join(Context.ConnectionId, room.Code);
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(room.Code), Context.ConnectionAborted);
        return room.ToDto();
    }

    public async Task<SyncPlayRoomDto?> JoinRoom(string code)
    {
        var normalized = NormalizeCode(code);
        var room = await db.SyncPlayRooms.AsNoTracking().SingleOrDefaultAsync(
            item => item.Code == normalized && item.ExpiresAtUtc > DateTime.UtcNow,
            Context.ConnectionAborted);
        if (room is null) return null;

        connections.Join(Context.ConnectionId, room.Code);
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(room.Code), Context.ConnectionAborted);
        await Clients.OthersInGroup(Group(room.Code)).SendAsync("ParticipantJoined", AccountId(), Context.ConnectionAborted);
        return room.ToDto();
    }

    public async Task<SyncPlayRoomDto?> GetCurrentRoom()
    {
        if (!connections.TryGetRoom(Context.ConnectionId, out var code)) return null;
        return (await FindActiveRoomAsync(code))?.ToDto();
    }

    public async Task SyncPlayback(double positionSeconds, bool isPlaying, double playbackRate)
    {
        var room = await CurrentRoomAsync();
        if (room is null || room.HostAccountId != AccountId()) return;

        room.Synchronize(positionSeconds, isPlaying, playbackRate);
        await db.SaveChangesAsync(Context.ConnectionAborted);
        await Clients.OthersInGroup(Group(room.Code)).SendAsync("PlaybackStateSynced", room.ToDto(), Context.ConnectionAborted);
    }

    public async Task SendChatMessage(string message)
    {
        var room = await CurrentRoomAsync();
        var trimmed = message.Trim();
        if (room is null || trimmed.Length == 0) return;
        if (trimmed.Length > 500) trimmed = trimmed[..500];
        await Clients.Group(Group(room.Code)).SendAsync("ChatMessageReceived",
            new { accountId = AccountId(), message = trimmed, timestampUtc = DateTime.UtcNow },
            Context.ConnectionAborted);
    }

    public async Task CloseRoom()
    {
        var room = await CurrentRoomAsync();
        if (room is null || room.HostAccountId != AccountId()) return;
        db.SyncPlayRooms.Remove(room);
        await db.SaveChangesAsync(Context.ConnectionAborted);
        connections.Leave(Context.ConnectionId, out _);
        await Clients.Group(Group(room.Code)).SendAsync("RoomClosed", cancellationToken: Context.ConnectionAborted);
    }

    public async Task LeaveRoom()
    {
        if (!connections.Leave(Context.ConnectionId, out var code)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(code), Context.ConnectionAborted);
        await Clients.Group(Group(code)).SendAsync("ParticipantLeft", AccountId(), Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveRoom();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<SyncPlayRoom?> CurrentRoomAsync() =>
        connections.TryGetRoom(Context.ConnectionId, out var code) ? await FindActiveRoomAsync(code) : null;

    private Task<SyncPlayRoom?> FindActiveRoomAsync(string code) => db.SyncPlayRooms.SingleOrDefaultAsync(
        item => item.Code == code && item.ExpiresAtUtc > DateTime.UtcNow,
        Context.ConnectionAborted);

    private Guid AccountId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(value, out var accountId) ? accountId : throw new HubException("Account identity is missing.");
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return normalized.StartsWith("SYNC-", StringComparison.Ordinal) ? normalized : $"SYNC-{normalized}";
    }

    private static string Group(string code) => $"syncplay:{code}";
}
