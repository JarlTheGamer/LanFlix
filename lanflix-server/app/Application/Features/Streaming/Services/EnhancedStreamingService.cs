using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Services;

/// <summary>
/// Enhanced streaming service that uses transcoding profiles and decision engine
/// Provides optimal media delivery based on client capabilities
/// </summary>
public class EnhancedStreamingService
{
    private readonly TranscodingDecisionEngine _decisionEngine;
    private readonly IEnumerable<IStreamingStrategy> _strategies;
    private readonly ILogger<EnhancedStreamingService> _logger;

    public EnhancedStreamingService(
        TranscodingDecisionEngine decisionEngine,
        IEnumerable<IStreamingStrategy> strategies,
        ILogger<EnhancedStreamingService> logger)
    {
        _decisionEngine = decisionEngine;
        _strategies = strategies;
        _logger = logger;
    }

    /// <summary>
    /// Streams media content using optimal transcoding decision
    /// </summary>
    /// <param name="request">Stream request with media info</param>
    /// <param name="clientProfiles">Client transcoding profiles</param>
    /// <param name="hwAccel">Hardware acceleration capabilities</param>
    /// <param name="settings">Transcoding settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream result with optimal delivery method</returns>
    public async Task<StreamResult> StreamAsync(
        StreamRequest request,
        TranscodingProfile[] clientProfiles,
        HardwareAcceleration hwAccel,
        TranscodingSettings settings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing stream request for session {SessionId}, file: {FilePath}",
            request.SessionId, request.FilePath);

        // Validate input
        if (clientProfiles.Length == 0)
        {
            throw new ArgumentException("At least one client transcoding profile must be provided", nameof(clientProfiles));
        }

        // Make transcoding decision
        var decision = _decisionEngine.MakeDecision(request.MediaInfo, clientProfiles, hwAccel, settings);

        // HLS segments always need a real MPEG-TS payload. A source that is otherwise direct-playable
        // must still be remuxed here; serving the original MKV/MP4 bytes with a .ts label creates a
        // non-seekable timeline in Media3 and makes rewind/fast-forward controls unavailable.
        if (!string.IsNullOrWhiteSpace(request.ForceOutputFormat))
        {
            decision = decision with
            {
                PlaybackMethod = decision.PlaybackMethod == PlaybackMethod.DirectPlay
                    ? PlaybackMethod.Remux
                    : decision.PlaybackMethod,
                TargetContainer = request.ForceOutputFormat
            };
        }

        _logger.LogInformation("Transcoding decision for session {SessionId}: {PlaybackMethod}, Reason: {Reason}",
            request.SessionId, decision.PlaybackMethod, decision.Reason);

        // Find strategy that can handle this decision
        var strategy = _strategies
            .Where(s => s.CanHandle(decision))
            .OrderBy(s => s.Priority)
            .FirstOrDefault();

        if (strategy == null)
        {
            _logger.LogError("No streaming strategy found for playback method: {PlaybackMethod}", decision.PlaybackMethod);
            throw new InvalidOperationException($"No streaming strategy available for playback method: {decision.PlaybackMethod}");
        }

        _logger.LogInformation("Using streaming strategy: {Strategy} (Priority: {Priority})",
            strategy.Mode, strategy.Priority);

