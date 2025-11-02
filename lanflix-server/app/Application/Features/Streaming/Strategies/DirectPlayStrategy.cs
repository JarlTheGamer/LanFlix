using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Strategies;

/// <summary>
/// Direct Play strategy - serves the file as-is without any transcoding
/// Highest priority strategy when media is fully compatible with client
/// </summary>
public class DirectPlayStrategy : IStreamingStrategy
{
    private readonly ILogger<DirectPlayStrategy> _logger;

    public DirectPlayStrategy(ILogger<DirectPlayStrategy> logger)
    {
        _logger = logger;
    }

    public StreamingMode Mode => StreamingMode.DirectPlay;
    public int Priority => 1; // Highest priority

    public bool CanHandle(TranscodingDecision decision)
    {
        return decision.PlaybackMethod == PlaybackMethod.DirectPlay;
    }

    public Task<StreamResult> ExecuteAsync(StreamRequest request, TranscodingDecision decision, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException($"Media file not found: {request.FilePath}");
        }

        _logger.LogInformation("Starting DirectPlay for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        var fileInfo = new FileInfo(request.FilePath);
        var fileStream = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // Handle range requests for seeking
        Stream dataStream = fileStream;
        long? rangeStart = null;
        long? rangeEnd = null;
        long contentLength = fileInfo.Length;

        if (!string.IsNullOrEmpty(request.RangeHeader))
        {
            var (start, end) = ParseRangeHeader(request.RangeHeader, fileInfo.Length);
            if (start.HasValue)
            {
                rangeStart = start.Value;
                rangeEnd = end ?? fileInfo.Length - 1;
                contentLength = rangeEnd.Value - rangeStart.Value + 1;

                fileStream.Seek(rangeStart.Value, SeekOrigin.Begin);
                dataStream = new RangeStream(fileStream, contentLength);
            }
        }

        var mimeType = GetMimeType(request.MediaInfo.Container);

        _logger.LogInformation("DirectPlay prepared: {Container}, Size: {Size} bytes, Range: {RangeStart}-{RangeEnd}",
            request.MediaInfo.Container, contentLength, rangeStart, rangeEnd);

        return Task.FromResult(new StreamResult
        {
            DataStream = dataStream,
            ContentType = mimeType,
            ContentLength = contentLength,
            Mode = StreamingMode.DirectPlay,
            SupportsRangeRequests = true,
            RangeStart = rangeStart,
            RangeEnd = rangeEnd,
            CleanupAction = () =>
            {
                _logger.LogDebug("Cleaning up DirectPlay stream for session {SessionId}", request.SessionId);
                dataStream.Dispose();
            }
        });
    }

    private (long? start, long? end) ParseRangeHeader(string rangeHeader, long fileSize)
    {
        try
        {
            // Parse "bytes=start-end" format
            if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
                return (null, null);

            var range = rangeHeader.Substring(6);
            var parts = range.Split('-');

            if (parts.Length != 2)
                return (null, null);

            long? start = null;
            long? end = null;

            if (!string.IsNullOrEmpty(parts[0]) && long.TryParse(parts[0], out var startValue))
                start = startValue;

            if (!string.IsNullOrEmpty(parts[1]) && long.TryParse(parts[1], out var endValue))
                end = endValue;

            // Validate range
            if (start.HasValue && start.Value >= fileSize)
                return (null, null);

            if (end.HasValue && end.Value >= fileSize)
                end = fileSize - 1;

            return (start, end);
        }
        catch
        {
            return (null, null);
        }
    }

    private string GetMimeType(string container)
    {
        return container.ToLowerInvariant() switch
        {
            "mp4" => "video/mp4",
            "mkv" => "video/x-matroska",
            "avi" => "video/x-msvideo",
            "webm" => "video/webm",
            "mov" => "video/quicktime",
            "wmv" => "video/x-ms-wmv",
            "flv" => "video/x-flv",
            "m4v" => "video/x-m4v",
            "3gp" => "video/3gpp",
            "ts" => "video/mp2t",
            "mpg" or "mpeg" => "video/mpeg",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Stream wrapper that limits reading to a specific range
    /// </summary>
    private class RangeStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _length;
        private long _position;

        public RangeStream(Stream baseStream, long length)
        {
            _baseStream = baseStream;
            _length = length;
            _position = 0;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remainingBytes = _length - _position;
            if (remainingBytes <= 0)
                return 0;

            var bytesToRead = (int)Math.Min(count, remainingBytes);
            var bytesRead = _baseStream.Read(buffer, offset, bytesToRead);
            _position += bytesRead;
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remainingBytes = _length - _position;
            if (remainingBytes <= 0)
                return 0;

            var bytesToRead = (int)Math.Min(count, remainingBytes);
            var bytesRead = await _baseStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
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
                _baseStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// Interface for streaming strategies
/// </summary>
public interface IStreamingStrategy
{
    StreamingMode Mode { get; }
    int Priority { get; }
    bool CanHandle(TranscodingDecision decision);
    Task<StreamResult> ExecuteAsync(StreamRequest request, TranscodingDecision decision, CancellationToken cancellationToken);
}