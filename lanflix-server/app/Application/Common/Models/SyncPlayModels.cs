namespace Lanflix.Application.Common.Models;

public class SyncPlayRoomDto
{
    public string RoomCode { get; set; } = string.Empty;
    public int HostProfileId { get; set; }
    public string HostConnectionId { get; set; } = string.Empty;
    public int ContentId { get; set; }
    public string ContentType { get; set; } = "movie";
    public int? EpisodeId { get; set; }
    public double CurrentTimeSeconds { get; set; }
    public bool IsPlaying { get; set; }
    public double PlaybackRate { get; set; } = 1.0;
    public DateTime LastStateUpdateUtc { get; set; } = DateTime.UtcNow;
    public List<SyncPlayParticipantDto> Participants { get; set; } = new();
}

public class SyncPlayParticipantDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string? ProfileAvatar { get; set; }
    public bool IsHost { get; set; }
    public bool IsReady { get; set; } = true;
    public double CurrentTimeSeconds { get; set; }
    public int PingMs { get; set; }
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SyncPlayActionDto
{
    public string RoomCode { get; set; } = string.Empty;
    public string ActionType { get; set; } = "Play"; // Play, Pause, Seek, BufferWait, RateChange
    public double PositionSeconds { get; set; }
    public bool IsPlaying { get; set; }
    public double PlaybackRate { get; set; } = 1.0;
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
}

public class SyncPlayChatMessageDto
{
    public string RoomCode { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string? ProfileAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsSystem { get; set; }
}

public class SyncPlayEmojiReactionDto
{
    public string RoomCode { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}

public class SyncPlayMediaChangeDto
{
    public string RoomCode { get; set; } = string.Empty;
    public int ContentId { get; set; }
    public string ContentType { get; set; } = "movie";
    public int? EpisodeId { get; set; }
    public string MediaTitle { get; set; } = string.Empty;
}
