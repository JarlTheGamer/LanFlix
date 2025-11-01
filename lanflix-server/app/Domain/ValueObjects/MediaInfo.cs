namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents comprehensive media information for a content file
/// </summary>
public record MediaInfo
{
    /// <summary>
    /// Video stream information
    /// </summary>
    public VideoStream Video { get; init; } = null!;

    /// <summary>
    /// Collection of audio streams
    /// </summary>
    public List<AudioStream> Audio { get; init; } = new();

    /// <summary>
    /// Collection of subtitle streams
    /// </summary>
    public List<SubtitleStream> Subtitles { get; init; } = new();

    /// <summary>
    /// Total duration of the media
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Container format (e.g., mp4, mkv, avi, webm)
    /// </summary>
    public string Container { get; init; } = string.Empty;

    /// <summary>
    /// Overall bitrate in bits per second
    /// </summary>
    public long? OverallBitrate { get; init; }
}
