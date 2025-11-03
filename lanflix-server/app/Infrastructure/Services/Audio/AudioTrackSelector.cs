using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Audio;

/// <summary>
/// Service for selecting the best audio track based on user preferences
/// </summary>
public class AudioTrackSelector
{
    private readonly ILogger<AudioTrackSelector> _logger;

    public AudioTrackSelector(ILogger<AudioTrackSelector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Selects the best audio track index based on user language preference
    /// </summary>
    /// <param name="audioStreams">Available audio streams</param>
    /// <param name="preferredLanguage">User's preferred audio language (e.g., "en", "es", "fr")</param>
    /// <returns>The index of the best matching audio track, or null if no preference</returns>
    public int? SelectBestAudioTrack(AudioStream[] audioStreams, string? preferredLanguage)
    {
        if (audioStreams == null || audioStreams.Length == 0)
        {
            _logger.LogWarning("No audio streams available for selection");
            return null;
        }

        if (string.IsNullOrEmpty(preferredLanguage))
        {
            _logger.LogInformation("No preferred language specified, using first audio track");
            return 0; // Default to first track
        }

        _logger.LogInformation("Selecting audio track for preferred language: {PreferredLanguage} from {AudioStreamCount} available streams", 
            preferredLanguage, audioStreams.Length);

        // Log available audio tracks for debugging
        for (int i = 0; i < audioStreams.Length; i++)
        {
            var stream = audioStreams[i];
            _logger.LogDebug("Audio track {Index}: Language={Language}, Codec={Codec}, Channels={Channels}, Title={Title}",
                i, stream.Language ?? "unknown", stream.Codec, stream.Channels, stream.Title ?? "untitled");
        }

        var normalizedPreference = NormalizeLanguageCode(preferredLanguage);

        // First pass: Exact language match
        for (int i = 0; i < audioStreams.Length; i++)
        {
            var stream = audioStreams[i];
            if (!string.IsNullOrEmpty(stream.Language))
            {
                var normalizedStreamLang = NormalizeLanguageCode(stream.Language);
                if (string.Equals(normalizedStreamLang, normalizedPreference, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found exact language match: track {Index} ({Language})", i, stream.Language);
                    return i;
                }
            }
        }

        // Second pass: Check for language in title or metadata
        for (int i = 0; i < audioStreams.Length; i++)
        {
            var stream = audioStreams[i];
            if (!string.IsNullOrEmpty(stream.Title))
            {
                if (ContainsLanguageInTitle(stream.Title, preferredLanguage))
                {
                    _logger.LogInformation("Found language match in title: track {Index} ({Title})", i, stream.Title);
                    return i;
                }
            }
        }

        // Third pass: Partial language code match (e.g., "en" matches "eng")
        for (int i = 0; i < audioStreams.Length; i++)
        {
            var stream = audioStreams[i];
            if (!string.IsNullOrEmpty(stream.Language))
            {
                if (IsLanguageCodeMatch(stream.Language, preferredLanguage))
                {
                    _logger.LogInformation("Found partial language match: track {Index} ({Language})", i, stream.Language);
                    return i;
                }
            }
        }

        // Fourth pass: Prefer higher quality tracks if no language match
        var bestQualityIndex = FindBestQualityTrack(audioStreams);
        if (bestQualityIndex.HasValue)
        {
            _logger.LogInformation("No language match found, selecting best quality track: {Index}", bestQualityIndex.Value);
            return bestQualityIndex.Value;
        }

        // Fallback to first track
        _logger.LogInformation("No preferred language match found, using first audio track");
        return 0;
    }

    /// <summary>
    /// Normalizes language codes to a standard format
    /// </summary>
    private string NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return string.Empty;

        // Convert to lowercase and handle common variations
        var normalized = languageCode.ToLowerInvariant().Trim();

        // Map common 3-letter codes to 2-letter codes
        return normalized switch
        {
            "eng" => "en",
            "spa" => "es",
            "fre" or "fra" => "fr",
            "ger" or "deu" => "de",
            "ita" => "it",
            "por" => "pt",
            "rus" => "ru",
            "jpn" => "ja",
            "chi" or "zho" => "zh",
            "kor" => "ko",
            "ara" => "ar",
            "hin" => "hi",
            "dut" or "nld" => "nl",
            "swe" => "sv",
            "nor" => "no",
            "dan" => "da",
            "fin" => "fi",
            "pol" => "pl",
            "cze" or "ces" => "cs",
            "hun" => "hu",
            "tur" => "tr",
            "gre" or "ell" => "el",
            "heb" => "he",
            "tha" => "th",
            "vie" => "vi",
            _ => normalized.Length > 2 ? normalized.Substring(0, 2) : normalized
        };
    }

    /// <summary>
    /// Checks if the title contains language information
    /// </summary>
    private bool ContainsLanguageInTitle(string title, string preferredLanguage)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(preferredLanguage))
            return false;

