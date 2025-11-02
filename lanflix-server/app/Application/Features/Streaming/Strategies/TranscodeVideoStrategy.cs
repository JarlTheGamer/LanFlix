using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Transcode video strategy - transcodes video while copying audio
/// Used when video codec is incompatible but audio is compatible
/// </summary>
public class TranscodeVideoStrategy : BaseStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly IHardwareAccelerationDetector _hwAccelDetector;

    public TranscodeVideoStrategy(
        ITranscodingPipeline transcodingPipeline,
        IHardwareAccelerationDetector hwAccelDetector,
        ILogger<TranscodeVideoStrategy> logger) : base(logger)
    {
        _transcodingPipeline = transcodingPipeline;
        _hwAccelDetector = hwAccelDetector;
    }

    public override StreamingMode Mode => StreamingMode.TranscodeVideo;

    public override int Priority => 4; // Fourth priority (after audio transcoding)

    public override bool CanHandle(MediaInfo media, ClientCapabilities client)
    {
        // Check if video codec is NOT supported (that's why we need to transcode)
        if (IsVideoCodecSupported(media.Video.Codec, client))
        {
            Logger.LogDebug("TranscodeVideo not needed: Video codec {Codec} is already supported", media.Video.Codec);
            return false;
        }

        // Check if at least one audio codec IS supported (so we can copy it)
        var hasCompatibleAudio = media.Audio.Any(a => IsAudioCodecSupported(a.Codec, client));
        if (!hasCompatibleAudio)
        {
            Logger.LogDebug("TranscodeVideo not optimal: No compatible audio codec found, full transcode needed");
            return false;
        }

        Logger.LogInformation("TranscodeVideo is needed: Video codec {VideoCodec} -> compatible codec, Audio: copy",
            media.Video.Codec);

        return true;
    }

    public override async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken)
    {
        ValidateFilePath(request.FilePath);

        Logger.LogInformation("Starting TranscodeVideo for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        // Detect hardware acceleration capabilities
        var hwAccelCapabilities = await _hwAccelDetector.DetectAsync();

        // Determine target video codec based on client capabilities
        var targetVideoCodec = DetermineTargetVideoCodec(request.ClientCapabilities, hwAccelCapabilities);

        // Determine target bitrate and resolution
        var (targetBitrate, targetWidth, targetHeight) = DetermineTargetVideoSettings(
            request.MediaInfo.Video,
            request.ClientCapabilities,
            request.UserPreferences);

        // Determine target container
        var targetContainer = DetermineTargetContainer(request.ClientCapabilities);

        // Create transcode request
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.TranscodeVideo,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = targetVideoCodec,
            TargetAudioCodec = "copy", // Copy audio codec
            TargetVideoBitrate = targetBitrate,
            TargetWidth = targetWidth,
            TargetHeight = targetHeight,
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = hwAccelCapabilities.PreferredMethod,
            OutputFormat = targetContainer
        };

        // Create transcoding stream
        var transcodeStream = new TranscodeStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            Logger,
            cancellationToken);

        var mimeType = GetMimeType(targetContainer);

        Logger.LogInformation(
            "TranscodeVideo prepared: {SourceCodec} -> {TargetCodec}, Bitrate: {Bitrate}, Resolution: {Width}x{Height}, HwAccel: {HwAccel}",
            request.MediaInfo.Video.Codec, targetVideoCodec, targetBitrate, targetWidth, targetHeight, hwAccelCapabilities.PreferredMethod);

        return new StreamResult
        {
            DataStream = transcodeStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming transcode
            Mode = StreamingMode.TranscodeVideo,
            SupportsRangeRequests = false, // Range requests not supported during transcode
            CleanupAction = () =>
            {
                Logger.LogDebug("Cleaning up TranscodeVideo stream for session {SessionId}", request.SessionId);
                transcodeStream.Dispose();
            }
        };
    }

    /// <summary>
    /// Determines the best target video codec based on client capabilities and hardware acceleration
    /// </summary>
    private string DetermineTargetVideoCodec(ClientCapabilities client, HwAccelCapabilities hwAccel)
    {
        // Try H.265/HEVC with hardware acceleration if supported
        if (client.SupportedVideoCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase) ||
            client.SupportedVideoCodecs.Contains("h265", StringComparer.OrdinalIgnoreCase))
        {
            return hwAccel.PreferredMethod switch
            {
                HwAccelMethod.Nvenc => "hevc_nvenc",
                HwAccelMethod.QuickSync => "hevc_qsv",
                HwAccelMethod.Amf => "hevc_amf",
                HwAccelMethod.Vaapi => "hevc_vaapi",
                HwAccelMethod.VideoToolbox => "hevc_videotoolbox",
                _ => "libx265" // Software encoding
            };
        }

        // Fall back to H.264 (most widely supported)
        return hwAccel.PreferredMethod switch
        {
            HwAccelMethod.Nvenc => "h264_nvenc",
            HwAccelMethod.QuickSync => "h264_qsv",
            HwAccelMethod.Amf => "h264_amf",
            HwAccelMethod.Vaapi => "h264_vaapi",
            HwAccelMethod.VideoToolbox => "h264_videotoolbox",
            _ => "libx264" // Software encoding
        };
    }

    /// <summary>
    /// Determines target video settings (bitrate, resolution) based on client capabilities and preferences
    /// </summary>
    private (long bitrate, int? width, int? height) DetermineTargetVideoSettings(
        VideoStream sourceVideo,
        ClientCapabilities client,
        UserPreferences? preferences)
    {
        // Start with source resolution
        var targetWidth = sourceVideo.Width;
        var targetHeight = sourceVideo.Height;

        // Scale down if resolution exceeds client capabilities
        var maxPixels = client.MaxResolution switch
        {
            VideoResolution.SD480p => 720 * 480,
            VideoResolution.HD720p => 1280 * 720,
            VideoResolution.HD1080p => 1920 * 1080,
            VideoResolution.UHD4K => 3840 * 2160,
            VideoResolution.UHD8K => 7680 * 4320,
            _ => 1920 * 1080
        };

        var sourcePixels = sourceVideo.Width * sourceVideo.Height;
        if (sourcePixels > maxPixels)
        {
            // Calculate scale factor
            var scaleFactor = Math.Sqrt((double)maxPixels / sourcePixels);
            targetWidth = (int)(sourceVideo.Width * scaleFactor);
            targetHeight = (int)(sourceVideo.Height * scaleFactor);

            // Ensure dimensions are even (required by most codecs)
            targetWidth = targetWidth / 2 * 2;
            targetHeight = targetHeight / 2 * 2;
        }

        // Determine target bitrate
        long targetBitrate;
        if (client.MaxBitrate > 0 && sourceVideo.Bitrate > client.MaxBitrate)
        {
            // Use client's max bitrate
            targetBitrate = client.MaxBitrate;
        }
        else
        {
            // Use reasonable bitrate based on resolution
            targetBitrate = (targetWidth * targetHeight) switch
            {
                <= 720 * 480 => 2_000_000,      // 2 Mbps for SD
                <= 1280 * 720 => 4_000_000,     // 4 Mbps for 720p
                <= 1920 * 1080 => 8_000_000,    // 8 Mbps for 1080p
                <= 3840 * 2160 => 20_000_000,   // 20 Mbps for 4K
                _ => 40_000_000                  // 40 Mbps for 8K
            };

            // Don't exceed source bitrate
            if (sourceVideo.Bitrate > 0 && targetBitrate > sourceVideo.Bitrate)
            {
                targetBitrate = sourceVideo.Bitrate;
            }
        }

        // Return null for width/height if no scaling needed
        int? finalWidth = targetWidth != sourceVideo.Width ? targetWidth : null;
        int? finalHeight = targetHeight != sourceVideo.Height ? targetHeight : null;

        return (targetBitrate, finalWidth, finalHeight);
    }

    /// <summary>
    /// Determines the best target container format based on client capabilities
    /// </summary>
    private string DetermineTargetContainer(ClientCapabilities client)
    {
        // Prefer MP4 if supported
        if (client.SupportedContainers.Contains("mp4", StringComparer.OrdinalIgnoreCase))
            return "mp4";

        // Fall back to MPEG-TS for streaming
        if (client.SupportedContainers.Contains("mpegts", StringComparer.OrdinalIgnoreCase) ||
            client.SupportedContainers.Contains("ts", StringComparer.OrdinalIgnoreCase))
            return "mpegts";

        // Default to MPEG-TS
        return "mpegts";
    }

    /// <summary>
    /// Stream implementation that reads from FFmpeg transcode output
    /// </summary>
    private class TranscodeStream : Stream
    {
        private readonly ITranscodingPipeline _pipeline;
        private readonly TranscodeRequest _request;
        private readonly string _sessionId;
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;
        private IAsyncEnumerator<ReadOnlyMemory<byte>>? _enumerator;
        private ReadOnlyMemory<byte> _currentChunk;
        private int _currentChunkPosition;
        private bool _disposed;

        public TranscodeStream(
            ITranscodingPipeline pipeline,
            TranscodeRequest request,
            string sessionId,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            _pipeline = pipeline;
            _request = request;
            _sessionId = sessionId;
            _logger = logger;
            _cancellationToken = cancellationToken;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, _cancellationToken).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_disposed)
                return 0;

            // Initialize enumerator on first read
            if (_enumerator == null)
            {
                _enumerator = _pipeline.StreamAsync(_request, cancellationToken).GetAsyncEnumerator(cancellationToken);
            }

            var totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                // If current chunk is exhausted, get next chunk
                if (_currentChunkPosition >= _currentChunk.Length)
                {
                    if (!await _enumerator.MoveNextAsync())
                    {
                        // No more data
                        break;
                    }

                    _currentChunk = _enumerator.Current;
                    _currentChunkPosition = 0;
                }

                // Copy from current chunk to buffer
                var bytesToCopy = Math.Min(count - totalBytesRead, _currentChunk.Length - _currentChunkPosition);
                _currentChunk.Slice(_currentChunkPosition, bytesToCopy).Span.CopyTo(
                    buffer.AsSpan(offset + totalBytesRead, bytesToCopy));

                _currentChunkPosition += bytesToCopy;
                totalBytesRead += bytesToCopy;
            }

            return totalBytesRead;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                _enumerator?.DisposeAsync().AsTask().Wait();
                _logger.LogDebug("TranscodeStream disposed for session {SessionId}", _sessionId);
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync();
                }
                _logger.LogDebug("TranscodeStream disposed asynchronously for session {SessionId}", _sessionId);
            }
            await base.DisposeAsync();
        }
    }
}
