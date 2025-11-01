using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Library.Queries.GetContentDetails;
using Lanflix.Application.Features.Library.Queries.GetLibraryItems;
using Lanflix.Application.Features.Profiles.Queries.GetProfiles;
using Lanflix.Application.Features.Profiles.Queries.GetWatchHistory;
using Lanflix.Application.Features.Streaming.Commands.StartStream;
using Lanflix.Domain.Enums;
using Lanflix.WebApi.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

/// <summary>
/// Legacy API controller that provides backward compatibility with the old Node.js backend
/// Maps old endpoint paths to new backend functionality
/// </summary>
[ApiController]
[Route("api")]
[LegacyResponseWrapper]
public class LegacyApiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LegacyApiController> _logger;

    public LegacyApiController(IMediator mediator, ILogger<LegacyApiController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Legacy endpoint: GET /api/content
    /// Maps to: GET /api/library/items
    /// </summary>
    [HttpGet("content")]
    [ProducesResponseType(typeof(LegacyApiResponse<PaginatedList<ContentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegacyApiResponse<PaginatedList<ContentDto>>>> GetContent(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Legacy API: Getting content (type={Type}, page={Page})", type, page);

        // Map legacy type parameter to ContentType enum
        ContentType? contentType = type?.ToLowerInvariant() switch
        {
            "movie" => ContentType.Movie,
            "series" => ContentType.Series,
            _ => null
        };

        var query = new GetLibraryItemsQuery
        {
            Type = contentType,
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(LegacyApiResponse<PaginatedList<ContentDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Legacy endpoint: GET /api/content/:id
    /// Maps to: GET /api/library/items/:id
    /// </summary>
    [HttpGet("content/{id:int}")]
    [ProducesResponseType(typeof(LegacyApiResponse<ContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LegacyApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegacyApiResponse<ContentDto>>> GetContentById(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Legacy API: Getting content by ID {ContentId}", id);

        var query = new GetContentDetailsQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(LegacyApiResponse<object>.ErrorResponse($"Content with ID {id} not found"));
        }

        return Ok(LegacyApiResponse<ContentDto>.SuccessResponse(result));
    }

    /// <summary>
    /// Legacy endpoint: POST /api/stream/start
    /// Maps to: POST /api/stream/{id}/start
    /// </summary>
    [HttpPost("stream/start")]
    [ProducesResponseType(typeof(LegacyApiResponse<StreamSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LegacyApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LegacyApiResponse<StreamSessionDto>>> StartStreamLegacy(
        [FromBody] LegacyStartStreamRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Legacy API: Starting stream for content {ContentId}, profile {ProfileId}",
            request.ContentId, request.ProfileId);

        var command = new StartStreamCommand
        {
            ContentId = request.ContentId,
            ProfileId = request.ProfileId,
            EpisodeId = request.EpisodeId,
            ClientCapabilities = request.ClientCapabilities ?? new Domain.ValueObjects.ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264", "hevc" },
                SupportedAudioCodecs = new[] { "aac", "mp3", "ac3" },
                SupportedContainers = new[] { "mp4", "mkv" },
                MaxBitrate = 20_000_000,
                MaxResolution = Domain.ValueObjects.VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(LegacyApiResponse<StreamSessionDto>.SuccessResponse(result));
    }

    /// <summary>
    /// Legacy endpoint: GET /api/stream/:id
    /// Maps to: GET /api/stream/:sessionId/stream
    /// Note: This redirects to the new endpoint
    /// </summary>
    [HttpGet("stream/{sessionId}")]
    public IActionResult GetStreamLegacy(string sessionId)
    {
        _logger.LogInformation("Legacy API: Redirecting stream request for session {SessionId}", sessionId);
        
        // Redirect to new endpoint
        return RedirectToAction(
            nameof(StreamingController.StreamContent),
            "Streaming",
            new { sessionId });
    }

    /// <summary>
    /// Legacy endpoint: GET /api/profiles
    /// Maps to: GET /api/profiles (same endpoint, but with legacy response format)
    /// </summary>
    [HttpGet("profiles")]
    [ProducesResponseType(typeof(LegacyApiResponse<List<ProfileDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegacyApiResponse<List<ProfileDto>>>> GetProfilesLegacy(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Legacy API: Getting profiles");

        var query = new GetProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(LegacyApiResponse<List<ProfileDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Legacy endpoint: GET /api/watchhistory/:profileId
    /// Maps to: GET /api/profiles/:id/history
    /// </summary>
    [HttpGet("watchhistory/{profileId:int}")]
    [ProducesResponseType(typeof(LegacyApiResponse<List<WatchHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegacyApiResponse<List<WatchHistoryDto>>>> GetWatchHistoryLegacy(
        int profileId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Legacy API: Getting watch history for profile {ProfileId}", profileId);

        var query = new GetWatchHistoryQuery
        {
            ProfileId = profileId,
            Limit = limit
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(LegacyApiResponse<List<WatchHistoryDto>>.SuccessResponse(result));
    }
}

/// <summary>
/// Legacy request model for starting a stream
/// </summary>
public class LegacyStartStreamRequest
{
    public int ContentId { get; set; }
    public int ProfileId { get; set; }
    public int? EpisodeId { get; set; }
    public Domain.ValueObjects.ClientCapabilities? ClientCapabilities { get; set; }
}
