using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class FeedEndpoints
{
    public static void MapFeedEndpoints(this RouteGroupBuilder social)
    {
        social.MapGet("/feed", GetFeedAsync);
        social.MapPost("/posts", CreatePostAsync);
        social.MapDelete("/posts/{activityId:guid}", DeletePostAsync);
        social.MapGet("/posts/{activityId:guid}/comments", GetCommentsAsync);
        social.MapPost("/posts/{activityId:guid}/comments", AddCommentAsync);
        social.MapDelete("/comments/{commentId:guid}", DeleteCommentAsync);
        social.MapPut("/posts/{activityId:guid}/reaction", SaveReactionAsync);
        social.MapDelete("/posts/{activityId:guid}/reaction", DeleteReactionAsync);
    }

    private static async Task<IResult> GetFeedAsync(int? offset, int? limit, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var skip = Math.Max(offset ?? 0, 0);
        var take = Math.Clamp(limit ?? 30, 1, 100);
        var blocked = await db.SocialBlocks.AsNoTracking()
            .Where(x => x.AccountId == accountId || x.BlockedAccountId == accountId)
            .Select(x => x.AccountId == accountId ? x.BlockedAccountId : x.AccountId).ToArrayAsync(ct);
        var muted = await db.SocialMutes.AsNoTracking().Where(x => x.AccountId == accountId).Select(x => x.MutedAccountId).ToArrayAsync(ct);
        var friends = await db.SocialRelationships.AsNoTracking()
            .Where(x => x.Kind == RelationshipKind.Friend && x.Status == RelationshipStatus.Accepted
                && (x.SourceAccountId == accountId || x.TargetAccountId == accountId))
            .Select(x => x.SourceAccountId == accountId ? x.TargetAccountId : x.SourceAccountId).ToArrayAsync(ct);

        var activities = await db.SocialActivities.AsNoTracking()
            .Where(x => !blocked.Contains(x.AccountId) && !muted.Contains(x.AccountId)
                && (x.AccountId == accountId || x.Visibility == SocialVisibility.Server || x.Visibility == SocialVisibility.Household
                    || (x.Visibility == SocialVisibility.Friends && friends.Contains(x.AccountId))))
            .OrderByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
        var ids = activities.Select(x => x.Id).ToArray();
        var comments = await db.SocialComments.AsNoTracking().Where(x => ids.Contains(x.ActivityId))
            .GroupBy(x => x.ActivityId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var reactions = await db.SocialReactions.AsNoTracking().Where(x => ids.Contains(x.ActivityId))
            .GroupBy(x => x.ActivityId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var accounts = await directory.GetAccountsAsync(activities.Select(x => x.AccountId), ct);
        return Results.Ok(activities.Select(x => new SocialActivityDto(x.Id, SocialEndpointSupport.Author(x.AccountId, accounts), x.Kind,
            x.ContentId, x.ReviewId, x.Body, x.Visibility.ToString().ToLowerInvariant(),
            comments.GetValueOrDefault(x.Id), reactions.GetValueOrDefault(x.Id), x.CreatedAtUtc)));
    }

    private static async Task<IResult> CreatePostAsync(CreatePostRequest request, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        SocialActivity activity;
        try { activity = SocialActivity.Post(SocialEndpointSupport.AccountId(user), request.Body, request.Visibility); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        db.SocialActivities.Add(activity); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v2/social/posts/{activity.Id}", new { activity.Id });
    }

    private static async Task<IResult> DeletePostAsync(Guid activityId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var activity = await db.SocialActivities.SingleOrDefaultAsync(x => x.Id == activityId, ct);
        if (activity is null) return Results.NotFound();
        if (activity.AccountId != accountId && !user.IsInRole("Owner") && !user.IsInRole("Administrator")) return Results.Forbid();
        db.SocialActivities.Remove(activity); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> GetCommentsAsync(Guid activityId, ClaimsPrincipal user, ISocialDbContext db,
        ISocialResourceDirectory directory, CancellationToken ct)
    {
        var activity = await db.SocialActivities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == activityId, ct);
        if (activity is null) return Results.NotFound();
        if (!await SocialEndpointSupport.CanViewAsync(db, activity, SocialEndpointSupport.AccountId(user), ct)) return Results.Forbid();
        var comments = await db.SocialComments.AsNoTracking().Where(x => x.ActivityId == activityId).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var accounts = await directory.GetAccountsAsync(comments.Select(x => x.AccountId), ct);
        return Results.Ok(comments.Select(x => new SocialCommentDto(x.Id, SocialEndpointSupport.Author(x.AccountId, accounts), x.Body, x.CreatedAtUtc)));
    }

    private static async Task<IResult> AddCommentAsync(Guid activityId, AddCommentRequest request, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var activity = await db.SocialActivities.SingleOrDefaultAsync(x => x.Id == activityId, ct);
        if (activity is null) return Results.NotFound();
        if (!await SocialEndpointSupport.CanViewAsync(db, activity, accountId, ct)) return Results.Forbid();
        SocialComment comment;
        try { comment = SocialComment.Create(activityId, accountId, request.Body); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        db.SocialComments.Add(comment); await db.SaveChangesAsync(ct);
        if (activity.AccountId != accountId)
            await SocialEndpointSupport.NotifyAsync(db, directory, publisher, activity.AccountId, accountId, "comment", "activity", activityId.ToString(), ct);
        return Results.Created($"/api/v2/social/posts/{activityId}/comments/{comment.Id}", new { comment.Id });
    }

    private static async Task<IResult> DeleteCommentAsync(Guid commentId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var comment = await db.SocialComments.SingleOrDefaultAsync(x => x.Id == commentId, ct);
        if (comment is null) return Results.NotFound();
        if (comment.AccountId != accountId && !user.IsInRole("Owner") && !user.IsInRole("Administrator")) return Results.Forbid();
        db.SocialComments.Remove(comment); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> SaveReactionAsync(Guid activityId, AddReactionRequest request, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var activity = await db.SocialActivities.SingleOrDefaultAsync(x => x.Id == activityId, ct);
        if (activity is null) return Results.NotFound();
        if (!await SocialEndpointSupport.CanViewAsync(db, activity, accountId, ct)) return Results.Forbid();
        var reaction = await db.SocialReactions.SingleOrDefaultAsync(x => x.ActivityId == activityId && x.AccountId == accountId, ct);
        try
        {
            if (reaction is null) { reaction = SocialReaction.Create(activityId, accountId, request.Kind); db.SocialReactions.Add(reaction); }
            else reaction.Change(request.Kind);
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        await db.SaveChangesAsync(ct);
        if (activity.AccountId != accountId)
            await SocialEndpointSupport.NotifyAsync(db, directory, publisher, activity.AccountId, accountId, "reaction", "activity", activityId.ToString(), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteReactionAsync(Guid activityId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        await db.SocialReactions.Where(x => x.ActivityId == activityId && x.AccountId == accountId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }
}
