using Lanflix.SharedKernel;

namespace Lanflix.Modules.Social;

public sealed class SocialBlock : Entity<Guid>
{
    private SocialBlock() { }
    public Guid AccountId { get; private set; }
    public Guid BlockedAccountId { get; private set; }
    public static SocialBlock Create(Guid accountId, Guid blockedId)
    {
        if (accountId == blockedId) throw new ArgumentException("An account cannot block itself.");
        return new() { Id = Guid.NewGuid(), AccountId = accountId, BlockedAccountId = blockedId };
    }
}

public sealed class SocialMute : Entity<Guid>
{
    private SocialMute() { }
    public Guid AccountId { get; private set; }
    public Guid MutedAccountId { get; private set; }
    public static SocialMute Create(Guid accountId, Guid mutedId)
    {
        if (accountId == mutedId) throw new ArgumentException("An account cannot mute itself.");
        return new() { Id = Guid.NewGuid(), AccountId = accountId, MutedAccountId = mutedId };
    }
}

public sealed class SocialPrivacy : Entity<Guid>
{
    private SocialPrivacy() { }
    public Guid AccountId { get; private set; }
    public SocialVisibility DefaultVisibility { get; private set; }
    public bool ActivityEnabled { get; private set; }
    public static SocialPrivacy Create(Guid accountId) => new()
    {
        Id = Guid.NewGuid(), AccountId = accountId, DefaultVisibility = SocialVisibility.Friends, ActivityEnabled = true
    };
    public void Update(SocialVisibility visibility, bool enabled) { DefaultVisibility = visibility; ActivityEnabled = enabled; MarkUpdated(); }
}

public sealed class SocialNotification : Entity<Guid>
{
    private SocialNotification() { }
    public Guid AccountId { get; private set; }
    public Guid? ActorAccountId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public DateTime? ReadAtUtc { get; private set; }
    public static SocialNotification Create(Guid accountId, Guid? actorId, string kind, string resourceType, string resourceId) => new()
    {
        Id = Guid.NewGuid(), AccountId = accountId, ActorAccountId = actorId, Kind = kind,
        ResourceType = resourceType, ResourceId = resourceId
    };
    public void MarkRead() { if (ReadAtUtc is null) { ReadAtUtc = DateTime.UtcNow; MarkUpdated(); } }
}

public sealed class SocialReport : Entity<Guid>
{
    private SocialReport() { }
    public Guid ReporterAccountId { get; private set; }
    public string TargetType { get; private set; } = string.Empty;
    public string TargetId { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public string? Resolution { get; private set; }
    public Guid? ModeratedByAccountId { get; private set; }
    public static SocialReport Create(Guid reporter, string targetType, string targetId, string reason) => new()
    {
        Id = Guid.NewGuid(), ReporterAccountId = reporter,
        TargetType = NormalizeTarget(targetType), TargetId = targetId.Trim(),
        Reason = SocialReview.Clean(reason, 2000) ?? throw new ArgumentException("A report reason is required."),
        Status = ReportStatus.Open
    };
    public void Resolve(Guid moderator, bool dismiss, string resolution)
    {
        Status = dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
        Resolution = SocialReview.Clean(resolution, 2000) ?? throw new ArgumentException("A resolution is required.");
        ModeratedByAccountId = moderator;
        MarkUpdated();
    }
    private static string NormalizeTarget(string value) => value.Trim().ToLowerInvariant() switch
    {
        "activity" => "activity", "review" => "review", "comment" => "comment", "account" => "account",
        _ => throw new ArgumentException("Unsupported report target.")
    };
}
