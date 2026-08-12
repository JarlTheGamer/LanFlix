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
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime lastWrite, MediaInfo info)> _cache = new();

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

        var fileInfo = new FileInfo(filePath);
        if (_cache.TryGetValue(filePath, out var cached) && cached.lastWrite == fileInfo.LastWriteTimeUtc)
        {
            return cached.info;
        }

        _logger.LogInformation("Analyzing media file: {FilePath}", filePath);

        try
        {
            var arguments = $"-v error -print_format json -show_format -show_streams \"{filePath}\"";
            var output = await ExecuteFFprobeAsync(arguments, cancellationToken);
            var probeResult = ParseFFprobeOutput(output);
            var mediaInfo = BuildMediaInfo(probeResult, filePath);

            _logger.LogInformation(
                "Media analysis complete: {Container}, Video: {VideoCodec} {Width}x{Height}, Audio: {AudioTracks} tracks",
                mediaInfo.Container,
                mediaInfo.Video.Codec,
                mediaInfo.Video.Width,
                mediaInfo.Video.Height,
                mediaInfo.Audio.Count);

            _cache[filePath] = (fileInfo.LastWriteTimeUtc, mediaInfo);
            return mediaInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze media file: {FilePath}", filePath);
            throw new InvalidOperationException(
                $"Playback cannot be planned because FFprobe could not analyze '{Path.GetFileName(filePath)}'.", ex);
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

        var fileInfo = new FileInfo(filePath);
        var duration = ParseDuration(probeResult.Format?.Duration);
        var bitrate = ParseBitrate(probeResult.Format?.BitRate);

        return new MediaInfo
        {
            Video = videoStream,
            Audio = audioStreams,
            Subtitles = new List<SubtitleStream>(),
            Duration = duration,
            FileSize = fileInfo.Length,
            Container = NormalizeContainerName(probeResult.Format?.FormatName, filePath),
            OverallBitrate = bitrate
        };
    }

    private string NormalizeContainerName(string? formatName, string filePath)
    {
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(formatName))
        {
            var lower = formatName.ToLowerInvariant();
            if (lower.Contains("mp4") || lower.Contains("m4v") || ext == "mp4" || ext == "m4v")
            {
                return "mp4";
            }
            if (lower.Contains("matroska") || lower.Contains("mkv") || ext == "mkv")
            {
                return "mkv";
            }
            if (lower.Contains("webm") || ext == "webm")
            {
                return "webm";
            }
            if (lower.Contains("mov") || ext == "mov")
            {
                return "mov";
            }
            return formatName.Split(',').FirstOrDefault() ?? "unknown";
        }
        return !string.IsNullOrEmpty(ext) ? ext : "unknown";
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
            HdrFormat = hdrFormat,
            Profile = videoStream.Profile,
            Level = videoStream.Level,
            BitDepth = ParseBitDepth(videoStream.BitsPerRawSample, videoStream.PixFmt)
        };
    }

    private static int ParseBitDepth(string? raw, string? pixelFormat)
    {
        if (int.TryParse(raw, out var bits) && bits > 0) return bits;
        var format = pixelFormat?.ToLowerInvariant() ?? string.Empty;
        if (format.Contains("12")) return 12;
        if (format.Contains("10") || format.Contains("p010")) return 10;
        return 8;
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

    private bool DetectHDR(FFprobeStream stream)
    {
        if (stream.CodecTagString?.StartsWith("dv", StringComparison.OrdinalIgnoreCase) == true)
            return true;
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
            // bt2020 color space is only truly HDR when paired with an HDR transfer function.
            // Alone it may just be a wide-gamut SDR encode, but we still treat it conservatively.
            if (stream.ColorTransfer != null)
            {
                var hdrTransfers = new[] { "smpte2084", "arib-std-b67" };
                return hdrTransfers.Contains(stream.ColorTransfer.ToLowerInvariant());
            }
            // No transfer function info — assume not HDR to avoid unnecessary transcoding
            return false;
        }

        // 10-bit pixel format (yuv420p10le, yuv444p10le, etc.) is NOT HDR by itself.
        // Many BluRay x265 encodes use 10-bit SDR for improved gradient quality.
        // Only flag as HDR if the transfer function explicitly says so (handled above).

        return false;
    }

    private string? DetectHDRFormat(FFprobeStream stream)
    {
        if (stream.CodecTagString?.StartsWith("dv", StringComparison.OrdinalIgnoreCase) == true)
            return "Dolby Vision";
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

        [JsonPropertyName("codec_tag_string")]
        public string? CodecTagString { get; set; }
        
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("pix_fmt")]
        public string? PixFmt { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("bits_per_raw_sample")]
        public string? BitsPerRawSample { get; set; }
        
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
