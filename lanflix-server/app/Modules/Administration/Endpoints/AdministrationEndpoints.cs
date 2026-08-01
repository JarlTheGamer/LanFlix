using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Modules.Administration;

public static class AdministrationModule
{
    private static readonly HashSet<string> SupportedJobs = new(StringComparer.OrdinalIgnoreCase)
    { "library-scan", "music-scan", "live-tv-refresh", "update-check", "cleanup-transcodes" };

    public static IServiceCollection AddAdministrationModule(this IServiceCollection services)
    {
        services.AddSingleton<AdminJobQueue>();
        services.AddHostedService<AdminJobWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapAdministrationModule(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v2/admin").WithTags("Administration").RequireAuthorization("AdminOnly");
        admin.MapGet("/overview", async (IAdministrationOperations operations, CancellationToken ct) => Results.Ok(await operations.GetOverviewAsync(ct)));
        admin.MapGet("/telemetry", (IAdministrationOperations operations) => Results.Ok(operations.GetTelemetry()));
        admin.MapGet("/settings", async (IAdministrationOperations operations, CancellationToken ct) =>
            Results.Ok(await operations.GetSettingsAsync(ct)));
        admin.MapPut("/settings", async (AdministrationSettingsDto settings, IAdministrationOperations operations, CancellationToken ct) =>
            Results.Ok(await operations.UpdateSettingsAsync(settings, ct)));
        admin.MapGet("/jobs", async (IAdministrationDbContext db, CancellationToken ct) =>
            Results.Ok((await db.BackgroundJobRuns.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).Take(100).ToListAsync(ct)).Select(item => item.ToDto())));
        admin.MapPost("/jobs", QueueJobAsync);
        admin.MapGet("/logs", async (IAdministrationOperations operations, CancellationToken ct) => Results.Ok(await operations.GetLogsAsync(ct)));
        admin.MapGet("/logs/{name}", async (string name, int? lines, IAdministrationOperations operations, CancellationToken ct) =>
            await operations.ReadLogAsync(name, Math.Clamp(lines ?? 500, 1, 2000), ct) is { } log ? Results.Ok(log) : Results.NotFound());
        admin.MapGet("/updates/check", async (IAdministrationOperations operations, CancellationToken ct) => Results.Ok(await operations.CheckForUpdatesAsync(ct)));
        admin.MapGet("/updates/progress", (IAdministrationOperations operations) => Results.Ok(operations.GetUpdateProgress()));
        admin.MapPost("/updates/apply", async (ApplyUpdateRequest request, IAdministrationOperations operations, CancellationToken ct) =>
        {
            if (!Uri.TryCreate(request.DownloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return Results.Problem(statusCode: 400, title: "A valid HTTPS update URL is required");
            return await operations.ApplyUpdateAsync(uri.AbsoluteUri, ct)
                ? Results.Accepted()
                : Results.Problem(statusCode: 500, title: "Update could not be applied");
        });
        return endpoints;
    }

    private static async Task<IResult> QueueJobAsync(
        TriggerJobRequest request, IAdministrationDbContext db, AdminJobQueue queue, CancellationToken ct)
    {
        if (!SupportedJobs.Contains(request.Name))
            return Results.Problem(statusCode: 400, title: "Unsupported job", detail: string.Join(", ", SupportedJobs));
        if (await db.BackgroundJobRuns.AnyAsync(item => item.Name == request.Name && (item.Status == "pending" || item.Status == "running"), ct))
            return Results.Problem(statusCode: 409, title: "Job already queued");
        var job = BackgroundJobRun.Create(request.Name.ToLowerInvariant());
        db.BackgroundJobRuns.Add(job);
        await db.SaveChangesAsync(ct);
        if (!queue.TryEnqueue(job.Id)) return Results.Problem(statusCode: 503, title: "Job queue is full");
        return Results.Accepted($"/api/v2/admin/jobs/{job.Id}", job.ToDto());
    }
}
