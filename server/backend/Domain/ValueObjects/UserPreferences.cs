namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents user preferences for a profile
/// </summary>
public record UserPreferences
{
    /// <summary>
    /// Preferred audio language (ISO 639-2 code, e.g., "eng", "spa")
    /// </summary>
    public string? PreferredAudioLanguage { get; init; }

    /// <summary>
    /// Preferred subtitle language (ISO 639-2 code, e.g., "eng", "spa")
    /// </summary>
    public string? PreferredSubtitleLanguage { get; init; }

    /// <summary>
    /// Indicates whether subtitles should be enabled by default
    /// </summary>
    public bool SubtitlesEnabled { get; init; }

    /// <summary>
    /// Preferred video quality/bitrate in bits per second
    /// </summary>
    public long? PreferredBitrate { get; init; }

    /// <summary>
    /// Indicates whether to skip intro sequences automatically
    /// </summary>
    public bool AutoSkipIntro { get; init; }

    /// <summary>
    /// Indicates whether to auto-play next episode
    /// </summary>
    public bool AutoPlayNextEpisode { get; init; } = true;

    /// <summary>
    /// Maximum video resolution (e.g., "1080p", "4K")
    /// </summary>
    public string? MaxResolution { get; init; }

    /// <summary>
    /// Indicates whether to allow hardware acceleration for transcoding
    /// </summary>
    public bool AllowHardwareAcceleration { get; init; } = true;

    /// <summary>
    /// Theme preference (e.g., "light", "dark", "auto")
    /// </summary>
    public string Theme { get; init; } = "dark";
}
