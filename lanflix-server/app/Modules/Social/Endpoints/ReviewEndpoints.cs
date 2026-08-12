using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this RouteGroupBuilder social)
    {
        social.MapGet("/reviews/{contentId:int}", ListAsync);
        social.MapPut("/reviews/{contentId:int}", SaveAsync);
        social.MapDelete("/reviews/{contentId:int}", DeleteAsync);
    }

    private static async Task<IResult> ListAsync(int contentId, ClaimsPrincipal user, ISocialDbContext db,
        ISocialResourceDirectory directory, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var blocked = await db.SocialBlocks.AsNoTracking().Where(x => x.AccountId == accountId || x.BlockedAccountId == accountId)
            .Select(x => x.AccountId == accountId ? x.BlockedAccountId : x.AccountId).ToArrayAsync(ct);
        var friends = await db.SocialRelationships.AsNoTracking().Where(x => x.Kind == RelationshipKind.Friend
                && x.Status == RelationshipStatus.Accepted && (x.SourceAccountId == accountId || x.TargetAccountId == accountId))
            .Select(x => x.SourceAccountId == accountId ? x.TargetAccountId : x.SourceAccountId).ToArrayAsync(ct);
        var reviews = await db.SocialReviews.AsNoTracking().Where(x => x.ContentId == contentId && !blocked.Contains(x.AccountId)
                && (x.AccountId == accountId || x.Visibility == SocialVisibility.Server || x.Visibility == SocialVisibility.Household
                    || (x.Visibility == SocialVisibility.Friends && friends.Contains(x.AccountId))))
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Take(200).ToListAsync(ct);
        var accounts = await directory.GetAccountsAsync(reviews.Select(x => x.AccountId), ct);
        return Results.Ok(reviews.Select(x => new SocialReviewDto(x.Id, SocialEndpointSupport.Author(x.AccountId, accounts), x.ContentId,
            x.Rating, x.Body, x.Visibility.ToString().ToLowerInvariant(), x.UpdatedAtUtc ?? x.CreatedAtUtc)));
    }

    private static async Task<IResult> SaveAsync(int contentId, SaveReviewRequest request, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        if (!await directory.MediaExistsAsync(contentId, ct)) return Results.NotFound();
        var accountId = SocialEndpointSupport.AccountId(user);
        var review = await db.SocialReviews.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ContentId == contentId, ct);
        try
        {
            if (review is null)
            {
                review = SocialReview.Create(accountId, contentId, request.Rating, request.Body, request.Visibility);
                db.SocialReviews.Add(review);
                db.SocialActivities.Add(SocialActivity.Review(accountId, review));
            }
            else
            {
                review.Update(request.Rating, request.Body, request.Visibility);
                var activity = await db.SocialActivities.SingleOrDefaultAsync(x => x.ReviewId == review.Id, ct);
                if (activity is null) db.SocialActivities.Add(SocialActivity.Review(accountId, review));
                else activity.UpdateFromReview(review);
            }
        }
        catch (ArgumentOutOfRangeException) { return Results.Problem(statusCode: 400, title: "Rating must be between 1 and 5"); }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { review.Id, review.Rating });
    }

    private static async Task<IResult> DeleteAsync(int contentId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var review = await db.SocialReviews.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ContentId == contentId, ct);
        if (review is null) return Results.NoContent();
        await db.SocialActivities.Where(x => x.ReviewId == review.Id).ExecuteDeleteAsync(ct);
        db.SocialReviews.Remove(review); await db.SaveChangesAsync(ct); return Results.NoContent();
    }
}
