using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranscodingController : ControllerBase
{
    private readonly EnhancedStreamingService _streamingService;
    private readonly IMediaAnalyzer _mediaAnalyzer;
    private readonly IHardwareAccelerationDetector _hwAccelDetector;
    private readonly TranscodingSettings _settings;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TranscodingController> _logger;

    public TranscodingController(
        EnhancedStreamingService streamingService,
        IMediaAnalyzer mediaAnalyzer,
        IHardwareAccelerationDetector hwAccelDetector,
        TranscodingSettings settings,
        IApplicationDbContext context,
        ILogger<TranscodingController> logger)
    {
        _streamingService = streamingService;
        _mediaAnalyzer = mediaAnalyzer;
        _hwAccelDetector = hwAccelDetector;
        _settings = settings;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Streams media content with optimal transcoding (replaces old streaming endpoint)
    /// </summary>
    [HttpGet("stream/{contentId}")]
    public async Task<IActionResult> StreamContent(
        int contentId,
        [FromQuery] string clientType = "web",
        [FromQuery] string? sessionId = null,
        [FromQuery] double? startTime = null,
        [FromQuery] int? profileId = null)
    {
        try
        {
            _logger.LogInformation("Stream request for content ID: {ContentId}, profileId: {ProfileId}, clientType: {ClientType}", 
                contentId, profileId, clientType);

            // Get content from database
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == contentId);
            
            if (content == null)
            {
                _logger.LogWarning("Content not found in database: {ContentId}", contentId);
                return NotFound("Content not found");
            }

            var filePath = content.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("No file path found for content ID: {ContentId}", contentId);
                return NotFound("Content not found");
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                return NotFound("Content not found");
            }

            sessionId ??= Guid.NewGuid().ToString();

            // Analyze media
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);

            // Detect hardware acceleration
            var hwAccel = await _hwAccelDetector.DetectAsync();

            // Create client profiles
            var clientProfiles = _streamingService.CreateDefaultProfiles(clientType);

            // Create stream request
            var request = new StreamRequest
            {
                SessionId = sessionId,
                FilePath = filePath,
                MediaInfo = mediaInfo,
                UserPreferences = null,
                StartPosition = startTime,
                AudioStreamIndex = null,
                SubtitleStreamIndex = null,
                RangeHeader = Request.Headers["Range"].FirstOrDefault()
            };

            // Stream the content
            var result = await _streamingService.StreamAsync(request, clientProfiles, hwAccel, _settings);

            // Set response headers
            Response.ContentType = result.ContentType;
            
            if (result.ContentLength.HasValue)
            {
                Response.ContentLength = result.ContentLength.Value;
            }

            if (result.SupportsRangeRequests && result.RangeStart.HasValue)
            {
                Response.StatusCode = 206; // Partial Content
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.Headers.Add("Content-Range", 
                    $"bytes {result.RangeStart.Value}-{result.RangeEnd ?? result.ContentLength - 1}/{result.ContentLength}");
            }

            // Return the stream
            return new FileStreamResult(result.DataStream, result.ContentType)
            {
                EnableRangeProcessing = result.SupportsRangeRequests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream content: {ContentId}", contentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets transcoding decision without streaming
    /// </summary>
    [HttpPost("decision")]
    public async Task<IActionResult> GetTranscodingDecision([FromBody] TranscodingDecisionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath) || !System.IO.File.Exists(request.FilePath))
            {
                return BadRequest("Invalid file path");
            }

            // Analyze media
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(request.FilePath);

            // Detect hardware acceleration
            var hwAccel = await _hwAccelDetector.DetectAsync();

            // Create client profiles
            var clientProfiles = _streamingService.CreateDefaultProfiles(request.ClientType ?? "web");

            // Get transcoding decision
            var decision = _streamingService.GetTranscodingDecision(mediaInfo, clientProfiles, hwAccel, _settings);

            return Ok(new
            {
                PlaybackMethod = decision.PlaybackMethod.ToString(),
                decision.Reason,
                SelectedProfile = decision.SelectedProfile?.Name,
                decision.TargetVideoCodec,
                decision.TargetAudioCodec,
                decision.TargetContainer,
                decision.TargetVideoBitrate,
                decision.TargetAudioBitrate,
                decision.TargetWidth,
                decision.TargetHeight,
                HwAccelMethod = decision.HwAccelMethod.ToString(),
                decision.RequiresToneMapping,
                decision.TranscodingComplexity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transcoding decision for file: {FilePath}", request.FilePath);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets hardware acceleration capabilities
    /// </summary>
    [HttpGet("hardware")]
    public async Task<IActionResult> GetHardwareCapabilities()
    {
        try
        {
            var hwAccel = await _hwAccelDetector.DetectAsync();
            
            return Ok(new
            {
                PreferredMethod = hwAccel.PreferredMethod.ToString(),
                hwAccel.IsAvailable,
                hwAccel.MaxConcurrentSessions,
                hwAccel.SupportsToneMapping,
                Nvenc = new
                {
                    hwAccel.Nvenc.IsAvailable,
                    hwAccel.Nvenc.GpuName,
                    hwAccel.Nvenc.SupportsH264,
                    hwAccel.Nvenc.SupportsHevc,
                    hwAccel.Nvenc.SupportsAv1
                },
                QuickSync = new
                {
                    hwAccel.QuickSync.IsAvailable,
                    hwAccel.QuickSync.GpuName,
                    hwAccel.QuickSync.SupportsH264,
                    hwAccel.QuickSync.SupportsHevc,
                    hwAccel.QuickSync.SupportsAv1
                },
                Amf = new
                {
                    hwAccel.Amf.IsAvailable,
                    hwAccel.Amf.GpuName,
                    hwAccel.Amf.SupportsH264,
                    hwAccel.Amf.SupportsHevc,
                    hwAccel.Amf.SupportsAv1
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hardware capabilities");
            return StatusCode(500, "Internal server error");
        }
    }



    /// <summary>
    /// Test endpoint to stream a file directly (for development)
    /// </summary>
    [HttpGet("stream-file")]
    public async Task<IActionResult> StreamFile(
        [FromQuery] string filePath,
        [FromQuery] string clientType = "web",
        [FromQuery] string? sessionId = null,
        [FromQuery] double? startTime = null)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return BadRequest("Invalid file path");
            }

            sessionId ??= Guid.NewGuid().ToString();

            // Analyze media
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);

            // Detect hardware acceleration
            var hwAccel = await _hwAccelDetector.DetectAsync();

            // Create client profiles
            var clientProfiles = _streamingService.CreateDefaultProfiles(clientType);

            // Create stream request
            var request = new StreamRequest
            {
                SessionId = sessionId,
                FilePath = filePath,
                MediaInfo = mediaInfo,
                UserPreferences = null,
                StartPosition = startTime,
                AudioStreamIndex = null,
                SubtitleStreamIndex = null,
                RangeHeader = Request.Headers["Range"].FirstOrDefault()
            };

            // Stream the content
            var result = await _streamingService.StreamAsync(request, clientProfiles, hwAccel, _settings);

            // Set response headers
            Response.ContentType = result.ContentType;
            
            if (result.ContentLength.HasValue)
            {
                Response.ContentLength = result.ContentLength.Value;
            }

            if (result.SupportsRangeRequests && result.RangeStart.HasValue)
            {
                Response.StatusCode = 206; // Partial Content
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.Headers.Add("Content-Range", 
                    $"bytes {result.RangeStart.Value}-{result.RangeEnd ?? result.ContentLength - 1}/{result.ContentLength}");
            }

            // Return the stream
            return new FileStreamResult(result.DataStream, result.ContentType)
            {
                EnableRangeProcessing = result.SupportsRangeRequests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream file: {FilePath}", filePath);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class TranscodingDecisionRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? ClientType { get; set; }
}