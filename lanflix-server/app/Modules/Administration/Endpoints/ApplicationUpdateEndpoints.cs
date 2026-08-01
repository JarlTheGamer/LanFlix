using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lanflix.Modules.Administration;

public static class ApplicationUpdateEndpoints
{
    public static IEndpointRouteBuilder MapApplicationUpdatesModule(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v2/app").WithTags("Application updates");
        api.MapGet("/update-check", async (int currentVersion, IApplicationReleaseCatalog catalog, CancellationToken ct) =>
        {
            var release = await catalog.GetLatestAsync(currentVersion, ct);
            return release is null ? Results.Ok(new { hasUpdate = false }) : Results.Ok(new { hasUpdate = true, release });
        });
        api.MapGet("/download/{fileName}", async (string fileName, IApplicationReleaseCatalog catalog, CancellationToken ct) =>
        {
            var release = await catalog.GetFileAsync(fileName, ct);
            return release is null ? Results.NotFound() : Results.File(release.Path, "application/vnd.android.package-archive", release.DownloadName, enableRangeProcessing: true);
        });
        api.MapGet("/version", () => Results.Ok(new
        {
            serverVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0",
            apiVersion = "2.0"
        }));
        return endpoints;
    }
}
