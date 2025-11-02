using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Services;

/// <summary>
/// Transcoding decision engine for optimal media delivery
/// Determines the optimal playback method based on media info and client transcoding profiles
/// </summary>
public class TranscodingDecisionEngine
{
    private readonly ILogger<TranscodingDecisionEngine> _logger;

    public TranscodingDecisionEngine(ILogger<TranscodingDecisionEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Makes a transcoding decision based on media info and client transcoding profiles
    /// The client sends profiles and the server picks the best option for optimal quality
    /// </summary>
    public TranscodingDecision MakeDecision(
        MediaInfo mediaInfo,
        TranscodingProfile[] clientProfiles,
        HardwareAcceleration hwAccel,
        TranscodingSettings settings)
    {
        _logger.LogInformation("Making transcoding decision for media: {Container}, Video: {VideoCodec} {Width}x{Height}, Audio: {AudioCodecs}",
            mediaInfo.Container,
            mediaInfo.Video.Codec,
            mediaInfo.Video.Width,
            mediaInfo.Video.Height,
            string.Join(", ", mediaInfo.Audio.Select(a => a.Codec)));

        // Try Direct Play first (highest priority)
        var directPlayResult = TryDirectPlay(mediaInfo, clientProfiles);
        if (directPlayResult != null)
        {
            _logger.LogInformation("Direct Play selected: {Reason}", directPlayResult.Reason);
            return directPlayResult;
        }

        // Try Remux (container change only)
        var remuxResult = TryRemux(mediaInfo, clientProfiles);
        if (remuxResult != null)
        {
            _logger.LogInformation("Remux selected: {Reason}", remuxResult.Reason);
            return remuxResult;
        }

        // Try Direct Stream (audio transcode only)
        var directStreamResult = TryDirectStream(mediaInfo, clientProfiles, hwAccel, settings);
        if (directStreamResult != null)
        {
            _logger.LogInformation("Direct Stream selected: {Reason}", directStreamResult.Reason);
            return directStreamResult;
        }

        // Fall back to full transcode
        var transcodeResult = CreateTranscodeDecision(mediaInfo, clientProfiles, hwAccel, settings);
        _logger.LogInformation("Transcode selected: {Reason}", transcodeResult.Reason);
        return transcodeResult;
    }

    /// <summary>
    /// Attempts Direct Play - no transcoding required
    /// </summary>
    private TranscodingDecision? TryDirectPlay(MediaInfo mediaInfo, TranscodingProfile[] clientProfiles)
    {
        foreach (var profile in clientProfiles)
        {
            // Check if container is supported
            if (!profile.SupportedContainers.Contains(mediaInfo.Container, StringComparer.OrdinalIgnoreCase))
                continue;

            // Check video codec compatibility
            var videoCodec = profile.VideoCodecs.FirstOrDefault(vc => 
                string.Equals(vc.Codec, mediaInfo.Video.Codec, StringComparison.OrdinalIgnoreCase));
            
            if (videoCodec == null)
                continue;

            // Check video constraints
            if (!CheckVideoConstraints(mediaInfo.Video, videoCodec, profile))
                continue;

            // Check audio codec compatibility
            var compatibleAudio = mediaInfo.Audio.Any(audio =>
                profile.AudioCodecs.Any(ac => 
                    string.Equals(ac.Codec, audio.Codec, StringComparison.OrdinalIgnoreCase) &&
                    CheckAudioConstraints(audio, ac)));

            if (!compatibleAudio && mediaInfo.Audio.Count > 0)
                continue;

            // Check bitrate constraints
            if (profile.MaxBitrate > 0 && (mediaInfo.OverallBitrate ?? mediaInfo.Video.Bitrate) > profile.MaxBitrate)
                continue;

            // Check HDR support
            if (mediaInfo.Video.IsHDR && !profile.SupportsHDR)
                continue;

            return new TranscodingDecision
            {
                PlaybackMethod = PlaybackMethod.DirectPlay,
                Reason = $"Media is compatible with client profile '{profile.Name}'",
                SelectedProfile = profile,
                TranscodingComplexity = 1
            };
        }

        return null;
    }

    /// <summary>
    /// Attempts Remux - container change only, codecs preserved
    /// </summary>
    private TranscodingDecision? TryRemux(MediaInfo mediaInfo, TranscodingProfile[] clientProfiles)
    {
        foreach (var profile in clientProfiles)
        {
            // Check video codec compatibility
            var videoCodec = profile.VideoCodecs.FirstOrDefault(vc => 
                string.Equals(vc.Codec, mediaInfo.Video.Codec, StringComparison.OrdinalIgnoreCase));
            
            if (videoCodec == null)
                continue;

            // Check video constraints
            if (!CheckVideoConstraints(mediaInfo.Video, videoCodec, profile))
                continue;

            // Check audio codec compatibility
            var compatibleAudio = mediaInfo.Audio.Any(audio =>
                profile.AudioCodecs.Any(ac => 
                    string.Equals(ac.Codec, audio.Codec, StringComparison.OrdinalIgnoreCase) &&
                    CheckAudioConstraints(audio, ac)));

            if (!compatibleAudio && mediaInfo.Audio.Count > 0)
                continue;

            // Check bitrate constraints
            if (profile.MaxBitrate > 0 && (mediaInfo.OverallBitrate ?? mediaInfo.Video.Bitrate) > profile.MaxBitrate)
                continue;

            // Check HDR support
            if (mediaInfo.Video.IsHDR && !profile.SupportsHDR)
                continue;

            // Find a compatible container
            var targetContainer = profile.SupportedContainers.FirstOrDefault(container =>
                !string.Equals(container, mediaInfo.Container, StringComparison.OrdinalIgnoreCase));

            if (targetContainer == null)
                continue;

            return new TranscodingDecision
            {
                PlaybackMethod = PlaybackMethod.Remux,
                Reason = $"Container remux required: {mediaInfo.Container} -> {targetContainer}",
                SelectedProfile = profile,
                TargetContainer = targetContainer,
                TranscodingComplexity = 2
            };
        }

        return null;
    }

    /// <summary>
    /// Attempts Direct Stream - audio transcode only, video copied
    /// </summary>
    private TranscodingDecision? TryDirectStream(
        MediaInfo mediaInfo, 
        TranscodingProfile[] clientProfiles, 
        HardwareAcceleration hwAccel, 
        TranscodingSettings settings)
    {
        foreach (var profile in clientProfiles)
        {
            // Check video codec compatibility (must be compatible for direct stream)
            var videoCodec = profile.VideoCodecs.FirstOrDefault(vc => 
                string.Equals(vc.Codec, mediaInfo.Video.Codec, StringComparison.OrdinalIgnoreCase));
            
            if (videoCodec == null)
                continue;

            // Check video constraints
            if (!CheckVideoConstraints(mediaInfo.Video, videoCodec, profile))
                continue;

            // Check if audio needs transcoding
            var needsAudioTranscode = !mediaInfo.Audio.Any(audio =>
                profile.AudioCodecs.Any(ac => 
                    string.Equals(ac.Codec, audio.Codec, StringComparison.OrdinalIgnoreCase) &&
                    CheckAudioConstraints(audio, ac)));

            if (!needsAudioTranscode)
                continue; // Would be direct play or remux

            // Check HDR support
            if (mediaInfo.Video.IsHDR && !profile.SupportsHDR)
                continue;

            // Select target audio codec
            var targetAudioCodec = SelectBestAudioCodec(profile.AudioCodecs);
            var targetContainer = SelectBestContainer(profile.SupportedContainers, mediaInfo.Container);

            return new TranscodingDecision
            {
                PlaybackMethod = PlaybackMethod.DirectStream,
                Reason = $"Audio transcode required: {string.Join(", ", mediaInfo.Audio.Select(a => a.Codec))} -> {targetAudioCodec}",
                SelectedProfile = profile,
                TargetAudioCodec = targetAudioCodec,
                TargetContainer = targetContainer,
                TargetAudioBitrate = DetermineTargetAudioBitrate(profile.AudioCodecs, targetAudioCodec),
                TranscodingComplexity = 3
            };
        }

        return null;
    }

    /// <summary>
    /// Creates a full transcode decision - video and possibly audio transcoded
    /// </summary>
    private TranscodingDecision CreateTranscodeDecision(
        MediaInfo mediaInfo, 
        TranscodingProfile[] clientProfiles, 
        HardwareAcceleration hwAccel, 
        TranscodingSettings settings)
    {
        // Select the best profile for transcoding
        var bestProfile = SelectBestTranscodingProfile(clientProfiles, mediaInfo);
        
        // Determine target video codec based on hardware acceleration and client support
        var targetVideoCodec = DetermineTargetVideoCodec(bestProfile, hwAccel, settings);
        var targetAudioCodec = SelectBestAudioCodec(bestProfile.AudioCodecs);
        var targetContainer = SelectBestContainer(bestProfile.SupportedContainers, mediaInfo.Container);

        // Calculate target resolution and bitrate
        var (targetWidth, targetHeight, targetBitrate) = CalculateTargetVideoSettings(
            mediaInfo.Video, bestProfile, settings);

        var requiresToneMapping = mediaInfo.Video.IsHDR && !bestProfile.SupportsHDR && settings.EnableToneMapping;

        return new TranscodingDecision
        {
            PlaybackMethod = PlaybackMethod.Transcode,
            Reason = $"Video transcode required: {mediaInfo.Video.Codec} -> {targetVideoCodec}",
            SelectedProfile = bestProfile,
            TargetVideoCodec = targetVideoCodec,
            TargetAudioCodec = targetAudioCodec,
            TargetContainer = targetContainer,
            TargetVideoBitrate = targetBitrate,
            TargetAudioBitrate = DetermineTargetAudioBitrate(bestProfile.AudioCodecs, targetAudioCodec),
            TargetWidth = targetWidth,
            TargetHeight = targetHeight,
            HwAccelMethod = settings.PreferredHwAccelMethod != HwAccelMethod.None 
                ? settings.PreferredHwAccelMethod 
                : hwAccel.PreferredMethod,
            RequiresToneMapping = requiresToneMapping,
            TranscodingComplexity = CalculateTranscodingComplexity(mediaInfo, targetWidth, targetHeight, requiresToneMapping)
        };
    }

    /// <summary>
    /// Checks if video stream meets codec profile constraints
    /// </summary>
    private bool CheckVideoConstraints(VideoStream video, VideoCodecProfile codecProfile, TranscodingProfile profile)
    {
        // Check bitrate
        if (codecProfile.MaxBitrate.HasValue && video.Bitrate > codecProfile.MaxBitrate.Value)
            return false;

        // Check resolution
        if (codecProfile.MaxResolution.HasValue)
        {
            var maxPixels = GetMaxPixels(codecProfile.MaxResolution.Value);
            if (video.Width * video.Height > maxPixels)
                return false;
        }

        // Check frame rate
        if (codecProfile.MaxFrameRate.HasValue && video.FrameRate > codecProfile.MaxFrameRate.Value)
            return false;

        // Check HDR support
        if (video.IsHDR && !codecProfile.SupportsHDR)
            return false;

        // Check additional conditions
        foreach (var condition in codecProfile.Conditions)
        {
            if (!CheckCondition(condition, video))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if audio stream meets codec profile constraints
    /// </summary>
    private bool CheckAudioConstraints(AudioStream audio, AudioCodecProfile codecProfile)
    {
        // Check bitrate
        if (codecProfile.MaxBitrate.HasValue && audio.Bitrate > codecProfile.MaxBitrate.Value)
            return false;

        // Check channels
        if (codecProfile.MaxChannels.HasValue && audio.Channels > codecProfile.MaxChannels.Value)
            return false;

        // Check sample rate
        if (codecProfile.SupportedSampleRates.Length > 0 && 
            !codecProfile.SupportedSampleRates.Contains(audio.SampleRate))
            return false;

        return true;
    }

    /// <summary>
    /// Calculates target video settings based on input and constraints
    /// Uses intelligent resolution determination based on client capabilities
    /// </summary>
    private (int width, int height, long bitrate) CalculateTargetVideoSettings(
        VideoStream sourceVideo, 
        TranscodingProfile profile, 
        TranscodingSettings settings)
    {
        var targetWidth = sourceVideo.Width;
        var targetHeight = sourceVideo.Height;

        // Scale down if exceeds profile maximum
        var maxPixels = GetMaxPixels(profile.MaxResolution);
        var sourcePixels = sourceVideo.Width * sourceVideo.Height;

        if (sourcePixels > maxPixels)
        {
            var scaleFactor = Math.Sqrt((double)maxPixels / sourcePixels);
            targetWidth = (int)(sourceVideo.Width * scaleFactor);
            targetHeight = (int)(sourceVideo.Height * scaleFactor);

            // Ensure even dimensions (required by most codecs)
            targetWidth = (targetWidth / 2) * 2;
            targetHeight = (targetHeight / 2) * 2;
        }

        // Calculate target bitrate based on resolution, frame rate, and codec
        var targetBitrate = CalculateTargetBitrate(targetWidth, targetHeight, sourceVideo.FrameRate, profile);

        // Respect profile maximum bitrate
        if (profile.MaxBitrate > 0 && targetBitrate > profile.MaxBitrate)
        {
            targetBitrate = profile.MaxBitrate;
        }

        return (targetWidth, targetHeight, targetBitrate);
    }

    /// <summary>
    /// Calculates target bitrate using advanced algorithms
    /// Based on resolution, frame rate, input bitrate, and codec efficiency
    /// </summary>
    private long CalculateTargetBitrate(int width, int height, double frameRate, TranscodingProfile profile)
    {
        var pixels = width * height;
        var fps = Math.Max(frameRate, 24); // Minimum 24 fps for calculation

        // Base bitrate per pixel per frame (bits per pixel per second)
        var baseRate = pixels switch
        {
            <= 720 * 480 => 0.1,      // SD
            <= 1280 * 720 => 0.08,    // 720p
            <= 1920 * 1080 => 0.06,   // 1080p
            <= 3840 * 2160 => 0.04,   // 4K
            _ => 0.03                  // 8K+
        };

        // Adjust for frame rate
        var frameRateMultiplier = fps / 24.0;
        
        // Calculate target bitrate
        var targetBitrate = (long)(pixels * baseRate * frameRateMultiplier);

        // Apply codec efficiency factors
        var codecMultiplier = GetCodecEfficiencyMultiplier(profile);
        targetBitrate = (long)(targetBitrate * codecMultiplier);

        return Math.Max(targetBitrate, 500_000); // Minimum 500 kbps
    }

    private double GetCodecEfficiencyMultiplier(TranscodingProfile profile)
    {
        // Check for HEVC/H.265 support (more efficient)
        if (profile.VideoCodecs.Any(vc => vc.Codec.Equals("hevc", StringComparison.OrdinalIgnoreCase) ||
                                         vc.Codec.Equals("h265", StringComparison.OrdinalIgnoreCase)))
            return 0.7; // 30% more efficient than H.264

        // Check for AV1 support (most efficient)
        if (profile.VideoCodecs.Any(vc => vc.Codec.Equals("av1", StringComparison.OrdinalIgnoreCase)))
            return 0.5; // 50% more efficient than H.264

        return 1.0; // H.264 baseline
    }

    private int GetMaxPixels(VideoResolution resolution)
    {
        return resolution switch
        {
            VideoResolution.SD480p => 720 * 480,
            VideoResolution.HD720p => 1280 * 720,
            VideoResolution.HD1080p => 1920 * 1080,
            VideoResolution.UHD4K => 3840 * 2160,
            VideoResolution.UHD8K => 7680 * 4320,
            _ => 1920 * 1080
        };
    }

    private TranscodingProfile SelectBestTranscodingProfile(TranscodingProfile[] profiles, MediaInfo mediaInfo)
    {
        // Select profile with highest resolution and bitrate support
        return profiles
            .OrderByDescending(p => GetMaxPixels(p.MaxResolution))
            .ThenByDescending(p => p.MaxBitrate)
            .First();
    }

    private string DetermineTargetVideoCodec(TranscodingProfile profile, HardwareAcceleration hwAccel, TranscodingSettings settings)
    {
        if (!settings.EnableHardwareAcceleration || !hwAccel.IsAvailable)
        {
            // Software encoding
            if (profile.VideoCodecs.Any(vc => vc.Codec.Equals("hevc", StringComparison.OrdinalIgnoreCase)))
                return "libx265";
            
            return "libx264";
        }

        // Hardware encoding
        var preferredMethod = settings.PreferredHwAccelMethod != HwAccelMethod.None 
            ? settings.PreferredHwAccelMethod 
            : hwAccel.PreferredMethod;

        // Try HEVC first if supported
        if (profile.VideoCodecs.Any(vc => vc.Codec.Equals("hevc", StringComparison.OrdinalIgnoreCase)))
        {
            return preferredMethod switch
            {
                HwAccelMethod.Nvenc => "hevc_nvenc",
                HwAccelMethod.QuickSync => "hevc_qsv",
                HwAccelMethod.Amf => "hevc_amf",
                HwAccelMethod.Vaapi => "hevc_vaapi",
                HwAccelMethod.VideoToolbox => "hevc_videotoolbox",
                _ => "libx265"
            };
        }

        // Fall back to H.264
        return preferredMethod switch
        {
            HwAccelMethod.Nvenc => "h264_nvenc",
            HwAccelMethod.QuickSync => "h264_qsv",
            HwAccelMethod.Amf => "h264_amf",
            HwAccelMethod.Vaapi => "h264_vaapi",
            HwAccelMethod.VideoToolbox => "h264_videotoolbox",
            _ => "libx264"
        };
    }

    private string SelectBestAudioCodec(AudioCodecProfile[] audioCodecs)
    {
        // Prefer AAC for compatibility
        var aac = audioCodecs.FirstOrDefault(ac => ac.Codec.Equals("aac", StringComparison.OrdinalIgnoreCase));
        if (aac != null) return "aac";

        // Fall back to first supported codec
        return audioCodecs.FirstOrDefault()?.Codec ?? "aac";
    }

    private string SelectBestContainer(string[] supportedContainers, string sourceContainer)
    {
        // Prefer MP4 for compatibility
        if (supportedContainers.Contains("mp4", StringComparer.OrdinalIgnoreCase))
            return "mp4";

        // Keep source container if supported
        if (supportedContainers.Contains(sourceContainer, StringComparer.OrdinalIgnoreCase))
            return sourceContainer;

        // Fall back to first supported container
        return supportedContainers.FirstOrDefault() ?? "mp4";
    }

    private long DetermineTargetAudioBitrate(AudioCodecProfile[] audioCodecs, string targetCodec)
    {
        var codecProfile = audioCodecs.FirstOrDefault(ac => 
            ac.Codec.Equals(targetCodec, StringComparison.OrdinalIgnoreCase));

        return codecProfile?.MaxBitrate ?? 192_000; // Default 192 kbps
    }

    private int CalculateTranscodingComplexity(MediaInfo mediaInfo, int targetWidth, int targetHeight, bool requiresToneMapping)
    {
        var complexity = 5; // Base complexity for transcoding

        // Add complexity for resolution
        var targetPixels = targetWidth * targetHeight;
        if (targetPixels > 1920 * 1080) complexity += 2; // 4K+
        else if (targetPixels > 1280 * 720) complexity += 1; // 1080p

        // Add complexity for HDR tone mapping
        if (requiresToneMapping) complexity += 2;

        // Add complexity for high frame rates
        if (mediaInfo.Video.FrameRate > 30) complexity += 1;

        return Math.Min(complexity, 10);
    }

    private bool CheckCondition(ProfileCondition condition, VideoStream video)
    {
        var value = condition.Property.ToLowerInvariant() switch
        {
            "width" => video.Width.ToString(),
            "height" => video.Height.ToString(),
            "bitrate" => video.Bitrate.ToString(),
            "framerate" => video.FrameRate.ToString(),
            "pixelformat" => video.PixelFormat,
            "colorspace" => video.ColorSpace ?? "",
            _ => ""
        };

        return condition.Condition switch
        {
            ProfileConditionType.Equals => value.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            ProfileConditionType.NotEquals => !value.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            ProfileConditionType.LessThanEqual => double.TryParse(value, out var d1) && double.TryParse(condition.Value, out var d2) && d1 <= d2,
            ProfileConditionType.GreaterThanEqual => double.TryParse(value, out var d3) && double.TryParse(condition.Value, out var d4) && d3 >= d4,
            _ => true
        };
    }
}