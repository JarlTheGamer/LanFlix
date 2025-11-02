namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents a transcoding profile sent by the client containing its capabilities and constraints
/// The client sends its capabilities to the server, which then selects optimal settings
/// </summary>
public record TranscodingProfile
{
    /// <summary>
    /// Unique identifier for this profile
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Profile name/description
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Container formats supported by the client
    /// </summary>
    public string[] SupportedContainers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Video codecs supported by the client
    /// </summary>
    public VideoCodecProfile[] VideoCodecs { get; init; } = Array.Empty<VideoCodecProfile>();

    /// <summary>
    /// Audio codecs supported by the client
    /// </summary>
    public AudioCodecProfile[] AudioCodecs { get; init; } = Array.Empty<AudioCodecProfile>();

    /// <summary>
    /// Subtitle formats supported by the client
    /// </summary>
    public string[] SupportedSubtitleFormats { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Maximum bitrate the client can handle (in bits per second)
    /// </summary>
    public long MaxBitrate { get; init; }

    /// <summary>
    /// Maximum resolution the client can handle
    /// </summary>
    public VideoResolution MaxResolution { get; init; } = VideoResolution.HD1080p;

    /// <summary>
    /// Whether the client supports HDR content
    /// </summary>
    public bool SupportsHDR { get; init; }

    /// <summary>
    /// Maximum audio channels supported
    /// </summary>
    public int MaxAudioChannels { get; init; } = 2;

    /// <summary>
    /// Whether the client supports hardware acceleration
    /// </summary>
    public bool SupportsHardwareAcceleration { get; init; }

    /// <summary>
    /// Additional constraints or conditions
    /// </summary>
    public ProfileCondition[] Conditions { get; init; } = Array.Empty<ProfileCondition>();
}

/// <summary>
/// Video codec profile with specific constraints
/// </summary>
public record VideoCodecProfile
{
    /// <summary>
    /// Codec name (e.g., h264, hevc, vp9, av1)
    /// </summary>
    public string Codec { get; init; } = string.Empty;

    /// <summary>
    /// Maximum bitrate for this codec
    /// </summary>
    public long? MaxBitrate { get; init; }

    /// <summary>
    /// Maximum resolution for this codec
    /// </summary>
    public VideoResolution? MaxResolution { get; init; }

    /// <summary>
    /// Maximum frame rate for this codec
    /// </summary>
    public double? MaxFrameRate { get; init; }

    /// <summary>
    /// Supported profiles (e.g., baseline, main, high for H.264)
    /// </summary>
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Supported levels (e.g., 3.1, 4.0, 5.1 for H.264)
    /// </summary>
    public string[] SupportedLevels { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether HDR is supported for this codec
    /// </summary>
    public bool SupportsHDR { get; init; }

    /// <summary>
    /// Additional conditions for this codec
    /// </summary>
    public ProfileCondition[] Conditions { get; init; } = Array.Empty<ProfileCondition>();
}

/// <summary>
/// Audio codec profile with specific constraints
/// </summary>
public record AudioCodecProfile
{
    /// <summary>
    /// Codec name (e.g., aac, mp3, ac3, eac3, opus, dts)
    /// </summary>
    public string Codec { get; init; } = string.Empty;

    /// <summary>
    /// Maximum bitrate for this codec
    /// </summary>
    public long? MaxBitrate { get; init; }

    /// <summary>
    /// Maximum number of channels for this codec
    /// </summary>
    public int? MaxChannels { get; init; }

    /// <summary>
    /// Supported sample rates
    /// </summary>
    public int[] SupportedSampleRates { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Additional conditions for this codec
    /// </summary>
    public ProfileCondition[] Conditions { get; init; } = Array.Empty<ProfileCondition>();
}

/// <summary>
/// Profile condition for additional constraints
/// </summary>
public record ProfileCondition
{
    /// <summary>
    /// Property to check (e.g., "Width", "Height", "Bitrate", "FrameRate")
    /// </summary>
    public string Property { get; init; } = string.Empty;

    /// <summary>
    /// Condition type (e.g., "LessThanEqual", "GreaterThanEqual", "Equals")
    /// </summary>
    public ProfileConditionType Condition { get; init; }

    /// <summary>
    /// Value to compare against
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Whether this condition is required or optional
    /// </summary>
    public bool IsRequired { get; init; } = true;
}

/// <summary>
/// Types of profile conditions
/// </summary>
public enum ProfileConditionType
{
    Equals,
    NotEquals,
    LessThanEqual,
    GreaterThanEqual,
    EqualsAny,
    NotEqualsAny
}

/// <summary>
/// Video resolution options
/// </summary>
public enum VideoResolution
{
    SD480p,
    HD720p,
    HD1080p,
    UHD4K,
    UHD8K
}