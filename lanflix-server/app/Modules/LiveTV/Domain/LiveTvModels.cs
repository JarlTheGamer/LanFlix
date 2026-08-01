using Lanflix.SharedKernel;

namespace Lanflix.Modules.LiveTV;

public enum LiveTvSourceKind { M3uXmlTv, HdHomeRun }

public sealed class LiveTvSource : Entity<long>
{
    private LiveTvSource() { }
    public string Name { get; private set; } = string.Empty;
    public LiveTvSourceKind Kind { get; private set; }
    public string SourceUri { get; private set; } = string.Empty;
    public string? GuideUri { get; private set; }
    public int MaxTuners { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime? LastRefreshedUtc { get; private set; }
    public string? LastError { get; private set; }
    public static LiveTvSource Create(string name, LiveTvSourceKind kind, string sourceUri, string? guideUri, int maxTuners) => new() { Name = Validate(name, 160), Kind = kind, SourceUri = Validate(sourceUri, 2048), GuideUri = Clean(guideUri, 2048), MaxTuners = Math.Clamp(maxTuners, 1, 32), Enabled = true };
    public void Update(string name, string sourceUri, string? guideUri, int maxTuners, bool enabled) { Name = Validate(name, 160); SourceUri = Validate(sourceUri, 2048); GuideUri = Clean(guideUri, 2048); MaxTuners = Math.Clamp(maxTuners, 1, 32); Enabled = enabled; MarkUpdated(); }
    public void RefreshSucceeded() { LastRefreshedUtc = DateTime.UtcNow; LastError = null; MarkUpdated(); }
    public void RefreshFailed(string error) { LastRefreshedUtc = DateTime.UtcNow; LastError = error.Length > 1000 ? error[..1000] : error; MarkUpdated(); }
    private static string Validate(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim().Length <= max ? value.Trim() : throw new ArgumentException("Value is too long.");
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw new ArgumentException("Value is too long.");
}

public sealed class LiveTvChannel : Entity<long>
{
    private LiveTvChannel() { }
    public long SourceId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string StreamUri { get; private set; } = string.Empty;
    public string? GroupName { get; private set; }
    public bool Enabled { get; private set; }
    public static LiveTvChannel Create(long sourceId, ChannelImport value) { var item = new LiveTvChannel(); item.Apply(sourceId, value); return item; }
    public void Update(ChannelImport value) { Apply(SourceId, value); MarkUpdated(); }
    private void Apply(long sourceId, ChannelImport value) { SourceId = sourceId; ExternalId = value.ExternalId.Trim(); Number = value.Number.Trim(); Name = value.Name.Trim(); LogoUrl = Blank(value.LogoUrl); StreamUri = value.StreamUri.Trim(); GroupName = Blank(value.GroupName); Enabled = true; }
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ChannelImport(string ExternalId, string Number, string Name, string? LogoUrl, string StreamUri, string? GroupName);

public sealed class LiveTvProgram : Entity<long>
{
    private LiveTvProgram() { }
    public long ChannelId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? EpisodeTitle { get; private set; }
    public string? ArtworkUrl { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public static LiveTvProgram Create(long channelId, ProgramImport value) => new() { ChannelId = channelId, ExternalId = value.ExternalId, Title = value.Title, Description = value.Description, Category = value.Category, EpisodeTitle = value.EpisodeTitle, ArtworkUrl = value.ArtworkUrl, StartsAtUtc = value.StartsAtUtc, EndsAtUtc = value.EndsAtUtc };
}

public sealed record ProgramImport(string ExternalId, string Title, string? Description, string? Category, string? EpisodeTitle, string? ArtworkUrl, DateTime StartsAtUtc, DateTime EndsAtUtc);

public sealed class LiveTvFavorite : Entity<long>
{
    private LiveTvFavorite() { }
    public Guid AccountId { get; private set; }
    public long ChannelId { get; private set; }
    public static LiveTvFavorite Create(Guid accountId, long channelId) => new() { AccountId = accountId, ChannelId = channelId };
}

public sealed class LiveTvTunerLease : Entity<Guid>
{
    private LiveTvTunerLease() { }
    public long SourceId { get; private set; }
    public long ChannelId { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public static LiveTvTunerLease Create(long sourceId, long channelId, Guid accountId, TimeSpan lifetime) => new() { Id = Guid.NewGuid(), SourceId = sourceId, ChannelId = channelId, AccountId = accountId, ExpiresAtUtc = DateTime.UtcNow.Add(lifetime) };
}
