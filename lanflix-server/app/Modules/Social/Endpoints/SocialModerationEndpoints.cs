using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class SocialModerationEndpoints
{
    public static void MapSocialModerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var moderation = endpoints.MapGroup("/api/v2/admin/social/reports").WithTags("Social moderation").RequireAuthorization("AdminOnly");
        moderation.MapGet("/", async (string? status, ISocialDbContext db, CancellationToken ct) =>
        {
            var query = db.SocialReports.AsNoTracking();
            if (Enum.TryParse<ReportStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
            var rows = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(ct);
            return Results.Ok(rows.Select(ToDto));
        });
        moderation.MapPost("/{id:guid}/resolve", ResolveAsync);
    }

    private static async Task<IResult> ResolveAsync(Guid id, ResolveReportRequest request, ClaimsPrincipal user, ISocialDbContext db, CancellationToken ct)
    {
        var report = await db.SocialReports.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (report is null) return Results.NotFound();
        try { report.Resolve(SocialEndpointSupport.AccountId(user), request.Dismiss, request.Resolution); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        await db.SaveChangesAsync(ct); return Results.Ok(ToDto(report));
    }

    private static SocialReportDto ToDto(SocialReport x) => new(x.Id, x.ReporterAccountId, x.TargetType, x.TargetId,
        x.Reason, x.Status.ToString().ToLowerInvariant(), x.Resolution, x.ModeratedByAccountId, x.CreatedAtUtc);
}
