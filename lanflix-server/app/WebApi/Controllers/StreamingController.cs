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

        // Check if content has media info
        if (content.MediaInfo == null)
        {
            _logger.LogError("Content {ContentId} has no media info", id);
            return BadRequest(new { message = "Content media information not available" });
        }

        // Get client capabilities from request headers or use defaults
        var clientCapabilities = GetClientCapabilities();

        // Select optimal streaming strategy
        var strategy = _strategySelector.SelectOptimalStrategy(
            content.MediaInfo,
            clientCapabilities,
            profile.Preferences);

        if (strategy == null)
        {
            _logger.LogError("No suitable streaming strategy found for content {ContentId}", id);
            return BadRequest(new { message = "No suitable streaming strategy available" });
        }

        _logger.LogInformation(
            "Using {Strategy} strategy for direct stream of content {ContentId}",
            strategy.Mode, id);

        // Record stream start metric
        _metrics.RecordStreamStart(
            strategy.Mode.ToString(),
            content.Type.ToString());

        // Create stream request
        var streamRequest = new StreamRequest
        {
            SessionId = $"direct-{id}-{profileId}-{Guid.NewGuid():N}",
            FilePath = content.FilePath,
            MediaInfo = content.MediaInfo,
            ClientCapabilities = clientCapabilities,
            UserPreferences = profile.Preferences,
            RangeHeader = Request.Headers["Range"].ToString()
        };

        // Execute streaming strategy
        StreamResult streamResult;
        try
        {
            streamResult = await strategy.ExecuteAsync(streamRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing streaming strategy for content {ContentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error starting stream" });
        }

        // Register cleanup action
        Response.RegisterForDispose(new DisposableAction(() =>
        {
            streamResult.CleanupAction?.Invoke();
        }));

        // Return appropriate response based on range request
        if (streamResult.RangeStart.HasValue || streamResult.RangeEnd.HasValue)
        {
            var rangeStart = streamResult.RangeStart ?? 0;
            var rangeEnd = streamResult.RangeEnd ?? ((streamResult.ContentLength ?? 1) - 1);
            var contentLength = streamResult.ContentLength ?? 0;

            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Range",
                $"bytes {rangeStart}-{rangeEnd}/{contentLength}");
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return new FileStreamResult(streamResult.DataStream, streamResult.ContentType)
            {
                EnableRangeProcessing = streamResult.SupportsRangeRequests
            };
        }

        // Return full content
        if (streamResult.SupportsRangeRequests)
        {
            Response.Headers.Append("Accept-Ranges", "bytes");
        }

        return new FileStreamResult(streamResult.DataStream, streamResult.ContentType)
        {
            EnableRangeProcessing = streamResult.SupportsRangeRequests
        };
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

        // Get content media info
        var content = session.Content;
        if (content.MediaInfo == null)
        {
            _logger.LogError("Content {ContentId} has no media info", content.Id);
            return BadRequest(new { message = "Content media information not available" });
        }

        // Get client capabilities from session or use defaults
        // In a real implementation, this would be stored with the session
        var clientCapabilities = new Domain.ValueObjects.ClientCapabilities
        {
            SupportedVideoCodecs = new[] { "h264", "hevc", "vp9", "av1" },
            SupportedAudioCodecs = new[] { "aac", "mp3", "ac3", "eac3", "opus" },
            SupportedContainers = new[] { "mp4", "mkv", "webm" },
            MaxBitrate = 20_000_000,
            MaxResolution = Domain.ValueObjects.VideoResolution.UHD4K,
            SupportsHDR = true
        };

        // Select streaming strategy
        var strategy = _strategySelector.SelectOptimalStrategy(
            content.MediaInfo,
            clientCapabilities,
            session.Profile.Preferences);

        if (strategy == null)
        {
            _logger.LogError("No suitable streaming strategy found for session {SessionId}", sessionId);
            return BadRequest(new { message = "No suitable streaming strategy available" });
        }

        _logger.LogInformation(
            "Using {Strategy} strategy for session {SessionId}",
            strategy.Mode, sessionId);

        // Record stream start metric
        _metrics.RecordStreamStart(
            strategy.Mode.ToString(),
            content.Type.ToString());

        // Create stream request
        var streamRequest = new StreamRequest
        {
            SessionId = sessionId,
            FilePath = content.FilePath,
            MediaInfo = content.MediaInfo,
            ClientCapabilities = clientCapabilities,
            UserPreferences = session.Profile.Preferences,
            RangeHeader = Request.Headers["Range"].ToString()
        };

        // Execute streaming strategy
        StreamResult streamResult;
        try
        {
            streamResult = await strategy.ExecuteAsync(streamRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing streaming strategy for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error starting stream" });
        }

        // Register cleanup action
        Response.RegisterForDispose(new DisposableAction(() =>
        {
            streamResult.CleanupAction?.Invoke();
        }));

        // Return appropriate response based on range request
        if (streamResult.RangeStart.HasValue || streamResult.RangeEnd.HasValue)
        {
            var rangeStart = streamResult.RangeStart ?? 0;
            var rangeEnd = streamResult.RangeEnd ?? ((streamResult.ContentLength ?? 1) - 1);
            var contentLength = streamResult.ContentLength ?? 0;

            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Range",
                $"bytes {rangeStart}-{rangeEnd}/{contentLength}");
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return new FileStreamResult(streamResult.DataStream, streamResult.ContentType)
            {
                EnableRangeProcessing = streamResult.SupportsRangeRequests
            };
        }

        // Return full content
        if (streamResult.SupportsRangeRequests)
        {
            Response.Headers.Append("Accept-Ranges", "bytes");
        }

        return new FileStreamResult(streamResult.DataStream, streamResult.ContentType)
        {
            EnableRangeProcessing = streamResult.SupportsRangeRequests
        };
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
