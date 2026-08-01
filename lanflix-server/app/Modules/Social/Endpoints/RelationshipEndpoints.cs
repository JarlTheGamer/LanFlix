using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class RelationshipEndpoints
{
    public static void MapRelationshipEndpoints(this RouteGroupBuilder social)
    {
        social.MapGet("/relationships", ListAsync);
        social.MapPut("/follows/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db,
            ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct) =>
            CreateAsync(targetId, RelationshipKind.Follow, user, db, directory, publisher, ct));
        social.MapDelete("/follows/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct) =>
            DeleteAsync(targetId, RelationshipKind.Follow, user, db, ct));
        social.MapPost("/friends/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db,
            ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct) =>
            CreateAsync(targetId, RelationshipKind.Friend, user, db, directory, publisher, ct));
        social.MapPost("/friends/requests/{relationshipId:guid}/accept", AcceptAsync);
        social.MapDelete("/friends/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct) =>
            DeleteAsync(targetId, RelationshipKind.Friend, user, db, ct));
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal user, ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var entities = await db.SocialRelationships.AsNoTracking()
            .Where(x => x.SourceAccountId == accountId || x.TargetAccountId == accountId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var accounts = await directory.GetAccountsAsync(entities.Select(x => x.SourceAccountId == accountId ? x.TargetAccountId : x.SourceAccountId), ct);
        return Results.Ok(entities.Select(x =>
        {
            var incoming = x.TargetAccountId == accountId;
            var otherId = incoming ? x.SourceAccountId : x.TargetAccountId;
            return new SocialRelationshipDto(x.Id, accounts[otherId], x.Kind.ToString().ToLowerInvariant(),
                x.Status.ToString().ToLowerInvariant(), incoming, x.CreatedAtUtc);
        }));
    }

    private static async Task<IResult> CreateAsync(Guid targetId, RelationshipKind kind, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        if (!await directory.AccountExistsAsync(targetId, ct)) return Results.NotFound();
        if (!await SocialEndpointSupport.CanInteractAsync(db, accountId, targetId, ct))
            return Results.Problem(statusCode: 409, title: "Relationship is not allowed");

        var existing = kind == RelationshipKind.Friend
            ? await db.SocialRelationships.SingleOrDefaultAsync(x => x.Kind == kind &&
                ((x.SourceAccountId == accountId && x.TargetAccountId == targetId) ||
                 (x.SourceAccountId == targetId && x.TargetAccountId == accountId)), ct)
            : await db.SocialRelationships.SingleOrDefaultAsync(x => x.Kind == kind && x.SourceAccountId == accountId && x.TargetAccountId == targetId, ct);
        if (existing is not null) return Results.Conflict();

        var relationship = SocialRelationship.Create(accountId, targetId, kind);
        db.SocialRelationships.Add(relationship);
        await db.SaveChangesAsync(ct);
        await SocialEndpointSupport.NotifyAsync(db, directory, publisher, targetId, accountId,
            kind == RelationshipKind.Friend ? "friend-request" : "new-follower", "relationship", relationship.Id.ToString(), ct);
        return Results.Created($"/api/v2/social/relationships/{relationship.Id}", new { relationship.Id, status = relationship.Status.ToString().ToLowerInvariant() });
    }

    private static async Task<IResult> AcceptAsync(Guid relationshipId, ClaimsPrincipal user, ISocialDbContext db,
        ISocialResourceDirectory directory, ISocialNotificationPublisher publisher, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var relationship = await db.SocialRelationships.SingleOrDefaultAsync(x => x.Id == relationshipId, ct);
        if (relationship is null) return Results.NotFound();
        try { relationship.Accept(accountId); }
        catch (InvalidOperationException) { return Results.Forbid(); }
        await db.SaveChangesAsync(ct);
        await SocialEndpointSupport.NotifyAsync(db, directory, publisher, relationship.SourceAccountId, accountId,
            "friend-accepted", "relationship", relationship.Id.ToString(), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(Guid targetId, RelationshipKind kind, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var accountId = SocialEndpointSupport.AccountId(user);
        var query = db.SocialRelationships.Where(x => x.Kind == kind &&
            (kind == RelationshipKind.Friend
                ? (x.SourceAccountId == accountId && x.TargetAccountId == targetId) || (x.SourceAccountId == targetId && x.TargetAccountId == accountId)
                : x.SourceAccountId == accountId && x.TargetAccountId == targetId));
        await query.ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }
}
