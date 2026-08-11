using Lanflix.Domain.Enums;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Playback;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Adapters.Playback;

internal sealed class SqlitePlaybackSourceCatalog(ApplicationDbContext db) : IPlaybackSourceCatalog
{
    public async Task<PlaybackSource?> FindAsync(string kind, int id, CancellationToken cancellationToken)
    {
        if (string.Equals(kind, "movie", StringComparison.OrdinalIgnoreCase))
        {
            var movie = await db.Contents.AsNoTracking()
                .Where(item => item.Id == id && item.Type == ContentType.Movie)
                .Select(item => new { item.Id, item.Title, item.FilePath, Duration = item.MediaInfo != null ? item.MediaInfo.Duration : TimeSpan.Zero })
                .SingleOrDefaultAsync(cancellationToken);
            return movie is null ? null : Create(movie.Id, "movie", movie.Title, movie.FilePath, null, null, null, null, null, movie.Duration.TotalSeconds);
        }

        if (!string.Equals(kind, "episode", StringComparison.OrdinalIgnoreCase)) return null;
        var episode = await db.Episodes.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id, item.Title, item.FilePath, item.SeasonNumber, item.EpisodeNumber,
                item.IntroStartTime, item.IntroEndTime, item.CreditsStartTime,
                Duration = item.MediaInfo != null ? item.MediaInfo.Duration : TimeSpan.Zero
            })
            .SingleOrDefaultAsync(cancellationToken);
        return episode is null ? null : Create(episode.Id, "episode", episode.Title, episode.FilePath,
            episode.SeasonNumber, episode.EpisodeNumber, episode.IntroStartTime, episode.IntroEndTime, episode.CreditsStartTime, episode.Duration.TotalSeconds);
    }

    private static PlaybackSource? Create(
        int id, string kind, string title, string? path, int? season, int? episode,
        double? introStart, double? introEnd, double? creditsStart, double durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        var file = new FileInfo(path);
        return new PlaybackSource(id, kind, title, file.FullName, MimeType(file.Extension), file.Length,
            season, episode, introStart, introEnd, creditsStart, durationSeconds);
    }

    private static string MimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".mkv" => "video/x-matroska",
        ".webm" => "video/webm",
        ".m4v" => "video/x-m4v",
        ".mov" => "video/quicktime",
        ".avi" => "video/x-msvideo",
        _ => "application/octet-stream"
    };
}
