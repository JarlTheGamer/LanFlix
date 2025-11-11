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
    /// Subtitle appearance settings
    /// </summary>
    public SubtitleAppearance SubtitleAppearance { get; init; } = new();

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
    /// Forces transcoding even when direct play is possible (for testing or bandwidth control)
    /// </summary>
    public bool ForceTranscode { get; init; }

    /// <summary>
    /// Theme preference (e.g., "light", "dark", "auto")
    /// </summary>
    public string Theme { get; init; } = "dark";
}

/// <summary>
/// Subtitle appearance and styling preferences (Jellyfin-style)
/// </summary>
public record SubtitleAppearance
{
    /// <summary>
    /// Font size in percentage (50-200, default 100)
    /// </summary>
    public int FontSize { get; init; } = 100;

    /// <summary>
    /// Font family (e.g., "Arial", "Roboto", "sans-serif")
    /// </summary>
    public string FontFamily { get; init; } = "Arial";

    /// <summary>
    /// Text color in hex format (e.g., "#FFFFFF")
    /// </summary>
    public string TextColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Background color in hex format with alpha (e.g., "#000000")
    /// </summary>
    public string BackgroundColor { get; init; } = "#000000";

    /// <summary>
    /// Background opacity (0-100, default 75)
    /// </summary>
    public int BackgroundOpacity { get; init; } = 75;

    /// <summary>
    /// Text outline/stroke width (0-4, default 2)
    /// </summary>
    public int OutlineWidth { get; init; } = 2;

    /// <summary>
    /// Outline color in hex format (e.g., "#000000")
    /// </summary>
    public string OutlineColor { get; init; } = "#000000";

    /// <summary>
    /// Vertical position (0-100, 0=top, 100=bottom, default 90)
    /// </summary>
    public int VerticalPosition { get; init; } = 90;

    /// <summary>
    /// Text alignment ("left", "center", "right", default "center")
    /// </summary>
    public string TextAlign { get; init; } = "center";

    /// <summary>
    /// Font weight ("normal", "bold", default "normal")
    /// </summary>
    public string FontWeight { get; init; } = "normal";

    /// <summary>
    /// Font style ("normal", "italic", default "normal")
    /// </summary>
    public string FontStyle { get; init; } = "normal";
}
