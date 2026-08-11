using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Remux strategy - changes container format while preserving codecs
/// Used when codecs are compatible but container format needs to be changed
/// </summary>
public class RemuxStrategy : IStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly ILogger<RemuxStrategy> _logger;

    public RemuxStrategy(
        ITranscodingPipeline transcodingPipeline,
        ILogger<RemuxStrategy> logger)
    {
        _transcodingPipeline = transcodingPipeline;
        _logger = logger;
    }

    public StreamingMode Mode => StreamingMode.DirectStream;
    public int Priority => 2; // Second highest priority

    public bool CanHandle(TranscodingDecision decision)
    {
        return decision.PlaybackMethod == PlaybackMethod.Remux;
    }

    public Task<StreamResult> ExecuteAsync(StreamRequest request, TranscodingDecision decision, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException($"Media file not found: {request.FilePath}");
        }

        _logger.LogInformation("Starting Remux for session {SessionId}, file: {FilePath}, {SourceContainer} -> {TargetContainer}",
            request.SessionId, request.FilePath, request.MediaInfo.Container, decision.TargetContainer);

        // Create transcode request for remuxing
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.DirectStream,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = "copy", // Copy video stream
            TargetAudioCodec = "copy", // Copy audio stream
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = HwAccelMethod.None, // No transcoding needed
            OutputFormat = decision.TargetContainer ?? "mpegts", // Use MPEG-TS for better seeking
            SessionId = request.SessionId,
            TotalDuration = request.MediaInfo.Duration.TotalSeconds,
            Duration = request.SegmentDuration
        };

        // Create remux stream
        var remuxStream = new RemuxStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            _logger,
            cancellationToken);

        var mimeType = GetMimeType(decision.TargetContainer ?? "mpegts");

        _logger.LogInformation("Remux prepared: {SourceContainer} -> {TargetContainer}",
            request.MediaInfo.Container, decision.TargetContainer);

        return Task.FromResult(new StreamResult
        {
            DataStream = remuxStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming remux
            Mode = StreamingMode.DirectStream,
            SupportsRangeRequests = false, // Range requests not supported during remux
            CleanupAction = () =>
            {
                _logger.LogDebug("Cleaning up Remux stream for session {SessionId}", request.SessionId);
                remuxStream.Dispose();
            }
        });
    }

    private string GetMimeType(string container)
    {
        return container.ToLowerInvariant() switch
        {
            "mp4" => "video/mp4",
            "mkv" => "video/x-matroska",
            "webm" => "video/webm",
            "mov" => "video/quicktime",
            "ts" or "mpegts" => "video/mp2t",
            "m3u8" or "hls" => "application/vnd.apple.mpegurl",
            "mpd" or "dash" => "application/dash+xml",
            _ => "video/mp2t" // Default to MPEG-TS MIME type for better seeking
        };
    }

    /// <summary>
    /// Stream implementation for remuxing operations
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
