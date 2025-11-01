using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Direct play strategy - serves media file as-is without any transcoding
/// Highest priority strategy for optimal performance
/// </summary>
public class DirectPlayStrategy : BaseStreamingStrategy
{
    public DirectPlayStrategy(ILogger<DirectPlayStrategy> logger) : base(logger)
    {
    }

    public override StreamingMode Mode => StreamingMode.DirectPlay;

    public override int Priority => 1; // Highest priority

    public override bool CanHandle(MediaInfo media, ClientCapabilities client)
    {
        // Check if video codec is supported
        if (!IsVideoCodecSupported(media.Video.Codec, client))
        {
            Logger.LogDebug("DirectPlay not possible: Video codec {Codec} not supported", media.Video.Codec);
            return false;
        }

        // Check if at least one audio codec is supported
        var hasCompatibleAudio = media.Audio.Any(a => IsAudioCodecSupported(a.Codec, client));
        if (!hasCompatibleAudio)
        {
            Logger.LogDebug("DirectPlay not possible: No compatible audio codec found");
            return false;
        }

        // Check if container is supported
        if (!IsContainerSupported(media.Container, client))
        {
            Logger.LogDebug("DirectPlay not possible: Container {Container} not supported", media.Container);
            return false;
        }

        // Check if resolution is supported
        if (!IsResolutionSupported(media.Video, client))
        {
            Logger.LogDebug("DirectPlay not possible: Resolution {Width}x{Height} exceeds client max {MaxResolution}",
                media.Video.Width, media.Video.Height, client.MaxResolution);
            return false;
        }

        // Check if bitrate is within limits
        if (!IsBitrateSupported(media, client))
        {
            Logger.LogDebug("DirectPlay not possible: Bitrate {Bitrate} exceeds client max {MaxBitrate}",
                media.OverallBitrate ?? media.Video.Bitrate, client.MaxBitrate);
            return false;
        }

        // Check if HDR is supported (if content is HDR)
        if (!IsHdrSupported(media.Video, client))
        {
            Logger.LogDebug("DirectPlay not possible: HDR content not supported by client");
            return false;
        }

        Logger.LogInformation("DirectPlay is possible for {Container} with {VideoCodec}/{AudioCodec}",
            media.Container, media.Video.Codec, media.Audio.FirstOrDefault()?.Codec ?? "unknown");

        return true;
    }

    public override async Task<StreamResult> ExecuteAsync(StreamRequest request, CancellationToken cancellationToken)
    {
        ValidateFilePath(request.FilePath);

        Logger.LogInformation("Starting DirectPlay stream for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        var fileInfo = new FileInfo(request.FilePath);
        var fileSize = fileInfo.Length;

        // Parse range header if present
        var (rangeStart, rangeEnd) = ParseRangeHeader(request.RangeHeader, fileSize);
        var actualRangeEnd = rangeEnd ?? fileSize - 1;

        // Open file stream with optimal buffer size for streaming
        var fileStream = new FileStream(
            request.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920, // 80KB buffer
            useAsync: true);

        // Seek to start position if range request
        if (rangeStart > 0)
        {
            fileStream.Seek(rangeStart, SeekOrigin.Begin);
        }

        // Create a limited stream if range end is specified
        Stream dataStream = fileStream;
        long? contentLength = fileSize;

        if (rangeEnd.HasValue && rangeEnd.Value < fileSize - 1)
        {
            var rangeLength = actualRangeEnd - rangeStart + 1;
            dataStream = new LimitedStream(fileStream, rangeLength);
            contentLength = rangeLength;
        }

        var mimeType = GetMimeType(request.MediaInfo.Container);

        Logger.LogDebug("DirectPlay stream prepared: {MimeType}, Size: {Size}, Range: {Start}-{End}",
            mimeType, contentLength, rangeStart, actualRangeEnd);

        return new StreamResult
        {
            DataStream = dataStream,
            ContentType = mimeType,
            ContentLength = contentLength,
            Mode = StreamingMode.DirectPlay,
            SupportsRangeRequests = true,
            RangeStart = rangeStart > 0 ? rangeStart : null,
            RangeEnd = rangeEnd,
            CleanupAction = () =>
            {
                Logger.LogDebug("Cleaning up DirectPlay stream for session {SessionId}", request.SessionId);
                dataStream.Dispose();
            }
        };
    }

    /// <summary>
    /// Stream wrapper that limits the number of bytes read
    /// </summary>
    private class LimitedStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _maxLength;
        private long _position;

        public LimitedStream(Stream baseStream, long maxLength)
        {
            _baseStream = baseStream;
            _maxLength = maxLength;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _maxLength;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _maxLength - _position;
            if (remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(count, remaining);
            var bytesRead = _baseStream.Read(buffer, offset, toRead);
            _position += bytesRead;
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = _maxLength - _position;
            if (remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(count, remaining);
            var bytesRead = await _baseStream.ReadAsync(buffer.AsMemory(offset, toRead), cancellationToken);
            _position += bytesRead;
            return bytesRead;
        }

        public override void Flush() => _baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
