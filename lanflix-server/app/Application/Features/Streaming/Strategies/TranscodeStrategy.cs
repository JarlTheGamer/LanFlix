using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Transcode strategy - transcodes both video and audio
/// Fallback strategy that handles all incompatible media
/// </summary>
public class TranscodeStrategy : IStreamingStrategy
{
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly ILogger<TranscodeStrategy> _logger;

    public TranscodeStrategy(
        ITranscodingPipeline transcodingPipeline,
        ILogger<TranscodeStrategy> logger)
    {
        _transcodingPipeline = transcodingPipeline;
        _logger = logger;
    }

    public StreamingMode Mode => StreamingMode.FullTranscode;
    public int Priority => 4; // Lowest priority (fallback)

    public bool CanHandle(TranscodingDecision decision)
    {
        return decision.PlaybackMethod == PlaybackMethod.Transcode;
    }

    public Task<StreamResult> ExecuteAsync(StreamRequest request, TranscodingDecision decision, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException($"Media file not found: {request.FilePath}");
        }

        // Validate that we have usable media information
        if (request.MediaInfo.Video.Codec == "unknown" || 
            request.MediaInfo.Video.Width == 0 || 
            request.MediaInfo.Video.Height == 0)
        {
            _logger.LogError("Cannot transcode media with unknown or invalid video stream information. " +
                           "Video codec: {VideoCodec}, Resolution: {Width}x{Height}",
                           request.MediaInfo.Video.Codec, request.MediaInfo.Video.Width, request.MediaInfo.Video.Height);
            throw new InvalidOperationException("Media file has invalid or undetectable video stream. " +
                                              "The file may be corrupted or in an unsupported format.");
        }

        _logger.LogInformation("Starting Transcode for session {SessionId}, file: {FilePath}, " +
                             "Video: {SourceVideo} -> {TargetVideo}, Audio: {SourceAudio} -> {TargetAudio}",
            request.SessionId, request.FilePath,
            request.MediaInfo.Video.Codec, decision.TargetVideoCodec,
            string.Join(", ", request.MediaInfo.Audio.Select(a => a.Codec)), decision.TargetAudioCodec);

        // Create transcode request
        var transcodeRequest = new TranscodeRequest
        {
            InputPath = request.FilePath,
            Mode = StreamingMode.FullTranscode,
            SourceMedia = request.MediaInfo,
            TargetVideoCodec = decision.TargetVideoCodec ?? "libx264",
            TargetAudioCodec = decision.TargetAudioCodec ?? "aac",
            TargetVideoBitrate = decision.TargetVideoBitrate,
            TargetAudioBitrate = decision.TargetAudioBitrate,
            TargetWidth = decision.TargetWidth,
            TargetHeight = decision.TargetHeight,
            StartPosition = request.StartPosition,
            AudioStreamIndex = request.AudioStreamIndex,
            SubtitleStreamIndex = request.SubtitleStreamIndex,
            HwAccelMethod = decision.HwAccelMethod,
            OutputFormat = decision.TargetContainer ?? "mpegts", // Use MPEG-TS for better seeking (Jellyfin-style)
            SessionId = request.SessionId,
            TotalDuration = request.MediaInfo.Duration.TotalSeconds
        };

        // Create transcoding stream
        var transcodeStream = new FullTranscodeStream(
            _transcodingPipeline,
            transcodeRequest,
            request.SessionId,
            _logger,
            cancellationToken);

        var mimeType = GetMimeType(decision.TargetContainer ?? "mp4");

        _logger.LogInformation(
            "Transcode prepared: Video: {SourceVideoCodec} -> {TargetVideoCodec} ({VideoBitrate} bps), " +
            "Audio: {SourceAudioCodec} -> {TargetAudioCodec} ({AudioBitrate} bps), " +
            "Resolution: {Width}x{Height}, HwAccel: {HwAccel}, Container: {Container}",
            request.MediaInfo.Video.Codec, decision.TargetVideoCodec, decision.TargetVideoBitrate,
            string.Join(", ", request.MediaInfo.Audio.Select(a => a.Codec)), decision.TargetAudioCodec, decision.TargetAudioBitrate,
            decision.TargetWidth ?? request.MediaInfo.Video.Width, decision.TargetHeight ?? request.MediaInfo.Video.Height,
            decision.HwAccelMethod, decision.TargetContainer);

        return Task.FromResult(new StreamResult
        {
            DataStream = transcodeStream,
            ContentType = mimeType,
            ContentLength = null, // Unknown for streaming transcode
            Mode = StreamingMode.FullTranscode,
            SupportsRangeRequests = false, // Range requests not supported during transcode
            CleanupAction = () =>
            {
                _logger.LogDebug("Cleaning up Transcode stream for session {SessionId}", request.SessionId);
                transcodeStream.Dispose();
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
    /// Stream implementation for full transcoding
    /// </summary>
    private class FullTranscodeStream : Stream
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

        public FullTranscodeStream(
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
                _logger.LogDebug("FullTranscodeStream disposed for session {SessionId}", _sessionId);
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
                _logger.LogDebug("FullTranscodeStream disposed asynchronously for session {SessionId}", _sessionId);
            }
            await base.DisposeAsync();
        }
    }
}