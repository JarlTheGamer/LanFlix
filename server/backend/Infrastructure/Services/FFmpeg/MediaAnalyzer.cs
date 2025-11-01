using System.Diagnostics;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Analyzes media files using FFprobe to extract stream information
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

        var arguments = BuildFFprobeArguments(filePath);
        var output = await ExecuteFFprobeAsync(arguments, cancellationToken);
        var probeResult = ParseFFprobeOutput(output);

        var mediaInfo = BuildMediaInfo(probeResult, filePath);

        _logger.LogInformation(
            "Media analysis complete: {Container}, {VideoCodec}, {AudioTracks} audio, {SubtitleTracks} subtitles",
            mediaInfo.Container,
            mediaInfo.Video.Codec,
            mediaInfo.Audio.Count,
            mediaInfo.Subtitles.Count);

        return mediaInfo;
    }

    private string FindFFprobePath()
    {
        // Try to find ffprobe in PATH
        var ffprobeCommand = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
        
        // Check if ffprobe is in PATH
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

        // Default to just "ffprobe" and let the system find it
        return ffprobeCommand;
    }

    private string BuildFFprobeArguments(string filePath)
    {
        // Use JSON output format for easier parsing
        return $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"";
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
        
        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFprobe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                throw new InvalidOperationException($"FFprobe failed: {error}");
            }

            return output;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to execute FFprobe");
            throw new InvalidOperationException("Failed to analyze media file", ex);
        }
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
                throw new InvalidOperationException("Failed to parse FFprobe output");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse FFprobe JSON output");
            throw new InvalidOperationException("Failed to parse media information", ex);
        }
    }

    private MediaInfo BuildMediaInfo(FFprobeResult probeResult, string filePath)
    {
        var videoStream = ExtractVideoStream(probeResult);
        var audioStreams = ExtractAudioStreams(probeResult);
        var subtitleStreams = ExtractSubtitleStreams(probeResult);

        var fileInfo = new FileInfo(filePath);
        var duration = ParseDuration(probeResult.Format?.Duration);
        var bitrate = ParseBitrate(probeResult.Format?.BitRate);

        return new MediaInfo
        {
            Video = videoStream,
            Audio = audioStreams,
            Subtitles = subtitleStreams,
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
            throw new InvalidOperationException("No video stream found in media file");
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
        var audioStreams = probeResult.Streams?
            .Where(s => s.CodecType == "audio")
            .Select((stream, index) => new AudioStream
            {
                Index = stream.Index ?? index,
                Codec = NormalizeCodecName(stream.CodecName ?? "unknown"),
                Channels = stream.Channels ?? 0,
                SampleRate = stream.SampleRate != null ? int.Parse(stream.SampleRate) : 0,
                Bitrate = ParseBitrate(stream.BitRate) ?? 0,
                Language = stream.Tags?.Language,
                Title = stream.Tags?.Title,
                IsDefault = stream.Disposition?.Default == 1
            })
            .ToList() ?? new List<AudioStream>();

        return audioStreams;
    }

    private List<SubtitleStream> ExtractSubtitleStreams(FFprobeResult probeResult)
    {
        var subtitleStreams = probeResult.Streams?
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

        return subtitleStreams;
    }

    private bool DetectHDR(FFprobeStream stream)
    {
        // Check for HDR indicators
        if (stream.ColorTransfer != null)
        {
            var hdrTransfers = new[] { "smpte2084", "arib-std-b67" }; // PQ (HDR10) and HLG
            if (hdrTransfers.Contains(stream.ColorTransfer.ToLowerInvariant()))
            {
                return true;
            }
        }

        // Check color space for BT.2020
        if (stream.ColorSpace != null && stream.ColorSpace.Contains("bt2020", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check pixel format for 10-bit
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
        // Normalize common codec names
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

        // Frame rate is often in format "24000/1001" or "30/1"
        var parts = frameRate.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var numerator) &&
            double.TryParse(parts[1], out var denominator) &&
            denominator != 0)
        {
            return numerator / denominator;
        }

        // Try parsing as simple double
        if (double.TryParse(frameRate, out var fps))
        {
            return fps;
        }

        return 0;
    }

    #region FFprobe JSON Models

    private class FFprobeResult
    {
        public List<FFprobeStream>? Streams { get; set; }
        public FFprobeFormat? Format { get; set; }
    }

    private class FFprobeStream
    {
        public int? Index { get; set; }
        public string? CodecName { get; set; }
        public string? CodecType { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? PixFmt { get; set; }
        public string? ColorSpace { get; set; }
        public string? ColorTransfer { get; set; }
        public string? RFrameRate { get; set; }
        public string? BitRate { get; set; }
        public int? Channels { get; set; }
        public string? SampleRate { get; set; }
        public FFprobeDisposition? Disposition { get; set; }
        public FFprobeTags? Tags { get; set; }
    }

    private class FFprobeFormat
    {
        public string? FormatName { get; set; }
        public string? Duration { get; set; }
        public string? BitRate { get; set; }
    }

    private class FFprobeDisposition
    {
        public int? Default { get; set; }
        public int? Forced { get; set; }
    }

    private class FFprobeTags
    {
        public string? Language { get; set; }
        public string? Title { get; set; }
    }

    #endregion
}
