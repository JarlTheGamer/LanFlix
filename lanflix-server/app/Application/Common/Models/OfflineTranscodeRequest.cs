namespace Lanflix.Application.Common.Models;

/// <summary>
/// Request to start an offline transcoding process for a media item
/// </summary>
public class OfflineTranscodeRequest
{
    /// <summary>
    /// Database ID of the content or episode
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// Type of media: "movie", "series", or "episode"
    /// </summary>
    public string Type { get; set; } = "movie";
}
