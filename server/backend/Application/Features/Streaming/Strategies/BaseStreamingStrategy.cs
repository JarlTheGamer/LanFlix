using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Base class for streaming strategies with common functionality
/// </summary>
public abstract class BaseStreamingStrategy : IStreamingStrategy
{
    protected readonly ILogger Logger;

    protected BaseStreamingStrategy(ILogger logger)
    {
        Logger = logger;
    }

    public abstract StreamingMode Mode { get; }
    public abstract int Priority { get; }

    public abstract bool CanHandle(MediaInfo media, ClientCapabilities client);

    public abstract Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a video codec is supported by the client
    /// </summary>
    protected bool IsVideoCodecSupported(string codec, ClientCapabilities client)
    {
        return client.SupportedVideoCodecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if an audio codec is supported by the client
    /// </summary>
    protected bool IsAudioCodecSupported(string codec, ClientCapabilities client)
    {
        return client.SupportedAudioCodecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a container format is supported by the client
    /// </summary>
    protected bool IsContainerSupported(string container, ClientCapabilities client)
    {
        return client.SupportedContainers.Contains(container, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the video resolution exceeds client capabilities
    /// </summary>
    protected bool IsResolutionSupported(VideoStream video, ClientCapabilities client)
    {
        var videoPixels = video.Width * video.Height;
        
        return client.MaxResolution switch
        {
            VideoResolution.SD480p => videoPixels <= 720 * 480,
            VideoResolution.HD720p => videoPixels <= 1280 * 720,
            VideoResolution.HD1080p => videoPixels <= 1920 * 1080,
            VideoResolution.UHD4K => videoPixels <= 3840 * 2160,
            VideoResolution.UHD8K => videoPixels <= 7680 * 4320,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the video bitrate exceeds client capabilities
    /// </summary>
    protected bool IsBitrateSupported(MediaInfo media, ClientCapabilities client)
    {
        if (client.MaxBitrate <= 0)
            return true; // No bitrate limit

        var totalBitrate = media.OverallBitrate ?? media.Video.Bitrate;
        return totalBitrate <= client.MaxBitrate;
    }

    /// <summary>
    /// Checks if HDR content is supported by the client
    /// </summary>
    protected bool IsHdrSupported(VideoStream video, ClientCapabilities client)
    {
        if (!video.IsHDR)
            return true; // Not HDR content, always supported

        return client.SupportsHDR;
    }

    /// <summary>
    /// Gets the MIME type for a container format
    /// </summary>
    protected string GetMimeType(string container)
    {
        return container.ToLowerInvariant() switch
        {
            "mp4" => "video/mp4",
            "mkv" => "video/x-matroska",
            "webm" => "video/webm",
            "avi" => "video/x-msvideo",
            "mov" => "video/quicktime",
            "mpegts" or "ts" => "video/mp2t",
            "m3u8" => "application/vnd.apple.mpegurl",
            "mpd" => "application/dash+xml",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Parses HTTP range header
    /// </summary>
    protected (long start, long? end) ParseRangeHeader(string? rangeHeader, long fileSize)
    {
        if (string.IsNullOrEmpty(rangeHeader))
            return (0, null);

        // Format: "bytes=start-end" or "bytes=start-"
        var range = rangeHeader.Replace("bytes=", "").Trim();
        var parts = range.Split('-');

        if (parts.Length != 2)
            return (0, null);

        var start = long.TryParse(parts[0], out var s) ? s : 0;
        var end = long.TryParse(parts[1], out var e) ? e : fileSize - 1;

        return (start, end);
    }

    /// <summary>
    /// Validates that the file exists and is accessible
    /// </summary>
    protected void ValidateFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Media file not found: {filePath}");
        }
    }
}
