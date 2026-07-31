using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

public interface ISyncPlayRoomService
{
    SyncPlayRoomDto CreateRoom(int profileId, string connectionId, string profileName, string? profileAvatar, int contentId, string contentType, int? episodeId);
    SyncPlayRoomDto? JoinRoom(string roomCode, int profileId, string connectionId, string profileName, string? profileAvatar);
    (SyncPlayRoomDto? Room, SyncPlayParticipantDto? LeftParticipant, bool RoomClosed) LeaveRoomByConnectionId(string connectionId);
    SyncPlayRoomDto? GetRoom(string roomCode);
    SyncPlayRoomDto? GetRoomByConnectionId(string connectionId);
    SyncPlayRoomDto? UpdatePlaybackState(string roomCode, string connectionId, string actionType, double positionSeconds, bool isPlaying, double playbackRate);
    SyncPlayRoomDto? ChangeMedia(string roomCode, string connectionId, int contentId, string contentType, int? episodeId, string mediaTitle);
    void UpdateParticipantPing(string connectionId, int pingMs, double currentTimeSeconds);
}
