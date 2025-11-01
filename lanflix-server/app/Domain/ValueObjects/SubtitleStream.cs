namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents subtitle stream information
/// </summary>
public record SubtitleStream
{
    /// <summary>
    /// Stream index in the media file
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Subtitle format (e.g., srt, ass, subrip, webvtt)
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// Language code (ISO 639-2, e.g., "eng", "spa", "fra")
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Human-readable title or description of the subtitle track
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Indicates whether this is the default subtitle track
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Indicates whether this is a forced subtitle track
    /// </summary>
    public bool IsForced { get; init; }

    /// <summary>
    /// Indicates whether subtitles are embedded in the video file
    /// </summary>
    public bool IsEmbedded { get; init; }

    /// <summary>
    /// External subtitle file path (if not embedded)
    /// </summary>
    public string? ExternalFilePath { get; init; }
}
