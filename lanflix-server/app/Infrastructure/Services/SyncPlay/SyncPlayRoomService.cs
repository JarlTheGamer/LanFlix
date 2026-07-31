using System.Collections.Concurrent;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.SyncPlay;

public class SyncPlayRoomService : ISyncPlayRoomService
{
    private readonly ConcurrentDictionary<string, SyncPlayRoomDto> _roomsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _roomCodeByConnectionId = new();
    private readonly ILogger<SyncPlayRoomService> _logger;
    private static readonly Random _random = new();

    public SyncPlayRoomService(ILogger<SyncPlayRoomService> logger)
    {
        _logger = logger;
    }

    public SyncPlayRoomDto CreateRoom(int profileId, string connectionId, string profileName, string? profileAvatar, int contentId, string contentType, int? episodeId)
    {
        string roomCode = GenerateUniqueRoomCode();

        var hostParticipant = new SyncPlayParticipantDto
        {
            ConnectionId = connectionId,
            ProfileId = profileId,
            ProfileName = profileName,
            ProfileAvatar = profileAvatar,
            IsHost = true,
            IsReady = true,
            CurrentTimeSeconds = 0,
            JoinedAtUtc = DateTime.UtcNow
        };

        var room = new SyncPlayRoomDto
        {
            RoomCode = roomCode,
            HostProfileId = profileId,
            HostConnectionId = connectionId,
            ContentId = contentId,
            ContentType = contentType,
            EpisodeId = episodeId,
            CurrentTimeSeconds = 0,
            IsPlaying = false,
            PlaybackRate = 1.0,
            LastStateUpdateUtc = DateTime.UtcNow,
            Participants = new List<SyncPlayParticipantDto> { hostParticipant }
        };

        _roomsByCode[roomCode] = room;
        _roomCodeByConnectionId[connectionId] = roomCode;

        _logger.LogInformation("Created SyncPlay room {RoomCode} for Profile {ProfileName} (Content: {ContentType} #{ContentId})",
            roomCode, profileName, contentType, contentId);

        return room;
    }

    public SyncPlayRoomDto? JoinRoom(string roomCode, int profileId, string connectionId, string profileName, string? profileAvatar)
    {
        if (!_roomsByCode.TryGetValue(roomCode, out var room))
        {
            return null;
        }

        lock (room)
        {
            // Remove connection if already in another room
            if (_roomCodeByConnectionId.TryGetValue(connectionId, out var oldRoomCode) && oldRoomCode != roomCode)
            {
                LeaveRoomByConnectionId(connectionId);
            }

            var existingParticipant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId || (p.ProfileId == profileId && p.ProfileId != 0));
            if (existingParticipant != null)
            {
                existingParticipant.ConnectionId = connectionId;
                existingParticipant.ProfileName = profileName;
                existingParticipant.ProfileAvatar = profileAvatar;
            }
            else
            {
                var newParticipant = new SyncPlayParticipantDto
                {
                    ConnectionId = connectionId,
                    ProfileId = profileId,
                    ProfileName = profileName,
                    ProfileAvatar = profileAvatar,
                    IsHost = room.Participants.Count == 0,
                    IsReady = true,
                    CurrentTimeSeconds = GetEstimatedCurrentTime(room),
                    JoinedAtUtc = DateTime.UtcNow
                };

                room.Participants.Add(newParticipant);
            }

            _roomCodeByConnectionId[connectionId] = roomCode;

            // Recalculate room position
            room.CurrentTimeSeconds = GetEstimatedCurrentTime(room);
        }

