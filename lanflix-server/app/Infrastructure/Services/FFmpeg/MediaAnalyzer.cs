using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Media analyzer that properly extracts file information using FFprobe
/// </summary>
public class MediaAnalyzer : IMediaAnalyzer
{
    private readonly ILogger<MediaAnalyzer> _logger;
    private readonly string _ffprobePath;

    public MediaAnalyzer(ILogger<MediaAnalyzer> logger)
    {
        _logger = logger;
        _ffprobePath = FindFFprobePath();
    }

    public async Task<MediaInfo> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Media file not found: {filePath}");
        }

        _logger.LogInformation("Analyzing media file: {FilePath}", filePath);

        try
        {
            var arguments = $"-v error -print_format json -show_format -show_streams \"{filePath}\"";
            var output = await ExecuteFFprobeAsync(arguments, cancellationToken);
            var probeResult = ParseFFprobeOutput(output);
            var mediaInfo = BuildMediaInfo(probeResult, filePath);

            _logger.LogInformation(
                "Media analysis complete: {Container}, Video: {VideoCodec} {Width}x{Height}, Audio: {AudioTracks} tracks, Subtitles: {SubtitleTracks} tracks",
                mediaInfo.Container,
                mediaInfo.Video.Codec,
                mediaInfo.Video.Width,
                mediaInfo.Video.Height,
                mediaInfo.Audio.Count,
                mediaInfo.Subtitles.Count);

            return mediaInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze media file, using defaults: {FilePath}", filePath);
            
            // Return basic defaults if analysis fails
            var fileInfo = new FileInfo(filePath);
            var container = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

            return new MediaInfo
            {
                Video = new VideoStream
                {
                    Codec = "h264",
                    Width = 1920,
                    Height = 1080,
                    Bitrate = 5_000_000,
                    FrameRate = 24,
                    PixelFormat = "yuv420p",
                    ColorSpace = "bt709",
                    IsHDR = false,
                    HdrFormat = null
                },
                Audio = new List<AudioStream>
                {
                    new AudioStream
                    {
                        Index = 0,
                        Codec = "aac",
                        Channels = 2,
                        SampleRate = 48000,
                        Bitrate = 192_000,
                        Language = "eng",
                        Title = null,
                        IsDefault = true
                    }
                },
                Subtitles = new List<SubtitleStream>(),
                Duration = TimeSpan.FromHours(2),
                FileSize = fileInfo.Length,
                Container = container,
                OverallBitrate = 5_000_000
            };
        }
    }

    private async Task<string> ExecuteFFprobeAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        _logger.LogInformation("Executing FFprobe: {FileName} {Arguments}", _ffprobePath, arguments);
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogDebug("FFprobe stderr: {Error}", error);
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFprobe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"FFprobe failed: {error}");
        }

        _logger.LogInformation("FFprobe output length: {Length} characters", output.Length);
        
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("FFprobe returned empty output");
        }

        var preview = output.Length > 1000 ? output.Substring(0, 1000) + "..." : output;
        _logger.LogInformation("FFprobe output preview: {Preview}", preview);

        return output;
    }

    private FFprobeResult ParseFFprobeOutput(string output)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<FFprobeResult>(output, options);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to parse FFprobe output - deserialization returned null");
            }

            _logger.LogDebug("Successfully parsed FFprobe output: {StreamCount} streams, format: {Format}", 
                result.Streams?.Count ?? 0, result.Format?.FormatName);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse FFprobe JSON output");
            throw new InvalidOperationException("Failed to parse media information - invalid JSON format", ex);
        }
    }

    private MediaInfo BuildMediaInfo(FFprobeResult probeResult, string filePath)
    {
        var videoStream = ExtractVideoStream(probeResult);
        var audioStreams = ExtractAudioStreams(probeResult);
        var embeddedSubtitles = ExtractSubtitleStreams(probeResult);
        
        // Scan for external subtitle files
        var externalSubtitles = ScanExternalSubtitles(filePath, embeddedSubtitles.Count);
        
        // Combine embedded and external subtitles
        var allSubtitles = embeddedSubtitles.Concat(externalSubtitles).ToList();

        var fileInfo = new FileInfo(filePath);
        var duration = ParseDuration(probeResult.Format?.Duration);
        var bitrate = ParseBitrate(probeResult.Format?.BitRate);

        _logger.LogInformation("Total subtitles found: {Total} (Embedded: {Embedded}, External: {External})",
            allSubtitles.Count, embeddedSubtitles.Count, externalSubtitles.Count);

        return new MediaInfo
        {
            Video = videoStream,
            Audio = audioStreams,
            Subtitles = allSubtitles,
            Duration = duration,
            FileSize = fileInfo.Length,
            Container = probeResult.Format?.FormatName?.Split(',').FirstOrDefault() ?? "unknown",
            OverallBitrate = bitrate
        };
    }

    private VideoStream ExtractVideoStream(FFprobeResult probeResult)
    {
        var videoStream = probeResult.Streams?.FirstOrDefault(s => s.CodecType == "video");
        if (videoStream == null)
        {
            _logger.LogWarning("No video stream found in media file");
            
            return new VideoStream
            {
                Codec = "unknown",
                Width = 0,
                Height = 0,
                Bitrate = 0,
                FrameRate = 0,
                PixelFormat = "unknown",
                ColorSpace = null,
                IsHDR = false,
                HdrFormat = null
            };
        }

        var isHDR = DetectHDR(videoStream);
        var hdrFormat = DetectHDRFormat(videoStream);

        return new VideoStream
        {
            Codec = NormalizeCodecName(videoStream.CodecName ?? "unknown"),
            Width = videoStream.Width ?? 0,
            Height = videoStream.Height ?? 0,
            Bitrate = ParseBitrate(videoStream.BitRate) ?? 0,
            FrameRate = ParseFrameRate(videoStream.RFrameRate),
            PixelFormat = videoStream.PixFmt ?? "unknown",
            ColorSpace = videoStream.ColorSpace,
            IsHDR = isHDR,
            HdrFormat = hdrFormat
        };
    }

    private List<AudioStream> ExtractAudioStreams(FFprobeResult probeResult)
    {
        return probeResult.Streams?
            .Where(s => s.CodecType == "audio")
            .Select((stream, index) => new AudioStream
            {
                Index = stream.Index ?? index,
                Codec = NormalizeCodecName(stream.CodecName ?? "unknown"),
                Channels = stream.Channels ?? 0,
                SampleRate = stream.SampleRate != null && int.TryParse(stream.SampleRate, out var sr) ? sr : 0,
                Bitrate = ParseBitrate(stream.BitRate) ?? 0,
                Language = stream.Tags?.Language,
                Title = stream.Tags?.Title,
                IsDefault = stream.Disposition?.Default == 1
            })
            .ToList() ?? new List<AudioStream>();
    }

    private List<SubtitleStream> ExtractSubtitleStreams(FFprobeResult probeResult)
    {
        var embeddedSubtitles = probeResult.Streams?
            .Where(s => s.CodecType == "subtitle")
            .Select((stream, index) => new SubtitleStream
            {
                Index = stream.Index ?? index,
                Format = NormalizeCodecName(stream.CodecName ?? "unknown"),
                Language = stream.Tags?.Language,
                Title = stream.Tags?.Title,
                IsDefault = stream.Disposition?.Default == 1,
                IsForced = stream.Disposition?.Forced == 1,
                IsEmbedded = true,
                ExternalFilePath = null
            })
            .ToList() ?? new List<SubtitleStream>();

        return embeddedSubtitles;
    }

    /// <summary>
    /// Scans for external subtitle files in the same directory as the video file
    /// </summary>
    private List<SubtitleStream> ScanExternalSubtitles(string videoFilePath, int startIndex)
    {
        var externalSubtitles = new List<SubtitleStream>();

        try
        {
            var directory = Path.GetDirectoryName(videoFilePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFilePath);

            if (string.IsNullOrEmpty(directory))
                return externalSubtitles;

            // Supported subtitle extensions
            var subtitleExtensions = new[] { ".srt", ".vtt", ".ass", ".ssa", ".sub" };

            // Find all subtitle files matching the video filename
            var subtitleFiles = Directory.GetFiles(directory, $"{fileNameWithoutExt}*")
                .Where(f => subtitleExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            int index = startIndex;
            foreach (var subtitleFile in subtitleFiles)
            {
                var fileName = Path.GetFileName(subtitleFile);
                var extension = Path.GetExtension(subtitleFile).ToLowerInvariant();

                // Try to extract language from filename (e.g., "movie.en.srt", "movie.eng.srt")
                var language = ExtractLanguageFromFilename(fileName);
                var isForced = fileName.Contains(".forced.", StringComparison.OrdinalIgnoreCase);

                externalSubtitles.Add(new SubtitleStream
                {
                    Index = index++,
                    Format = extension.TrimStart('.'),
                    Language = language,
                    Title = $"External - {language ?? "Unknown"}",
                    IsDefault = false,
                    IsForced = isForced,
                    IsEmbedded = false,
                    ExternalFilePath = subtitleFile
                });

                _logger.LogInformation("Found external subtitle: {FileName}, Language: {Language}, Forced: {IsForced}",
                    fileName, language ?? "unknown", isForced);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan for external subtitles for file: {FilePath}", videoFilePath);
        }

        return externalSubtitles;
    }

    /// <summary>
    /// Extracts language code from subtitle filename
    /// </summary>
    private string? ExtractLanguageFromFilename(string filename)
    {
        // Common patterns: movie.en.srt, movie.eng.srt, movie.english.srt
        var patterns = new Dictionary<string, string>
        {
            { "en", "eng" }, { "eng", "eng" }, { "english", "eng" },
            { "es", "spa" }, { "spa", "spa" }, { "spanish", "spa" },
            { "fr", "fra" }, { "fra", "fra" }, { "french", "fra" },
            { "de", "ger" }, { "ger", "ger" }, { "german", "ger" },
            { "it", "ita" }, { "ita", "ita" }, { "italian", "ita" },
            { "pt", "por" }, { "por", "por" }, { "portuguese", "por" },
            { "ja", "jpn" }, { "jpn", "jpn" }, { "japanese", "jpn" },
            { "ko", "kor" }, { "kor", "kor" }, { "korean", "kor" },
            { "zh", "chi" }, { "chi", "chi" }, { "chinese", "chi" },
            { "ar", "ara" }, { "ara", "ara" }, { "arabic", "ara" },
            { "ru", "rus" }, { "rus", "rus" }, { "russian", "rus" },
            { "hi", "hin" }, { "hin", "hin" }, { "hindi", "hin" }
        };

        var lowerFilename = filename.ToLowerInvariant();
        
        foreach (var pattern in patterns)
        {
            // Match patterns like ".en.", ".eng.", etc.
            if (lowerFilename.Contains($".{pattern.Key}.") || 
                lowerFilename.Contains($".{pattern.Key}_") ||
                lowerFilename.EndsWith($".{pattern.Key}"))
            {
                return pattern.Value;
            }
        }

        return null;
    }

    private bool DetectHDR(FFprobeStream stream)
    {
        if (stream.ColorTransfer != null)
        {
            var hdrTransfers = new[] { "smpte2084", "arib-std-b67" };
            if (hdrTransfers.Contains(stream.ColorTransfer.ToLowerInvariant()))
            {
                return true;
            }
        }

        if (stream.ColorSpace != null && stream.ColorSpace.Contains("bt2020", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (stream.PixFmt != null && stream.PixFmt.Contains("10le"))
        {
            return true;
        }

        return false;
    }

    private string? DetectHDRFormat(FFprobeStream stream)
    {
        if (stream.ColorTransfer == null)
        {
            return null;
        }

        return stream.ColorTransfer.ToLowerInvariant() switch
        {
            "smpte2084" => "HDR10",
            "arib-std-b67" => "HLG",
            _ => null
        };
    }

    private string NormalizeCodecName(string codecName)
    {
        return codecName.ToLowerInvariant() switch
        {
            "h264" or "avc" or "avc1" => "h264",
            "h265" or "hevc" or "hev1" => "hevc",
            "vp8" => "vp8",
            "vp9" => "vp9",
            "av1" or "av01" => "av1",
            "aac" => "aac",
            "mp3" => "mp3",
            "ac3" => "ac3",
            "eac3" or "e-ac-3" => "eac3",
            "opus" => "opus",
            "vorbis" => "vorbis",
            "dts" => "dts",
            "truehd" => "truehd",
            "subrip" or "srt" => "srt",
            "ass" => "ass",
            "ssa" => "ssa",
            "webvtt" => "webvtt",
            _ => codecName.ToLowerInvariant()
        };
    }

    private TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration) || !double.TryParse(duration, out var seconds))
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private long? ParseBitrate(string? bitrate)
    {
        if (string.IsNullOrEmpty(bitrate) || !long.TryParse(bitrate, out var value))
        {
            return null;
        }

        return value;
    }

    private double ParseFrameRate(string? frameRate)
    {
        if (string.IsNullOrEmpty(frameRate))
        {
            return 0;
        }

        var parts = frameRate.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var numerator) &&
            double.TryParse(parts[1], out var denominator) &&
            denominator != 0)
        {
            return numerator / denominator;
        }

        if (double.TryParse(frameRate, out var fps))
        {
            return fps;
        }

        return 0;
    }

    private string FindFFprobePath()
    {
        var ffprobeCommand = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
        
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable != null)
        {
            var paths = pathVariable.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, ffprobeCommand);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return ffprobeCommand;
    }

    #region FFprobe JSON Models

    private class FFprobeResult
    {
        [JsonPropertyName("streams")]
        public List<FFprobeStream>? Streams { get; set; }
        
        [JsonPropertyName("format")]
        public FFprobeFormat? Format { get; set; }
    }

    private class FFprobeStream
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }
        
        [JsonPropertyName("codec_name")]
        public string? CodecName { get; set; }
        
        [JsonPropertyName("codec_type")]
        public string? CodecType { get; set; }
        
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("pix_fmt")]
        public string? PixFmt { get; set; }
        
        [JsonPropertyName("color_space")]
        public string? ColorSpace { get; set; }
        
        [JsonPropertyName("color_transfer")]
        public string? ColorTransfer { get; set; }
        
        [JsonPropertyName("r_frame_rate")]
        public string? RFrameRate { get; set; }
        
        [JsonPropertyName("bit_rate")]
        public string? BitRate { get; set; }
        
        [JsonPropertyName("channels")]
        public int? Channels { get; set; }
        
        [JsonPropertyName("sample_rate")]
        public string? SampleRate { get; set; }
        
        [JsonPropertyName("disposition")]
        public FFprobeDisposition? Disposition { get; set; }
        
        [JsonPropertyName("tags")]
        public FFprobeTags? Tags { get; set; }
    }

    private class FFprobeFormat
    {
        [JsonPropertyName("format_name")]
        public string? FormatName { get; set; }
        
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
        
        [JsonPropertyName("bit_rate")]
        public string? BitRate { get; set; }
    }

    private class FFprobeDisposition
    {
        [JsonPropertyName("default")]
        public int? Default { get; set; }
        
        [JsonPropertyName("forced")]
        public int? Forced { get; set; }
    }

    private class FFprobeTags
    {
        [JsonPropertyName("language")]
        public string? Language { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    #endregion
}
