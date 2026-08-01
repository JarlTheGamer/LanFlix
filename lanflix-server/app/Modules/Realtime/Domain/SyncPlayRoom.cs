using Lanflix.SharedKernel;

namespace Lanflix.Modules.Realtime;

public sealed class SyncPlayRoom : Entity<Guid>
{
    private SyncPlayRoom() { }

    public string Code { get; private set; } = string.Empty;
    public Guid HostAccountId { get; private set; }
    public int ContentId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public int? EpisodeId { get; private set; }
    public double PositionSeconds { get; private set; }
    public bool IsPlaying { get; private set; }
    public double PlaybackRate { get; private set; } = 1;
    public DateTime ExpiresAtUtc { get; private set; }

    public static SyncPlayRoom Create(string code, Guid hostAccountId, int contentId, string contentType, int? episodeId) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        HostAccountId = hostAccountId,
        ContentId = contentId,
        ContentType = contentType,
        EpisodeId = episodeId,
        ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
    };

    public void Synchronize(double positionSeconds, bool isPlaying, double playbackRate)
    {
        PositionSeconds = Math.Max(positionSeconds, 0);
        IsPlaying = isPlaying;
        PlaybackRate = Math.Clamp(playbackRate, .5, 2);
        ExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        MarkUpdated();
    }

    public SyncPlayRoomDto ToDto() => new(Code, HostAccountId, ContentId, ContentType, EpisodeId,
        PositionSeconds, IsPlaying, PlaybackRate, ExpiresAtUtc);
}

public sealed record SyncPlayRoomDto(
    string Code,
    Guid HostAccountId,
    int ContentId,
    string ContentType,
    int? EpisodeId,
    double PositionSeconds,
    bool IsPlaying,
    double PlaybackRate,
    DateTime ExpiresAtUtc);
