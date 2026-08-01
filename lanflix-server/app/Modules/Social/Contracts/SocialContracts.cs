namespace Lanflix.Modules.Social;

public enum SocialVisibility { Private, Household, Friends, Server }
public enum RelationshipKind { Follow, Friend }
public enum RelationshipStatus { Pending, Accepted }
public enum ReportStatus { Open, Resolved, Dismissed }

public sealed record SocialAccountDto(Guid Id, string DisplayName, string Role);
public sealed record SocialAuthorDto(Guid Id, string DisplayName);
public sealed record SocialRelationshipDto(Guid Id, SocialAccountDto Account, string Kind, string Status, bool Incoming, DateTime CreatedAtUtc);
public sealed record SocialReviewDto(Guid Id, SocialAuthorDto Author, int ContentId, int Rating, string? Body, string Visibility, DateTime UpdatedAtUtc);
public sealed record SocialCommentDto(Guid Id, SocialAuthorDto Author, string Body, DateTime CreatedAtUtc);
public sealed record SocialActivityDto(Guid Id, SocialAuthorDto Author, string Kind, int? ContentId, Guid? ReviewId,
    string? Body, string Visibility, int CommentCount, int ReactionCount, DateTime CreatedAtUtc);
public sealed record SocialNotificationDto(Guid Id, SocialAuthorDto? Actor, string Kind, string ResourceType,
    string ResourceId, bool IsRead, DateTime CreatedAtUtc);
public sealed record SocialReportDto(Guid Id, Guid ReporterAccountId, string TargetType, string TargetId,
    string Reason, string Status, string? Resolution, Guid? ModeratedByAccountId, DateTime CreatedAtUtc);

public sealed record CreatePostRequest(string Body, SocialVisibility Visibility = SocialVisibility.Friends);
public sealed record SaveReviewRequest(int Rating, string? Body, SocialVisibility Visibility = SocialVisibility.Friends);
public sealed record AddCommentRequest(string Body);
public sealed record AddReactionRequest(string Kind);
public sealed record UpdatePrivacyRequest(SocialVisibility DefaultVisibility, bool ActivityEnabled);
public sealed record CreateReportRequest(string TargetType, string TargetId, string Reason);
public sealed record ResolveReportRequest(bool Dismiss, string Resolution);

public interface ISocialResourceDirectory
{
    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken);
    Task<bool> MediaExistsAsync(int contentId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, SocialAccountDto>> GetAccountsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken);
}

public interface ISocialNotificationPublisher
{
    Task PublishAsync(Guid accountId, SocialNotificationDto notification, CancellationToken cancellationToken);
}
