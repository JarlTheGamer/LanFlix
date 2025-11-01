namespace Lanflix.Domain.Enums;

/// <summary>
/// Represents the streaming mode used for media delivery
/// </summary>
public enum StreamingMode
{
    /// <summary>
    /// Direct play - no transcoding, file served as-is
    /// </summary>
    DirectPlay = 0,

    /// <summary>
    /// Direct stream - container remux only, codecs preserved
    /// </summary>
    DirectStream = 1,

    /// <summary>
    /// Transcode video only - video transcoded, audio copied
    /// </summary>
    TranscodeVideo = 2,

    /// <summary>
    /// Full transcode - both video and audio transcoded
    /// </summary>
    FullTranscode = 3
}
