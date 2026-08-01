using Lanflix.SharedKernel;

namespace Lanflix.Modules.Social;

public sealed class SocialReview : Entity<Guid>
{
    private SocialReview() { }
    public Guid AccountId { get; private set; }
    public int ContentId { get; private set; }
    public int Rating { get; private set; }
    public string? Body { get; private set; }
    public SocialVisibility Visibility { get; private set; }

    public static SocialReview Create(Guid accountId, int contentId, int rating, string? body, SocialVisibility visibility)
    {
        var review = new SocialReview { Id = Guid.NewGuid(), AccountId = accountId, ContentId = contentId };
        review.Update(rating, body, visibility);
        return review;
    }

    public void Update(int rating, string? body, SocialVisibility visibility)
    {
        if (rating is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(rating));
        Rating = rating;
        Body = Clean(body, 4000);
        Visibility = visibility;
        MarkUpdated();
    }

    internal static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = value.Trim();
        return clean.Length <= max ? clean : clean[..max];
    }
}

public sealed class SocialActivity : Entity<Guid>
{
    private SocialActivity() { }
    public Guid AccountId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public int? ContentId { get; private set; }
    public Guid? ReviewId { get; private set; }
    public string? Body { get; private set; }
    public SocialVisibility Visibility { get; private set; }

    public static SocialActivity Post(Guid accountId, string body, SocialVisibility visibility) => new()
    {
        Id = Guid.NewGuid(), AccountId = accountId, Kind = "post",
        Body = SocialReview.Clean(body, 2000) ?? throw new ArgumentException("Post body is required."), Visibility = visibility
    };

    public static SocialActivity Review(Guid accountId, SocialReview review) => new()
    {
        Id = Guid.NewGuid(), AccountId = accountId, Kind = "review", ContentId = review.ContentId,
        ReviewId = review.Id, Body = review.Body, Visibility = review.Visibility
    };

    public void UpdateFromReview(SocialReview review)
    {
        Body = review.Body;
        Visibility = review.Visibility;
        MarkUpdated();
    }
}

public sealed class SocialComment : Entity<Guid>
{
    private SocialComment() { }
    public Guid ActivityId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public static SocialComment Create(Guid activityId, Guid accountId, string body) => new()
    {
        Id = Guid.NewGuid(), ActivityId = activityId, AccountId = accountId,
        Body = SocialReview.Clean(body, 1000) ?? throw new ArgumentException("Comment body is required.")
    };
}

public sealed class SocialReaction : Entity<Guid>
{
    private SocialReaction() { }
    public Guid ActivityId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public static SocialReaction Create(Guid activityId, Guid accountId, string kind) => new()
    {
        Id = Guid.NewGuid(), ActivityId = activityId, AccountId = accountId, Kind = Normalize(kind)
    };
    public void Change(string kind) { Kind = Normalize(kind); MarkUpdated(); }
    private static string Normalize(string kind) => kind.Trim().ToLowerInvariant() switch
    {
        "like" => "like", "love" => "love", "laugh" => "laugh", "wow" => "wow",
        _ => throw new ArgumentException("Unsupported reaction.")
    };
}
