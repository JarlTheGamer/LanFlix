using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.Modules.Downloads;

public static class DownloadEndpoints
{
    public static IEndpointRouteBuilder MapDownloadsModule(this IEndpointRouteBuilder endpoints)
    {
        var downloads = endpoints.MapGroup("/api/v2/downloads")
            .WithTags("Downloads")
            .RequireAuthorization();

        downloads.MapGet("/queue", async (IDownloadQueue queue, CancellationToken ct) => Results.Ok(await queue.GetAsync(ct)));
        downloads.MapDelete("/queue/{provider:regex(^(radarr|sonarr)$)}/{queueId:int}", async (
            string provider, int queueId, [FromBody] CancelDownloadRequest request, IDownloadQueue queue, CancellationToken ct) =>
            await queue.CancelAsync(provider, queueId, request, ct)
                ? Results.NoContent()
                : Results.Problem(statusCode: 404, title: "Download not found"))
            .RequireAuthorization("ServerManage");
        return endpoints;
    }
}
