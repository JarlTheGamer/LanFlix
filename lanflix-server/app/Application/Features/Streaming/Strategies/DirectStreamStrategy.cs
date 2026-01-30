using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Direct Stream strategy - transcodes audio while copying video
/// Used when video codec is compatible but audio needs transcoding
/// </summary>
public class DirectStreamStrategy : IStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly ILogger<DirectStreamStrategy> _logger;

    public DirectStreamStrategy(
        ITranscodingPipeline transcodingPipeline,
        ILogger<DirectStreamStrategy> logger)
    {
        _transcodingPipeline = transcodingPipeline;
        _logger = logger;
    }

    public StreamingMode Mode => StreamingMode.TranscodeAudio;
    public int Priority => 3; // Third priority

    public bool CanHandle(TranscodingDecision decision)
    {
        return decision.PlaybackMethod == PlaybackMethod.DirectStream;
    }

    public Task<StreamResult> ExecuteAsync(StreamRequest request, TranscodingDecision decision, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException($"Media file not found: {request.FilePath}");
        }

        _logger.LogInformation("Starting DirectStream for session {SessionId}, file: {FilePath}, Audio: {SourceAudio} -> {TargetAudio}",
            request.SessionId, request.FilePath, 
            string.Join(", ", request.MediaInfo.Audio.Select(a => a.Codec)), 
            decision.TargetAudioCodec);

        // Create transcode request for audio-only transcoding
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.TranscodeAudio,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = "copy", // Copy video stream
            TargetAudioCodec = decision.TargetAudioCodec ?? "aac",
            TargetAudioBitrate = decision.TargetAudioBitrate,
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = HwAccelMethod.None, // No video transcoding
            OutputFormat = decision.TargetContainer ?? "mpegts", // Use MPEG-TS for better seeking
            SessionId = request.SessionId,
            TotalDuration = request.MediaInfo.Duration.TotalSeconds
        };

        // Create direct stream
        var directStream = new DirectStreamTranscodeStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            _logger,
            cancellationToken);

        var mimeType = GetMimeType(decision.TargetContainer ?? "mpegts");

        _logger.LogInformation("DirectStream prepared: Video -> copy, Audio: {SourceAudio} -> {TargetAudio}, Container: {Container}",
            string.Join(", ", request.MediaInfo.Audio.Select(a => a.Codec)), 
            decision.TargetAudioCodec, 
            decision.TargetContainer);

        return Task.FromResult(new StreamResult
        {
            DataStream = directStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming transcode
            Mode = StreamingMode.TranscodeAudio,
            SupportsRangeRequests = false, // Range requests not supported during transcode
            CleanupAction = () =>
            {
                _logger.LogDebug("Cleaning up DirectStream for session {SessionId}", request.SessionId);
                directStream.Dispose();
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
    /// Stream implementation for DirectStream (audio transcode only)
    /// </summary>
    private class DirectStreamTranscodeStream : Stream
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

        public DirectStreamTranscodeStream(
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
                _logger.LogDebug("DirectStreamTranscodeStream disposed for session {SessionId}", _sessionId);
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
                _logger.LogDebug("DirectStreamTranscodeStream disposed asynchronously for session {SessionId}", _sessionId);
            }
            await base.DisposeAsync();
        }
    }
}