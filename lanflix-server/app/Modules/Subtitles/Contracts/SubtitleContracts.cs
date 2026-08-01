namespace Lanflix.Modules.Subtitles;

public sealed record SubtitleTrackDto(
    int Index, string Language, string Title, string Format, bool IsForced, bool IsDefault, bool IsEmbedded, string Url);

public interface ISubtitleCatalog
{
    Task<IReadOnlyList<SubtitleTrackDto>?> GetTracksAsync(int contentId, int? episodeId, CancellationToken cancellationToken);
    Task<string?> GetWebVttAsync(int contentId, int subtitleIndex, int? episodeId, double? startSeconds, CancellationToken cancellationToken);
}
