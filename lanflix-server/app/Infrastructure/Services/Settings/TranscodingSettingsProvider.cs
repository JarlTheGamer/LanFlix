using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lanflix.Infrastructure.Services.Settings;

/// <summary>
/// Provides transcoding settings by combining user preferences with server defaults
/// </summary>
public class TranscodingSettingsProvider
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TranscodingSettingsProvider> _logger;

    public TranscodingSettingsProvider(
        ISettingsService settingsService,
        ILogger<TranscodingSettingsProvider> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific setting value by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>Setting value as JSON string</returns>
    public async Task<string?> GetSettingAsync(string key)
    {
        return await _settingsService.GetSettingAsync(key);
    }

    /// <summary>
    /// Gets transcoding settings for a specific profile, combining user preferences with defaults
    /// </summary>
    /// <param name="profileId">User profile ID</param>
    /// <returns>Configured transcoding settings</returns>
    public async Task<TranscodingSettings> GetSettingsAsync(int? profileId = null)
    {
        try
        {
            // Get user settings
            var userSettingsKey = profileId.HasValue ? $"userSettings_{profileId}" : "userSettings_1";
            var streamingSettingsKey = profileId.HasValue ? $"streamingPreferences_{profileId}" : "streamingPreferences_1";

            var userSettingsJson = await _settingsService.GetSettingAsync(userSettingsKey);
            var streamingSettingsJson = await _settingsService.GetSettingAsync(streamingSettingsKey);

            // Parse user settings
            var userSettings = ParseUserSettings(userSettingsJson);
            var streamingSettings = ParseStreamingSettings(streamingSettingsJson);

            // Build transcoding settings
            var enableHwAccel = userSettings.UseHardwareAccel && streamingSettings.VideoTranscoding;
            
            _logger.LogInformation("Building transcoding settings - HW Accel: {EnableHwAccel} (User: {UserHwAccel}, Video Transcoding: {VideoTranscoding})", 
                enableHwAccel, userSettings.UseHardwareAccel, streamingSettings.VideoTranscoding);

            return new TranscodingSettings
            {
                EnableHardwareAcceleration = enableHwAccel,
                PreferredHwAccelMethod = HwAccelMethod.None, // Auto-detect
                ThreadCount = 0, // Auto
                EnableToneMapping = true,
                ToneMappingAlgorithm = ToneMappingAlgorithm.Hable,
                AllowSoftwareFallback = true,
                MaxConcurrentTranscodes = 2,
                EnableLowPowerEncoding = false,
                EncodingPreset = ParseEncodingPreset(userSettings.TranscodePreset),
                EnableBFrames = true,
                TargetQuality = userSettings.Quality == "auto" ? 18 : ParseQuality(userSettings.Quality), // Default to high quality
                EnableAdaptiveBitrate = streamingSettings.TranscodingMode == "auto",
                SegmentDuration = 6,
                PlaylistLength = 6,
                DeleteSegmentsAfterStreaming = true,
                TempDirectory = null, // Use system temp
                FFmpegPath = null, // Auto-detect
                FFprobePath = null // Auto-detect
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user transcoding settings, using defaults");
            return GetDefaultSettings();
        }
    }

    private UserSettings ParseUserSettings(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("User settings JSON is empty, using defaults");
            return new UserSettings();
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var settings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();
            
            _logger.LogInformation("Parsed user settings - UseHardwareAccel: {UseHardwareAccel}, TranscodePreset: {TranscodePreset}", 
                settings.UseHardwareAccel, settings.TranscodePreset);
                
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse user settings JSON: {Json}", json);
            return new UserSettings();
        }
    }

    private StreamingSettings ParseStreamingSettings(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new StreamingSettings();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<StreamingSettings>(json, options) ?? new StreamingSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse streaming settings JSON");
            return new StreamingSettings();
        }
    }

    private EncodingPreset ParseEncodingPreset(string preset)
    {
        return preset?.ToLowerInvariant() switch
        {
            "p1" or "ultrafast" => EncodingPreset.UltraFast,
            "p2" or "superfast" => EncodingPreset.SuperFast,
            "p3" or "veryfast" => EncodingPreset.VeryFast,
            "p4" or "faster" => EncodingPreset.Faster,
            "p5" or "fast" => EncodingPreset.Fast,
            "p6" or "medium" => EncodingPreset.Medium,
            "p7" or "slow" => EncodingPreset.Slow,
            "slower" => EncodingPreset.Slower,
            "veryslow" => EncodingPreset.VerySlow,
            _ => EncodingPreset.Medium
        };
    }

    private int? ParseQuality(string quality)
    {
        return quality?.ToLowerInvariant() switch
        {
            "high" => 15,      // Much higher quality (lower CRF = better quality)
            "medium" => 18,    // Still high quality
            "low" => 23,       // Reasonable quality
            _ => null
        };
    }

    private TranscodingSettings GetDefaultSettings()
    {
        return new TranscodingSettings
        {
            EnableHardwareAcceleration = true,
            PreferredHwAccelMethod = HwAccelMethod.None,
            ThreadCount = 0,
            EnableToneMapping = true,
            ToneMappingAlgorithm = ToneMappingAlgorithm.Hable,
            AllowSoftwareFallback = true,
            MaxConcurrentTranscodes = 2,
            EnableLowPowerEncoding = false,
            EncodingPreset = EncodingPreset.Slow, // Use slower preset for better quality by default
            EnableBFrames = true,
            TargetQuality = 18, // Default to high quality (CRF 18)
            EnableAdaptiveBitrate = true,
            SegmentDuration = 6,
            PlaylistLength = 6,
            DeleteSegmentsAfterStreaming = true
        };
    }

    private class UserSettings
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";
        
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "utc";
        
        [JsonPropertyName("auto-play-next")]
        public bool AutoPlayNext { get; set; } = true;
        
        [JsonPropertyName("skip-intro")]
        public bool SkipIntro { get; set; } = true;
        
        [JsonPropertyName("quality")]
        public string Quality { get; set; } = "auto";
        
        [JsonPropertyName("data-saver")]
        public bool DataSaver { get; set; } = false;
        
        [JsonPropertyName("audio-lang")]
        public string AudioLang { get; set; } = "en";
        
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "dark";
        
        [JsonPropertyName("show-backdrop")]
        public bool ShowBackdrop { get; set; } = true;
        
        [JsonPropertyName("transcoding-mode")]
        public string TranscodingMode { get; set; } = "auto";
        
        [JsonPropertyName("use-hardware-accel")]
        public bool UseHardwareAccel { get; set; } = true;
        
        [JsonPropertyName("transcode-preset")]
        public string TranscodePreset { get; set; } = "p6";
        
        [JsonPropertyName("audio-transcoding")]
        public bool AudioTranscoding { get; set; } = true;
        
        [JsonPropertyName("video-transcoding")]
        public bool VideoTranscoding { get; set; } = true;
    }

    private class StreamingSettings
    {
        public string TranscodingMode { get; set; } = "auto";
        public bool AudioTranscoding { get; set; } = true;
        public bool VideoTranscoding { get; set; } = true;
    }
}