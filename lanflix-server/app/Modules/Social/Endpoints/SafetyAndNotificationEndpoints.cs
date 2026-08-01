using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class SafetyAndNotificationEndpoints
{
    public static void MapSafetyAndNotificationEndpoints(this RouteGroupBuilder social)
    {
        social.MapGet("/privacy", GetPrivacyAsync);
        social.MapPut("/privacy", UpdatePrivacyAsync);
        social.MapPut("/blocks/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct) => SaveBlockAsync(targetId, user, db, directory, ct));
        social.MapDelete("/blocks/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct) => DeleteBlockAsync(targetId, user, db, ct));
        social.MapPut("/mutes/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct) => SaveMuteAsync(targetId, user, db, directory, ct));
        social.MapDelete("/mutes/{targetId:guid}", (Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct) => DeleteMuteAsync(targetId, user, db, ct));
        social.MapGet("/notifications", GetNotificationsAsync);
        social.MapGet("/notifications/unread-count", GetUnreadCountAsync);
        social.MapPost("/notifications/{id:guid}/read", MarkReadAsync);
        social.MapPost("/notifications/read-all", MarkAllReadAsync);
        social.MapPost("/reports", CreateReportAsync);
    }

    private static async Task<IResult> GetPrivacyAsync(ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var id = SocialEndpointSupport.AccountId(user);
        var value = await db.SocialPrivacy.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == id, ct);
        return Results.Ok(new { defaultVisibility = (value?.DefaultVisibility ?? SocialVisibility.Friends).ToString().ToLowerInvariant(), activityEnabled = value?.ActivityEnabled ?? true });
    }

    private static async Task<IResult> UpdatePrivacyAsync(UpdatePrivacyRequest request, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var id = SocialEndpointSupport.AccountId(user);
        var value = await db.SocialPrivacy.SingleOrDefaultAsync(x => x.AccountId == id, ct);
        if (value is null) { value = SocialPrivacy.Create(id); db.SocialPrivacy.Add(value); }
        value.Update(request.DefaultVisibility, request.ActivityEnabled); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> SaveBlockAsync(Guid targetId, ClaimsPrincipal user, ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        var id = SocialEndpointSupport.AccountId(user);
        if (id == targetId || !await directory.AccountExistsAsync(targetId, ct)) return Results.NotFound();
        if (!await db.SocialBlocks.AnyAsync(x => x.AccountId == id && x.BlockedAccountId == targetId, ct)) db.SocialBlocks.Add(SocialBlock.Create(id, targetId));
        await db.SocialRelationships.Where(x => (x.SourceAccountId == id && x.TargetAccountId == targetId) || (x.SourceAccountId == targetId && x.TargetAccountId == id)).ExecuteDeleteAsync(ct);
        await db.SocialNotifications.Where(x => x.AccountId == id && x.ActorAccountId == targetId).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> DeleteBlockAsync(Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    { await db.SocialBlocks.Where(x => x.AccountId == SocialEndpointSupport.AccountId(user) && x.BlockedAccountId == targetId).ExecuteDeleteAsync(ct); return Results.NoContent(); }

    private static async Task<IResult> SaveMuteAsync(Guid targetId, ClaimsPrincipal user, ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        var id = SocialEndpointSupport.AccountId(user);
        if (id == targetId || !await directory.AccountExistsAsync(targetId, ct)) return Results.NotFound();
        if (!await db.SocialMutes.AnyAsync(x => x.AccountId == id && x.MutedAccountId == targetId, ct)) { db.SocialMutes.Add(SocialMute.Create(id, targetId)); await db.SaveChangesAsync(ct); }
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteMuteAsync(Guid targetId, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    { await db.SocialMutes.Where(x => x.AccountId == SocialEndpointSupport.AccountId(user) && x.MutedAccountId == targetId).ExecuteDeleteAsync(ct); return Results.NoContent(); }

    private static async Task<IResult> GetNotificationsAsync(int? offset, int? limit, ClaimsPrincipal user,
        ISocialDbContext db, ISocialResourceDirectory directory, CancellationToken ct)
    {
        var id = SocialEndpointSupport.AccountId(user); var skip = Math.Max(offset ?? 0, 0); var take = Math.Clamp(limit ?? 50, 1, 100);
        var values = await db.SocialNotifications.AsNoTracking().Where(x => x.AccountId == id).OrderByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
        var accounts = await directory.GetAccountsAsync(values.Where(x => x.ActorAccountId.HasValue).Select(x => x.ActorAccountId!.Value), ct);
        return Results.Ok(values.Select(x => new SocialNotificationDto(x.Id, x.ActorAccountId is Guid actor ? SocialEndpointSupport.Author(actor, accounts) : null,
            x.Kind, x.ResourceType, x.ResourceId, x.ReadAtUtc is not null, x.CreatedAtUtc)));
    }

    private static async Task<IResult> GetUnreadCountAsync(ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct) =>
        Results.Ok(new { count = await db.SocialNotifications.CountAsync(x => x.AccountId == SocialEndpointSupport.AccountId(user) && x.ReadAtUtc == null, ct) });

    private static async Task<IResult> MarkReadAsync(Guid id, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var value = await db.SocialNotifications.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == SocialEndpointSupport.AccountId(user), ct);
        if (value is null) return Results.NotFound(); value.MarkRead(); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var values = await db.SocialNotifications.Where(x => x.AccountId == SocialEndpointSupport.AccountId(user) && x.ReadAtUtc == null).ToListAsync(ct);
        foreach (var value in values) value.MarkRead(); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> CreateReportAsync(CreateReportRequest request, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        SocialReport report;
        try { report = SocialReport.Create(SocialEndpointSupport.AccountId(user), request.TargetType, request.TargetId, request.Reason); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        db.SocialReports.Add(report); await db.SaveChangesAsync(ct); return Results.Accepted($"/api/v2/social/reports/{report.Id}", new { report.Id });
    }
}