        _logger.LogInformation("Profile {ProfileName} joined SyncPlay room {RoomCode}", profileName, roomCode);
        return room;
    }

    public (SyncPlayRoomDto? Room, SyncPlayParticipantDto? LeftParticipant, bool RoomClosed) LeaveRoomByConnectionId(string connectionId)
    {
        if (!_roomCodeByConnectionId.TryRemove(connectionId, out var roomCode) || !_roomsByCode.TryGetValue(roomCode, out var room))
        {
            return (null, null, false);
        }

        SyncPlayParticipantDto? leftParticipant = null;
        bool roomClosed = false;

        lock (room)
        {
            leftParticipant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (leftParticipant != null)
            {
                room.Participants.Remove(leftParticipant);
            }

            if (room.Participants.Count == 0)
            {
                _roomsByCode.TryRemove(roomCode, out _);
                roomClosed = true;
                _logger.LogInformation("SyncPlay room {RoomCode} closed as all participants left", roomCode);
            }
            else if (leftParticipant != null && leftParticipant.IsHost)
            {
                // Assign new host
                var newHost = room.Participants.First();
                newHost.IsHost = true;
                room.HostProfileId = newHost.ProfileId;
                room.HostConnectionId = newHost.ConnectionId;

                _logger.LogInformation("Host left room {RoomCode}. Reassigned host to {NewHostName}", roomCode, newHost.ProfileName);
            }
        }

        return (room, leftParticipant, roomClosed);
    }

    public SyncPlayRoomDto? GetRoom(string roomCode)
    {
        if (_roomsByCode.TryGetValue(roomCode, out var room))
        {
            lock (room)
            {
                room.CurrentTimeSeconds = GetEstimatedCurrentTime(room);
                return room;
            }
        }
        return null;
    }

    public SyncPlayRoomDto? GetRoomByConnectionId(string connectionId)
    {
        if (_roomCodeByConnectionId.TryGetValue(connectionId, out var roomCode))
        {
            return GetRoom(roomCode);
        }
        return null;
    }

    public SyncPlayRoomDto? UpdatePlaybackState(string roomCode, string connectionId, string actionType, double positionSeconds, bool isPlaying, double playbackRate)
    {
        if (!_roomsByCode.TryGetValue(roomCode, out var room))
        {
            return null;
        }

        lock (room)
        {
            room.CurrentTimeSeconds = positionSeconds;
            room.IsPlaying = isPlaying;
            room.PlaybackRate = playbackRate > 0 ? playbackRate : 1.0;
            room.LastStateUpdateUtc = DateTime.UtcNow;

            var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (participant != null)
            {
                participant.CurrentTimeSeconds = positionSeconds;
            }
        }

        _logger.LogDebug("SyncPlay room {RoomCode} state updated ({Action}): Time={Time:F1}s, Playing={Playing}",
            roomCode, actionType, positionSeconds, isPlaying);

        return room;
    }

    public SyncPlayRoomDto? ChangeMedia(string roomCode, string connectionId, int contentId, string contentType, int? episodeId, string mediaTitle)
    {
        if (!_roomsByCode.TryGetValue(roomCode, out var room))
        {
            return null;
        }

        lock (room)
        {
            // Only host or any member if host allows (we enforce host check or open control)
            room.ContentId = contentId;
            room.ContentType = contentType;
            room.EpisodeId = episodeId;
            room.CurrentTimeSeconds = 0;
            room.IsPlaying = true;
            room.LastStateUpdateUtc = DateTime.UtcNow;
        }

        _logger.LogInformation("SyncPlay room {RoomCode} changed media to {Title} ({ContentType} #{ContentId})",
            roomCode, mediaTitle, contentType, contentId);

        return room;
    }

    public void UpdateParticipantPing(string connectionId, int pingMs, double currentTimeSeconds)
    {
        if (_roomCodeByConnectionId.TryGetValue(connectionId, out var roomCode) && _roomsByCode.TryGetValue(roomCode, out var room))
        {
            lock (room)
            {
                var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (participant != null)
                {
                    participant.PingMs = pingMs;
                    participant.CurrentTimeSeconds = currentTimeSeconds;
                }

                if (room.HostConnectionId == connectionId && currentTimeSeconds >= 0)
                {
                    room.CurrentTimeSeconds = currentTimeSeconds;
                    room.LastStateUpdateUtc = DateTime.UtcNow;
                }
            }
        }
    }

    private double GetEstimatedCurrentTime(SyncPlayRoomDto room)
    {
        if (!room.IsPlaying)
        {
            return room.CurrentTimeSeconds;
        }

        double elapsed = (DateTime.UtcNow - room.LastStateUpdateUtc).TotalSeconds * room.PlaybackRate;
        return room.CurrentTimeSeconds + Math.Max(0, elapsed);
    }

    private string GenerateUniqueRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (int attempt = 0; attempt < 100; attempt++)
        {
            char[] code = new char[6];
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[_random.Next(chars.Length)];
            }
            string formatted = $"SYNC-{new string(code)}";
            if (!_roomsByCode.ContainsKey(formatted))
            {
                return formatted;
            }
        }
        return $"SYNC-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}
