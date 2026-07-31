using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lanflix.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time SyncPlay Watch Party synchronization
/// </summary>
[AllowAnonymous] // Allow guests / open profile connections
public class SyncPlayHub : Hub
{
    private readonly ISyncPlayRoomService _roomService;
    private readonly ILogger<SyncPlayHub> _logger;

    public SyncPlayHub(ISyncPlayRoomService roomService, ILogger<SyncPlayHub> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public async Task<SyncPlayRoomDto?> CreateRoom(int profileId, string profileName, string? profileAvatar, int contentId, string contentType, int? episodeId)
    {
        var room = _roomService.CreateRoom(profileId, Context.ConnectionId, profileName, profileAvatar, contentId, contentType, episodeId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(room.RoomCode));

        await Clients.Caller.SendAsync("RoomJoined", room);
        return room;
    }

    public async Task<SyncPlayRoomDto?> JoinRoom(string roomCode, int profileId, string profileName, string? profileAvatar)
    {
        string normalizedCode = roomCode.Trim().ToUpper();
        if (!normalizedCode.StartsWith("SYNC-"))
        {
            normalizedCode = $"SYNC-{normalizedCode}";
        }

        var room = _roomService.JoinRoom(normalizedCode, profileId, Context.ConnectionId, profileName, profileAvatar);
        if (room == null)
        {
            await Clients.Caller.SendAsync("JoinFailed", "Room not found or invalid room code.");
            return null;
        }

        string groupName = GetGroupName(room.RoomCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

        // Notify caller of initial state
        await Clients.Caller.SendAsync("RoomJoined", room);

        // Broadcast to existing room members
        if (participant != null)
        {
            await Clients.OthersInGroup(groupName).SendAsync("UserJoined", participant, room);
        }

        return room;
    }

    public async Task LeaveRoom()
    {
        var (room, leftParticipant, roomClosed) = _roomService.LeaveRoomByConnectionId(Context.ConnectionId);
        if (room != null && leftParticipant != null)
        {
            string groupName = GetGroupName(room.RoomCode);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (roomClosed)
            {
                await Clients.Group(groupName).SendAsync("RoomClosed");
            }
            else
            {
                await Clients.Group(groupName).SendAsync("UserLeft", leftParticipant, room);
            }
        }
    }

    public async Task SendPlaybackAction(string actionType, double positionSeconds, bool isPlaying, double playbackRate, int profileId, string profileName)
    {
        var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
        if (room == null) return;

        if (room.HostConnectionId != Context.ConnectionId)
        {
            _logger.LogWarning("Non-host participant {ConnectionId} attempted to control playback in room {RoomCode}", Context.ConnectionId, room.RoomCode);
            return;
        }

        var updatedRoom = _roomService.UpdatePlaybackState(room.RoomCode, Context.ConnectionId, actionType, positionSeconds, isPlaying, playbackRate);
        if (updatedRoom == null) return;

        var actionDto = new SyncPlayActionDto
        {
            RoomCode = room.RoomCode,
            ActionType = actionType,
            PositionSeconds = positionSeconds,
            IsPlaying = isPlaying,
            PlaybackRate = playbackRate,
            ProfileId = profileId,
            ProfileName = profileName
        };

        // Broadcast sync event to all OTHER participants in the room
        await Clients.OthersInGroup(GetGroupName(room.RoomCode)).SendAsync("PlaybackStateSynced", actionDto, updatedRoom);
    }

    public async Task SendChatMessage(string message, int profileId, string profileName, string? profileAvatar)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
        if (room == null) return;

        var chatDto = new SyncPlayChatMessageDto
        {
            RoomCode = room.RoomCode,
            ProfileId = profileId,
            ProfileName = profileName,
            ProfileAvatar = profileAvatar,
            Message = message.Trim(),
            TimestampUtc = DateTime.UtcNow,
            IsSystem = false
        };

        await Clients.Group(GetGroupName(room.RoomCode)).SendAsync("ChatMessageReceived", chatDto);
    }

    public async Task SendEmojiReaction(string emoji, int profileId, string profileName)
    {
        if (string.IsNullOrWhiteSpace(emoji)) return;

        var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
        if (room == null) return;

        var emojiDto = new SyncPlayEmojiReactionDto
        {
            RoomCode = room.RoomCode,
            ProfileId = profileId,
            ProfileName = profileName,
            Emoji = emoji
        };

        await Clients.OthersInGroup(GetGroupName(room.RoomCode)).SendAsync("EmojiReactionReceived", emojiDto);
    }

    public async Task ChangeMedia(int contentId, string contentType, int? episodeId, string mediaTitle)
    {
        var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
        if (room == null) return;

        if (room.HostConnectionId != Context.ConnectionId)
        {
            _logger.LogWarning("Non-host participant {ConnectionId} attempted to change media in room {RoomCode}", Context.ConnectionId, room.RoomCode);
            return;
        }

        var updatedRoom = _roomService.ChangeMedia(room.RoomCode, Context.ConnectionId, contentId, contentType, episodeId, mediaTitle);
        if (updatedRoom == null) return;

        var changeDto = new SyncPlayMediaChangeDto
        {
            RoomCode = room.RoomCode,
            ContentId = contentId,
            ContentType = contentType,
            EpisodeId = episodeId,
            MediaTitle = mediaTitle
        };

        await Clients.Group(GetGroupName(room.RoomCode)).SendAsync("MediaChanged", changeDto, updatedRoom);
    }

    public async Task Ping(long clientTimestamp, double currentTimeSeconds)
    {
        _roomService.UpdateParticipantPing(Context.ConnectionId, 0, currentTimeSeconds);
        long serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Clients.Caller.SendAsync("Pong", clientTimestamp, serverTimestamp);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveRoom();
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(string roomCode) => $"room-{roomCode.Trim().ToUpper()}";
}
