using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Direct stream strategy - remuxes container format while preserving codecs
/// Used when codecs are compatible but container format is not
/// </summary>
public class DirectStreamStrategy : BaseStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;

    public DirectStreamStrategy(
        ITranscodingPipeline transcodingPipeline,
        ILogger<DirectStreamStrategy> logger) : base(logger)
    {
        _transcodingPipeline = transcodingPipeline;
    }

    public override StreamingMode Mode => StreamingMode.DirectStream;

    public override int Priority => 2; // Second priority after DirectPlay

    public override bool CanHandle(MediaInfo media, ClientCapabilities client)
    {
        // Check if video codec is supported
        if (!IsVideoCodecSupported(media.Video.Codec, client))
        {
            Logger.LogDebug("DirectStream not possible: Video codec {Codec} not supported", media.Video.Codec);
            return false;
        }

        // Check if at least one audio codec is supported
        var hasCompatibleAudio = media.Audio.Any(a => IsAudioCodecSupported(a.Codec, client));
        if (!hasCompatibleAudio)
        {
            Logger.LogDebug("DirectStream not possible: No compatible audio codec found");
            return false;
        }

        // Check if container is NOT supported (that's why we need remux)
        if (IsContainerSupported(media.Container, client))
        {
            Logger.LogDebug("DirectStream not needed: Container {Container} is already supported", media.Container);
            return false;
        }

        // Check if resolution is supported
        if (!IsResolutionSupported(media.Video, client))
        {
            Logger.LogDebug("DirectStream not possible: Resolution {Width}x{Height} exceeds client max {MaxResolution}",
                media.Video.Width, media.Video.Height, client.MaxResolution);
            return false;
        }

        // Check if bitrate is within limits
        if (!IsBitrateSupported(media, client))
        {
            Logger.LogDebug("DirectStream not possible: Bitrate {Bitrate} exceeds client max {MaxBitrate}",
                media.OverallBitrate ?? media.Video.Bitrate, client.MaxBitrate);
            return false;
        }

        // Check if HDR is supported (if content is HDR)
        if (!IsHdrSupported(media.Video, client))
        {
            Logger.LogDebug("DirectStream not possible: HDR content not supported by client");
            return false;
        }

        Logger.LogInformation("DirectStream is possible: Remux {SourceContainer} to compatible container",
            media.Container);

        return true;
    }

    public override async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken)
    {
        ValidateFilePath(request.FilePath);

        Logger.LogInformation("Starting DirectStream (remux) for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        // Determine target container format based on client capabilities
        var targetContainer = DetermineTargetContainer(request.ClientCapabilities);

        // Create transcode request for remuxing (copy codecs, change container)
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.DirectStream,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = "copy", // Copy video codec
            TargetAudioCodec = "copy", // Copy audio codec
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = HwAccelMethod.None, // No transcoding, just remuxing
            OutputFormat = targetContainer
        };

        // Create a stream that reads from FFmpeg output
        var remuxStream = new RemuxStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            Logger,
            cancellationToken);

        var mimeType = GetMimeType(targetContainer);

        Logger.LogInformation("DirectStream remux prepared: {SourceContainer} -> {TargetContainer}, MimeType: {MimeType}",
            request.MediaInfo.Container, targetContainer, mimeType);

        return new StreamResult
        {
            DataStream = remuxStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming transcode
            Mode = StreamingMode.DirectStream,
            SupportsRangeRequests = false, // Range requests not supported during remux
            CleanupAction = () =>
            {
                Logger.LogDebug("Cleaning up DirectStream for session {SessionId}", request.SessionId);
                remuxStream.Dispose();
            }
        };
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

        // WebM as another option
        if (client.SupportedContainers.Contains("webm", StringComparer.OrdinalIgnoreCase))
            return "webm";

        // Default to MPEG-TS for streaming
        return "mpegts";
    }

    /// <summary>
    /// Stream implementation that reads from FFmpeg remux output
    /// </summary>
    private class RemuxStream : Stream
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

        public RemuxStream(
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
                _logger.LogDebug("RemuxStream disposed for session {SessionId}", _sessionId);
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
                _logger.LogDebug("RemuxStream disposed asynchronously for session {SessionId}", _sessionId);
            }
            await base.DisposeAsync();
        }
    }
}
