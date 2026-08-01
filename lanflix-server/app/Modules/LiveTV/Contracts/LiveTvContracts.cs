namespace Lanflix.Modules.LiveTV;

public sealed record LiveTvSourceDto(long Id, string Name, string Kind, int MaxTuners, bool Enabled, DateTime? LastRefreshedUtc, string? LastError);
public sealed record LiveTvChannelDto(long Id, string Number, string Name, string? LogoUrl, string? GroupName, bool Favorite, LiveTvProgramDto? Now, LiveTvProgramDto? Next);
public sealed record LiveTvProgramDto(long Id, string Title, string? Description, string? Category, string? EpisodeTitle, string? ArtworkUrl, DateTime StartsAtUtc, DateTime EndsAtUtc);
public sealed record LiveTvGuideDto(DateTime FromUtc, DateTime ToUtc, IReadOnlyList<LiveTvChannelDto> Channels, IReadOnlyDictionary<long, IReadOnlyList<LiveTvProgramDto>> Programs);
public sealed record SaveLiveTvSourceRequest(string Name, string Kind, string SourceUri, string? GuideUri, int MaxTuners = 1, bool Enabled = true);
public sealed record LiveTvRefreshResult(int ChannelsImported, int ChannelsUpdated, int ChannelsRemoved, int ProgramsImported, string? Error);
public sealed record LiveTvStream(string Uri, string ContentType, Guid LeaseId);

public interface ILiveTvCatalog
{
    Task<IReadOnlyList<LiveTvSourceDto>> GetSourcesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LiveTvChannelDto>> GetChannelsAsync(Guid accountId, CancellationToken cancellationToken);
    Task<LiveTvGuideDto> GetGuideAsync(Guid accountId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    Task<LiveTvRefreshResult> RefreshAsync(long sourceId, CancellationToken cancellationToken);
    Task<LiveTvStream?> AcquireStreamAsync(long channelId, Guid accountId, CancellationToken cancellationToken);
    Task ReleaseStreamAsync(Guid leaseId, CancellationToken cancellationToken);
}