        var titleLower = title.ToLowerInvariant();
        var prefLower = preferredLanguage.ToLowerInvariant();

        // Check for common language names in titles
        var languageNames = GetLanguageNames(prefLower);
        return languageNames.Any(name => titleLower.Contains(name));
    }

    /// <summary>
    /// Gets common names for a language code
    /// </summary>
    private string[] GetLanguageNames(string languageCode)
    {
        return languageCode switch
        {
            "en" => new[] { "english", "eng" },
            "es" => new[] { "spanish", "español", "spa", "castellano" },
            "fr" => new[] { "french", "français", "fre", "fra" },
            "de" => new[] { "german", "deutsch", "ger", "deu" },
            "it" => new[] { "italian", "italiano", "ita" },
            "pt" => new[] { "portuguese", "português", "por" },
            "ru" => new[] { "russian", "русский", "rus" },
            "ja" => new[] { "japanese", "日本語", "jpn" },
            "zh" => new[] { "chinese", "中文", "chi", "zho", "mandarin" },
            "ko" => new[] { "korean", "한국어", "kor" },
            "ar" => new[] { "arabic", "العربية", "ara" },
            "hi" => new[] { "hindi", "हिन्दी", "hin" },
            _ => new[] { languageCode }
        };
    }

    /// <summary>
    /// Checks if two language codes match (handles variations)
    /// </summary>
    private bool IsLanguageCodeMatch(string streamLanguage, string preferredLanguage)
    {
        if (string.IsNullOrEmpty(streamLanguage) || string.IsNullOrEmpty(preferredLanguage))
            return false;

        var streamNorm = NormalizeLanguageCode(streamLanguage);
        var prefNorm = NormalizeLanguageCode(preferredLanguage);

        // Check if either starts with the other (e.g., "en" matches "en-US")
        return streamNorm.StartsWith(prefNorm, StringComparison.OrdinalIgnoreCase) ||
               prefNorm.StartsWith(streamNorm, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds the best quality audio track based on codec and channel count
    /// </summary>
    private int? FindBestQualityTrack(AudioStream[] audioStreams)
    {
        if (audioStreams.Length == 0)
            return null;

        var bestIndex = 0;
        var bestScore = CalculateAudioQualityScore(audioStreams[0]);

        for (int i = 1; i < audioStreams.Length; i++)
        {
            var score = CalculateAudioQualityScore(audioStreams[i]);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Calculates a quality score for an audio stream
    /// </summary>
    private int CalculateAudioQualityScore(AudioStream stream)
    {
        var score = 0;

        // Codec quality scoring
        score += stream.Codec.ToLowerInvariant() switch
        {
            "truehd" => 100,
            "dts-hd" or "dts-hd ma" => 95,
            "dts" => 80,
            "ac3" or "eac3" => 70,
            "aac" => 60,
            "mp3" => 40,
            "pcm" => 90,
            "flac" => 85,
            _ => 30
        };

        // Channel count scoring
        score += stream.Channels switch
        {
            >= 8 => 50,  // 7.1 or higher
            >= 6 => 40,  // 5.1
            >= 4 => 30,  // 4.0/4.1
            >= 2 => 20,  // Stereo
            _ => 10      // Mono
        };

        // Bitrate scoring (if available)
        if (stream.Bitrate > 0)
        {
            score += stream.Bitrate switch
            {
                >= 1000000 => 30,  // 1+ Mbps
                >= 500000 => 20,   // 500+ kbps
                >= 256000 => 15,   // 256+ kbps
                >= 128000 => 10,   // 128+ kbps
                _ => 5
            };
        }

        return score;
    }
}