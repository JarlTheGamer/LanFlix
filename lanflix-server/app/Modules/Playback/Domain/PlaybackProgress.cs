using Lanflix.SharedKernel;

namespace Lanflix.Modules.Playback;

public sealed class PlaybackProgress : Entity<long>
{
    private PlaybackProgress() { }
    public Guid AccountId { get; private set; }
    public string MediaKind { get; private set; } = string.Empty;
    public int MediaId { get; private set; }
    public long PositionMilliseconds { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public bool Completed { get; private set; }

    public static PlaybackProgress Create(Guid accountId, string mediaKind, int mediaId) => new()
    {
        AccountId = accountId,
        MediaKind = mediaKind,
        MediaId = mediaId
    };

    public void Update(long positionMilliseconds, long durationMilliseconds, bool completed)
    {
        PositionMilliseconds = Math.Max(0, positionMilliseconds);
        DurationMilliseconds = Math.Max(0, durationMilliseconds);
        Completed = completed || (DurationMilliseconds > 0 && PositionMilliseconds >= DurationMilliseconds * 0.95);
        MarkUpdated();
    }

    public PlaybackProgressDto ToDto() => new(MediaKind, MediaId, PositionMilliseconds, DurationMilliseconds,
        DurationMilliseconds == 0 ? 0 : Math.Clamp(PositionMilliseconds * 100d / DurationMilliseconds, 0, 100),
        Completed, UpdatedAtUtc ?? CreatedAtUtc);
}
