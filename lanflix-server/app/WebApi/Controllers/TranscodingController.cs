using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Domain.Entities;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Infrastructure.Services.Settings;
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
    private readonly ITranscodingSessionManager _sessionManager;
    private readonly TranscodingSettingsProvider _settingsProvider;
    private readonly IApplicationDbContext _context;
    private readonly Lanflix.Infrastructure.Services.Audio.AudioTrackSelector _audioTrackSelector;
    private readonly ILogger<TranscodingController> _logger;

    public TranscodingController(
        EnhancedStreamingService streamingService,
        IMediaAnalyzer mediaAnalyzer,
        IHardwareAccelerationDetector hwAccelDetector,
        ITranscodingSessionManager sessionManager,
        TranscodingSettingsProvider settingsProvider,
        IApplicationDbContext context,
        Lanflix.Infrastructure.Services.Audio.AudioTrackSelector audioTrackSelector,
        ILogger<TranscodingController> logger)
    {
        _streamingService = streamingService;
        _mediaAnalyzer = mediaAnalyzer;
        _hwAccelDetector = hwAccelDetector;
        _sessionManager = sessionManager;
        _settingsProvider = settingsProvider;
        _context = context;
        _audioTrackSelector = audioTrackSelector;
        _logger = logger;
    }

    /// <summary>
    /// Streams media content with optimal transcoding (replaces old streaming endpoint)
    /// </summary>
    [HttpGet("stream/{contentId}")]
    [HttpHead("stream/{contentId}")]
    public async Task<IActionResult> StreamContent(
        int contentId,
        [FromQuery] string clientType = "web",
        [FromQuery] string? sessionId = null,
        [FromQuery] double? startTime = null,
        [FromQuery] int? profileId = null,
        [FromQuery] int? episodeId = null)
    {
        var requestStart = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Stream request for content ID: {ContentId}, profileId: {ProfileId}, clientType: {ClientType}, startTime: {StartTime}s, episodeId: {EpisodeId}", 
                contentId, profileId, clientType, startTime ?? 0, episodeId);

            string filePath;
            
            // Handle episode streaming
            if (episodeId.HasValue)
            {
                var episode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.Id == episodeId.Value && e.ContentId == contentId);
                
                if (episode == null)
                {
                    _logger.LogWarning("Episode not found: EpisodeId={EpisodeId}, ContentId={ContentId}", episodeId, contentId);
                    return NotFound("Episode not found");
                }
                
                filePath = episode.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("No file path found for episode: EpisodeId={EpisodeId}", episodeId);
                    return NotFound("Episode file not found");
                }
            }
            else
            {
                // Handle movie or series-level streaming
                var content = await _context.Contents
                    .FirstOrDefaultAsync(c => c.Id == contentId);
                
                if (content == null)
                {
                    _logger.LogWarning("Content not found in database: {ContentId}", contentId);
                    return NotFound("Content not found");
                }

                filePath = content.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("No file path found for content ID: {ContentId}", contentId);
                    return NotFound("Content not found");
                }
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                
                // Additional debugging for episode files
                if (episodeId.HasValue)
                {
                    _logger.LogWarning("Episode file path issue - EpisodeId: {EpisodeId}, ContentId: {ContentId}, FilePath: {FilePath}", 
                        episodeId, contentId, filePath);
                    
                    // Check if it's a directory instead of a file
                    if (Directory.Exists(filePath))
                    {
                        _logger.LogWarning("FilePath points to a directory, not a file: {FilePath}", filePath);
                        
                        // Try to find video files in the directory
                        var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v" };
                        var videoFiles = Directory.GetFiles(filePath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                            .ToArray();
                        
                        _logger.LogWarning("Found {Count} video files in directory: {Files}", 
                            videoFiles.Length, string.Join(", ", videoFiles.Select(Path.GetFileName)));
                    }
                }
                
                return NotFound("Content not found");
            }

            sessionId ??= Guid.NewGuid().ToString();

            // Handle HEAD requests - return headers without streaming
            if (Request.Method == "HEAD")
            {
                Response.ContentType = "video/mp4";
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Cache-Control"] = "no-cache";
                
                // For HEAD requests, we need to determine playback mode to set headers
                var headMediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);
                var headHwAccel = await _hwAccelDetector.DetectAsync();
                var headClientProfiles = _streamingService.CreateDefaultProfiles(clientType);
                var headSettings = await _settingsProvider.GetSettingsAsync(profileId);
                var headDecision = _streamingService.GetTranscodingDecision(headMediaInfo, headClientProfiles, headHwAccel, headSettings);
                SetPlaybackModeHeaders(headDecision);
                
                return Ok();
            }

            // Create session key based on content and session ID
            // We include the unique sessionId to prevent reusing stale sessions from previous playback attempts
            var sessionKey = $"content_{contentId}_{episodeId ?? 0}_{clientType}_{profileId}_{startTime?.ToString("F3") ?? "0"}_{sessionId}";
            
            if (startTime.HasValue && startTime.Value > 0)
            {
                _logger.LogInformation("Restarting transcoding at {StartTime}s for content {ContentId}, episode {EpisodeId}, session: {SessionKey}", 
                    startTime.Value, contentId, episodeId, sessionKey);
            }

            // Use session manager to get or create transcoding session
            var result = await _sessionManager.GetOrCreateSessionAsync(sessionKey, async (cancellationToken) =>
            {
                // Get transcoding settings for this profile
                var settings = await _settingsProvider.GetSettingsAsync(profileId);

                // Analyze media
                var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);

                // Detect hardware acceleration
                var hwAccel = await _hwAccelDetector.DetectAsync();

                // Create client profiles
                var clientProfiles = _streamingService.CreateDefaultProfiles(clientType);

                // Get user's preferred audio language and select best audio track
                var userSettingsKey = profileId.HasValue ? $"userSettings_{profileId}" : "userSettings_1";
                var userSettingsJson = await _settingsProvider.GetSettingAsync(userSettingsKey);
                var preferredAudioLanguage = ExtractAudioLanguagePreference(userSettingsJson);
                var selectedAudioTrack = _audioTrackSelector.SelectBestAudioTrack(mediaInfo.Audio.ToArray(), preferredAudioLanguage);

                _logger.LogInformation("Audio track selection for session {SessionId}: Preferred language={PreferredLanguage}, Selected track={SelectedTrack}",
                    sessionId, preferredAudioLanguage ?? "none", selectedAudioTrack?.ToString() ?? "default");

                // Validate seeking position
                if (startTime.HasValue && startTime.Value > mediaInfo.Duration.TotalSeconds)
                {
                    _logger.LogWarning("Seek position {StartTime}s exceeds media duration {Duration}s for content {ContentId}", 
                        startTime.Value, mediaInfo.Duration.TotalSeconds, contentId);
                    startTime = null; // Reset to beginning if seek is beyond duration
                }

                // Create stream request
                var request = new StreamRequest
                {
                    SessionId = sessionId,
                    FilePath = filePath,
                    MediaInfo = mediaInfo,
                    UserPreferences = null,
                    StartPosition = startTime,
                    AudioStreamIndex = selectedAudioTrack,
                    RangeHeader = Request.Headers["Range"].FirstOrDefault()
                };

                // Stream the content
                var streamResult = await _streamingService.StreamAsync(request, clientProfiles, hwAccel, settings, cancellationToken);
                
                // Set playback mode headers for client detection
                var playbackDecision = _streamingService.GetTranscodingDecision(mediaInfo, clientProfiles, hwAccel, settings);
                HttpContext.Response.Headers["Access-Control-Expose-Headers"] = "Content-Type, X-Playback-Mode, X-Transcode-Mode, X-Direct-Play";
                
                switch (playbackDecision.PlaybackMethod)
                {
                    case PlaybackMethod.DirectPlay:
                        HttpContext.Response.Headers["X-Direct-Play"] = "true";
                        HttpContext.Response.Headers["X-Playback-Mode"] = "direct-play";
                        break;
                        
                    case PlaybackMethod.DirectStream:
                        HttpContext.Response.Headers["X-Direct-Play"] = "false";
                        HttpContext.Response.Headers["X-Playback-Mode"] = "direct-stream";
                        HttpContext.Response.Headers["X-Transcode-Mode"] = "direct-stream";
                        break;
                        
                    case PlaybackMethod.Remux:
                        HttpContext.Response.Headers["X-Direct-Play"] = "false";
                        HttpContext.Response.Headers["X-Playback-Mode"] = "remux";
                        HttpContext.Response.Headers["X-Transcode-Mode"] = "remux";
                        break;
                        
                    case PlaybackMethod.Transcode:
                        HttpContext.Response.Headers["X-Direct-Play"] = "false";
                        HttpContext.Response.Headers["X-Playback-Mode"] = "transcode";
                        HttpContext.Response.Headers["X-Transcode-Mode"] = "transcode";
                        break;
                }
                
                return streamResult;
            }, HttpContext.RequestAborted);

            // Set response headers
            Response.ContentType = result.ContentType;
            
            if (result.ContentLength.HasValue)
            {
                Response.ContentLength = result.ContentLength.Value;
            }

            if (result.SupportsRangeRequests && result.RangeStart.HasValue)
            {
                Response.StatusCode = 206; // Partial Content
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Content-Range"] = 
                    $"bytes {result.RangeStart.Value}-{result.RangeEnd ?? result.ContentLength - 1}/{result.ContentLength}";
            }

            // Log seeking performance metrics
            LogSeekingMetrics(contentId, startTime, sessionKey, requestStart);

            // Set up cleanup when client disconnects
            HttpContext.RequestAborted.Register(() =>
            {
                _logger.LogInformation("Client disconnected for session: {SessionKey}", sessionKey);
                // Don't immediately remove session - other clients might be using it
                // Let the session manager handle cleanup based on timeout
            });

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

            // Get default settings for transcoding decision
            var settings = await _settingsProvider.GetSettingsAsync(1);

            // Get transcoding decision
            var decision = _streamingService.GetTranscodingDecision(mediaInfo, clientProfiles, hwAccel, settings);

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
    /// Tests FFmpeg encoders (for debugging hardware acceleration)
    /// </summary>
    [HttpGet("test-encoders")]
    public async Task<IActionResult> TestEncoders()
    {
        try
        {
            // Use the existing detector to see the logs
            var hwAccel = await _hwAccelDetector.DetectAsync();
            
            return Ok(new
            {
                Message = "Check server logs for encoder detection details",
                PreferredMethod = hwAccel.PreferredMethod.ToString(),
                IsAvailable = hwAccel.IsAvailable
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test encoders");
            return StatusCode(500, "Internal server error");
        }
    }



    /// <summary>
    /// Gets media information for a content item or episode
    /// </summary>
    [HttpGet("stream/{contentId}/info")]
    public async Task<IActionResult> GetMediaInfo(int contentId, [FromQuery] int? profileId = null, [FromQuery] int? episodeId = null)
    {
        try
        {
            string filePath;
            
            // Handle episode info request
            if (episodeId.HasValue)
            {
                var episode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.Id == episodeId.Value && e.ContentId == contentId);
                
                if (episode == null)
                {
                    return NotFound("Episode not found");
                }
                
                filePath = episode.FilePath;
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return NotFound("Episode file not found");
                }
            }
            else
            {
                // Handle content info request
                var content = await _context.Contents
                    .FirstOrDefaultAsync(c => c.Id == contentId);
                
                if (content == null)
                {
                    return NotFound("Content not found");
                }

                filePath = content.FilePath;
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return NotFound("Content file not found");
                }
            }

            // Analyze media
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);

            _logger.LogInformation("Media info for content {ContentId}, episode {EpisodeId}: Duration={Duration}s, Video={VideoCodec} {Width}x{Height}", 
                contentId, episodeId, mediaInfo.Duration.TotalSeconds, mediaInfo.Video.Codec, mediaInfo.Video.Width, mediaInfo.Video.Height);

            return Ok(new
            {
                Duration = mediaInfo.Duration.TotalSeconds,
                Video = new
                {
                    mediaInfo.Video.Codec,
                    mediaInfo.Video.Width,
                    mediaInfo.Video.Height,
                    mediaInfo.Video.FrameRate,
                    mediaInfo.Video.Bitrate,
                    mediaInfo.Video.PixelFormat,
                    mediaInfo.Video.ColorSpace,
                    mediaInfo.Video.IsHDR,
                    mediaInfo.Video.HdrFormat
                },
                AudioStreams = mediaInfo.Audio?.Select(a => new
                {
                    a.Index,
                    a.Codec,
                    a.Language,
                    a.Channels,
                    a.SampleRate,
                    a.Bitrate
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media info for content: {ContentId}", contentId);
            return StatusCode(500, "Internal server error");
        }
    }



    /// <summary>
    /// Creates a new transcoding session starting at the specified time
    /// </summary>
    [HttpGet("stream/{contentId}/seek")]
    public async Task<IActionResult> SeekContent(
        int contentId,
        [FromQuery] double startTime,
        [FromQuery] string clientType = "web",
        [FromQuery] int? profileId = null,
        [FromQuery] int? episodeId = null)
    {
        try
        {
            _logger.LogInformation("Seek request for content ID: {ContentId}, episode {EpisodeId} to position {StartTime}s", 
                contentId, episodeId, startTime);

            // Redirect to the main streaming endpoint with the start time
            return RedirectToAction(nameof(StreamContent), new 
            { 
                contentId = contentId, 
                startTime = startTime, 
                clientType = clientType, 
                profileId = profileId,
                episodeId = episodeId,
                sessionId = Guid.NewGuid().ToString() // Force new session for seek
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seek content: {ContentId} to {StartTime}s", contentId, startTime);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update watch progress for a content item
    /// </summary>
    [HttpPost("stream/{contentId}/progress")]
    public async Task<IActionResult> UpdateWatchProgress(int contentId, [FromBody] UpdateProgressRequest request)
    {
        try
        {
            _logger.LogInformation("Updating watch progress for content {ContentId}: {Progress}s / {Duration}s", 
                contentId, request.ProgressSeconds, request.DurationSeconds);

            if (request.ProfileId <= 0)
            {
                return BadRequest("Valid ProfileId is required");
            }

            // Convert seconds to ticks (1 second = 10,000,000 ticks)
            // Cast to long BEFORE multiplication to prevent 32-bit integer overflow (max ~214 seconds)
            var positionTicks = (long)request.ProgressSeconds * 10_000_000L;
            
            // Calculate watched percentage
            var watchedPercentage = request.DurationSeconds.HasValue && request.DurationSeconds.Value > 0
                ? ((double)request.ProgressSeconds / request.DurationSeconds.Value) * 100
                : 0;

            // Consider completed if watched > 90%
            var isCompleted = watchedPercentage >= 90;

            // Find existing watch history record
            var watchHistory = await _context.WatchHistories
                .FirstOrDefaultAsync(wh => 
                    wh.ProfileId == request.ProfileId && 
                    wh.ContentId == contentId &&
                    (request.EpisodeId == null || wh.EpisodeId == request.EpisodeId));

            if (watchHistory != null)
            {
                // Update existing record
                watchHistory.PositionTicks = positionTicks;
                watchHistory.WatchedPercentage = watchedPercentage;
                watchHistory.IsCompleted = isCompleted;
                watchHistory.LastWatchedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                watchHistory = new WatchHistory
                {
                    ProfileId = request.ProfileId,
                    ContentId = contentId,
                    EpisodeId = request.EpisodeId,
                    PositionTicks = positionTicks,
                    WatchedPercentage = watchedPercentage,
                    IsCompleted = isCompleted,
                    LastWatchedAt = DateTime.UtcNow
                };
                _context.WatchHistories.Add(watchHistory);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Watch progress saved for content {ContentId}, episode {EpisodeId}, profile {ProfileId}: {Percentage}%", 
                contentId, request.EpisodeId, request.ProfileId, watchedPercentage);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update watch progress for content: {ContentId}", contentId);
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

            // Get user's preferred audio language and select best audio track (use default profile for test endpoint)
            var userSettingsJson = await _settingsProvider.GetSettingAsync("userSettings_1");
            var preferredAudioLanguage = ExtractAudioLanguagePreference(userSettingsJson);
            var selectedAudioTrack = _audioTrackSelector.SelectBestAudioTrack(mediaInfo.Audio.ToArray(), preferredAudioLanguage);

            _logger.LogInformation("Audio track selection for test stream {SessionId}: Preferred language={PreferredLanguage}, Selected track={SelectedTrack}",
                sessionId, preferredAudioLanguage ?? "none", selectedAudioTrack?.ToString() ?? "default");

            // Create stream request
            var request = new StreamRequest
            {
                SessionId = sessionId,
                FilePath = filePath,
                MediaInfo = mediaInfo,
                UserPreferences = null,
                StartPosition = startTime,
                AudioStreamIndex = selectedAudioTrack,
                RangeHeader = Request.Headers["Range"].FirstOrDefault()
            };

            // Get default settings
            var settings = await _settingsProvider.GetSettingsAsync(1);

            // Stream the content
            var result = await _streamingService.StreamAsync(request, clientProfiles, hwAccel, settings);

            // Set response headers
            Response.ContentType = result.ContentType;
            
            if (result.ContentLength.HasValue)
            {
                Response.ContentLength = result.ContentLength.Value;
            }

            if (result.SupportsRangeRequests && result.RangeStart.HasValue)
            {
                Response.StatusCode = 206; // Partial Content
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Content-Range"] = 
                    $"bytes {result.RangeStart.Value}-{result.RangeEnd ?? result.ContentLength - 1}/{result.ContentLength}";
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

    /// <summary>
    /// Logs seeking performance metrics 
    /// </summary>
    private void LogSeekingMetrics(int contentId, double? startTime, string sessionKey, DateTime requestStart)
    {
        if (!startTime.HasValue || startTime.Value <= 0) return;

        var seekSetupTime = DateTime.UtcNow - requestStart;
        
        _logger.LogInformation("Seeking Performance - Content: {ContentId}, Position: {StartTime}s, " +
                             "Setup Time: {SetupTime}ms, Session: {SessionKey}",
            contentId, startTime.Value, seekSetupTime.TotalMilliseconds, sessionKey);
    }

    /// <summary>
    /// Sets playback mode headers for client detection
    /// </summary>
    private void SetPlaybackModeHeaders(TranscodingDecision decision)
    {
        // Set CORS headers to expose our custom headers
        Response.Headers["Access-Control-Expose-Headers"] = "Content-Type, X-Playback-Mode, X-Transcode-Mode, X-Direct-Play";
        
        switch (decision.PlaybackMethod)
        {
            case PlaybackMethod.DirectPlay:
                Response.Headers["X-Direct-Play"] = "true";
                Response.Headers["X-Playback-Mode"] = "direct-play";
                break;
                
            case PlaybackMethod.DirectStream:
                Response.Headers["X-Direct-Play"] = "false";
                Response.Headers["X-Playback-Mode"] = "direct-stream";
                Response.Headers["X-Transcode-Mode"] = "direct-stream";
                break;
                
            case PlaybackMethod.Remux:
                Response.Headers["X-Direct-Play"] = "false";
                Response.Headers["X-Playback-Mode"] = "remux";
                Response.Headers["X-Transcode-Mode"] = "remux";
                break;
                
            case PlaybackMethod.Transcode:
                Response.Headers["X-Direct-Play"] = "false";
                Response.Headers["X-Playback-Mode"] = "transcode";
                Response.Headers["X-Transcode-Mode"] = "transcode";
                break;
        }
        
        _logger.LogInformation("Set playback mode headers: Method={Method}, DirectPlay={DirectPlay}", 
            decision.PlaybackMethod, Response.Headers["X-Direct-Play"]);
    }

    /// <summary>
    /// Extracts the audio language preference from user settings JSON
    /// </summary>
    private string? ExtractAudioLanguagePreference(string? userSettingsJson)
    {
        if (string.IsNullOrEmpty(userSettingsJson))
            return null;

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var document = System.Text.Json.JsonDocument.Parse(userSettingsJson);
            
            if (document.RootElement.TryGetProperty("audio-lang", out var audioLangElement))
            {
                return audioLangElement.GetString();
            }
            
            // Fallback to "language" property if "audio-lang" not found
            if (document.RootElement.TryGetProperty("language", out var languageElement))
            {
                return languageElement.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse user settings JSON for audio language preference");
        }

        return null;
    }
}

public class TranscodingDecisionRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? ClientType { get; set; }
}

public class UpdateProgressRequest
{
    public int ProfileId { get; set; }
    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }
    public int? EpisodeId { get; set; }
}