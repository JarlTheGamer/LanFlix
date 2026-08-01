using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lanflix.Modules.Subtitles;

public static class SubtitleEndpoints
{
    public static IEndpointRouteBuilder MapSubtitlesModule(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v2/subtitles").RequireAuthorization().WithTags("Subtitles");
        api.MapGet("/{contentId:int}", async (int contentId, int? episodeId, ISubtitleCatalog catalog, CancellationToken ct) =>
        {
            var tracks = await catalog.GetTracksAsync(contentId, episodeId, ct);
            return tracks is null ? Results.NotFound() : Results.Ok(new { subtitles = tracks });
        });
        api.MapGet("/track/{contentId:int}/{subtitleIndex:int}", async (
            int contentId, int subtitleIndex, int? episodeId, double? startTime, ISubtitleCatalog catalog, CancellationToken ct) =>
        {
            var vtt = await catalog.GetWebVttAsync(contentId, subtitleIndex, episodeId, startTime, ct);
            return vtt is null ? Results.NotFound() : Results.Text(vtt, "text/vtt");
        });
        return endpoints;
    }
}