        // Execute the strategy
        try
        {
            var result = await strategy.ExecuteAsync(request, decision, cancellationToken);
            
            _logger.LogInformation("Stream prepared successfully for session {SessionId} using {Strategy}",
                request.SessionId, strategy.Mode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute streaming strategy {Strategy} for session {SessionId}",
                strategy.Mode, request.SessionId);
            throw;
        }
    }

    /// <summary>
    /// Gets transcoding decision without executing streaming
    /// Useful for client-side decision making and diagnostics
    /// </summary>
    /// <param name="mediaInfo">Media information</param>
    /// <param name="clientProfiles">Client transcoding profiles</param>
    /// <param name="hwAccel">Hardware acceleration capabilities</param>
    /// <param name="settings">Transcoding settings</param>
    /// <returns>Transcoding decision</returns>
    public TranscodingDecision GetTranscodingDecision(
        MediaInfo mediaInfo,
        TranscodingProfile[] clientProfiles,
        HardwareAcceleration hwAccel,
        TranscodingSettings settings)
    {
        return _decisionEngine.MakeDecision(mediaInfo, clientProfiles, hwAccel, settings);
    }

    /// <summary>
    /// Gets available streaming strategies
    /// </summary>
    /// <returns>List of available strategies ordered by priority</returns>
    public IEnumerable<IStreamingStrategy> GetAvailableStrategies()
    {
        return _strategies.OrderBy(s => s.Priority);
    }

    /// <summary>
    /// Tests which strategies can handle a given transcoding decision
    /// Useful for diagnostics and debugging
    /// </summary>
    /// <param name="decision">Transcoding decision to test</param>
    /// <returns>Dictionary of strategy modes and whether they can handle the decision</returns>
    public Dictionary<StreamingMode, bool> TestStrategies(TranscodingDecision decision)
    {
        var results = new Dictionary<StreamingMode, bool>();

        foreach (var strategy in _strategies.OrderBy(s => s.Priority))
        {
            var canHandle = strategy.CanHandle(decision);
            results[strategy.Mode] = canHandle;

            _logger.LogDebug("Strategy test: {Strategy} (Priority: {Priority}) - CanHandle: {CanHandle}",
                strategy.Mode, strategy.Priority, canHandle);
        }

        return results;
    }

    /// <summary>
    /// Creates default transcoding profiles for common client types
    /// </summary>
    /// <param name="clientType">Type of client (web, mobile, tv, etc.)</param>
    /// <returns>Array of transcoding profiles suitable for the client type</returns>
    public TranscodingProfile[] CreateDefaultProfiles(string clientType)
    {
        return clientType.ToLowerInvariant() switch
        {
            "web" => CreateWebProfiles(),
            "mobile" => CreateMobileProfiles(),
            "mobile-high" => new[] { CreateMobileProfiles()[0] },
            "mobile-low" => new[] { CreateMobileProfiles()[1] },
            "tv" => CreateTvProfiles(),
            "chromecast" => CreateChromecastProfiles(),
            "roku" => CreateRokuProfiles(),
            _ => CreateUniversalProfiles()
        };
    }

    private TranscodingProfile[] CreateWebProfiles()
    {
        return new[]
        {
            new TranscodingProfile
            {
                Id = "web-high",
                Name = "Web Browser High Quality",
                SupportedContainers = new[] { "mp4", "webm", "mkv", "mov", "m4v" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 20_000_000, MaxResolution = VideoResolution.UHD4K },
                    new VideoCodecProfile { Codec = "hevc", MaxBitrate = 15_000_000, MaxResolution = VideoResolution.UHD4K },
                    new VideoCodecProfile { Codec = "vp9", MaxBitrate = 15_000_000, MaxResolution = VideoResolution.UHD4K },
                    new VideoCodecProfile { Codec = "av1", MaxBitrate = 10_000_000, MaxResolution = VideoResolution.UHD4K }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 320_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "opus", MaxBitrate = 256_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "mp3", MaxBitrate = 320_000, MaxChannels = 2 }
                },
                MaxBitrate = 25_000_000,
                MaxResolution = VideoResolution.UHD4K,
                SupportsHDR = true,
                MaxAudioChannels = 8
            },
            new TranscodingProfile
            {
                Id = "web-medium",
                Name = "Web Browser Medium Quality",
                SupportedContainers = new[] { "mp4", "webm", "mov", "m4v" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 8_000_000, MaxResolution = VideoResolution.HD1080p },
                    new VideoCodecProfile { Codec = "vp9", MaxBitrate = 6_000_000, MaxResolution = VideoResolution.HD1080p }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 512_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "opus", MaxBitrate = 256_000, MaxChannels = 8 }
                },
                MaxBitrate = 12_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false,
                MaxAudioChannels = 8
            }
        };
    }

    private TranscodingProfile[] CreateMobileProfiles()
    {
        // All common source containers are listed here. The output is always MPEG-TS for HLS so
        // the source container should NEVER be the reason for a full re-encode.
        var allContainers = new[] { "mp4", "mkv", "webm", "avi", "mov", "m4v", "ts", "flv", "wmv" };
        return new[]
        {
            new TranscodingProfile
            {
                Id = "mobile-high",
                Name = "Mobile High Quality",
                SupportedContainers = allContainers,
                VideoCodecs = new[]
                {
                    // H.264 and HEVC are natively playable on Android — copy them directly
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 20_000_000, MaxResolution = VideoResolution.UHD4K },
                    new VideoCodecProfile { Codec = "hevc", MaxBitrate = 20_000_000, MaxResolution = VideoResolution.UHD4K },
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac",  MaxBitrate = 320_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "mp3",  MaxBitrate = 320_000, MaxChannels = 2 },
                    new AudioCodecProfile { Codec = "flac", MaxBitrate = 1_411_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "opus", MaxBitrate = 256_000, MaxChannels = 8 },
                },
                MaxBitrate = 25_000_000,
                MaxResolution = VideoResolution.UHD4K,
                SupportsHDR = false,
                MaxAudioChannels = 8
            },
            new TranscodingProfile
            {
                Id = "mobile-low",
                Name = "Mobile Low Quality",
                SupportedContainers = allContainers,
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 1_500_000, MaxResolution = VideoResolution.HD720p },
                    new VideoCodecProfile { Codec = "hevc", MaxBitrate = 1_500_000, MaxResolution = VideoResolution.HD720p }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 96_000, MaxChannels = 2 }
                },
                MaxBitrate = 2_000_000,
                MaxResolution = VideoResolution.HD720p,
                SupportsHDR = false,
                MaxAudioChannels = 2
            }
        };
    }

    private TranscodingProfile[] CreateTvProfiles()
    {
        return new[]
        {
            new TranscodingProfile
            {
                Id = "tv-4k",
                Name = "Smart TV 4K",
                SupportedContainers = new[] { "mp4", "mkv" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 25_000_000, MaxResolution = VideoResolution.UHD4K },
                    new VideoCodecProfile { Codec = "hevc", MaxBitrate = 20_000_000, MaxResolution = VideoResolution.UHD4K }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 320_000, MaxChannels = 8 },
                    new AudioCodecProfile { Codec = "ac3", MaxBitrate = 640_000, MaxChannels = 6 },
                    new AudioCodecProfile { Codec = "eac3", MaxBitrate = 1_024_000, MaxChannels = 8 }
                },
                MaxBitrate = 30_000_000,
                MaxResolution = VideoResolution.UHD4K,
                SupportsHDR = true,
                MaxAudioChannels = 8
            }
        };
    }

    private TranscodingProfile[] CreateChromecastProfiles()
    {
        return new[]
        {
            new TranscodingProfile
            {
                Id = "chromecast",
                Name = "Chromecast",
                SupportedContainers = new[] { "mp4" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 10_000_000, MaxResolution = VideoResolution.HD1080p },
                    new VideoCodecProfile { Codec = "vp8", MaxBitrate = 8_000_000, MaxResolution = VideoResolution.HD1080p }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 192_000, MaxChannels = 6 },
                    new AudioCodecProfile { Codec = "mp3", MaxBitrate = 320_000, MaxChannels = 2 }
                },
                MaxBitrate = 12_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false,
                MaxAudioChannels = 6
            }
        };
    }

    private TranscodingProfile[] CreateRokuProfiles()
    {
        return new[]
        {
            new TranscodingProfile
            {
                Id = "roku",
                Name = "Roku Device",
                SupportedContainers = new[] { "mp4", "mkv" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 8_000_000, MaxResolution = VideoResolution.HD1080p }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 192_000, MaxChannels = 6 },
                    new AudioCodecProfile { Codec = "ac3", MaxBitrate = 640_000, MaxChannels = 6 }
                },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false,
                MaxAudioChannels = 6
            }
        };
    }

    private TranscodingProfile[] CreateUniversalProfiles()
    {
        return new[]
        {
            new TranscodingProfile
            {
                Id = "universal",
                Name = "Universal Compatibility",
                SupportedContainers = new[] { "mp4" },
                VideoCodecs = new[]
                {
                    new VideoCodecProfile { Codec = "h264", MaxBitrate = 8_000_000, MaxResolution = VideoResolution.HD1080p }
                },
                AudioCodecs = new[]
                {
                    new AudioCodecProfile { Codec = "aac", MaxBitrate = 192_000, MaxChannels = 2 }
                },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false,
                MaxAudioChannels = 2
            }
        };
    }
}
