namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Represents video stream information
/// </summary>
public record VideoStream
{
    /// <summary>
    /// Video codec (e.g., h264, hevc, vp9, av1)
    /// </summary>
    public string Codec { get; init; } = string.Empty;

    /// <summary>
    /// Video width in pixels
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Video height in pixels
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Video bitrate in bits per second
    /// </summary>
    public long Bitrate { get; init; }

    /// <summary>
    /// Frame rate (frames per second)
    /// </summary>
    public double FrameRate { get; init; }

    /// <summary>
    /// Pixel format (e.g., yuv420p, yuv420p10le)
    /// </summary>
    public string PixelFormat { get; init; } = string.Empty;

    /// <summary>
    /// Color space (e.g., bt709, bt2020)
    /// </summary>
    public string? ColorSpace { get; init; }

    /// <summary>
    /// Indicates whether the video contains HDR content
    /// </summary>
    public bool IsHDR { get; init; }

    /// <summary>
    /// HDR format (e.g., HDR10, Dolby Vision, HDR10+)
    /// </summary>
    public string? HdrFormat { get; init; }
}
