namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents audio stream information
/// </summary>
public record AudioStream
{
    /// <summary>
    /// Stream index in the media file
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Audio codec (e.g., aac, mp3, ac3, eac3, opus, dts)
    /// </summary>
    public string Codec { get; init; } = string.Empty;

    /// <summary>
    /// Number of audio channels (e.g., 2 for stereo, 6 for 5.1)
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000)
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    /// Audio bitrate in bits per second
    /// </summary>
    public long Bitrate { get; init; }

    /// <summary>
    /// Language code (ISO 639-2, e.g., "eng", "spa", "fra")
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Human-readable title or description of the audio track
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Indicates whether this is the default audio track
    /// </summary>
    public bool IsDefault { get; init; }
}
