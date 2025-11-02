using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Streaming.Commands.StartStream;
using Lanflix.Application.Features.Streaming.Commands.StopStream;
using Lanflix.Application.Features.Streaming.Commands.UpdateProgress;
using Lanflix.Application.Features.Streaming.Queries.GetStreamInfo;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Telemetry;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/stream")]
public class StreamingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly StreamingStrategySelector _strategySelector;
    private readonly StreamingMetrics _metrics;
    private readonly ILogger<StreamingController> _logger;

    public StreamingController(
        IMediator mediator,
        IApplicationDbContext context,
        StreamingStrategySelector strategySelector,
        StreamingMetrics metrics,
        ILogger<StreamingController> logger)
    {
        _mediator = mediator;
        _context = context;
        _strategySelector = strategySelector;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Direct stream endpoint for content with profile
    /// </summary>
    [HttpGet("{id:int}")]
    [HttpHead("{id:int}")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("streaming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> StreamContentDirect(
        int id,
        [FromQuery] int profileId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Direct streaming request for content {ContentId}, profile {ProfileId}", id, profileId);

        // Get content
        var content = await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (content == null)
        {
            _logger.LogWarning("Content {ContentId} not found", id);
            return NotFound(new { message = "Content not found" });
        }

        // Get profile
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Profile {ProfileId} not found", profileId);
            return NotFound(new { message = "Profile not found" });
        }

        // For HEAD requests, just return headers
        if (Request.Method == "HEAD")
        {
            if (content.MediaInfo?.Duration != null)
            {
                Response.Headers.Append("Content-Length", content.MediaInfo.FileSize.ToString());
                Response.Headers.Append("Content-Type", "video/mp4");
                Response.Headers.Append("Accept-Ranges", "bytes");
            }
            return Ok();
        }

        // Check if file exists
        if (!System.IO.File.Exists(content.FilePath))
        {
            _logger.LogError("Content file not found: {FilePath}", content.FilePath);
            return NotFound(new { message = "Content file not found" });
        }

        // Use metadata.json file from media folder instead of database MediaInfo
        var mediaFolderPath = Path.GetDirectoryName(content.FilePath);
        if (string.IsNullOrEmpty(mediaFolderPath))
        {
            _logger.LogError("Content {ContentId} has invalid file path: {FilePath}", id, content.FilePath);
            return BadRequest(new { message = "Content file path is invalid" });
        }

        var metadataPath = Path.Combine(mediaFolderPath, "metadata.json");
        if (!System.IO.File.Exists(metadataPath))
        {
            _logger.LogError("Content {ContentId} metadata.json not found at: {MetadataPath}", id, metadataPath);
            return BadRequest(new { message = "Content metadata.json file not found" });
        }

        _logger.LogInformation("Using metadata.json for content {ContentId} from {MetadataPath}", id, metadataPath);

        // Direct file streaming - bypass MediaInfo requirement
        _logger.LogInformation("Direct streaming content {ContentId} from file: {FilePath}", id, content.FilePath);

        // Get file info for basic streaming
        var fileInfo = new FileInfo(content.FilePath);
        if (!fileInfo.Exists)
        {
            _logger.LogError("Content file does not exist: {FilePath}", content.FilePath);
            return NotFound(new { message = "Content file not found" });
        }

        // Handle range requests for video streaming
        var rangeHeader = Request.Headers["Range"].ToString();
        if (!string.IsNullOrEmpty(rangeHeader))
        {
            return HandleRangeRequest(content.FilePath, rangeHeader, fileInfo.Length);
        }

        // Return full file stream
        var stream = new FileStream(content.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = GetContentType(content.FilePath);
        
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Length", fileInfo.Length.ToString());
        
        return new FileStreamResult(stream, contentType);


    }

    /// <summary>
    /// Gets client capabilities from request headers or returns defaults
    /// </summary>
    private ClientCapabilities GetClientCapabilities()
    {
        // In a real implementation, you might parse User-Agent header or 
        // have the client send capabilities in a custom header
        // For now, return reasonable defaults that support most modern browsers
        
        var userAgent = Request.Headers["User-Agent"].ToString().ToLowerInvariant();
        
        // Detect basic client type from User-Agent
        var isModernBrowser = userAgent.Contains("chrome") || userAgent.Contains("firefox") || 
                             userAgent.Contains("safari") || userAgent.Contains("edge");
        
        return new ClientCapabilities
        {
            SupportedVideoCodecs = isModernBrowser 
                ? new[] { "h264", "hevc", "vp9", "av1" }
                : new[] { "h264" }, // Fallback for older clients
            SupportedAudioCodecs = new[] { "aac", "mp3", "ac3", "eac3", "opus" },
            SupportedContainers = isModernBrowser 
                ? new[] { "mp4", "webm", "mkv" }
                : new[] { "mp4" }, // Fallback for older clients
            MaxBitrate = 20_000_000, // 20 Mbps
            MaxResolution = VideoResolution.UHD4K,
            SupportsHDR = isModernBrowser
        };
    }

    /// <summary>
    /// Get stream info for content
    /// </summary>
    [HttpGet("{id:int}/info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStreamInfo(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting stream info for content {ContentId}", id);

        // Get content
        var content = await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (content == null)
        {
            _logger.LogWarning("Content {ContentId} not found", id);
            return NotFound(new { message = "Content not found" });
        }

        // Return media info
        var streamInfo = new
        {
            id = content.Id,
            title = content.Title,
            duration = content.MediaInfo?.Duration.TotalSeconds ?? 0,
            fileSize = content.MediaInfo?.FileSize ?? 0,
            container = content.MediaInfo?.Container ?? "unknown",
            video = content.MediaInfo?.Video != null ? new
            {
                codec = content.MediaInfo.Video.Codec,
                width = content.MediaInfo.Video.Width,
                height = content.MediaInfo.Video.Height,
                bitrate = content.MediaInfo.Video.Bitrate,
                frameRate = content.MediaInfo.Video.FrameRate
            } : null,
            audio = content.MediaInfo?.Audio?.Select(a => new
            {
                codec = a.Codec,
                language = a.Language,
                channels = a.Channels,
                bitrate = a.Bitrate,
                sampleRate = a.SampleRate
            }).ToArray() ?? Array.Empty<object>()
        };

        return Ok(streamInfo);
    }

    /// <summary>
    /// Get available subtitles for content
    /// </summary>
    [HttpGet("{id:int}/subtitles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubtitles(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting subtitles for content {ContentId}", id);

        // Get content
        var content = await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (content == null)
        {
            _logger.LogWarning("Content {ContentId} not found", id);
            return NotFound(new { message = "Content not found" });
        }

        // Return subtitle streams from media info
        var subtitles = content.MediaInfo?.Subtitles?.Select(s => new
        {
            index = s.Index,
            language = s.Language,
            title = s.Title,
            format = s.Format,
            isDefault = s.IsDefault,
            isForced = s.IsForced,
            isEmbedded = s.IsEmbedded,
            externalFilePath = s.ExternalFilePath
        }).ToArray() ?? Array.Empty<object>();

        return Ok(subtitles);
    }

    /// <summary>
    /// Start a new streaming session
    /// </summary>
    [HttpPost("{id:int}/start")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("streaming")]
    [ProducesResponseType(typeof(StreamSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StreamSessionDto>> StartStream(
        int id,
        [FromBody] StartStreamCommand command,
        CancellationToken cancellationToken)
    {
        // Override ContentId from route
        command.ContentId = id;

        _logger.LogInformation(
            "Starting stream for content {ContentId}, profile {ProfileId}",
            command.ContentId, command.ProfileId);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Stream media content with range support
    /// </summary>
    [HttpGet("{sessionId}/stream")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("streaming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> StreamContent(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Streaming content for session {SessionId}", sessionId);

        // Get session from database
        var session = await _context.StreamSessions
            .Include(s => s.Content)
            .Include(s => s.Profile)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (session == null || !session.IsActive)
        {
            _logger.LogWarning("Stream session {SessionId} not found or inactive", sessionId);
            return NotFound(new { message = "Stream session not found or inactive" });
        }

        // Update last activity
        session.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Get content and use direct file streaming
        var content = session.Content;
        
        // Use metadata.json file from media folder instead of database MediaInfo
        var mediaFolderPath = Path.GetDirectoryName(content.FilePath);
        if (string.IsNullOrEmpty(mediaFolderPath))
        {
            _logger.LogError("Content {ContentId} has invalid file path: {FilePath}", content.Id, content.FilePath);
            return BadRequest(new { message = "Content file path is invalid" });
        }

        var metadataPath = Path.Combine(mediaFolderPath, "metadata.json");
        if (!System.IO.File.Exists(metadataPath))
        {
            _logger.LogError("Content {ContentId} metadata.json not found at: {MetadataPath}", content.Id, metadataPath);
            return BadRequest(new { message = "Content metadata.json file not found" });
        }

        _logger.LogInformation("Using metadata.json for content {ContentId} from {MetadataPath}", content.Id, metadataPath);

        // Check if file exists
        if (!System.IO.File.Exists(content.FilePath))
        {
            _logger.LogError("Content file not found: {FilePath}", content.FilePath);
            return NotFound(new { message = "Content file not found" });
        }

        // Direct file streaming for session - bypass MediaInfo requirement
        _logger.LogInformation("Direct streaming session {SessionId} content from file: {FilePath}", sessionId, content.FilePath);

        // Get file info for basic streaming
        var fileInfo = new FileInfo(content.FilePath);
        if (!fileInfo.Exists)
        {
            _logger.LogError("Content file does not exist: {FilePath}", content.FilePath);
            return NotFound(new { message = "Content file not found" });
        }

        // Handle range requests for video streaming
        var rangeHeader = Request.Headers["Range"].ToString();
        if (!string.IsNullOrEmpty(rangeHeader))
        {
            return HandleRangeRequest(content.FilePath, rangeHeader, fileInfo.Length);
        }

        // Return full file stream
        var stream = new FileStream(content.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = GetContentType(content.FilePath);
        
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Length", fileInfo.Length.ToString());
        
        return new FileStreamResult(stream, contentType);
    }

    /// <summary>
    /// Update playback progress for a streaming session
    /// </summary>
    [HttpPost("{sessionId}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProgress(
        string sessionId,
        [FromBody] UpdateProgressCommand command,
        CancellationToken cancellationToken)
    {
        // Override SessionId from route
        command.SessionId = sessionId;

        _logger.LogDebug(
            "Updating progress for session {SessionId}: Position={Position}, Completed={Completed}",
            sessionId, command.PositionTicks, command.IsCompleted);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Stop a streaming session
    /// </summary>
    [HttpDelete("{sessionId}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopStream(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping stream session {SessionId}", sessionId);

        // Get session to record metrics before stopping
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (session != null && session.IsActive)
        {
            var duration = (DateTime.UtcNow - session.StartedAt).TotalSeconds;
            _metrics.RecordStreamDuration(
                duration,
                session.Mode.ToString(),
                completed: true);
        }

        var command = new StopStreamCommand { SessionId = sessionId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Handle HTTP range requests for video streaming
    /// </summary>
    private IActionResult HandleRangeRequest(string filePath, string rangeHeader, long fileSize)
    {
        try
        {
            // Parse range header (e.g., "bytes=0-1023")
            var range = rangeHeader.Replace("bytes=", "").Split('-');
            var start = string.IsNullOrEmpty(range[0]) ? 0 : long.Parse(range[0]);
            var end = string.IsNullOrEmpty(range[1]) ? fileSize - 1 : long.Parse(range[1]);

            // Validate range
            if (start >= fileSize || end >= fileSize || start > end)
            {
                return StatusCode(StatusCodes.Status416RangeNotSatisfiable);
            }

            var contentLength = end - start + 1;
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(start, SeekOrigin.Begin);

            var contentType = GetContentType(filePath);
            
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Range", $"bytes {start}-{end}/{fileSize}");
            Response.Headers.Append("Content-Length", contentLength.ToString());
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return new FileStreamResult(stream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling range request for file: {FilePath}", filePath);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get MIME content type based on file extension
    /// </summary>
    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".webm" => "video/webm",
            ".m4v" => "video/mp4",
            ".flv" => "video/x-flv",
            _ => "video/mp4" // Default fallback
        };
    }

    /// <summary>
    /// Helper class to execute cleanup actions on dispose
    /// </summary>
    private class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private bool _disposed;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _action?.Invoke();
                _disposed = true;
            }
        }
    }
}
