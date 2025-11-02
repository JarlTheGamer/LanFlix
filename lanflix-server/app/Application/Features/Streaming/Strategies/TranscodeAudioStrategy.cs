using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Transcode audio strategy - transcodes audio while copying video
/// Used when audio codec is incompatible but video is compatible
/// This corresponds to "Direct Stream" in Jellyfin terminology
/// </summary>
public class TranscodeAudioStrategy : BaseStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;

    public TranscodeAudioStrategy(
        ITranscodingPipeline transcodingPipeline,
        ILogger<TranscodeAudioStrategy> logger) : base(logger)
    {
        _transcodingPipeline = transcodingPipeline;
    }

    public override StreamingMode Mode => StreamingMode.TranscodeAudio;

    public override int Priority => 3; // Between DirectStream (2) and TranscodeVideo (4)

    public override bool CanHandle(MediaInfo media, ClientCapabilities client)
    {
        // Check if video codec IS supported (so we can copy it)
        if (!IsVideoCodecSupported(media.Video.Codec, client))
        {
            Logger.LogDebug("TranscodeAudio not optimal: Video codec {Codec} not supported, full transcode needed", media.Video.Codec);
            return false;
        }

        // Check if container is supported
        if (!IsContainerSupported(media.Container, client))
        {
            Logger.LogDebug("TranscodeAudio not optimal: Container {Container} not supported, remux needed first", media.Container);
            return false;
        }

        // Check if NO audio codec is supported (that's why we need to transcode audio)
        var hasCompatibleAudio = media.Audio.Any(a => IsAudioCodecSupported(a.Codec, client));
        if (hasCompatibleAudio)
        {
            Logger.LogDebug("TranscodeAudio not needed: Compatible audio codec found");
            return false;
        }

        // Check if resolution is supported
        if (!IsResolutionSupported(media.Video, client))
        {
            Logger.LogDebug("TranscodeAudio not possible: Resolution {Width}x{Height} exceeds client max {MaxResolution}",
                media.Video.Width, media.Video.Height, client.MaxResolution);
            return false;
        }

        // Check if bitrate is within limits
        if (!IsBitrateSupported(media, client))
        {
            Logger.LogDebug("TranscodeAudio not possible: Bitrate {Bitrate} exceeds client max {MaxBitrate}",
                media.OverallBitrate ?? media.Video.Bitrate, client.MaxBitrate);
            return false;
        }

        // Check if HDR is supported (if content is HDR)
        if (!IsHdrSupported(media.Video, client))
        {
            Logger.LogDebug("TranscodeAudio not possible: HDR content not supported by client");
            return false;
        }

        Logger.LogInformation("TranscodeAudio is needed: Video codec {VideoCodec} -> copy, Audio: {AudioCodec} -> compatible codec",
            media.Video.Codec, string.Join(", ", media.Audio.Select(a => a.Codec)));

        return true;
    }

    public override async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken)
    {
        ValidateFilePath(request.FilePath);

        Logger.LogInformation("Starting TranscodeAudio (Direct Stream) for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        // Determine target audio codec based on client capabilities
        var targetAudioCodec = DetermineTargetAudioCodec(request.ClientCapabilities);

        // Determine target container (keep same if supported, otherwise use compatible one)
        var targetContainer = IsContainerSupported(request.MediaInfo.Container, request.ClientCapabilities)
            ? request.MediaInfo.Container
            : DetermineTargetContainer(request.ClientCapabilities);

        // Create transcode request for audio-only transcoding
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.TranscodeAudio,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = "copy", // Copy video codec
            TargetAudioCodec = targetAudioCodec, // Transcode audio
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = HwAccelMethod.None, // No video transcoding
            OutputFormat = targetContainer
        };

        // Create transcoding stream
        var transcodeStream = new TranscodeAudioStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            Logger,
            cancellationToken);

        var mimeType = GetMimeType(targetContainer);

        Logger.LogInformation(
            "TranscodeAudio prepared: Video -> copy, Audio: {SourceAudio} -> {TargetAudio}, Container: {Container}",
            string.Join(", ", request.MediaInfo.Audio.Select(a => a.Codec)), 
            targetAudioCodec, 
            targetContainer);

        return new StreamResult
        {
            DataStream = transcodeStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming transcode
            Mode = StreamingMode.TranscodeAudio,
            SupportsRangeRequests = false, // Range requests not supported during transcode
            CleanupAction = () =>
            {
                Logger.LogDebug("Cleaning up TranscodeAudio stream for session {SessionId}", request.SessionId);
                transcodeStream.Dispose();
            }
        };
    }

    /// <summary>
    /// Determines the best target audio codec based on client capabilities
    /// </summary>
    private string DetermineTargetAudioCodec(ClientCapabilities client)
    {
        // Prefer AAC (most widely supported)
        if (client.SupportedAudioCodecs.Contains("aac", StringComparer.OrdinalIgnoreCase))
            return "aac";

        // Fall back to MP3 (universal compatibility)
        if (client.SupportedAudioCodecs.Contains("mp3", StringComparer.OrdinalIgnoreCase))
            return "mp3";

        // AC3 for surround sound
        if (client.SupportedAudioCodecs.Contains("ac3", StringComparer.OrdinalIgnoreCase))
            return "ac3";

        // Default to AAC
        return "aac";
    }

    /// <summary>
    /// Determines the best target container format based on client capabilities
    /// </summary>
    private string DetermineTargetContainer(ClientCapabilities client)
    {
        // Prefer MP4 if supported (widely compatible)
        if (client.SupportedContainers.Contains("mp4", StringComparer.OrdinalIgnoreCase))
            return "mp4";

        // Fall back to MPEG-TS (universal streaming format)
        if (client.SupportedContainers.Contains("mpegts", StringComparer.OrdinalIgnoreCase) ||
            client.SupportedContainers.Contains("ts", StringComparer.OrdinalIgnoreCase))
            return "mpegts";

        // Default to MP4
        return "mp4";
    }

    /// <summary>
    /// Stream implementation that reads from FFmpeg audio transcode output
    /// </summary>
    private class TranscodeAudioStream : Stream
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

        public TranscodeAudioStream(
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
                _logger.LogDebug("TranscodeAudioStream disposed for session {SessionId}", _sessionId);
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
                _logger.LogDebug("TranscodeAudioStream disposed asynchronously for session {SessionId}", _sessionId);
            }
            await base.DisposeAsync();
        }
    }
}