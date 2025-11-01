namespace Lanflix.Application.Common.Models;

/// <summary>
/// Represents transcoding progress information
/// </summary>
public class TranscodingProgress
{
    /// <summary>
    /// Session ID
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Current frame number being processed
    /// </summary>
    public long Frame { get; init; }

    /// <summary>
    /// Frames per second being processed
    /// </summary>
    public double Fps { get; init; }

    /// <summary>
    /// Current bitrate in bits per second
    /// </summary>
    public long Bitrate { get; init; }

    /// <summary>
    /// Total size of output in bytes
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// Current time position in the video (seconds)
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    /// Total duration of the video (seconds)
    /// </summary>
    public double TotalDuration { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double PercentComplete { get; init; }

    /// <summary>
    /// Processing speed relative to playback speed (e.g., 2.0 = 2x realtime)
    /// </summary>
    public double Speed { get; init; }

    /// <summary>
    /// Timestamp when this progress was reported
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
